using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NativeCameraExample : MonoBehaviour
{
    [Header("Photo Display")]
    public RawImage imageDisplay;
    public GameObject closeButton;
    public string currentArea;

    [Header("Sticker Settings")]
    public Transform stickerMenuContent;
    public GameObject stickerMenuScrollView;
    public GameObject[] area1Stickers;
    public GameObject[] cursoDaguaStickers;
    public GameObject[] subosqueStickers;
    public GameObject[] dosselStickers;
    public GameObject[] epifitasStickers;
    public GameObject[] serrapilheiraStickers;
    public GameObject[] areaTesteStickers;

    [Header("Dependencies")]
    public LocationServiceManager locationManager;
    public GalleryManager galleryManager;
    public RectTransform photoAreaToCapture;
    public GameObject okButton;

    [Header("Sticker Limit")]
    public int maxStickersPerPhoto = 6;

    [Header("Progresso")]
    public TextMeshProUGUI progressText;
    private int areasVisitadas = 0;
    private const int TOTAL_AREAS = 5;
    private List<string> areasContabilizadas = new List<string>();

    [Header("Mensagem")]
    public Image messageImage;
    public TextMeshProUGUI messageText;
    public string stickerLimitMessage = "Limite de Sticker na foto excedido!";
    public string errorMessage = "Há stickers de outra área na foto!";
    public string warningMessage = "Ainda há stickers dessa área que não foram utilizados!";

    [Header("Sticker Count UI")]
    public TextMeshProUGUI area1CountText;
    public TextMeshProUGUI cursoDaguaCountText;
    public TextMeshProUGUI subosqueCountText;
    public TextMeshProUGUI dosselCountText;
    public TextMeshProUGUI epifitasCountText;
    public TextMeshProUGUI serrapilheiraCountText;
    public TextMeshProUGUI areaTesteCountText;

    private List<StickerController> activeStickers = new List<StickerController>();
    private Dictionary<GameObject, string> stickerAreaCache = new Dictionary<GameObject, string>();
    private Dictionary<GameObject, int> stickerIndexCache = new Dictionary<GameObject, int>(); // NOVO: mapa prefab -> índice
    private Dictionary<string, int> spawnedStickersCount = new Dictionary<string, int>();

    private void Start()
    {
        stickerMenuScrollView?.SetActive(false);
        closeButton?.SetActive(false);
        locationManager ??= FindAnyObjectByType<LocationServiceManager>();
        if (progressText != null)
            progressText.text = $"{areasVisitadas} de {TOTAL_AREAS} áreas visitadas";
        messageImage?.gameObject.SetActive(false);

        CacheStickerAreas();
        spawnedStickersCount.Clear();

        InicializarContadores();
        UpdateAllCountersFromLocationManager();

        // Se os used stickers mudarem em runtime (LocationServiceManager notifica), atualiza UI
        if (locationManager != null)
            locationManager.OnUsedStickersChanged += UpdateAllCountersFromLocationManager;
    }

    private void OnDestroy()
    {
        if (locationManager != null)
            locationManager.OnUsedStickersChanged -= UpdateAllCountersFromLocationManager;
    }

    private void CacheStickerAreas()
    {
        stickerAreaCache.Clear();
        stickerIndexCache.Clear();

        CacheAreaStickers("Area1", area1Stickers);
        CacheAreaStickers("CursoDagua", cursoDaguaStickers);
        CacheAreaStickers("Subosque", subosqueStickers);
        CacheAreaStickers("Dossel", dosselStickers);
        CacheAreaStickers("Epifitas", epifitasStickers);
        CacheAreaStickers("Serrapilheira", serrapilheiraStickers);
        CacheAreaStickers("AreaTeste", areaTesteStickers);
    }

    private void CacheAreaStickers(string areaName, GameObject[] stickers)
    {
        if (stickers == null) return;

        for (int i = 0; i < stickers.Length; i++)
        {
            GameObject sticker = stickers[i];
            if (sticker != null)
            {
                if (!stickerAreaCache.ContainsKey(sticker))
                    stickerAreaCache[sticker] = areaName;
                if (!stickerIndexCache.ContainsKey(sticker))
                    stickerIndexCache[sticker] = i;
            }
        }
    }

    public bool CanAddSticker() => activeStickers.Count < maxStickersPerPhoto;

    public void RegisterSticker(StickerController sticker)
    {
        if (!activeStickers.Contains(sticker))
            activeStickers.Add(sticker);
    }

    public void UnregisterSticker(StickerController sticker)
    {
        if (activeStickers.Contains(sticker))
            activeStickers.Remove(sticker);
    }

    private IEnumerator ShowMessageCoroutine(string message, float duration)
    {
        if (messageImage != null && messageText != null)
        {
            messageText.text = message;
            messageImage.gameObject.SetActive(true);
            yield return new WaitForSeconds(duration);
            messageImage.gameObject.SetActive(false);
        }
    }

    public void ShowStickerLimitMessage() => StartCoroutine(ShowMessageCoroutine(stickerLimitMessage, 3f));
    public void ShowErrorMessage() => StartCoroutine(ShowMessageCoroutine(errorMessage, 3f));
    public void ShowWarningMessage() => StartCoroutine(ShowMessageCoroutine(warningMessage, 3f));

    public Sprite GetStickerSprite(string areaName, int index)
    {
        GameObject[] stickers = areaName switch
        {
            "Area1" => area1Stickers,
            "CursoDagua" => cursoDaguaStickers,
            "Subosque" => subosqueStickers,
            "Dossel" => dosselStickers,
            "Epifitas" => epifitasStickers,
            "Serrapilheira" => serrapilheiraStickers,
            "AreaTeste" => areaTesteStickers,
            _ => null
        };

        if (stickers != null && index >= 0 && index < stickers.Length)
        {
            var renderer = stickers[index].GetComponentInChildren<SpriteRenderer>();
            if (renderer != null) return renderer.sprite;

            var image = stickers[index].GetComponentInChildren<Image>();
            if (image != null) return image.sprite;
        }

        return null;
    }

    public void OpenCamera()
    {
        if (string.IsNullOrEmpty(currentArea))
        {
            Debug.LogWarning("Nenhuma área válida detectada.");
            return;
        }

        NativeCamera.TakePicture((path) =>
        {
            if (path != null)
            {
                Texture2D texture = NativeCamera.LoadImageAtPath(path, 1024);
                if (texture != null)
                {
                    imageDisplay.texture = texture;
                    imageDisplay.gameObject.SetActive(true);
                    ShowStickers();
                    closeButton?.SetActive(true);
                    okButton?.SetActive(true);
                }
            }
        }, maxSize: 1024);
    }

    private void ShowStickers()
    {
        if (stickerMenuContent == null || stickerMenuScrollView == null || locationManager == null) return;

        foreach (Transform child in stickerMenuContent)
            Destroy(child.gameObject);

        stickerMenuScrollView.SetActive(true);
        spawnedStickersCount.Clear();

        // Recebe lista já filtrada
        GameObject[] stickersToShow = GetAllStickers();
        if (stickersToShow == null || stickersToShow.Length == 0) return;

        foreach (var stickerPrefab in stickersToShow)
        {
            if (stickerPrefab != null)
            {
                var sticker = Instantiate(stickerPrefab, stickerMenuContent);
                var controller = sticker.GetComponent<StickerController>() ?? sticker.AddComponent<StickerController>();
                controller.SetRawImageRect(imageDisplay.rectTransform);

                // Preenche AreaName e StickerIndex para que possamos marcar como "used" mais tarde
                string areaName = null;
                if (stickerAreaCache.TryGetValue(stickerPrefab, out areaName))
                {
                    controller.AreaName = areaName;
                }
                int idx = GetIndexForPrefab(stickerPrefab);
                controller.StickerIndex = idx;

                if (areaName != null)
                {
                    if (spawnedStickersCount.ContainsKey(areaName))
                        spawnedStickersCount[areaName]++;
                    else
                        spawnedStickersCount[areaName] = 1;
                }
            }
        }

        // Atualiza os contadores ao mostrar o menu (pois spawnedStickersCount foi recalculado)
        UpdateAllCountersFromLocationManager();
    }

    private int GetIndexForPrefab(GameObject prefab)
    {
        if (prefab == null) return -1;

        if (stickerIndexCache.TryGetValue(prefab, out int idx))
            return idx;

        // fallback: procurar nos arrays (mais lento)
        int found = -1;
        found = IndexOfInArray(area1Stickers, prefab); if (found != -1) return found;
        found = IndexOfInArray(cursoDaguaStickers, prefab); if (found != -1) return found;
        found = IndexOfInArray(subosqueStickers, prefab); if (found != -1) return found;
        found = IndexOfInArray(dosselStickers, prefab); if (found != -1) return found;
        found = IndexOfInArray(epifitasStickers, prefab); if (found != -1) return found;
        found = IndexOfInArray(serrapilheiraStickers, prefab); if (found != -1) return found;
        found = IndexOfInArray(areaTesteStickers, prefab); if (found != -1) return found;

        return -1;
    }

    private int IndexOfInArray(GameObject[] arr, GameObject item)
    {
        if (arr == null) return -1;
        for (int i = 0; i < arr.Length; i++)
            if (arr[i] == item) return i;
        return -1;
    }

    private GameObject[] GetAllStickers()
    {
        if (locationManager == null) return null;

        List<GameObject> stickers = new List<GameObject>();
        AddStickersForArea("Area1", area1Stickers, stickers, showAllForArea: currentArea == "Area1");
        AddStickersForArea("CursoDagua", cursoDaguaStickers, stickers, showAllForArea: currentArea == "CursoDagua");
        AddStickersForArea("Subosque", subosqueStickers, stickers, showAllForArea: currentArea == "Subosque");
        AddStickersForArea("Dossel", dosselStickers, stickers, showAllForArea: currentArea == "Dossel");
        AddStickersForArea("Epifitas", epifitasStickers, stickers, showAllForArea: currentArea == "Epifitas");
        AddStickersForArea("Serrapilheira", serrapilheiraStickers, stickers, showAllForArea: currentArea == "Serrapilheira");
        AddStickersForArea("AreaTeste", areaTesteStickers, stickers, showAllForArea: currentArea == "AreaTeste");

        return stickers.ToArray();
    }

    // showAllForArea = true => estamos abrindo o menu NA MESMA área -> então mostramos TUDO (fixos + coletados)
    // showAllForArea = false => estamos em outra área -> escondemos stickers que já foram usados nessa área
    private void AddStickersForArea(string areaName, GameObject[] stickersArray, List<GameObject> outputList, bool showAllForArea)
    {
        if (stickersArray == null || stickersArray.Length == 0 || locationManager == null) return;

        for (int i = 0; i < stickersArray.Length; i++)
        {
            GameObject prefab = stickersArray[i];
            if (prefab == null) continue;

            bool isCollectedExtra = (i < 3) ? true : locationManager.IsStickerCollected(areaName, i);

            if (!isCollectedExtra) continue; // não foi coletado (se for >=3)

            // Se não estamos na área atual, e o sticker já foi marcado como "used" nessa área, então não mostrar
            if (!showAllForArea && locationManager.IsStickerUsed(areaName, i))
                continue;

            outputList.Add(prefab);
        }
    }

    public void ConfirmarVisita()
    {
        if (!areasContabilizadas.Contains(currentArea) && areasVisitadas < TOTAL_AREAS)
        {
            areasVisitadas++;
            areasContabilizadas.Add(currentArea);
            progressText.text = $"{areasVisitadas} de {TOTAL_AREAS} áreas visitadas";
        }

        ClosePhotoView();
    }

    public void ClosePhotoView()
    {
        imageDisplay.gameObject.SetActive(false);
        closeButton?.SetActive(false);
        okButton?.SetActive(false);

        foreach (var sticker in FindObjectsOfType<StickerController>())
            Destroy(sticker.gameObject);

        activeStickers.Clear();

        if (stickerMenuContent != null)
            foreach (Transform child in stickerMenuContent)
                Destroy(child.gameObject);

        stickerMenuScrollView?.SetActive(false);
    }

    public void ConfirmarFotoDecorada()
    {
        if (!AreStickersFromCorrectArea())
        {
            ShowErrorMessage();
            return;
        }

        if (AreThereUnusedStickersFromCurrentArea())
        {
            ShowWarningMessage();
            return;
        }

        // Marca os stickers usados (persistente via LocationServiceManager)
        if (locationManager != null)
        {
            foreach (var st in activeStickers)
            {
                if (!string.IsNullOrEmpty(st.AreaName) && st.StickerIndex >= 0)
                {
                    locationManager.MarkStickerAsUsed(st.AreaName, st.StickerIndex);
                }
            }
        }

        int stickersCount = CountStickersFromAreaInPhoto(currentArea);
        AtualizarContadorStickers(currentArea, stickersCount);

        StartCoroutine(CaptureAndSave());
    }

    private void AtualizarContadorStickers(string areaName, int count)
    {
        string text = $"{count}/6";

        switch (areaName)
        {
            case "Area1": area1CountText.text = text; break;
            case "CursoDagua": cursoDaguaCountText.text = text; break;
            case "Subosque": subosqueCountText.text = text; break;
            case "Dossel": dosselCountText.text = text; break;
            case "Epifitas": epifitasCountText.text = text; break;
            case "Serrapilheira": serrapilheiraCountText.text = text; break;
            case "AreaTeste": areaTesteCountText.text = text; break;
        }

        // Não salvamos aqui em PlayerPrefs: a fonte da verdade agora é LocationServiceManager (usedStickers).
    }

    public bool AreStickersFromCorrectArea()
    {
        foreach (StickerController sticker in activeStickers)
            if (!IsStickerFromCurrentArea(sticker)) return false;
        return true;
    }

    private bool AreThereUnusedStickersFromCurrentArea()
    {
        if (!spawnedStickersCount.ContainsKey(currentArea) || spawnedStickersCount[currentArea] == 0)
            return false;

        int usedStickersCount = CountStickersFromAreaInPhoto(currentArea);
        int spawnedCount = spawnedStickersCount.ContainsKey(currentArea) ? spawnedStickersCount[currentArea] : 0;

        return usedStickersCount < spawnedCount;
    }

    private int CountStickersFromAreaInPhoto(string areaName)
    {
        int count = 0;
        foreach (StickerController sticker in activeStickers)
            if (sticker.AreaName == areaName) count++;

        return count;
    }

    private bool IsStickerFromCurrentArea(StickerController sticker)
    {
        return sticker.AreaName == currentArea;
    }

    private string GetAreaDisplayName(string areaCode)
    {
        return areaCode switch
        {
            "Area1" => "Área 1",
            "CursoDagua" => "Curso D'água",
            "Subosque" => "Subosque",
            "Dossel" => "Dossel",
            "Epifitas" => "Epífitas",
            "Serrapilheira" => "Serrapilheira",
            "AreaTeste" => "Área Teste",
            _ => areaCode,
        };
    }

    private IEnumerator CaptureAndSave()
    {
        yield return new WaitForEndOfFrame();

        Vector3[] corners = new Vector3[4];
        photoAreaToCapture.GetWorldCorners(corners);

        Vector2 bottomLeft = RectTransformUtility.WorldToScreenPoint(null, corners[0]);
        Vector2 topRight = RectTransformUtility.WorldToScreenPoint(null, corners[2]);

        int width = Mathf.RoundToInt(topRight.x - bottomLeft.x);
        int height = Mathf.RoundToInt(topRight.y - bottomLeft.y);

        Texture2D screenshot = new Texture2D(width, height, TextureFormat.RGB24, false);
        screenshot.ReadPixels(new Rect(bottomLeft.x, bottomLeft.y, width, height), 0, 0);
        screenshot.Apply();

        if (!string.IsNullOrEmpty(currentArea))
            galleryManager.SaveImage(currentArea, screenshot);

        ClosePhotoView();

        // Atualiza contadores após salvar/fechar
        UpdateAllCountersFromLocationManager();
    }

    private void InicializarContadores()
    {
        area1CountText.text = "0/6";
        cursoDaguaCountText.text = "0/6";
        subosqueCountText.text = "0/6";
        dosselCountText.text = "0/6";
        epifitasCountText.text = "0/6";
        serrapilheiraCountText.text = "0/6";
        areaTesteCountText.text = "0/6";
    }

    private void UpdateAllCountersFromLocationManager()
    {
        if (locationManager == null) return;

        area1CountText.text = $"Área 1: {locationManager.GetUsedStickerCount("Area1")}/6";
        cursoDaguaCountText.text = $"Curso D'água: {locationManager.GetUsedStickerCount("CursoDagua")}/6";
        subosqueCountText.text = $"Subosque: {locationManager.GetUsedStickerCount("Subosque")}/6";
        dosselCountText.text = $"Dossel: {locationManager.GetUsedStickerCount("Dossel")}/6";
        epifitasCountText.text = $"Epífitas: {locationManager.GetUsedStickerCount("Epifitas")}/6";
        serrapilheiraCountText.text = $"Serrapilheira: {locationManager.GetUsedStickerCount("Serrapilheira")}/6";
        areaTesteCountText.text = $"Área Teste: {locationManager.GetUsedStickerCount("AreaTeste")}/6";
    }

    public void ResetarProgresso()
    {
        areasVisitadas = 0;
        areasContabilizadas.Clear();
        progressText.text = $"{areasVisitadas} de {TOTAL_AREAS} áreas visitadas";

        // Reseta todos (cuidado: limpa tudo, inclusive dados persistidos em LocationServiceManager quando você implementar reset lá)
        InicializarContadores();

        // também pedir pra LocationServiceManager resetar (se quiser manter um único ponto de verdade)
        if (locationManager != null)
        {
            PlayerPrefs.DeleteKey("UsedStickers");
            PlayerPrefs.DeleteKey("CollectedStickers");
            // re-carrega internal states
            // uma forma limpa: reiniciar a cena, ou chamar métodos de reset no locationManager se implementar
            locationManager = FindAnyObjectByType<LocationServiceManager>();
            // força reload interno (se você quiser implementar um método ResetAll dentro do LSM, melhor)
            // aqui simplificamos: recarrega os dados da memória (eles já foram deletados)
            locationManager?.SendMessage("LoadCollectedStickers", SendMessageOptions.DontRequireReceiver);
        }
    }

    public bool IsStickerAlreadyRegistered(StickerController sticker)
    {
        return activeStickers.Contains(sticker);
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System; // Necessário para System.GC

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
    public GameObject[] area2Stickers; // NOVA ÁREA
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
    public GameObject backButton; // 🆕 Botão "Voltar"



    [Header("Imagem Deslizante")]
    public RectTransform slidingImage;      // arraste a Image aqui no Inspector
    public float slideDuration = 0.5f;      // tempo do movimento (entrada/saída)
    public float slideStayTime = 4f;        // tempo parada no centro



    // Parte do novo sistema de páginas 
    [Header("Sticker Pages")]
    public Button nextPageButton;
    public Button prevPageButton;

    private List<GameObject[]> stickerPages = new List<GameObject[]>();
    private int currentPageIndex = 0;






    [Header("Sticker Limit")]
    public int maxStickersPerPhoto = 6;

    [Header("Progresso")]
    public TextMeshProUGUI progressText;
    private int areasVisitadas = 0;
    private const int TOTAL_AREAS = 6; // Atualizado para incluir Área2
    private List<string> areasContabilizadas = new List<string>();

    [Header("Mensagem")]
    public Image messageImage;
    public TextMeshProUGUI messageText;
    public string stickerLimitMessage = "Limite de Sticker na foto excedido!";
    public string errorMessage = "Há stickers de outra área na foto!";
    public string warningMessage = "Ainda há stickers dessa área que não foram utilizados!";

    [Header("Sticker Count UI")]
    public TextMeshProUGUI area1CountText;
    public TextMeshProUGUI area2CountText; // NOVO contador
    public TextMeshProUGUI cursoDaguaCountText;
    public TextMeshProUGUI subosqueCountText;
    public TextMeshProUGUI dosselCountText;
    public TextMeshProUGUI epifitasCountText;
    public TextMeshProUGUI serrapilheiraCountText;
    public TextMeshProUGUI areaTesteCountText;

    private List<StickerController> activeStickers = new List<StickerController>();
    private Dictionary<GameObject, string> stickerAreaCache = new Dictionary<GameObject, string>();
    private Dictionary<GameObject, int> stickerIndexCache = new Dictionary<GameObject, int>();
    private Dictionary<string, int> spawnedStickersCount = new Dictionary<string, int>();


    public enum StickerState
    {
        InMenu,
        InPhoto
    }

    private Dictionary<string, StickerState> stickerStates
        = new Dictionary<string, StickerState>();


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
        CacheAreaStickers("Area2", area2Stickers); // NOVA ÁREA
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
            "Area2" => area2Stickers,
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

        StartCoroutine(OptimizeMemoryAndCapture());
    }

    private IEnumerator OptimizeMemoryAndCapture()
    {
        Debug.Log("Iniciando otimização de memória: Resources.UnloadUnusedAssets...");
        AsyncOperation unloadOperation = Resources.UnloadUnusedAssets();

        while (!unloadOperation.isDone)
            yield return null;

        Debug.Log("Coletando lixo (GC.Collect)...");
        GC.Collect();
        yield return null;

        Debug.Log("Otimização concluída. Abrindo a Câmera Nativa.");

        NativeCamera.TakePicture((path) =>
        {
            if (path != null)
            {
                // ✅ DESTRÓI A FOTO ANTERIOR ANTES DE CARREGAR A NOVA
                if (imageDisplay.texture != null)
                {
                    Destroy(imageDisplay.texture);
                    imageDisplay.texture = null;
                    Resources.UnloadUnusedAssets();
                    GC.Collect();
                }

                Texture2D texture = NativeCamera.LoadImageAtPath(path, 512);


                if (texture != null && imageDisplay != null)
                {
                    imageDisplay.texture = texture;
                    imageDisplay.gameObject.SetActive(true);
                    ShowStickers();
                    closeButton?.SetActive(true);
                    okButton?.SetActive(true);
                    backButton?.SetActive(true); // 🆕 Ativa o botão Voltar

                    StartCoroutine(PlaySlidingImage());

                }
                else
                {
                    Debug.LogError("Erro: Texture nula ou imageDisplay nulo após retorno da câmera.");
                }
            }
        }, maxSize: 1024);
    }

    public void ShowStickers()
    {
        if (stickerMenuContent == null || stickerMenuScrollView == null || locationManager == null)
            return;

        ClearStickerMenu();

        stickerMenuScrollView.SetActive(true);
        spawnedStickersCount.Clear();

        GameObject[] stickers = GetAllStickers();
        if (stickers == null || stickers.Length == 0) return;

        stickerStates.Clear();

        foreach (var sticker in GetAllStickers())
        {
            if (!stickerAreaCache.ContainsKey(sticker)) continue;

            string area = stickerAreaCache[sticker];
            int index = GetIndexForPrefab(sticker);
            string key = $"{area}_{index}";

            if (!stickerStates.ContainsKey(key))
                stickerStates[key] = StickerState.InMenu;
        }


        BuildStickerPages(stickers);
        currentPageIndex = 0;

        ShowCurrentPage();
        UpdateArrowVisibility();
    }



    public void SetStickerState(string area, int index, StickerState state)
    {
        string key = $"{area}_{index}";
        stickerStates[key] = state;
    }





    private void ClearStickerMenu()
    {
        foreach (Transform child in stickerMenuContent)
            Destroy(child.gameObject);
    }


    private void BuildStickerPages(GameObject[] stickers)
    {
        stickerPages.Clear();

        for (int i = 0; i < stickers.Length; i += 2)
        {
            if (i + 1 < stickers.Length)
                stickerPages.Add(new GameObject[] { stickers[i], stickers[i + 1] });
            else
                stickerPages.Add(new GameObject[] { stickers[i] }); // última com 1
        }
    }

    private void ShowCurrentPage()
    {
        ClearStickerMenu();

        GameObject[] page = stickerPages[currentPageIndex];

        foreach (var stickerPrefab in page)
        {
            if (stickerPrefab == null) continue;

            var sticker = Instantiate(stickerPrefab, stickerMenuContent);
            var controller = sticker.GetComponent<StickerController>()
                             ?? sticker.AddComponent<StickerController>();

            controller.SetRawImageRect(imageDisplay.rectTransform);

            if (stickerAreaCache.TryGetValue(stickerPrefab, out string area))
                controller.AreaName = area;

            controller.StickerIndex = GetIndexForPrefab(stickerPrefab);

            // ====== AQUI: FILTRO CRÍTICO ======
            string key = $"{controller.AreaName}_{controller.StickerIndex}";

            if (stickerStates.TryGetValue(key, out StickerState state)
                && state == StickerState.InPhoto)
            {
                Destroy(sticker);
                continue;
            }
            // ====== FIM ======

            if (!string.IsNullOrEmpty(controller.AreaName))
            {
                if (spawnedStickersCount.ContainsKey(controller.AreaName))
                    spawnedStickersCount[controller.AreaName]++;
                else
                    spawnedStickersCount[controller.AreaName] = 1;
            }
        }
    }


    public void NextPage()
    {
        if (currentPageIndex < stickerPages.Count - 1)
        {
            currentPageIndex++;
            ShowCurrentPage();
            UpdateArrowVisibility();
        }
    }


    public void PrevPage()
    {
        if (currentPageIndex > 0)
        {
            currentPageIndex--;
            ShowCurrentPage();
            UpdateArrowVisibility();
        }
    }

    private void UpdateArrowVisibility()
    {
        prevPageButton.gameObject.SetActive(currentPageIndex > 0);
        nextPageButton.gameObject.SetActive(currentPageIndex < stickerPages.Count - 1);
    }




    private int GetIndexForPrefab(GameObject prefab)
    {
        if (prefab == null) return -1;

        if (stickerIndexCache.TryGetValue(prefab, out int idx))
            return idx;

        int found = -1;
        found = IndexOfInArray(area1Stickers, prefab); if (found != -1) return found;
        found = IndexOfInArray(area2Stickers, prefab); if (found != -1) return found;
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
        List<GameObject> finalList = new List<GameObject>();

        // 1️⃣ Stickers da área atual (fixos + coletados)
        finalList.AddRange(GetStickersFromCurrentArea());

        // 2️⃣ Coleta SOMENTE OS FIXOS (0–2) das OUTRAS áreas
        List<GameObject> fixedOthers = GetOnlyFixedStickersFromOtherAreas();

        // ✅ GARANTE QUE SEMPRE EXISTAM 3
        if (fixedOthers.Count < 3)
        {
            Debug.LogError("❌ ERRO: Não existem 3 stickers fixos suficientes nas outras áreas!");
            return finalList.ToArray();
        }

        // 3️⃣ Sorteia EXATAMENTE 3 sem repetição
        for (int i = 0; i < 3; i++)
        {
            int rnd = UnityEngine.Random.Range(0, fixedOthers.Count);
            finalList.Add(fixedOthers[rnd]);
            fixedOthers.RemoveAt(rnd);
        }

        // ✅ ✅ ✅ EMBARALHA TUDO ANTES DE RETORNAR
        ShuffleList(finalList);

        return finalList.ToArray();
    }


    private List<GameObject> GetStickersFromCurrentArea()
    {
        List<GameObject> list = new List<GameObject>();

        GameObject[] arr = GetStickersArrayByArea(currentArea);
        if (arr == null) return list;

        for (int i = 0; i < arr.Length; i++)
        {
            bool isFixed = i < 3;
            bool isCollectedExtra = i >= 3 ? locationManager.IsStickerCollected(currentArea, i) : true;

            if (isFixed || isCollectedExtra)
                list.Add(arr[i]);
        }

        return list;
    }

    private List<GameObject> GetOnlyFixedStickersFromOtherAreas()
    {
        List<GameObject> list = new List<GameObject>();

        string[] areas = new string[]
        {
         "CursoDagua", "Subosque",
        "Dossel", "Epifitas", "Serrapilheira"
        };

        foreach (string areaName in areas)
        {
            if (areaName == currentArea) continue;

            GameObject[] arr = GetStickersArrayByArea(areaName);
            if (arr == null) continue;

            // ✅ Agora ele só adiciona se EXISTIR de verdade
            if (arr.Length > 0 && arr[0] != null) list.Add(arr[0]);
            if (arr.Length > 1 && arr[1] != null) list.Add(arr[1]);
            if (arr.Length > 2 && arr[2] != null) list.Add(arr[2]);
        }

        return list;
    }











    private GameObject[] GetStickersArrayByArea(string area)
    {
        return area switch
        {
            "Area1" => area1Stickers,
            "Area2" => area2Stickers,
            "CursoDagua" => cursoDaguaStickers,
            "Subosque" => subosqueStickers,
            "Dossel" => dosselStickers,
            "Epifitas" => epifitasStickers,
            "Serrapilheira" => serrapilheiraStickers,
            "AreaTeste" => areaTesteStickers,
            _ => null
        };
    }





    private void AddStickersForArea(string areaName, GameObject[] stickersArray, List<GameObject> outputList, bool showAllForArea)
    {
        if (stickersArray == null || stickersArray.Length == 0 || locationManager == null)
            return;

        int usedCount = locationManager.GetUsedStickerCount(areaName);
        if (usedCount >= 6) return;

        for (int i = 0; i < stickersArray.Length; i++)
        {
            GameObject prefab = stickersArray[i];
            if (prefab == null) continue;

            bool isFixedSticker = (i < 3);
            bool isCollectedExtra = (i >= 3) ? locationManager.IsStickerCollected(areaName, i) : true;

            if (!isCollectedExtra) continue;

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
        // ✅ DESTRÓI A TEXTURA DA FOTO EXIBIDA
        if (imageDisplay.texture != null)
        {
            Destroy(imageDisplay.texture);
            imageDisplay.texture = null;
            Resources.UnloadUnusedAssets();
            GC.Collect();
        }

        imageDisplay.gameObject.SetActive(false);
        closeButton?.SetActive(false);
        okButton?.SetActive(false);
        backButton?.SetActive(false);

        foreach (var sticker in FindObjectsOfType<StickerController>())
            Destroy(sticker.gameObject);

        activeStickers.Clear();

        if (stickerMenuContent != null)
            foreach (Transform child in stickerMenuContent)
                Destroy(child.gameObject);

        stickerMenuScrollView?.SetActive(false);
        stickerStates.Clear();

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

        if (locationManager != null)
        {
            foreach (var st in activeStickers)
            {
                if (!string.IsNullOrEmpty(st.AreaName) && st.StickerIndex >= 0)
                    locationManager.MarkStickerAsUsed(st.AreaName, st.StickerIndex);
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
            case "Area2": area2CountText.text = text; break; // NOVA ÁREA
            case "CursoDagua": cursoDaguaCountText.text = text; break;
            case "Subosque": subosqueCountText.text = text; break;
            case "Dossel": dosselCountText.text = text; break;
            case "Epifitas": epifitasCountText.text = text; break;
            case "Serrapilheira": serrapilheiraCountText.text = text; break;
            case "AreaTeste": areaTesteCountText.text = text; break;
        }
    }

    public bool AreStickersFromCorrectArea()
    {
        foreach (StickerController sticker in activeStickers)
            if (!IsStickerFromCurrentArea(sticker)) return false;
        return true;
    }

    private bool AreThereUnusedStickersFromCurrentArea()
    {
        foreach (var kv in stickerStates)
        {
            string key = kv.Key;
            StickerState state = kv.Value;

            // key = "Area1_3"
            if (!key.StartsWith(currentArea + "_"))
                continue;

            if (state == StickerState.InMenu)
                return true; // ainda existe sticker da área fora da foto
        }

        return false; // todos os stickers da área estão na foto
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
            "Area2" => "Área 2",
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
        {
            galleryManager.SaveImage(currentArea, screenshot);
            galleryManager.AtualizarMiniaturas();
        }

        // ✅ DESTRÓI A TEXTURA DO PRINT
        Destroy(screenshot);
        Resources.UnloadUnusedAssets();
        GC.Collect();

        ClosePhotoView();
        UpdateAllCountersFromLocationManager();
        // 🔴 AVISA O MAPA QUE A ÁREA FOI CONCLUÍDA
        if (!string.IsNullOrEmpty(currentArea) && MapPinsController.Instance != null)
        {
            MapPinsController.Instance.MarkPinVisited(currentArea);
        }

    }

    private void InicializarContadores()
    {
        area1CountText.text = "0/6";
        area2CountText.text = "0/6"; // NOVO
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
        area2CountText.text = $"Área 2: {locationManager.GetUsedStickerCount("Area2")}/6"; // NOVO
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

        InicializarContadores();

        if (locationManager != null)
        {
            PlayerPrefs.DeleteKey("UsedStickers");
            PlayerPrefs.DeleteKey("CollectedStickers");
            locationManager = FindAnyObjectByType<LocationServiceManager>();
            locationManager?.SendMessage("LoadCollectedStickers", SendMessageOptions.DontRequireReceiver);
        }
    }

    public bool IsStickerAlreadyRegistered(StickerController sticker)
    {
        return activeStickers.Contains(sticker);
    }

    public void FecharFotoDeTeste()
    {
        if (imageDisplay != null && imageDisplay.gameObject.activeSelf)
        {
            imageDisplay.gameObject.SetActive(false);
            Debug.Log("✅ Foto fechada (teste)");
        }
        else
        {
            Debug.LogWarning("⚠️ Nenhuma foto aberta para fechar.");
        }
    }

    public void VoltarFotoDecorada()
    {
        ClosePhotoView(); // Apenas fecha, sem verificar nem salvar
    }


    private void ShuffleList(List<GameObject> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int rnd = UnityEngine.Random.Range(0, i + 1);
            GameObject temp = list[i];
            list[i] = list[rnd];
            list[rnd] = temp;
        }
    }



    private Vector2 slidingFinalPos; // posição final (a do Unity)

    private IEnumerator PlaySlidingImage()
    {
        if (slidingImage == null)
            yield break;

        // garante que a imagem está ativa
        slidingImage.gameObject.SetActive(true);

        // posição final = onde você deixou no Unity
        slidingFinalPos = slidingImage.anchoredPosition;

        // calcula a posição inicial fora da tela (à esquerda)
        Vector2 offScreenLeft = new Vector2(
            slidingFinalPos.x - Screen.width,
            slidingFinalPos.y
        );

        // começa fora da tela
        slidingImage.anchoredPosition = offScreenLeft;

        // ---- ENTRADA DA ESQUERDA ----
        float t = 0;
        while (t < slideDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / slideDuration);
            slidingImage.anchoredPosition = Vector2.Lerp(offScreenLeft, slidingFinalPos, p);
            yield return null;
        }

        // fica parada um tempo
        yield return new WaitForSeconds(slideStayTime);

        // ---- SAÍDA PARA A ESQUERDA ----
        t = 0;
        while (t < slideDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / slideDuration);
            slidingImage.anchoredPosition = Vector2.Lerp(slidingFinalPos, offScreenLeft, p);
            yield return null;
        }

        // desativa depois de sair
        slidingImage.gameObject.SetActive(false);
    }






}


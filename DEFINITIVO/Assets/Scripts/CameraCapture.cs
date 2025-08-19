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
    public Transform stickerMenuContent; // Content do ScrollView
    public GameObject stickerMenuScrollView; // Objeto Scroll View inteiro
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

    [Header("Progresso")]
    public TextMeshProUGUI progressText;
    private int areasVisitadas = 0;
    private const int TOTAL_AREAS = 5;
    private List<string> areasContabilizadas = new List<string>();

    [Header("Sticker Limit")]
    public TextMeshProUGUI errorMessageText;
    public TextMeshProUGUI warningMessageText;
    public int maxStickersPerPhoto = 5;
    private List<StickerController> activeStickers = new List<StickerController>();

    // Cache para otimização de performance
    private Dictionary<GameObject, string> stickerAreaCache = new Dictionary<GameObject, string>();

    // Contador de stickers spawnados por área
    private Dictionary<string, int> spawnedStickersCount = new Dictionary<string, int>();

    private void Start()
    {
        if (stickerMenuScrollView != null)
            stickerMenuScrollView.SetActive(false);

        if (closeButton != null)
            closeButton.SetActive(false);

        if (locationManager == null)
            locationManager = FindAnyObjectByType<LocationServiceManager>();

        if (progressText != null)
            progressText.text = $"{areasVisitadas} de {TOTAL_AREAS} áreas visitadas";

        if (errorMessageText != null)
            errorMessageText.gameObject.SetActive(false);

        if (warningMessageText != null)
            warningMessageText.gameObject.SetActive(false);

        CacheStickerAreas();
        spawnedStickersCount.Clear();
    }

    private void CacheStickerAreas()
    {
        stickerAreaCache.Clear();
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

        foreach (GameObject sticker in stickers)
        {
            if (sticker != null && !stickerAreaCache.ContainsKey(sticker))
            {
                stickerAreaCache[sticker] = areaName;
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

    public void ShowErrorMessage(string message)
    {
        StartCoroutine(ShowMessageCoroutine(errorMessageText, message, 3f));
    }

    public void ShowWarningMessage(string message)
    {
        StartCoroutine(ShowMessageCoroutine(warningMessageText, message, 3f));
    }

    private IEnumerator ShowMessageCoroutine(TextMeshProUGUI textElement, string message, float duration)
    {
        textElement.text = message;
        textElement.gameObject.SetActive(true);
        yield return new WaitForSeconds(duration);
        textElement.gameObject.SetActive(false);
    }

    public Sprite GetStickerSprite(string areaName, int index)
    {
        GameObject[] stickers = null;

        switch (areaName)
        {
            case "Area1": stickers = area1Stickers; break;
            case "CursoDagua": stickers = cursoDaguaStickers; break;
            case "Subosque": stickers = subosqueStickers; break;
            case "Dossel": stickers = dosselStickers; break;
            case "Epifitas": stickers = epifitasStickers; break;
            case "Serrapilheira": stickers = serrapilheiraStickers; break;
            case "AreaTeste": stickers = areaTesteStickers; break;
        }

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
        if (stickerMenuContent == null || stickerMenuScrollView == null) return;

        foreach (Transform child in stickerMenuContent)
            Destroy(child.gameObject);

        stickerMenuScrollView.SetActive(true);
        spawnedStickersCount.Clear();

        GameObject[] stickersToShow = GetAllStickers();
        if (stickersToShow == null || stickersToShow.Length == 0) return;

        foreach (var stickerPrefab in stickersToShow)
        {
            if (stickerPrefab != null)
            {
                var sticker = Instantiate(stickerPrefab, stickerMenuContent);
                var controller = sticker.GetComponent<StickerController>() ?? sticker.AddComponent<StickerController>();
                controller.SetRawImageRect(imageDisplay.rectTransform);

                // ✅ NOVO: marca a área de origem do clone
                if (stickerAreaCache.TryGetValue(stickerPrefab, out string areaName))
                {
                    controller.AreaName = areaName;

                    if (spawnedStickersCount.ContainsKey(areaName))
                        spawnedStickersCount[areaName]++;
                    else
                        spawnedStickersCount[areaName] = 1;
                }
            }
        }
    }

    private GameObject[] GetAllStickers()
    {
        if (locationManager == null) return null;

        List<GameObject> stickers = new List<GameObject>();
        AddStickersForArea("Area1", area1Stickers, stickers);
        AddStickersForArea("CursoDagua", cursoDaguaStickers, stickers);
        AddStickersForArea("Subosque", subosqueStickers, stickers);
        AddStickersForArea("Dossel", dosselStickers, stickers);
        AddStickersForArea("Epifitas", epifitasStickers, stickers);
        AddStickersForArea("Serrapilheira", serrapilheiraStickers, stickers);
        AddStickersForArea("AreaTeste", areaTesteStickers, stickers);

        return stickers.ToArray();
    }

    private void AddStickersForArea(string areaName, GameObject[] stickersArray, List<GameObject> outputList)
    {
        if (stickersArray == null || stickersArray.Length == 0) return;

        for (int i = 0; i < Mathf.Min(3, stickersArray.Length); i++)
            if (stickersArray[i] != null) outputList.Add(stickersArray[i]);

        for (int i = 3; i < stickersArray.Length; i++)
            if (stickersArray[i] != null && locationManager.IsStickerCollected(areaName, i))
                outputList.Add(stickersArray[i]);
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

        activeStickers.Clear();

        foreach (var sticker in FindObjectsOfType<StickerController>())
            Destroy(sticker.gameObject);

        if (stickerMenuContent != null)
            foreach (Transform child in stickerMenuContent)
                Destroy(child.gameObject);

        if (stickerMenuScrollView != null)
            stickerMenuScrollView.SetActive(false);
    }

    public void ConfirmarFotoDecorada()
    {
        if (!AreStickersFromCorrectArea())
        {
            ShowErrorMessage("Há stickers de outra área na foto! Use apenas os stickers da área " + GetAreaDisplayName(currentArea));
            return;
        }

        if (AreThereUnusedStickersFromCurrentArea())
        {
            ShowWarningMessage("Ainda há stickers da área " + GetAreaDisplayName(currentArea) + " que não foram utilizados!");
            return;
        }

        StartCoroutine(CaptureAndSave());
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
            if (sticker.AreaName == areaName) count++; // ✅ usa o campo direto

        return count;
    }

    private bool IsStickerFromCurrentArea(StickerController sticker)
    {
        return sticker.AreaName == currentArea;
    }

    private string GetAreaDisplayName(string areaCode)
    {
        switch (areaCode)
        {
            case "Area1": return "Área 1";
            case "CursoDagua": return "Curso D'água";
            case "Subosque": return "Subosque";
            case "Dossel": return "Dossel";
            case "Epifitas": return "Epífitas";
            case "Serrapilheira": return "Serrapilheira";
            case "AreaTeste": return "Área Teste";
            default: return areaCode;
        }
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
    }

    public void ResetarProgresso()
    {
        areasVisitadas = 0;
        areasContabilizadas.Clear();
        progressText.text = $"{areasVisitadas} de {TOTAL_AREAS} áreas visitadas";
    }

    public bool IsStickerAlreadyRegistered(StickerController sticker)
    {
        return activeStickers.Contains(sticker);
    }
}

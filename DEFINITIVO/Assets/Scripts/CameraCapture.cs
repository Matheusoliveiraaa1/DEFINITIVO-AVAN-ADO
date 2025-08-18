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
    public int maxStickersPerPhoto = 5;
    private List<StickerController> activeStickers = new List<StickerController>();

    private void Start()
    {
        if (stickerMenuScrollView != null) stickerMenuScrollView.SetActive(false);
        if (closeButton != null) closeButton.SetActive(false);
        if (locationManager == null) locationManager = FindAnyObjectByType<LocationServiceManager>();
        if (progressText != null) progressText.text = $"{areasVisitadas} de {TOTAL_AREAS} áreas visitadas";

        // Garante que a mensagem de erro comece desativada
        if (errorMessageText != null) errorMessageText.gameObject.SetActive(false);
    }

    public bool CanAddSticker()
    {
        return activeStickers.Count < maxStickersPerPhoto;
    }

    public void RegisterSticker(StickerController sticker)
    {
        if (!activeStickers.Contains(sticker))
        {
            activeStickers.Add(sticker);
        }
    }

    public void UnregisterSticker(StickerController sticker)
    {
        if (activeStickers.Contains(sticker))
        {
            activeStickers.Remove(sticker);
        }
    }

    public void ShowErrorMessage(string message)
    {
        StartCoroutine(ShowErrorMessageCoroutine(message, 3f));
    }

    private IEnumerator ShowErrorMessageCoroutine(string message, float duration)
    {
        errorMessageText.text = message;
        errorMessageText.gameObject.SetActive(true);
        yield return new WaitForSeconds(duration);
        errorMessageText.gameObject.SetActive(false);
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
                    Debug.Log("Foto tirada em: " + path);
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

        foreach (Transform child in stickerMenuContent) Destroy(child.gameObject);
        stickerMenuScrollView.SetActive(true);

        // pega TODOS os stickers de todas as áreas
        GameObject[] stickersToShow = GetAllStickers();
        if (stickersToShow == null || stickersToShow.Length == 0)
        {
            Debug.LogWarning("Nenhum sticker definido.");
            return;
        }

        foreach (var stickerPrefab in stickersToShow)
        {
            if (stickerPrefab != null)
            {
                var sticker = Instantiate(stickerPrefab, stickerMenuContent);
                var controller = sticker.GetComponent<StickerController>() ?? sticker.AddComponent<StickerController>();
                controller.SetRawImageRect(imageDisplay.rectTransform);
            }
        }
    }

    private GameObject[] GetAllStickers()
    {
        if (locationManager == null)
        {
            Debug.LogError("LocationServiceManager não encontrado!");
            return null;
        }

        List<GameObject> stickers = new List<GameObject>();

        // Adiciona de todas as áreas
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

        // sempre adiciona os 3 fixos
        for (int i = 0; i < Mathf.Min(3, stickersArray.Length); i++)
        {
            if (stickersArray[i] != null) outputList.Add(stickersArray[i]);
        }

        // só adiciona os colecionáveis se já tiver coletado
        for (int i = 3; i < stickersArray.Length; i++)
        {
            if (stickersArray[i] != null && locationManager.IsStickerCollected(areaName, i))
                outputList.Add(stickersArray[i]);
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

        // Limpa a lista de stickers ativos
        activeStickers.Clear();

        // Remove todos os stickers ativos na cena
        foreach (var sticker in FindObjectsOfType<StickerController>())
        {
            Destroy(sticker.gameObject);
        }

        // Limpa o conteúdo do menu
        if (stickerMenuContent != null)
        {
            foreach (Transform child in stickerMenuContent)
                Destroy(child.gameObject);
        }

        if (stickerMenuScrollView != null)
            stickerMenuScrollView.SetActive(false);
    }

    public void ConfirmarFotoDecorada()
    {
        StartCoroutine(CaptureAndSave());
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
        }

        ClosePhotoView();
    }

    public void ResetarProgresso()
    {
        areasVisitadas = 0;
        areasContabilizadas.Clear();
        progressText.text = $"{areasVisitadas} de {TOTAL_AREAS} áreas visitadas";
    }
}
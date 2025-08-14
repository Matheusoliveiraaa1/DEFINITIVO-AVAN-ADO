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

        foreach (Transform child in stickerMenuContent)
            Destroy(child.gameObject);

        stickerMenuScrollView.SetActive(true);

        GameObject[] stickersToShow = GetStickersForCurrentArea();

        if (stickersToShow == null || stickersToShow.Length == 0)
        {
            Debug.LogWarning("Nenhum sticker definido para esta área.");
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

    private GameObject[] GetStickersForCurrentArea()
    {
        if (locationManager == null)
        {
            Debug.LogError("LocationServiceManager não encontrado!");
            return null;
        }

        List<GameObject> stickers = new List<GameObject>();

        switch (currentArea)
        {
            case "Area1": AddStickersForArea("Area1", area1Stickers, stickers); break;
            case "CursoDagua": AddStickersForArea("CursoDagua", cursoDaguaStickers, stickers); break;
            case "Subosque": AddStickersForArea("Subosque", subosqueStickers, stickers); break;
            case "Dossel": AddStickersForArea("Dossel", dosselStickers, stickers); break;
            case "Epifitas": AddStickersForArea("Epifitas", epifitasStickers, stickers); break;
            case "Serrapilheira": AddStickersForArea("Serrapilheira", serrapilheiraStickers, stickers); break;
            case "AreaTeste": AddStickersForArea("AreaTeste", areaTesteStickers, stickers); break;
            default:
                Debug.LogWarning($"Área desconhecida: {currentArea}");
                break;
        }

        return stickers.ToArray();
    }

    private void AddStickersForArea(string areaName, GameObject[] stickersArray, List<GameObject> outputList)
    {
        if (stickersArray == null || stickersArray.Length == 0)
        {
            Debug.LogWarning($"Nenhum sticker definido para a área: {areaName}");
            return;
        }

        for (int i = 0; i < Mathf.Min(3, stickersArray.Length); i++)
        {
            if (stickersArray[i] != null)
                outputList.Add(stickersArray[i]);
        }

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

        // Remove todos os stickers ativos na cena, mesmo os que foram arrastados
        foreach (var sticker in FindObjectsOfType<StickerController>())
        {
            Destroy(sticker.gameObject);
        }

        // Limpa o conteúdo do menu (caso ainda tenha filhos)
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

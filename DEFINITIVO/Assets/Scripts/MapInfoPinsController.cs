using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MapInfoPinsController : MonoBehaviour
{
    [System.Serializable]
    public class MapPointData
    {
        public string pointName;
        public Vector2 anchoredPosition;   // posição no mapa
        public Sprite buttonSprite;         // sprite do botão/prefab no mapa
        public Sprite overlayImage;         // imagem que aparece no overlay
        [TextArea(4, 10)]
        public string overlayText;          // texto que aparece no overlay
    }

    [Header("Mapa")]
    public RectTransform mapRectTransform;   // RectTransform do mapa
    public Button pinPrefab;                  // prefab base do botão
    public MapPointData[] points;             // seus 5 pontos

    [Header("Overlay")]
    public GameObject overlayPanel;           // painel escuro/overlay
    public RawImage overlayRawImage;          // imagem grande
    public TextMeshProUGUI overlayTextTMP;    // texto grande
    public Button closeButton;                // botão de sair

    private void Start()
    {
        if (overlayPanel != null)
            overlayPanel.SetActive(false);

        CreatePins();

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(CloseOverlay);
        }
    }

    void CreatePins()
    {
        if (mapRectTransform == null || pinPrefab == null || points == null)
        {
            Debug.LogError("MapInfoPinsController: faltam referências no Inspector.");
            return;
        }

        foreach (var point in points)
        {
            if (point == null) continue;

            Button newPin = Instantiate(pinPrefab, mapRectTransform);
            newPin.name = point.pointName;

            RectTransform rt = newPin.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchoredPosition = point.anchoredPosition;
                rt.localScale = Vector3.one;
                rt.localRotation = Quaternion.identity;
            }

            Image img = newPin.GetComponent<Image>();
            if (img != null && point.buttonSprite != null)
            {
                img.sprite = point.buttonSprite;
                img.preserveAspect = true;
            }

            MapPointData capturedPoint = point;
            newPin.onClick.RemoveAllListeners();
            newPin.onClick.AddListener(() => OpenOverlay(capturedPoint));
        }
    }

    void OpenOverlay(MapPointData data)
    {
        if (overlayPanel != null)
            overlayPanel.SetActive(true);

        if (overlayTextTMP != null)
            overlayTextTMP.text = data.overlayText;

        if (overlayRawImage != null)
            SetRawImageFromSprite(overlayRawImage, data.overlayImage);
    }

    void CloseOverlay()
    {
        if (overlayPanel != null)
            overlayPanel.SetActive(false);
    }

    void SetRawImageFromSprite(RawImage rawImage, Sprite sprite)
    {
        if (rawImage == null || sprite == null) return;

        rawImage.texture = sprite.texture;

        // Faz a RawImage mostrar apenas a região correta do sprite
        Rect rect = sprite.textureRect;
        Texture tex = sprite.texture;

        rawImage.uvRect = new Rect(
            rect.x / tex.width,
            rect.y / tex.height,
            rect.width / tex.width,
            rect.height / tex.height
        );
    }
}
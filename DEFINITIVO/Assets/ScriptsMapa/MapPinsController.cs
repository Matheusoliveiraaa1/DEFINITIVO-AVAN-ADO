using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MapPinsController : MonoBehaviour
{
    [Header("Referências")]
    public RectTransform mapRectTransform;   // Referência para o mapa
    public Button pinPrefab;                 // Prefab do pin
    public GameObject areaInfoPanel;         // Painel de informações da área
    public TextMeshProUGUI areaTitleText;    // Título (nome da área)
    public TextMeshProUGUI areaDescriptionText; // Descrição da área
    public Button viewStickersButton;        // Botão "Ver Stickers"

    [Header("Painel de Stickers")]
    public GameObject stickersPanel;         // Painel que contém o ScrollView
    public Transform stickersContentParent;  // O Content do ScrollView onde as imagens serão criadas
    public GameObject stickerImagePrefab;    // Prefab simples com componente Image (ou Button)

    [System.Serializable]
    public class PinData
    {
        public string pinName;   // Nome da área (ex: "CursoDagua")
        public Vector2 position; // Posição dentro do mapa
        [TextArea] public string description; // Texto da área
    }

    [Header("Pins configurados")]
    public PinData[] pins;

    [Header("Imagens de Stickers por Área")]
    public Sprite[] cursoDaguaStickers;
    public Sprite[] subosqueStickers;
    public Sprite[] dosselStickers;
    public Sprite[] epifitasStickers;
    public Sprite[] serrapilheiraStickers;

    // ================= NOVA SEÇÃO =================
    [Header("Painel de Detalhes da Espécie")]
    public GameObject speciesInfoPanel;      // Painel de detalhes
    public TextMeshProUGUI commonNameText;   // Nome popular
    public TextMeshProUGUI scientificNameText; // Nome científico
    public Image realImage;                  // Imagem real da espécie
    public Image stickerImage;               // Imagem do sticker
    public TextMeshProUGUI descriptionText;  // Descrição
    public Button backButton;                // Botão voltar

    [System.Serializable]
    public class SpeciesData
    {
        public string commonName;     // Nome popular
        public string scientificName; // Nome científico
        public Sprite realPhoto;      // Imagem real
        public Sprite stickerSprite;  // Sticker correspondente
        [TextArea] public string description; // Descrição
    }

    [Header("Espécies")]
    public SpeciesData[] speciesList;
    // ==============================================

    void Start()
    {
        foreach (var pin in pins)
            AddPin(pin);

        if (areaInfoPanel != null)
            areaInfoPanel.SetActive(false);

        if (stickersPanel != null)
            stickersPanel.SetActive(false);

        if (speciesInfoPanel != null)
            speciesInfoPanel.SetActive(false);
    }

    void AddPin(PinData pinData)
    {
        Button newPin = Instantiate(pinPrefab, mapRectTransform);
        newPin.GetComponent<RectTransform>().anchoredPosition = pinData.position;
        newPin.name = pinData.pinName;
        newPin.onClick.AddListener(() => OnPinClicked(pinData));
    }

    void OnPinClicked(PinData pinData)
    {
        if (pinData.pinName == "Área 1" || pinData.pinName == "Área 2" || pinData.pinName == "Área Teste")
            return;

        areaInfoPanel.SetActive(true);
        areaTitleText.text = GetDisplayName(pinData.pinName);
        areaDescriptionText.text = string.IsNullOrEmpty(pinData.description)
            ? "Sem descrição disponível."
            : pinData.description;

        viewStickersButton.onClick.RemoveAllListeners();
        viewStickersButton.onClick.AddListener(() => OpenStickers(pinData.pinName));
    }

    void OpenStickers(string areaName)
    {
        ShowStickerImages(areaName);

        if (stickersPanel != null)
            stickersPanel.SetActive(true);

        areaInfoPanel.SetActive(false);
        speciesInfoPanel.SetActive(false);
    }

    void ShowStickerImages(string areaName)
    {
        foreach (Transform child in stickersContentParent)
            Destroy(child.gameObject);

        Sprite[] stickerSprites = null;

        switch (areaName)
        {
            case "CursoDagua":
                stickerSprites = cursoDaguaStickers;
                break;
            case "Subosque":
                stickerSprites = subosqueStickers;
                break;
            case "Dossel":
                stickerSprites = dosselStickers;
                break;
            case "Epifitas":
                stickerSprites = epifitasStickers;
                break;
            case "Serrapilheira":
                stickerSprites = serrapilheiraStickers;
                break;
            default:
                Debug.LogWarning("Área não reconhecida: " + areaName);
                return;
        }

        if (stickerSprites == null || stickerSprites.Length == 0)
        {
            Debug.Log("Nenhuma imagem de sticker configurada para " + areaName);
            return;
        }

        foreach (var sprite in stickerSprites)
        {
            if (sprite == null) continue;

            GameObject newImageObj = Instantiate(stickerImagePrefab, stickersContentParent);
            Image newImage = newImageObj.GetComponent<Image>();
            newImage.sprite = sprite;
            newImage.preserveAspect = true;

            Button btn = newImageObj.GetComponent<Button>();
            if (btn == null) btn = newImageObj.AddComponent<Button>();

            btn.onClick.AddListener(() => OpenSpeciesInfo(sprite));
        }
    }

    void OpenSpeciesInfo(Sprite clickedSticker)
    {
        SpeciesData found = null;
        foreach (var s in speciesList)
        {
            if (s.stickerSprite == clickedSticker)
            {
                found = s;
                break;
            }
        }

        if (found == null)
        {
            Debug.LogWarning("Nenhuma espécie associada a este sticker!");
            return;
        }

        commonNameText.text = found.commonName;
        scientificNameText.text = found.scientificName;
        realImage.sprite = found.realPhoto;
        stickerImage.sprite = found.stickerSprite;
        descriptionText.text = found.description;

        speciesInfoPanel.SetActive(true);
        stickersPanel.SetActive(false);

        backButton.onClick.RemoveAllListeners();
        backButton.onClick.AddListener(() =>
        {
            speciesInfoPanel.SetActive(false);
            stickersPanel.SetActive(true);
        });
    }

    string GetDisplayName(string code)
    {
        return code switch
        {
            "CursoDagua" => "Curso D’água",
            "Subosque" => "Subosque",
            "Dossel" => "Dossel",
            "Epifitas" => "Epífitas",
            "Serrapilheira" => "Serrapilheira",
            _ => code
        };
    }
}

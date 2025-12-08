using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class MapPinsController : MonoBehaviour
{
    [Header("Referências")]
    public RectTransform mapRectTransform; // Referência para o mapa
    public Button pinPrefab; // Prefab do pin (deve ter Image)
    public GameObject areaInfoPanel; // Painel de informações da área
    public TextMeshProUGUI areaTitleText; // Título (nome da área)
    public TextMeshProUGUI areaDescriptionText; // Descrição da área
    public Button viewStickersButton; // Botão "Ver Stickers"

    [Header("Aparência do pin visitado (opcional)")]
    public Sprite visitedPinSprite; // se atribuir, será trocado o sprite
    public bool useSpriteSwap = false; // se true usa visitedPinSprite, senao aplica tint verde
    public Color visitedTint = Color.green; // cor usada se não usar sprite swap

    [Header("Painel de Stickers")]
    public GameObject stickersPanel;
    public Transform stickersContentParent;
    public GameObject stickerImagePrefab;

    [System.Serializable]
    public class PinData
    {
        public string pinName;
        public Vector2 position;
        [TextArea] public string description;
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
    public GameObject speciesInfoPanel;
    public TextMeshProUGUI commonNameText;
    public TextMeshProUGUI scientificNameText;
    public Image realImage;
    public Image stickerImage;
    public TextMeshProUGUI descriptionText;
    public Button backButton;

    [System.Serializable]
    public class SpeciesData
    {
        public string commonName;
        public string scientificName;
        public Sprite realPhoto;
        public Sprite stickerSprite;
        [TextArea] public string description;
    }

    [Header("Espécies")]
    public SpeciesData[] speciesList;
    // ==============================================

    // NOVO: Histórico de navegação
    private Stack<GameObject> panelHistory = new Stack<GameObject>();

    // NOVO: dicionário para acessar pins instanciados por nome
    private Dictionary<string, Button> spawnedPins = new Dictionary<string, Button>();

    // Opcional: singleton simples para facilitar chamadas externas
    public static MapPinsController Instance { get; private set; }

    void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) Destroy(gameObject);
    }

    void Start()
    {
        spawnedPins.Clear();

        foreach (var pin in pins) AddPin(pin);

        if (areaInfoPanel != null) areaInfoPanel.SetActive(false);
        if (stickersPanel != null) stickersPanel.SetActive(false);
        if (speciesInfoPanel != null) speciesInfoPanel.SetActive(false);
    }

    void AddPin(PinData pinData)
    {
        if (pinPrefab == null || mapRectTransform == null)
        {
            Debug.LogError("PinPrefab ou mapRectTransform não atribuído!");
            return;
        }

        Button newPin = Instantiate(pinPrefab, mapRectTransform);
        RectTransform rt = newPin.GetComponent<RectTransform>();
        if (rt != null) rt.anchoredPosition = pinData.position;

        newPin.name = pinData.pinName;
        newPin.onClick.AddListener(() => OnPinClicked(pinData));

        // Salva referência no dicionário (substitui se já existir)
        if (spawnedPins.ContainsKey(pinData.pinName))
            spawnedPins[pinData.pinName] = newPin;
        else
            spawnedPins.Add(pinData.pinName, newPin);
    }

    void OnPinClicked(PinData pinData)
    {
        if (pinData.pinName == "Área 1" || pinData.pinName == "Área 2" || pinData.pinName == "Área Teste") return;

        panelHistory.Push(areaInfoPanel);
        areaInfoPanel.SetActive(true);
        areaTitleText.text = GetDisplayName(pinData.pinName);
        areaDescriptionText.text = string.IsNullOrEmpty(pinData.description) ? "Sem descrição disponível." : pinData.description;

        viewStickersButton.onClick.RemoveAllListeners();
        viewStickersButton.onClick.AddListener(() => OpenStickers(pinData.pinName));
    }

    void OpenStickers(string areaName)
    {
        panelHistory.Push(stickersPanel);
        ShowStickerImages(areaName);

        if (stickersPanel != null) stickersPanel.SetActive(true);
        areaInfoPanel.SetActive(false);
        speciesInfoPanel.SetActive(false);
    }

    void ShowStickerImages(string areaName)
    {
        foreach (Transform child in stickersContentParent) Destroy(child.gameObject);

        Sprite[] stickerSprites = null;
        switch (areaName)
        {
            case "CursoDagua": stickerSprites = cursoDaguaStickers; break;
            case "Subosque": stickerSprites = subosqueStickers; break;
            case "Dossel": stickerSprites = dosselStickers; break;
            case "Epifitas": stickerSprites = epifitasStickers; break;
            case "Serrapilheira": stickerSprites = serrapilheiraStickers; break;
            default: Debug.LogWarning("Área não reconhecida: " + areaName); return;
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

    public void OpenSpeciesInfo(Sprite clickedSticker)
    {
        Debug.Log("MAP: OpenSpeciesInfo chamado com sticker: " + clickedSticker?.name);

        SpeciesData found = null;
        foreach (var s in speciesList)
        {
            Debug.Log("MAP: Comparando com sticker da espécie: " + s.stickerSprite?.name);

            if (s.stickerSprite == clickedSticker)
            {
                found = s;
                Debug.Log("MAP: ESPÉCIE ENCONTRADA: " + s.commonName);
                break;
            }
        }

        if (found == null)
        {
            Debug.LogError("MAP: ERRO — Nenhuma espécie associada a este sticker!");
            return;
        }

        panelHistory.Push(speciesInfoPanel);

        Debug.Log("MAP: Carregando dados no painel de espécies...");

        commonNameText.text = found.commonName;
        scientificNameText.text = found.scientificName;
        realImage.sprite = found.realPhoto;
        stickerImage.sprite = found.stickerSprite;
        descriptionText.text = found.description;

        Debug.Log("MAP: Ativando speciesInfoPanel");

        speciesInfoPanel.SetActive(true);
        stickersPanel.SetActive(false);
        areaInfoPanel.SetActive(false);

        backButton.onClick.RemoveAllListeners();
        backButton.onClick.AddListener(GoBack);
    }

    public void OpenSpeciesByName(string speciesCommonName)
    {
        SpeciesData found = null;
        foreach (var species in speciesList)
        {
            if (species.commonName == speciesCommonName)
            {
                found = species;
                break;
            }
        }

        if (found != null)
        {
            OpenSpeciesInfo(found.stickerSprite);
        }
        else
        {
            Debug.LogWarning($"Espécie não encontrada: {speciesCommonName}");
        }
    }

    public void GoBack()
    {
        if (panelHistory.Count > 0)
        {
            GameObject currentPanel = panelHistory.Pop();
            currentPanel.SetActive(false);

            if (panelHistory.Count > 0)
            {
                GameObject previousPanel = panelHistory.Peek();
                previousPanel.SetActive(true);
            }
        }
    }

    // === NOVO: marca pin como visitado (verde / sprite trocado)
    public void MarkPinVisited(string areaName)
    {
        if (string.IsNullOrEmpty(areaName)) return;

        if (!spawnedPins.TryGetValue(areaName, out Button pinButton))
        {
            Debug.LogWarning($"MarkPinVisited: pin não encontrado para área '{areaName}'");
            return;
        }

        Image pinImage = pinButton.GetComponent<Image>();
        if (pinImage == null)
        {
            // tenta procurar imagem no filho
            pinImage = pinButton.GetComponentInChildren<Image>();
            if (pinImage == null)
            {
                Debug.LogWarning("MarkPinVisited: nenhum Image encontrado no pin para aplicar mudança visual.");
                return;
            }
        }

        if (useSpriteSwap && visitedPinSprite != null)
        {
            // troca sprite mantendo o tipo de Image
            pinImage.sprite = visitedPinSprite;
            pinImage.preserveAspect = true;
        }
        else
        {
            // aplica tint verde
            pinImage.color = visitedTint;
        }

        Debug.Log($"Pin '{areaName}' marcado como visitado (visual atualizado).");
    }

    string GetDisplayName(string code)
    {
        return code switch
        {
            "CursoDagua" => "Curso D'água",
            "Subosque" => "Subosque",
            "Dossel" => "Dossel",
            "Epifitas" => "Epífitas",
            "Serrapilheira" => "Serrapilheira",
            _ => code
        };
    }

    public void ClearHistory()
    {
        panelHistory.Clear();
    }
}

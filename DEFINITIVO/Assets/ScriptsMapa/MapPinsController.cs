using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Collections;

public class MapPinsController : MonoBehaviour
{
    [Header("Referências")]
    public RectTransform mapRectTransform; // Referência para o mapa
    public Button pinPrefab; // Prefab do pin (deve ter Image)
    public GameObject areaInfoPanel; // Painel de informações da área
    public TextMeshProUGUI areaTitleText; // Título (nome da área)
    public TextMeshProUGUI areaDescriptionText; // Descrição da área
    public Button viewStickersButton; // Botão "Ver Stickers"

    [Header("Sprites dos Pins")]
    public Sprite defaultPinSprite;   // sprite inicial (cinza)
    public Sprite completedPinSprite; // sprite após concluir a área (vermelho)


    [Header("Painel de Stickers")]
    public GameObject stickersPanel;
    public Transform stickersContentParent;
    public GameObject stickerImagePrefab;

    [Header("Overlay do Species Info")]
    public GameObject speciesOverlayPanel;


    [Header("Visualização de Imagem Grande")]
    public Image largeImage;                 // imagem grande na tela
    public GameObject largeImageOverlay;     // overlay atrás
    public Button largeImageBackButton;      // botão voltar


    [Header("Imagem do Área")]
    public Image areaImage; // imagem que vai mudar de acordo com a área


    [Header("DEBUG MOBILE")]
    public TextMeshProUGUI debugText;

    [Header("Imagem Tutorial do Mapa")]
    public RectTransform tutorialImage;
    public float tutorialStayTime = 10f;
    public float tutorialAnimDuration = 0.5f;

    [Header("Tela do Mapa")]
    public GameObject mapScreen; // ARRASTE A TELA DO MAPA AQUI


    [Header("Animação Suave Tutorial")]
    public float floatAmplitude = 10f;   // quanto sobe/desce
    public float floatSpeed = 1.5f;       // velocidade



    private static bool tutorialImageAlreadyShown = false;




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


    [Header("Sprites por Área")]
    public Sprite cursoDaguaImage;
    public Sprite subosqueImage;
    public Sprite dosselImage;
    public Sprite epifitasImage;
    public Sprite serrapilheiraImage;


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
        if (largeImage != null)
            largeImage.gameObject.SetActive(false);

        if (largeImageOverlay != null)
            largeImageOverlay.SetActive(false);

        if (largeImageBackButton != null)
            largeImageBackButton.gameObject.SetActive(false);



        spawnedPins.Clear();
        if (speciesOverlayPanel != null)
            speciesOverlayPanel.SetActive(false);


        foreach (var pin in pins) AddPin(pin);

        if (areaInfoPanel != null) areaInfoPanel.SetActive(false);
        if (stickersPanel != null) stickersPanel.SetActive(false);
        if (speciesInfoPanel != null) speciesInfoPanel.SetActive(false);

     


    }

    void AddPin(PinData pinData)
    {
        Button newPin = Instantiate(pinPrefab, mapRectTransform);

        RectTransform rt = newPin.GetComponent<RectTransform>();
        if (rt != null) rt.anchoredPosition = pinData.position;

        newPin.name = pinData.pinName;
        newPin.onClick.AddListener(() => OnPinClicked(pinData));

        // 🔹 DEFINE SPRITE INICIAL (CINZA)
        Image img = newPin.GetComponent<Image>();
        if (img != null && defaultPinSprite != null)
        {
            img.sprite = defaultPinSprite;
            img.preserveAspect = true;
        }

        spawnedPins[pinData.pinName] = newPin;
    }


    void OnPinClicked(PinData pinData)
    {
        if (pinData.pinName == "Área 1" || pinData.pinName == "Área 2" || pinData.pinName == "Área Teste") return;

        panelHistory.Push(areaInfoPanel);
        areaInfoPanel.SetActive(true);
        areaTitleText.text = GetDisplayName(pinData.pinName);
        areaDescriptionText.text = string.IsNullOrEmpty(pinData.description) ? "Sem descrição disponível." : pinData.description;

        if (areaImage != null)
        {
            switch (pinData.pinName)
            {
                case "CursoDagua":
                    areaImage.sprite = cursoDaguaImage;
                    break;
                case "Subosque":
                    areaImage.sprite = subosqueImage;
                    break;
                case "Dossel":
                    areaImage.sprite = dosselImage;
                    break;
                case "Epifitas":
                    areaImage.sprite = epifitasImage;
                    break;
                case "Serrapilheira":
                    areaImage.sprite = serrapilheiraImage;
                    break;
                default:
                    areaImage.sprite = null; // ou uma imagem padrão
                    break;
            }
            areaImage.preserveAspect = true; // garante que não distorce
        }





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

        if (speciesOverlayPanel != null)
            speciesOverlayPanel.SetActive(true);


        speciesInfoPanel.SetActive(true);
        stickersPanel.SetActive(false);
        areaInfoPanel.SetActive(false);

        backButton.onClick.RemoveAllListeners();
        backButton.onClick.AddListener(GoBack);


        AddImageClick(realImage);
        AddImageClick(stickerImage);


    }
    void AddImageClick(Image img)
    {
        if (img == null || img.sprite == null) return;

        Button btn = img.GetComponent<Button>();
        if (btn == null)
            btn = img.gameObject.AddComponent<Button>();

        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => OpenLargeImage(img.sprite));
    }


    void OpenLargeImage(Sprite sprite)
    {
        if (sprite == null) return;

        largeImage.sprite = sprite;
        largeImage.preserveAspect = true;

        largeImageOverlay.SetActive(true);
        largeImage.gameObject.SetActive(true);
        largeImageBackButton.gameObject.SetActive(true);

        // esconde o conteúdo do species (opcional, mas recomendado)
        speciesInfoPanel.SetActive(false);

        largeImageBackButton.onClick.RemoveAllListeners();
        largeImageBackButton.onClick.AddListener(CloseLargeImage);
    }


    void CloseLargeImage()
    {
        largeImage.gameObject.SetActive(false);
        largeImageOverlay.SetActive(false);
        largeImageBackButton.gameObject.SetActive(false);

        speciesInfoPanel.SetActive(true);
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

            // Se estiver saindo do Species Info, desliga o overlay
            if (currentPanel == speciesInfoPanel && speciesOverlayPanel != null)
            {
                speciesOverlayPanel.SetActive(false);
            }

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
        if (debugText != null)
            debugText.text = "MarkPinVisited chamado com: " + areaName;

        if (!spawnedPins.TryGetValue(areaName, out Button pinButton))
        {
            if (debugText != null)
                debugText.text += "\nPIN NÃO ENCONTRADO";

            return;
        }

        if (debugText != null)
            debugText.text += "\nPin encontrado";

        Image pinImage = pinButton.GetComponent<Image>();
        if (pinImage == null)
        {
            if (debugText != null)
                debugText.text += "\nPin SEM Image";

            return;
        }

        if (completedPinSprite == null)
        {
            if (debugText != null)
                debugText.text += "\ncompletedPinSprite é NULL";

            return;
        }

        pinImage.sprite = completedPinSprite;
        pinImage.preserveAspect = true;

        if (debugText != null)
            debugText.text += "\nSPRITE ALTERADO COM SUCESSO";
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


    void LogDebug(string msg)
    {
        Debug.Log(msg); // continua logando no editor

        if (debugText != null)
        {
            debugText.text += "\n" + msg;
        }
    }

    IEnumerator PlayTutorialImage()
    {
        tutorialImageAlreadyShown = true;

        tutorialImage.gameObject.SetActive(false);
        yield return null; // espera 1 frame

        RectTransform parent = tutorialImage.parent as RectTransform;

        Vector2 finalPos = tutorialImage.anchoredPosition;

        Vector2 startPos = finalPos + new Vector2(parent.rect.width, 0);
        Vector2 exitPos = finalPos + new Vector2(parent.rect.width, 0);

        tutorialImage.anchoredPosition = startPos;
        tutorialImage.gameObject.SetActive(true);

        // ENTRA
        yield return MoveUI(tutorialImage, startPos, finalPos, tutorialAnimDuration);

        // 🔹 ANIMAÇÃO SUAVE ENQUANTO VISÍVEL
        Coroutine floatAnim = StartCoroutine(FloatTutorial(finalPos));

        // ESPERA
        yield return new WaitForSeconds(tutorialStayTime);

        // PARA ANIMAÇÃO SUAVE
        StopCoroutine(floatAnim);
        tutorialImage.anchoredPosition = finalPos;

        // SAI
        yield return MoveUI(tutorialImage, finalPos, exitPos, tutorialAnimDuration);

        tutorialImage.gameObject.SetActive(false);
    }


    IEnumerator FloatTutorial(Vector2 basePos)
    {
        float t = 0f;

        while (true)
        {
            t += Time.deltaTime * floatSpeed;
            float offsetY = Mathf.Sin(t) * floatAmplitude;
            tutorialImage.anchoredPosition = basePos + new Vector2(0, offsetY);
            yield return null;
        }
    }


    IEnumerator MoveUI(RectTransform rt, Vector2 from, Vector2 to, float duration)
    {
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            rt.anchoredPosition = Vector2.Lerp(from, to, t);
            yield return null;
        }

        rt.anchoredPosition = to;
    }

    public void OnMapScreenOpened()
    {
        if (tutorialImageAlreadyShown) return;

        tutorialImageAlreadyShown = true;

        StopAllCoroutines();
        StartCoroutine(DelayedTutorial());
    }



    IEnumerator DelayedTutorial()
    {
        yield return new WaitForSeconds(2f);
        StartCoroutine(PlayTutorialImage());
    }



}

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Collections;
using System.IO;

public class MapPinsController : MonoBehaviour


{

    [Header("Bloco de Stickers")]
    public GameObject stickersBlock;







    [Header("Video do Mapa")]
    public MapVideoPlayer mapVideoPlayer;

    private Texture placeholderDecoratedThumb; // guarda o placeholder original



    [Header("Thumbnail do Vídeo")]
    public Button videoButton;          // botão central
    public Image videoImage;            // imagem da thumb
    public Sprite lockedVideoSprite;    // imagem bloqueada
    public Sprite unlockedVideoSprite;  // imagem desbloqueada



    [Header("Foto Decorada")]
    public RawImage decoratedImageThumb;        // miniatura
    public GameObject decoratedImageFullPanel;  // overlay da foto grande
    public RawImage decoratedImageFull;         // foto grande
    public Button decoratedImageBackButton;     // botão voltar




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

        if (!Application.isPlaying)
            return;
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

        // Guarda a textura inicial do thumb como placeholder
        if (decoratedImageThumb != null)
            placeholderDecoratedThumb = decoratedImageThumb.texture;


        if (speciesOverlayPanel != null)
            speciesOverlayPanel.SetActive(false);

        // ======= ADICIONA E MARCA OS PINS =========
        foreach (var pin in pins)
        {
            AddPin(pin);

            // Verifica se já foi visitado em sessões anteriores
            if (PlayerPrefs.GetInt("Visited_" + pin.pinName, 0) == 1) // 0 = não visitado
            {
                // Marca o pin como visitado sem salvar de novo
                if (spawnedPins.TryGetValue(pin.pinName, out Button pinButton))
                {
                    Image pinImage = pinButton.GetComponent<Image>();
                    if (pinImage != null && completedPinSprite != null)
                    {
                        pinImage.sprite = completedPinSprite;
                        pinImage.preserveAspect = true;
                    }
                }
            }
        }

        if (areaInfoPanel != null)
            areaInfoPanel.SetActive(false);
      
        if (speciesInfoPanel != null)
            speciesInfoPanel.SetActive(false);
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

        

        // Evita pins de teste
        if (pinData.pinName == "Área 1" || pinData.pinName == "Área 2" || pinData.pinName == "Área Teste")
            return;

        // Histórico de navegação
        panelHistory.Push(areaInfoPanel);
        areaInfoPanel.SetActive(true);

        // Título e descrição
        areaTitleText.text = GetDisplayName(pinData.pinName);
        areaDescriptionText.text = string.IsNullOrEmpty(pinData.description) ? "Sem descrição disponível." : pinData.description;

        // ---------- IMAGEM ORIGINAL ----------
        if (areaImage != null)
        {
            switch (pinData.pinName)
            {
                case "CursoDagua": areaImage.sprite = cursoDaguaImage; break;
                case "Subosque": areaImage.sprite = subosqueImage; break;
                case "Dossel": areaImage.sprite = dosselImage; break;
                case "Epifitas": areaImage.sprite = epifitasImage; break;
                case "Serrapilheira": areaImage.sprite = serrapilheiraImage; break;
                default: areaImage.sprite = null; break;
            }
            areaImage.preserveAspect = true;
        }

        // ---------- FOTO DECORADA ----------
        string photoPath = Path.Combine(Application.temporaryCachePath, $"{pinData.pinName}_photo.jpg");

        // sempre mostra a miniatura (placeholder inicial)
        decoratedImageThumb.gameObject.SetActive(true);
        decoratedImageFullPanel.SetActive(false); // overlay inicia fechado

        Button thumbButton = decoratedImageThumb.GetComponent<Button>();
        thumbButton.onClick.RemoveAllListeners(); // limpa listeners antigos

        if (File.Exists(photoPath))
        {
            byte[] bytes = File.ReadAllBytes(photoPath);
            Texture2D tex = new Texture2D(2, 2);
            tex.LoadImage(bytes);

            // substitui placeholder pela foto real
            decoratedImageThumb.texture = tex;
            decoratedImageFull.texture = tex;

            // ativa botão e adiciona listener
            thumbButton.interactable = true;
            thumbButton.onClick.AddListener(() =>
            {
                decoratedImageFullPanel.SetActive(true);
            });
        }
        else
        {
            // restaura o placeholder original
            decoratedImageThumb.texture = placeholderDecoratedThumb;
            decoratedImageFull.texture = placeholderDecoratedThumb;

            // desativa botão, não clicável
            thumbButton.interactable = false;
        }

        // botão voltar da foto grande
        decoratedImageBackButton.onClick.RemoveAllListeners();
        decoratedImageBackButton.onClick.AddListener(() =>
        {
            decoratedImageFullPanel.SetActive(false);
        });

        // ---------- VÍDEO ----------
        UpdateVideoThumb(pinData.pinName);

        ShowStickerImages(pinData.pinName);


        // ---------- STICKERS ----------


    }











    void ShowStickerImages(string areaName)


    {

        Debug.Log("===== ShowStickerImages =====");
        Debug.Log("areaName: [" + areaName + "]");
        Debug.Log("stickersContentParent: " + stickersContentParent);
        Debug.Log("stickerImagePrefab: " + stickerImagePrefab);

        Debug.Log("Chegou antes da linha 377");


        // garante que o bloco está visível
        stickersBlock.SetActive(true);

        // limpa stickers antigos
        foreach (Transform child in stickersContentParent)
            Destroy(child.gameObject);

        Sprite[] stickerSprites = null;

        switch (areaName)
        {
            case "CursoDagua": stickerSprites = cursoDaguaStickers; break;
            case "Subosque": stickerSprites = subosqueStickers; break;
            case "Dossel": stickerSprites = dosselStickers; break;
            case "Epifitas": stickerSprites = epifitasStickers; break;
            case "Serrapilheira": stickerSprites = serrapilheiraStickers; break;
            default:
                Debug.LogWarning("Área não reconhecida: " + areaName);
                return;
        }

        if (stickerSprites == null || stickerSprites.Length == 0)
            return;

        foreach (var sprite in stickerSprites)
        {
            if (sprite == null) continue;

            // Instancia o prefab dentro do content
            GameObject newImageObj = Instantiate(stickerImagePrefab, stickersContentParent);

            // 🔹 Reset do transform para LayoutGroup funcionar
            RectTransform rt = newImageObj.GetComponent<RectTransform>();
            rt.localScale = Vector3.one;
            rt.anchoredPosition = Vector2.zero;
            rt.localRotation = Quaternion.identity;

            // Configura a imagem
            Image img = newImageObj.GetComponent<Image>();
            img.sprite = sprite;
            img.preserveAspect = true;

            // Configura o botão
            Button btn = newImageObj.GetComponent<Button>();
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => OpenSpeciesInfo(sprite));
        }


        // força o scroll recalcular tamanho
        Canvas.ForceUpdateCanvases();
    }


    public void OpenSpeciesInfo(Sprite clickedSticker)
    {
        Debug.Log("MAP: OpenSpeciesInfo chamado com sticker: " + clickedSticker?.name);

        SpeciesData found = null;
        foreach (var s in speciesList)
        {
            Debug.Log("MAP: Comparando com sticker da espécie: " + s.stickerSprite?.name);

            if (s.stickerSprite != null && clickedSticker != null &&
       s.stickerSprite.name == clickedSticker.name)

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

        Image pinImage = pinButton.GetComponent<Image>();
        if (pinImage == null || completedPinSprite == null) return;

        // Troca o sprite do pin
        pinImage.sprite = completedPinSprite;
        pinImage.preserveAspect = true;

        // ======= SALVA A PERSISTÊNCIA =======
        PlayerPrefs.SetInt("Visited_" + areaName, 1); // 1 = visitado
        PlayerPrefs.Save(); // garante que seja salvo imediatamente

        if (debugText != null)
            debugText.text += "\nSPRITE ALTERADO E SALVO COM SUCESSO";
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
        yield return new WaitForSeconds(1f);
        StartCoroutine(PlayTutorialImage());
    }


    string GetVideoFileForArea(string areaName)
    {
        switch (areaName)
        {
            case "CursoDagua":
                return "TESTE.mp4";

            case "Serrapilheira":
                return "TESTE.mp4";

            case "Epifitas":
                return "TESTE.mp4";

            case "Subosque":
                return "subosque.mp4";

            case "Dossel":
                return "dossel.mp4";

            default:
                Debug.LogError("❌ Área desconhecida recebida: [" + areaName + "]");
                return "TESTE.mp4";
        }
    }


    void UpdateVideoThumb(string areaName)
    {
        bool unlocked = VideoUnlockManager.IsUnlocked(areaName);

        // Define a imagem correta
        videoImage.sprite = unlocked ? unlockedVideoSprite : lockedVideoSprite;
        videoImage.preserveAspect = true;

        // Configura o botão
        videoButton.interactable = unlocked;
        videoButton.onClick.RemoveAllListeners();

        if (unlocked)
        {
            videoButton.onClick.AddListener(() =>
            {
                string videoFile = GetVideoFileForArea(areaName);
                mapVideoPlayer.Play(areaName, videoFile);
            });
        }
    }





}

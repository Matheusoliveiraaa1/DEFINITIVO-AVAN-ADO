using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public class StickerCatalogUI : MonoBehaviour
{


    [Header("UI Feedback")]
    public LockedStickerHintController lockedStickerHint;


    public RectTransform content;
    public GameObject stickerSlotPrefab;

    // NOVO: Lista com TODOS os sprites dos stickers possíveis
    public List<Sprite> allStickers = new List<Sprite>();

    // Lista de sprites dos stickers desbloqueados (iniciais + coletados)
    public List<Sprite> unlockedStickers = new List<Sprite>();

    // Referências
    public LocationServiceManager locationManager;
    public MapPinsController mapPinsController;
    public Sprite checkmarkSprite; // Sprite do check verde

    // Configuração do checkmark
    [Header("Checkmark Settings")]
    public Vector2 checkmarkSize = new Vector2(30f, 30f); // Tamanho do checkmark
    public Vector2 checkmarkPosition = new Vector2(35f, -35f); // Posição no canto superior direito

    private List<GameObject> slots = new List<GameObject>();
    private List<Image> checkmarkImages = new List<Image>();
    private List<GameObject> darkOverlays = new List<GameObject>(); // NOVO: Lista separada para overlays

    // NOVO: Mapa para comparação confiável de sprites
    private Dictionary<string, Sprite> stickerSpriteMap = new Dictionary<string, Sprite>();



    void Start()
    {
        Debug.Log("StickerCatalogUI START: " + GetInstanceID());





        InitializeStickerMap(); // Inicializa o mapa de sprites
        GenerateSlots();
        LoadInitialStickersFromLocationManager();
        UpdateSlots(); // Agora mostra todos os stickers

        if (locationManager != null)
        {
            locationManager.OnCollectedStickersChanged += RefreshStickersFromLocationManager;
            locationManager.OnUsedStickersChanged += RefreshCheckmarks;
        }
    }

    void OnDestroy()
    {
        if (locationManager != null)
        {
            locationManager.OnCollectedStickersChanged -= RefreshStickersFromLocationManager;
            locationManager.OnUsedStickersChanged -= RefreshCheckmarks;
        }
    }

    // NOVO: Inicializar mapa de sprites para comparação confiável
    void InitializeStickerMap()
    {
        foreach (Sprite sprite in allStickers)
        {
            if (sprite != null && !stickerSpriteMap.ContainsKey(sprite.name))
            {
                stickerSpriteMap[sprite.name] = sprite;
                Debug.Log($"MAPA: Sprite {sprite.name} adicionado ao mapa");
            }
        }
    }

    // Atualiza os checkmarks
    void RefreshCheckmarks()
    {
        UpdateCheckmarks();
    }

    // Verifica se um sticker foi usado
    bool IsStickerUsed(Sprite sticker)
    {
        if (locationManager == null || sticker == null) return false;

        // Verifica em todas as áreas se este sprite corresponde a um sticker usado
        string[] areas = { "Area1", "Area2", "CursoDagua", "Subosque", "Dossel", "Epifitas", "Serrapilheira", "AreaTeste" };
        foreach (string area in areas)
        {
            for (int i = 0; i < 6; i++)
            {
                if (locationManager.IsStickerUsed(area, i))
                {
                    Sprite usedStickerSprite = GetStickerSprite(area, i);
                    // MODIFICADO: Comparação por nome para maior confiabilidade
                    if (usedStickerSprite != null && sticker != null && usedStickerSprite.name == sticker.name)
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }

    // Atualiza a visibilidade dos checkmarks
    void UpdateCheckmarks()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            if (i < allStickers.Count && i < checkmarkImages.Count)
            {
                // Verifica se este sticker foi usado
                Sprite currentSticker = allStickers[i];
                bool isUsed = IsStickerUsed(currentSticker);

                // Ativa/desativa checkmark
                checkmarkImages[i].gameObject.SetActive(isUsed);

                // DEBUG
                if (isUsed)
                {
                    Debug.Log($"CHECKMARK: Slot {i} ({currentSticker.name}) marcado como usado");
                }
            }
        }
    }

    // Atualiza os overlays escuros (baseado no que está desbloqueado)
    void UpdateDarkOverlays()
    {
        Debug.Log($"OVERLAYS: Atualizando {slots.Count} slots, {unlockedStickers.Count} desbloqueados");

        for (int i = 0; i < slots.Count; i++)
        {
            if (i < allStickers.Count && i < darkOverlays.Count)
            {
                // Verifica se este sticker está desbloqueado
                Sprite currentSticker = allStickers[i];
                bool isUnlocked = unlockedStickers.Contains(currentSticker);

                // Se NÃO estiver desbloqueado, mostra overlay escuro
                darkOverlays[i].SetActive(!isUnlocked);

                // DEBUG
                Debug.Log($"Slot {i}: {currentSticker.name} - {(isUnlocked ? "DESBLOQUEADO" : "BLOQUEADO")}");
            }
        }
    }

    void LoadInitialStickersFromLocationManager()
    {
        if (locationManager == null) return;

        Debug.Log("CATALOG: Carregando stickers do Location Manager");

        // Para cada área, adiciona os stickers coletados
        string[] areas = { "Area1", "Area2", "CursoDagua", "Subosque", "Dossel", "Epifitas", "Serrapilheira", "AreaTeste" };

        int totalCarregados = 0;
        foreach (string area in areas)
        {
            for (int i = 0; i < 6; i++)
            {
                if (locationManager.IsStickerCollected(area, i))
                {
                    Sprite stickerSprite = GetStickerSprite(area, i);
                    if (stickerSprite != null)
                    {
                        // MODIFICADO: Usa o nome do sprite para adicionar via mapa
                        AddUnlockedStickerBySpriteName(stickerSprite.name);
                        totalCarregados++;
                    }
                }
            }
        }

        Debug.Log($"CATALOG: {totalCarregados} stickers carregados do Location Manager");
    }

    // NOVO: Método para adicionar sticker por nome (usando o mapa)
    void AddUnlockedStickerBySpriteName(string spriteName)
    {
        if (stickerSpriteMap.TryGetValue(spriteName, out Sprite sprite))
        {
            if (!unlockedStickers.Contains(sprite))
            {
                unlockedStickers.Add(sprite);
                Debug.Log($"DESBLOQUEIO: {spriteName} adicionado ao catálogo");
            }
        }
        else
        {
            Debug.LogWarning($"AVISO: Sprite {spriteName} não encontrado no mapa de sprites!");
        }
    }

    void RefreshStickersFromLocationManager()
    {
        Debug.Log("CATALOG: Refresh chamado - atualizando stickers do Location Manager");

        // Recarrega todos os stickers
        LoadInitialStickersFromLocationManager();

        // Atualiza overlays quando novos stickers são coletados
        UpdateDarkOverlays();
        UpdateCheckmarks();

        // DEBUG: Mostra todos os stickers desbloqueados
        DebugUnlockedStickers();
    }

    Sprite GetStickerSprite(string areaName, int index)
    {
        NativeCameraExample cameraExample = FindObjectOfType<NativeCameraExample>();
        if (cameraExample != null)
        {
            return cameraExample.GetStickerSprite(areaName, index);
        }
        return null;
    }

    void GenerateSlots()
    {
        // Determina quantos slots criar (baseado em allStickers ou 24)
        int slotCount = Mathf.Max(allStickers.Count, 24);

        for (int i = 0; i < slotCount; i++)
        {
            GameObject slot = Instantiate(stickerSlotPrefab, content);
            slots.Add(slot);

            // Adicionar handler de clique
            AddStickerClickHandler(slot, i);

            // Criar overlay escuro e checkmark
            CreateDarkOverlayForSlot(slot, i);
            CreateCheckmarkForSlot(slot, i);
        }
    }

    void AddStickerClickHandler(GameObject slot, int index)
    {
        Image stickerImage = slot.transform.GetChild(0).GetComponent<Image>();
        if (stickerImage != null)
        {
            Button button = stickerImage.GetComponent<Button>();
            if (button == null)
            {
                button = stickerImage.gameObject.AddComponent<Button>();
            }

            int currentIndex = index;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => OnStickerClicked(currentIndex));
        }
    }

    void OnStickerClicked(int slotIndex)
    {
        if (slotIndex < allStickers.Count)
        {
            Sprite clickedSticker = allStickers[slotIndex];

            // Só permite clique se o sticker estiver desbloqueado
            if (unlockedStickers.Contains(clickedSticker))
            {
                if (mapPinsController != null)
                {
                    mapPinsController.OpenSpeciesInfo(clickedSticker);
                }
            }
            else
            {
                Debug.Log("Sticker ainda não desbloqueado!");

                if (lockedStickerHint != null)
                {
                    lockedStickerHint.ShowOrExtend();
                }
            }

        }
    }

    // NOVO: Cria apenas o overlay escuro (sem checkmark)
    void CreateDarkOverlayForSlot(GameObject slot, int index)
    {
        GameObject darkOverlayObj = new GameObject("DarkOverlay");
        darkOverlayObj.transform.SetParent(slot.transform, false);

        Image darkOverlayImage = darkOverlayObj.AddComponent<Image>();
        darkOverlayImage.color = new Color(0f, 0f, 0f, 0.85f);

        // 🚨 ISSO É O MAIS IMPORTANTE
        darkOverlayImage.raycastTarget = false;

        RectTransform darkRect = darkOverlayObj.GetComponent<RectTransform>();
        darkRect.anchorMin = Vector2.zero;
        darkRect.anchorMax = Vector2.one;
        darkRect.offsetMin = Vector2.zero;
        darkRect.offsetMax = Vector2.zero;
        darkRect.localScale = Vector3.one;

        darkOverlayObj.transform.SetSiblingIndex(1);
        darkOverlayObj.SetActive(true);

        if (index >= darkOverlays.Count)
            darkOverlays.Add(darkOverlayObj);
        else
            darkOverlays[index] = darkOverlayObj;
    }


    // MODIFICADO: Checkmark pequeno no canto
    void CreateCheckmarkForSlot(GameObject slot, int index)
    {
        GameObject checkmarkObj = new GameObject("Checkmark");
        checkmarkObj.transform.SetParent(slot.transform, false);

        Image checkmarkImage = checkmarkObj.AddComponent<Image>();
        checkmarkImage.sprite = checkmarkSprite;
        checkmarkImage.color = Color.green; // Verde sólido

        RectTransform rectTransform = checkmarkObj.GetComponent<RectTransform>();

        // Configura como pequeno no canto superior direito
        rectTransform.sizeDelta = new Vector2(55f, 55f);      // MAIOR E VISÍVEL
        rectTransform.anchorMin = new Vector2(1f, 1f);
        rectTransform.anchorMax = new Vector2(1f, 1f);
        rectTransform.pivot = new Vector2(1f, 1f);
        rectTransform.anchoredPosition = new Vector2(-10f, -10f);  // POSIÇÃO CORRETA NO CANTO

        // Coloca por cima de tudo
        checkmarkObj.transform.SetAsLastSibling();

        // Começa desativado
        checkmarkObj.SetActive(false);

        // Guarda referência
        if (index >= checkmarkImages.Count)
        {
            checkmarkImages.Add(checkmarkImage);
        }
        else
        {
            checkmarkImages[index] = checkmarkImage;
        }
    }

    public void UpdateSlots()
    {
        Debug.Log($"CATALOG: UpdateSlots chamado. Total de allStickers = {allStickers.Count}, unlocked = {unlockedStickers.Count}");

        for (int i = 0; i < slots.Count; i++)
        {
            Image stickerImg = slots[i].transform.GetChild(0).GetComponent<Image>();

            if (i < allStickers.Count)
            {
                // Sempre mostra o sprite (mesmo que escuro)
                stickerImg.sprite = allStickers[i];
                Debug.Log($"CATALOG: Slot {i} atualizado com sprite: {allStickers[i].name}");
                stickerImg.gameObject.SetActive(true);
            }
            else
            {
                // Slot vazio se não houver sprite
                stickerImg.gameObject.SetActive(false);
            }
        }

        // Atualiza overlays e checkmarks
        UpdateDarkOverlays();
        UpdateCheckmarks();
    }

    public void AddUnlockedSticker(Sprite newSticker)
    {
        if (newSticker != null && !unlockedStickers.Contains(newSticker))
        {
            unlockedStickers.Add(newSticker);
            Debug.Log($"MANUAL: Sticker {newSticker.name} adicionado ao catálogo");
            UpdateDarkOverlays(); // Atualiza apenas os overlays
        }
    }

    public void AddUnlockedSticker(string areaName, int stickerIndex)
    {
        Sprite stickerSprite = GetStickerSprite(areaName, stickerIndex);
        if (stickerSprite != null)
        {
            // MODIFICADO: Usa o nome do sprite para adicionar via mapa
            AddUnlockedStickerBySpriteName(stickerSprite.name);
        }
    }

    // NOVO: Método para obter se um sticker está desbloqueado
    public bool IsStickerUnlocked(Sprite sticker)
    {
        return unlockedStickers.Contains(sticker);
    }

    // NOVO: Método para obter se um sticker foi usado
    public bool IsStickerMarkedAsUsed(Sprite sticker)
    {
        return IsStickerUsed(sticker);
    }

    // NOVO: Método para debug - mostrar todos os stickers desbloqueados
    public void DebugUnlockedStickers()
    {
        Debug.Log("=== DEBUG: STICKERS DESBLOQUEADOS ===");
        foreach (Sprite sprite in unlockedStickers)
        {
            if (sprite != null)
            {
                Debug.Log($"- {sprite.name}");
            }
        }
        Debug.Log($"Total: {unlockedStickers.Count} stickers");
        Debug.Log("====================================");
    }
}
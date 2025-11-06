using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class StickerCatalogUI : MonoBehaviour
{
    public RectTransform content;
    public GameObject stickerSlotPrefab;

    // Lista de sprites dos stickers desbloqueados (incluindo os manuais do Inspector)
    public List<Sprite> unlockedStickers = new List<Sprite>();

    // NOVO: Referência para o LocationServiceManager
    public LocationServiceManager locationManager;

    // NOVO: Sprite do check verde
    public Sprite checkmarkSprite;

    private List<GameObject> slots = new List<GameObject>();
    private List<Image> checkmarkImages = new List<Image>(); // NOVO: Referências aos checks

    void Start()
    {
        GenerateSlots();
        LoadInitialStickersFromLocationManager();
        UpdateSlots();

        if (locationManager != null)
        {
            locationManager.OnCollectedStickersChanged += RefreshStickersFromLocationManager;
            locationManager.OnUsedStickersChanged += RefreshCheckmarks; // NOVO: Atualizar checks quando stickers são usados
        }
    }

    void OnDestroy()
    {
        if (locationManager != null)
        {
            locationManager.OnCollectedStickersChanged -= RefreshStickersFromLocationManager;
            locationManager.OnUsedStickersChanged -= RefreshCheckmarks; // NOVO
        }
    }

    // NOVO: Método para atualizar os checkmarks
    void RefreshCheckmarks()
    {
        UpdateCheckmarks();
    }

    // NOVO: Verifica se um sticker foi usado
    bool IsStickerUsed(Sprite sticker)
    {
        if (locationManager == null) return false;

        // Verifica em todas as áreas se este sprite corresponde a um sticker usado
        string[] areas = { "Area1", "Area2", "CursoDagua", "Subosque", "Dossel", "Epifitas", "Serrapilheira", "AreaTeste" };

        foreach (string area in areas)
        {
            for (int i = 0; i < 6; i++)
            {
                if (locationManager.IsStickerUsed(area, i))
                {
                    Sprite usedStickerSprite = GetStickerSprite(area, i);
                    if (usedStickerSprite == sticker)
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    // NOVO: Atualiza a visibilidade dos checkmarks
    void UpdateCheckmarks()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            if (i < unlockedStickers.Count && i < checkmarkImages.Count)
            {
                bool isUsed = IsStickerUsed(unlockedStickers[i]);
                var checkImage = checkmarkImages[i];
                var overlayLink = checkImage.GetComponent<LinkedOverlay>();

                checkImage.gameObject.SetActive(isUsed);
                if (overlayLink != null && overlayLink.overlay != null)
                    overlayLink.overlay.SetActive(isUsed);
            }
        }
    }


    void LoadInitialStickersFromLocationManager()
    {
        if (locationManager == null) return;

        // Para cada área, adiciona os stickers coletados
        string[] areas = { "Area1", "Area2", "CursoDagua", "Subosque", "Dossel", "Epifitas", "Serrapilheira", "AreaTeste" };

        foreach (string area in areas)
        {
            for (int i = 0; i < 6; i++)
            {
                if (locationManager.IsStickerCollected(area, i))
                {
                    Sprite stickerSprite = GetStickerSprite(area, i);
                    if (stickerSprite != null && !unlockedStickers.Contains(stickerSprite))
                    {
                        unlockedStickers.Add(stickerSprite);
                    }
                }
            }
        }
    }

    void RefreshStickersFromLocationManager()
    {
        LoadInitialStickersFromLocationManager();
        UpdateSlots();
        UpdateCheckmarks(); // NOVO: Atualizar checks também
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
        for (int i = 0; i < 24; i++)
        {
            GameObject slot = Instantiate(stickerSlotPrefab, content);
            slots.Add(slot);

            // NOVO: Criar checkmark para este slot
            CreateCheckmarkForSlot(slot, i);
        }
    }

    // NOVO: Cria o checkmark para um slot - MODIFICADO PARA COBRIR TODO O SLOT
    // NOVO: Cria o checkmark para um slot
    // NOVO: Cria o overlay escuro e o checkmark por cima
    void CreateCheckmarkForSlot(GameObject slot, int index)
    {
        // === DARK OVERLAY ===
        GameObject darkOverlayObj = new GameObject("DarkOverlay");
        darkOverlayObj.transform.SetParent(slot.transform, false);

        Image darkOverlayImage = darkOverlayObj.AddComponent<Image>();
        darkOverlayImage.color = new Color(0f, 0f, 0f, 0.5f); // preto semi-transparente

        RectTransform darkRect = darkOverlayObj.GetComponent<RectTransform>();
        darkRect.anchorMin = Vector2.zero;
        darkRect.anchorMax = Vector2.one;
        darkRect.offsetMin = Vector2.zero;
        darkRect.offsetMax = Vector2.zero;
        darkRect.localScale = Vector3.one;

        // === CHECKMARK ===
        GameObject checkmarkObj = new GameObject("Checkmark");
        checkmarkObj.transform.SetParent(slot.transform, false);

        Image checkmarkImage = checkmarkObj.AddComponent<Image>();
        checkmarkImage.sprite = checkmarkSprite;
        checkmarkImage.color = new Color(0f, 1f, 0f, 0.4f); // verde semi-transparente

        RectTransform rectTransform = checkmarkObj.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        rectTransform.localScale = Vector3.one;

        // Ordem de renderização:
        // Sticker (base)
        // DarkOverlay (escurece)
        // Checkmark (por cima de tudo)
        darkOverlayObj.transform.SetSiblingIndex(slot.transform.childCount - 1);
        checkmarkObj.transform.SetAsLastSibling();

        // Ambos começam desativados
        darkOverlayObj.SetActive(false);
        checkmarkObj.SetActive(false);

        // Guardar referência para o check
        if (index >= checkmarkImages.Count)
        {
            checkmarkImages.Add(checkmarkImage);
        }
        else
        {
            checkmarkImages[index] = checkmarkImage;
        }

        // 💡 Vincular overlay ao check via tag interna (assim ativamos ambos juntos)
        checkmarkImage.gameObject.AddComponent<LinkedOverlay>().overlay = darkOverlayObj;
    }


    public void UpdateSlots()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            Image stickerImg = slots[i].transform.GetChild(0).GetComponent<Image>();

            if (i < unlockedStickers.Count)
            {
                stickerImg.sprite = unlockedStickers[i];
                stickerImg.gameObject.SetActive(true);
            }
            else
            {
                stickerImg.gameObject.SetActive(false);
            }
        }

        UpdateCheckmarks(); // NOVO: Atualizar checks quando slots são atualizados
    }

    public void AddUnlockedSticker(Sprite newSticker)
    {
        if (!unlockedStickers.Contains(newSticker))
        {
            unlockedStickers.Add(newSticker);
            UpdateSlots();
        }
    }

    public void AddUnlockedSticker(string areaName, int stickerIndex)
    {
        Sprite stickerSprite = GetStickerSprite(areaName, stickerIndex);
        if (stickerSprite != null)
        {
            AddUnlockedSticker(stickerSprite);
        }
    }

    // Classe auxiliar para ligar o checkmark ao overlay escuro
    public class LinkedOverlay : MonoBehaviour
    {
        public GameObject overlay;
    }





}
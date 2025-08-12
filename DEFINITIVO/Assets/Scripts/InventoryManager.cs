using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class InventoryManager : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject inventoryPanel;
    public TextMeshProUGUI[] areaProgressTexts;

    [Header("Sticker Icons")]
    public Image[] area1Icons;
    public Image[] cursoDaguaIcons;
    public Image[] subosqueIcons;
    public Image[] dosselIcons;
    public Image[] epifitasIcons;
    public Image[] serrapilheiraIcons;

    private LocationServiceManager locationManager;

    private void Awake()
    {
        locationManager = FindObjectOfType<LocationServiceManager>();
    }

    private void OnEnable()
    {
        // (re)obtem e subscreve o evento para atualizações em runtime
        locationManager = locationManager ?? FindObjectOfType<LocationServiceManager>();
        if (locationManager != null)
        {
            locationManager.OnCollectedStickersChanged += UpdateInventoryUI;
        }
    }

    private void OnDisable()
    {
        if (locationManager != null)
        {
            locationManager.OnCollectedStickersChanged -= UpdateInventoryUI;
        }
    }

    private void Start()
    {
        inventoryPanel.SetActive(false);

        // Atualiza UI ao iniciar (Load feito no Awake do LocationServiceManager)
        UpdateInventoryUI();
    }

    public void ToggleInventory()
    {
        inventoryPanel.SetActive(!inventoryPanel.activeSelf);
        if (inventoryPanel.activeSelf)
        {
            UpdateInventoryUI();
        }
    }

    public void UpdateInventoryUI()
    {
        // Garante referência caso não tenha sido setada por Awake
        locationManager = locationManager ?? FindObjectOfType<LocationServiceManager>();

        UpdateAreaProgress("Area1", 0, area1Icons);
        UpdateAreaProgress("CursoDagua", 1, cursoDaguaIcons);
        UpdateAreaProgress("Subosque", 2, subosqueIcons);
        UpdateAreaProgress("Dossel", 3, dosselIcons);
        UpdateAreaProgress("Epifitas", 4, epifitasIcons);
        UpdateAreaProgress("Serrapilheira", 5, serrapilheiraIcons);
    }

    private void UpdateAreaProgress(string areaName, int textIndex, Image[] stickerIcons)
    {
        if (locationManager == null || textIndex >= areaProgressTexts.Length) return;

        int collectedCount = locationManager.GetCollectedStickerCount(areaName);

        // Atualiza ícones: mapeando índices 3..5 para posições 0..2 no array de icons
        for (int i = 3; i <= 5; i++)
        {
            if (stickerIcons != null && (i - 3) < stickerIcons.Length)
            {
                bool active = locationManager.IsStickerCollected(areaName, i);
                stickerIcons[i - 3].gameObject.SetActive(active);
            }
        }

        areaProgressTexts[textIndex].text = $"{collectedCount} de 3 coletados";
    }
}

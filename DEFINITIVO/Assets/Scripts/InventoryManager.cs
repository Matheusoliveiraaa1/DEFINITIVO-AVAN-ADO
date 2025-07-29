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

    private void Start()
    {
        locationManager = FindObjectOfType<LocationServiceManager>();
        inventoryPanel.SetActive(false);
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

        // Atualiza ícones
        for (int i = 3; i <= 5; i++)
        {
            if (stickerIcons != null && (i - 3) < stickerIcons.Length)
            {
                stickerIcons[i - 3].gameObject.SetActive(locationManager.IsStickerCollected(areaName, i));
            }
        }

        areaProgressTexts[textIndex].text = $"{collectedCount} de 3 coletados";
    }
}
using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Linq;

public class LocationServiceManager : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI latitudeText;
    public TextMeshProUGUI longitudeText;
    public TextMeshProUGUI accuracyText;
    public TextMeshProUGUI messageText;
    public GameObject cameraButton;
    public Image stickerNotificationImage;

    [Header("Settings")]
    public bool areaTeste = false;
    public float detectionRadius = 7f;
    public float notificationDuration = 3f;

    [Header("Points of Interest")]
    public List<AreaPoint> areaPoints = new List<AreaPoint>();
    public List<StickerPoint> stickerPoints = new List<StickerPoint>();

    [Header("Inventory")]
    public InventoryManager inventoryManager;

    [System.Serializable]
    public class AreaPoint
    {
        public double latitude;
        public double longitude;
        public string message;
        public string areaName;
    }

    [System.Serializable]
    public class StickerPoint
    {
        public double latitude;
        public double longitude;
        public string message;
        public string areaName;
        public int stickerIndex;
    }

    private Dictionary<string, List<int>> collectedStickers = new Dictionary<string, List<int>>();
    private List<PointOfInterest> allPoints = new List<PointOfInterest>();

    private class PointOfInterest
    {
        public double latitude;
        public double longitude;
        public string message;
        public bool isStickerPoint;
        public string areaName;
        public int stickerIndex;
        public bool alreadyTriggered;

        public PointOfInterest(double lat, double lon, string msg, bool isSticker, string area, int index = -1)
        {
            latitude = lat;
            longitude = lon;
            message = msg;
            isStickerPoint = isSticker;
            areaName = area;
            stickerIndex = index;
            alreadyTriggered = false;
        }
    }

    private void Start()
    {
        InitializePoints();
        cameraButton.SetActive(false);
        if (stickerNotificationImage != null)
            stickerNotificationImage.gameObject.SetActive(false);
        StartCoroutine(StartLocationService());
    }

    private void InitializePoints()
    {
        foreach (var ap in areaPoints)
        {
            allPoints.Add(new PointOfInterest(
                ap.latitude, ap.longitude, ap.message, false, ap.areaName));
        }

        foreach (var sp in stickerPoints)
        {
            allPoints.Add(new PointOfInterest(
                sp.latitude, sp.longitude, sp.message, true, sp.areaName, sp.stickerIndex));
        }
    }

    private IEnumerator StartLocationService()
    {
        if (!Input.location.isEnabledByUser)
        {
            messageText.text = "Localização desativada.";
            yield break;
        }

        Input.location.Start(1f, 0.1f);

        int maxWait = 20;
        while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
        {
            yield return new WaitForSeconds(1);
            maxWait--;
        }

        if (maxWait <= 0 || Input.location.status == LocationServiceStatus.Failed)
        {
            messageText.text = maxWait <= 0 ?
                "Tempo limite ao iniciar serviço." : "Falha ao obter localização.";
            yield break;
        }

        InvokeRepeating(nameof(UpdateLocation), 0f, 0.5f);
    }

    private void UpdateLocation()
    {
        if (Input.location.status != LocationServiceStatus.Running)
        {
            messageText.text = "Serviço parado.";
            return;
        }

        if (areaTeste)
        {
            HandleTestArea();
            return;
        }

        var data = Input.location.lastData;
        UpdateLocationUI(data);
        CheckNearbyPoints(data);
    }

    private void HandleTestArea()
    {
        cameraButton.SetActive(true);
        messageText.text = "Modo teste ativado.";
    }

    private void UpdateLocationUI(LocationInfo data)
    {
        latitudeText.text = $"Latitude: {data.latitude:F6}";
        longitudeText.text = $"Longitude: {data.longitude:F6}";
        accuracyText.text = $"Precisão: {data.horizontalAccuracy:F1} m";
    }

    private void CheckNearbyPoints(LocationInfo data)
    {
        bool isInsideAnyArea = false;

        foreach (var poi in allPoints)
        {
            double distance = CalculateDistance(data.latitude, data.longitude, poi.latitude, poi.longitude);

            if (distance <= detectionRadius)
            {
                HandlePointTrigger(poi, ref isInsideAnyArea);
                break;
            }
            else
            {
                // Reseta o alreadyTriggered apenas para AreaPoints quando sai do raio
                if (!poi.isStickerPoint)
                {
                    poi.alreadyTriggered = false;
                }
            }
        }

        cameraButton.SetActive(isInsideAnyArea);
        if (!isInsideAnyArea && !string.IsNullOrEmpty(messageText.text))
        {
            StartCoroutine(HideNotificationAfterDelay(2f));
        }
    }

    private void HandlePointTrigger(PointOfInterest poi, ref bool isInsideAnyArea)
    {
        if (poi.isStickerPoint)
        {
            HandleStickerPoint(poi);
        }
        else
        {
            isInsideAnyArea = true;
            HandleAreaPoint(poi);
        }
    }

    private void HandleStickerPoint(PointOfInterest poi)
    {
        if (!poi.alreadyTriggered)
        {
            poi.alreadyTriggered = true;
            RegisterStickerCollection(poi);
            ShowStickerNotification(poi);
        }
        else
        {
            // Apenas registra novamente sem mostrar notificação
            RegisterStickerCollection(poi);
        }
    }

    private void ShowStickerNotification(PointOfInterest poi)
    {
        messageText.text = poi.message;
        Handheld.Vibrate();

        NativeCameraExample cameraExample = FindObjectOfType<NativeCameraExample>();
        if (cameraExample != null && stickerNotificationImage != null)
        {
            Sprite stickerSprite = cameraExample.GetStickerSprite(poi.areaName, poi.stickerIndex);
            if (stickerSprite != null)
            {
                stickerNotificationImage.sprite = stickerSprite;
                stickerNotificationImage.gameObject.SetActive(true);
                StartCoroutine(ScaleAnimation(true)); // Animação de entrada
            }
        }

        StartCoroutine(HideNotificationAfterDelay(notificationDuration));
    }

    private IEnumerator ScaleAnimation(bool isEntering)
    {
        float startScale = isEntering ? 0.1f : 1f;
        float endScale = isEntering ? 1f : 0.1f;
        float duration = 0.5f;
        float elapsedTime = 0f;

        stickerNotificationImage.transform.localScale = Vector3.one * startScale;

        while (elapsedTime < duration)
        {
            float scale = Mathf.Lerp(startScale, endScale, elapsedTime / duration);
            stickerNotificationImage.transform.localScale = Vector3.one * scale;
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        stickerNotificationImage.transform.localScale = Vector3.one * endScale;
    }

    private IEnumerator HideNotificationAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        messageText.text = "";

        if (stickerNotificationImage != null && stickerNotificationImage.gameObject.activeSelf)
        {
            yield return StartCoroutine(ScaleAnimation(false)); // Animação de saída
            stickerNotificationImage.gameObject.SetActive(false);
        }
    }

    private void HandleAreaPoint(PointOfInterest poi)
    {
        if (!poi.alreadyTriggered)
        {
            poi.alreadyTriggered = true;
            messageText.text = poi.message;
            Handheld.Vibrate();
            FindAnyObjectByType<NativeCameraExample>().currentArea = poi.areaName;
        }
    }

    private void RegisterStickerCollection(PointOfInterest poi)
    {
        if (!collectedStickers.ContainsKey(poi.areaName))
        {
            collectedStickers[poi.areaName] = new List<int>();
        }

        if (!collectedStickers[poi.areaName].Contains(poi.stickerIndex))
        {
            collectedStickers[poi.areaName].Add(poi.stickerIndex);
            if (inventoryManager != null)
            {
                inventoryManager.UpdateInventoryUI();
            }
            else
            {
                inventoryManager = FindObjectOfType<InventoryManager>();
                inventoryManager?.UpdateInventoryUI();
            }
        }
    }

    public bool IsStickerCollected(string areaName, int index)
    {
        return collectedStickers.ContainsKey(areaName) && collectedStickers[areaName].Contains(index);
    }

    private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371000;
        double dLat = DegToRad(lat2 - lat1);
        double dLon = DegToRad(lon2 - lon1);

        double a = System.Math.Sin(dLat / 2) * System.Math.Sin(dLat / 2) +
                  System.Math.Cos(DegToRad(lat1)) * System.Math.Cos(DegToRad(lat2)) *
                  System.Math.Sin(dLon / 2) * System.Math.Sin(dLon / 2);

        double c = 2 * System.Math.Atan2(System.Math.Sqrt(a), System.Math.Sqrt(1 - a));
        return R * c;
    }

    private double DegToRad(double deg) => deg * (System.Math.PI / 180);

    public int GetCollectedStickerCount(string areaName)
    {
        if (collectedStickers.ContainsKey(areaName))
        {
            int count = 0;
            foreach (int index in collectedStickers[areaName])
            {
                if (index >= 3 && index <= 5) count++;
            }
            return count;
        }
        return 0;
    }

    private void OnDisable()
    {
        Input.location.Stop();
        CancelInvoke();
    }
}
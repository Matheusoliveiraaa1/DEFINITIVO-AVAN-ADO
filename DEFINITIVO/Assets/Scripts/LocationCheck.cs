using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Linq;
using System;
using UnityEngine.Android;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class LocationServiceManager : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI latitudeText;
    public TextMeshProUGUI longitudeText;
    public TextMeshProUGUI accuracyText;
    public TextMeshProUGUI messageText;
    public GameObject cameraButton;
    public Image stickerNotificationImage;

    private string currentAreaName = null;

    [Header("Settings")]
    public bool areaTeste = false;
    public float detectionRadius = 7f;
    public float entryDetectionRadius = 25f;
    public float notificationDuration = 3f;

    [Header("GPS Dynamic Radius")]
    [Range(0.6f, 1.2f)]
    public float accuracyMultiplier = 0.8f;
    public float maxGpsRadius = 50f;

    [Header("Points of Interest")]
    public List<AreaPoint> areaPoints = new List<AreaPoint>();
    public List<StickerPoint> stickerPoints = new List<StickerPoint>();

    [Header("Park Entries")]
    public ParkEntryPoint entry1;
    public ParkEntryPoint entry2;

    private ParkStartMode currentStartMode = ParkStartMode.None;
    private const string START_MODE_KEY = "ParkStartMode";
    private const string VISITED_AREAS_KEY = "VisitedAreas";

    [Header("Inventory")]
    public InventoryManager inventoryManager;

    public event Action OnCollectedStickersChanged;
    public event Action OnUsedStickersChanged;



    [Header("GPS Panel")]
    public GameObject gpsDisabledPanel;


    [System.Serializable]
    public class AreaPoint
    {
        public double latitude;
        public double longitude;
        public string message;
        public string areaName;

        [Header("Detection")]
        public float detectionRadius = 12f;
    }

    [System.Serializable]
    public class ParkEntryPoint
    {
        public string entryName;
        public double latitude;
        public double longitude;
    }

    [Serializable]
    private class StringListWrapper
    {
        public List<string> list;
    }

    public enum ParkStartMode
    {
        None,
        Entry1,
        Entry2
    }

    public enum StickerEntryMode
    {
        Entry1,
        Entry2
    }

    [System.Serializable]
    public class StickerPoint
    {
        public double latitude;
        public double longitude;
        public string message;
        public string areaName;
        public int stickerIndex;
        public StickerEntryMode entryMode;

        [Header("Detection")]
        public float detectionRadius = 20f;
    }

    [NonSerialized]
    private Dictionary<string, List<int>> collectedStickers = new Dictionary<string, List<int>>();
    [NonSerialized]
    private Dictionary<string, List<int>> usedStickers = new Dictionary<string, List<int>>();

    private HashSet<string> visitedAreas = new HashSet<string>();
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
        public StickerEntryMode entryMode;
        public float baseDetectionRadius;

        public PointOfInterest(
            double lat,
            double lon,
            string msg,
            bool isSticker,
            string area,
            float radius,
            int index = -1,
            StickerEntryMode mode = StickerEntryMode.Entry1)
        {
            latitude = lat;
            longitude = lon;
            message = msg;
            isStickerPoint = isSticker;
            areaName = area;
            stickerIndex = index;
            entryMode = mode;
            alreadyTriggered = false;
            baseDetectionRadius = radius;
        }
    }

    [Serializable]
    public class StickerSaveData
    {
        public List<AreaStickerData> areas = new List<AreaStickerData>();
    }

    [Serializable]
    public class AreaStickerData
    {
        public string areaName;
        public List<int> stickerIndices = new List<int>();
    }

    [Serializable]
    public class UsedStickerSaveData
    {
        public List<AreaStickerData> areas = new List<AreaStickerData>();
    }

    private void Awake()
    {
        Debug.Log("LocationServiceManager AWAKE: " + GetInstanceID());

        if (!Application.isPlaying) return;
        LoadStartMode();
        LoadCollectedStickers();
        LoadUsedStickers();
        LoadVisitedAreas();
    }

    private void Start()
    {
        if (!Application.isPlaying) return;
        InitializePoints();
        cameraButton.SetActive(false);
        if (stickerNotificationImage != null) stickerNotificationImage.gameObject.SetActive(false);
        inventoryManager = inventoryManager ?? FindObjectOfType<InventoryManager>();
        inventoryManager?.UpdateInventoryUI();
        StartCoroutine(StartLocationService());
    }

    private void LoadStartMode()
    {
        if (PlayerPrefs.HasKey(START_MODE_KEY))
        {
            currentStartMode = (ParkStartMode)PlayerPrefs.GetInt(START_MODE_KEY);
        }
    }

    private void SetStartMode(ParkStartMode mode)
    {
        if (currentStartMode != ParkStartMode.None) return;
        currentStartMode = mode;
        PlayerPrefs.SetInt(START_MODE_KEY, (int)mode);
        PlayerPrefs.Save();
    }

    private void DetectStartEntry(LocationInfo data)
    {
        if (currentStartMode != ParkStartMode.None) return;
        double distEntry1 = CalculateDistance(data.latitude, data.longitude, entry1.latitude, entry1.longitude);
        double distEntry2 = CalculateDistance(data.latitude, data.longitude, entry2.latitude, entry2.longitude);

        if (distEntry1 <= entryDetectionRadius)
            SetStartMode(ParkStartMode.Entry1);
        else if (distEntry2 <= entryDetectionRadius)
            SetStartMode(ParkStartMode.Entry2);
    }

    private void InitializePoints()
    {
        allPoints.Clear();
        foreach (var ap in areaPoints)
            allPoints.Add(new PointOfInterest(ap.latitude, ap.longitude, ap.message, false, ap.areaName, ap.detectionRadius));

        foreach (var sp in stickerPoints)
            allPoints.Add(new PointOfInterest(sp.latitude, sp.longitude, sp.message, true, sp.areaName, sp.detectionRadius, sp.stickerIndex, sp.entryMode));
    }

    private IEnumerator StartLocationService()
    {
        yield return new WaitForSeconds(1.5f);

        // Aguarda permissões
        while (!Permission.HasUserAuthorizedPermission(Permission.FineLocation) ||
               !Permission.HasUserAuthorizedPermission(Permission.CoarseLocation))
        {
            messageText.text = "Aguardando permissão de localização...";
            yield return new WaitForSeconds(0.5f);
        }

        // 🔁 Aguarda o usuário ligar o GPS
        while (!Input.location.isEnabledByUser)
        {
            messageText.text = "Ative o GPS para continuar.";
            ShowGPSPanel();
            yield return new WaitForSeconds(1f);
        }

        // GPS ligado → esconde painel
        HideGPSPanel();


        Input.location.Start(1f, 0.1f);

        int maxWait = 20;
        while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
        {
            messageText.text = "Inicializando GPS...";
            yield return new WaitForSeconds(1);
            maxWait--;
        }

        if (Input.location.status == LocationServiceStatus.Failed)
        {
            messageText.text = "Falha ao obter localização.";
            yield break;
        }

        messageText.text = "GPS ativo.";

        InvokeRepeating(nameof(UpdateLocation), 0f, 0.5f);
    }



    private void UpdateLocation()
    {

        // 🔁 Recupera se o usuário desligar o GPS durante o uso
        if (!Input.location.isEnabledByUser && Input.location.status == LocationServiceStatus.Running)
        {
            messageText.text = "GPS desligado. Aguardando reativação...";
            ShowGPSPanel();

            Input.location.Stop();
            CancelInvoke();
            StopAllCoroutines();
            StartCoroutine(StartLocationService());
            return;
        }


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

    private bool IsStickerAllowedForCurrentMode(PointOfInterest poi)
    {
        if (currentStartMode == ParkStartMode.Entry1 && poi.entryMode == StickerEntryMode.Entry1) return true;
        if (currentStartMode == ParkStartMode.Entry2 && poi.entryMode == StickerEntryMode.Entry2) return true;
        return false;
    }

    private void CheckNearbyPoints(LocationInfo data)
    {
        DetectStartEntry(data);
        if (currentStartMode == ParkStartMode.None) return;

        bool isInsideAnyArea = false;
        PointOfInterest activeArea = null;

        foreach (var poi in allPoints)
        {
            double distance = CalculateDistance(data.latitude, data.longitude, poi.latitude, poi.longitude);
            float effectiveRadius = GetEffectiveRadius(poi, data);

            if (distance <= effectiveRadius)
            {
                if (poi.isStickerPoint && !IsStickerAllowedForCurrentMode(poi))
                    continue;

                // 🔒 BLOQUEIO CursoDagua
                if (!poi.isStickerPoint && poi.areaName == "CursoDagua" && !CanActivateCursoDagua())
                {
                    messageText.text = "Visite Serrapilheira, Subosque ou Dossel antes de acessar o Curso D'Água.";
                    cameraButton.SetActive(false);
                    return; // SAI completamente do método
                }

                // ✅ Só agora considera que está dentro
                isInsideAnyArea = true;
                activeArea = poi;

                HandlePointTrigger(poi, ref isInsideAnyArea);
                break;
            }


        }

        if (isInsideAnyArea && activeArea != null && !activeArea.isStickerPoint)
            cameraButton.SetActive(!IsAreaCompleted(activeArea.areaName));
        else
        {
            cameraButton.SetActive(false);
            if (!string.IsNullOrEmpty(messageText.text)) StartCoroutine(HideNotificationAfterDelay(1.5f));
        }

        foreach (var poi in allPoints)
        {
            double distance = CalculateDistance(data.latitude, data.longitude, poi.latitude, poi.longitude);
            float resetRadius = GetEffectiveRadius(poi, data) + 5f;

            if (distance > resetRadius)
                poi.alreadyTriggered = false;
        }
    }

    private void HandlePointTrigger(PointOfInterest poi, ref bool isInsideAnyArea)
    {
        if (poi.isStickerPoint)
            HandleStickerPoint(poi);
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
            RegisterStickerCollection(poi);
    }

    private void ShowStickerNotification(PointOfInterest poi)
    {
        messageText.text = poi.message;
        Handheld.Vibrate();
        NativeCameraExample cameraExample = FindObjectOfType<NativeCameraExample>();
        if (cameraExample != null)
        {
            Sprite stickerSprite = cameraExample.GetStickerSprite(poi.areaName, poi.stickerIndex);
            if (stickerSprite != null)
                PhotoAreaOverlay.ShowSticker(stickerSprite);
        }
    }

    private IEnumerator ScaleAnimation(bool isEntering)
    {
        float startScale = isEntering ? 0.1f : 1f;
        float endScale = isEntering ? 1f : 0.1f;
        float duration = 0.5f;
        float elapsedTime = 0f;

        if (stickerNotificationImage == null) yield break;
        stickerNotificationImage.transform.localScale = Vector3.one * startScale;
        while (elapsedTime < duration)
        {
            stickerNotificationImage.transform.localScale = Vector3.one * Mathf.Lerp(startScale, endScale, elapsedTime / duration);
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
            yield return StartCoroutine(ScaleAnimation(false));
            stickerNotificationImage.gameObject.SetActive(false);
        }
    }

    private void HandleAreaPoint(PointOfInterest poi)
    {
        if (!poi.alreadyTriggered)
        {
            poi.alreadyTriggered = true;

            if (!visitedAreas.Contains(poi.areaName))
            {
                visitedAreas.Add(poi.areaName);
                SaveVisitedAreas();
            }

            messageText.text = poi.message;
            Handheld.Vibrate();

            FindAnyObjectByType<NativeCameraExample>().currentArea = poi.areaName;

            currentAreaName = poi.areaName; // ← Salva a área atual

            PhotoAreaOverlay.Show();
        }

        cameraButton.SetActive(!IsAreaCompleted(poi.areaName));
    }

    // Método público para o overlay acessar a área atual
    public string GetCurrentAreaName()
    {
        return currentAreaName;
    }

    private void RegisterUsedSticker(string areaName, int index)
    {
        if (index < 0) return;
        if (!usedStickers.ContainsKey(areaName)) usedStickers[areaName] = new List<int>();
        if (!usedStickers[areaName].Contains(index))
        {
            usedStickers[areaName].Add(index);
            SaveUsedStickers();
            OnUsedStickersChanged?.Invoke();
        }
    }

    public void MarkStickerAsUsed(string areaName, int index)
    {
        RegisterUsedSticker(areaName, index);
        if (GetUsedStickerCount(areaName) >= 6)
            OnUsedStickersChanged?.Invoke();
    }

    public bool IsStickerUsed(string areaName, int index) => usedStickers.ContainsKey(areaName) && usedStickers[areaName].Contains(index);
    public int GetUsedStickerCount(string areaName) => usedStickers.ContainsKey(areaName) ? usedStickers[areaName].Count : 0;
    public bool IsAreaCompleted(string areaName) => GetUsedStickerCount(areaName) >= 6;

    private void SaveUsedStickers()
    {
        UsedStickerSaveData data = new UsedStickerSaveData();
        foreach (var kvp in usedStickers)
            data.areas.Add(new AreaStickerData { areaName = kvp.Key, stickerIndices = kvp.Value });

        PlayerPrefs.SetString(GetUsedKey(), JsonUtility.ToJson(data));
        PlayerPrefs.Save();
    }

    private void LoadUsedStickers()
    {
        usedStickers.Clear();
        string key = GetUsedKey();
        if (PlayerPrefs.HasKey(key))
        {
            string json = PlayerPrefs.GetString(key);
            if (!string.IsNullOrEmpty(json))
            {
                UsedStickerSaveData data = JsonUtility.FromJson<UsedStickerSaveData>(json);
                if (data != null)
                    foreach (var area in data.areas)
                        usedStickers[area.areaName] = new List<int>(area.stickerIndices);
            }
        }
    }

    private void RegisterStickerCollection(PointOfInterest poi)
    {
        if (!collectedStickers.ContainsKey(poi.areaName))
            collectedStickers[poi.areaName] = new List<int>();

        if (!collectedStickers[poi.areaName].Contains(poi.stickerIndex))
        {
            collectedStickers[poi.areaName].Add(poi.stickerIndex);
            SaveCollectedStickers();
            OnCollectedStickersChanged?.Invoke();

            NotifyStickerCatalog(poi.areaName, poi.stickerIndex);

            inventoryManager = inventoryManager ?? FindObjectOfType<InventoryManager>();
            inventoryManager?.UpdateInventoryUI();
        }
    }

    private void NotifyStickerCatalog(string areaName, int stickerIndex)
    {
        StickerCatalogUI catalog = FindObjectOfType<StickerCatalogUI>();
        if (catalog != null)
            catalog.AddUnlockedSticker(areaName, stickerIndex);
    }

    public bool IsStickerCollected(string areaName, int index) => collectedStickers.ContainsKey(areaName) && collectedStickers[areaName].Contains(index);

    private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371000;
        double dLat = DegToRad(lat2 - lat1);
        double dLon = DegToRad(lon2 - lon1);
        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                   Math.Cos(DegToRad(lat1)) * Math.Cos(DegToRad(lat2)) *
                   Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }

    private double DegToRad(double deg) => deg * (Math.PI / 180);

    public int GetCollectedStickerCount(string areaName)
    {
        if (!collectedStickers.ContainsKey(areaName)) return 0;
        int count = 0;
        foreach (int index in collectedStickers[areaName])
            if (index >= 3 && index <= 5) count++;
        return count;
    }

    private void SaveCollectedStickers()
    {
        StickerSaveData data = new StickerSaveData();
        foreach (var kvp in collectedStickers)
            data.areas.Add(new AreaStickerData { areaName = kvp.Key, stickerIndices = kvp.Value });

        PlayerPrefs.SetString(GetCollectedKey(), JsonUtility.ToJson(data));
        PlayerPrefs.Save();
    }

    private void LoadCollectedStickers()
    {
        collectedStickers.Clear();
        string key = GetCollectedKey();
        if (PlayerPrefs.HasKey(key))
        {
            string json = PlayerPrefs.GetString(key);
            if (!string.IsNullOrEmpty(json))
            {
                StickerSaveData data = JsonUtility.FromJson<StickerSaveData>(json);
                if (data != null)
                    foreach (var area in data.areas)
                        collectedStickers[area.areaName] = new List<int>(area.stickerIndices);
            }
        }
    }

    private void OnApplicationPause(bool pause)
    {
        if (!pause) return;
        SaveCollectedStickers();
        SaveUsedStickers();
        SaveVisitedAreas();
    }

    private void OnApplicationQuit()
    {
        SaveCollectedStickers();
        SaveUsedStickers();
        SaveVisitedAreas();
    }

    private void OnDisable()
    {
        Input.location.Stop();
        CancelInvoke();
    }

    private string GetCollectedKey() => "CollectedStickers_" + currentStartMode;
    private string GetUsedKey() => "UsedStickers_" + currentStartMode;

    private float GetEffectiveRadius(PointOfInterest poi, LocationInfo data)
    {
        if (data.horizontalAccuracy <= 0)
            return poi.baseDetectionRadius;

        float gpsRadius = Mathf.Min(data.horizontalAccuracy * accuracyMultiplier, maxGpsRadius);
        return Mathf.Max(poi.baseDetectionRadius, gpsRadius);
    }

    private bool CanActivateCursoDagua()
    {
        return visitedAreas.Contains("Serrapilheira") ||
               visitedAreas.Contains("Subosque") ||
               visitedAreas.Contains("Dossel");
    }


    private void SaveVisitedAreas()
    {
        StringListWrapper data = new StringListWrapper { list = visitedAreas.ToList() };
        PlayerPrefs.SetString(VISITED_AREAS_KEY, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
    }

    private void LoadVisitedAreas()
    {
        visitedAreas.Clear();
        if (!PlayerPrefs.HasKey(VISITED_AREAS_KEY)) return;
        string json = PlayerPrefs.GetString(VISITED_AREAS_KEY);
        if (string.IsNullOrEmpty(json)) return;
        StringListWrapper data = JsonUtility.FromJson<StringListWrapper>(json);
        if (data?.list == null) return;
        foreach (string area in data.list)
            visitedAreas.Add(area);
    }

    private void ShowGPSPanel()
    {
        if (gpsDisabledPanel != null && !gpsDisabledPanel.activeSelf)
            gpsDisabledPanel.SetActive(true);
    }

    private void HideGPSPanel()
    {
        if (gpsDisabledPanel != null && gpsDisabledPanel.activeSelf)
            gpsDisabledPanel.SetActive(false);
    }













}

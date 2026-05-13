using UnityEngine;
using UnityEngine.Android;
using TMPro;
using System.Collections;
using System.Globalization;
using System.Text;

public enum WaypointTipo
{
    Especie,
    AreaFoto,
    AreaVideo
}

[System.Serializable]
public class Waypoint
{
    public string nome = "Novo Ponto";
    public double lat;
    public double lng;
    public float radius = 50f;
    public WaypointTipo tipo = WaypointTipo.Especie;
}

public class LocationBridge : MonoBehaviour
{
    [Header("Pontos de Descoberta")]
    public Waypoint[] waypoints;

    [Header("UI - Localização em Tempo Real")]
    public TextMeshProUGUI locationText;

    private const string UNITY_TAG = "GPS_UNITY";

    void Start()
    {
        if (gameObject.name != "LocationManager")
        {
            gameObject.name = "LocationManager";
            Debug.Log($"[{UNITY_TAG}] Nome do GameObject ajustado para 'LocationManager'.");
        }

        if (locationText != null) locationText.text = "Checando permissões...";

        StartCoroutine(RequestPermissionsSequence());
    }

    IEnumerator RequestPermissionsSequence()
    {
        if (Application.platform != RuntimePlatform.Android) yield break;

        if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
        {
            Debug.Log($"[{UNITY_TAG}] Solicitando FineLocation...");
            Permission.RequestUserPermission(Permission.FineLocation);
            yield return new WaitUntil(() => Permission.HasUserAuthorizedPermission(Permission.FineLocation));
        }

        if (!Permission.HasUserAuthorizedPermission("android.permission.POST_NOTIFICATIONS"))
        {
            Debug.Log($"[{UNITY_TAG}] Solicitando permissão de Notificação...");
            Permission.RequestUserPermission("android.permission.POST_NOTIFICATIONS");
            yield return new WaitForSeconds(0.5f);
        }

        if (!Permission.HasUserAuthorizedPermission("android.permission.ACCESS_BACKGROUND_LOCATION"))
        {
            Debug.Log($"[{UNITY_TAG}] Solicitando Background Location (abrindo configurações)...");
            Permission.RequestUserPermission("android.permission.ACCESS_BACKGROUND_LOCATION");
            yield return new WaitUntil(() => Permission.HasUserAuthorizedPermission("android.permission.ACCESS_BACKGROUND_LOCATION"));
        }

        Debug.Log($"[{UNITY_TAG}] Todas as permissões concedidas! Iniciando serviço automaticamente...");
        StartLocationService();
    }

    public void StartLocationService()
    {
        if (Application.platform != RuntimePlatform.Android) return;

        try
        {
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                AndroidJavaObject context = activity.Call<AndroidJavaObject>("getApplicationContext");

                AndroidJavaObject intent = new AndroidJavaObject(
                    "android.content.Intent", context,
                    new AndroidJavaClass("com.unity.location.LocationService"));

                string json = BuildWaypointsJson();
                intent.Call<AndroidJavaObject>("putExtra", "waypoints", json);

                Debug.Log($"[{UNITY_TAG}] AUTO-START: Enviando Intent para o Java...");
                context.Call<AndroidJavaObject>("startForegroundService", intent);

                if (locationText != null) locationText.text = "GPS Ativo (Auto)";
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[{UNITY_TAG}] Erro no Auto-Start: " + e.Message);
        }
    }

    public void UpdateLocation(string data)
    {
        Debug.Log($"<color=#00FF00>[{UNITY_TAG}] RECEBIDO: {data}</color>");
        var parts = data.Split(',');
        if (parts.Length != 2) return;

        if (double.TryParse(parts[0], NumberStyles.Any, CultureInfo.InvariantCulture, out double lat) &&
            double.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out double lng))
        {
            if (locationText != null)
            {
                locationText.text = $"<b>Localização (Auto)</b>\nLat: {lat:F6}\nLng: {lng:F6}";
            }
        }
    }

    public void OnPointReached(string data)
    {
        Debug.Log($"<color=yellow>[{UNITY_TAG}] ALVO ALCANÇADO: {data}</color>");
    }

    string BuildWaypointsJson()
    {
        var sb = new StringBuilder("[");
        for (int i = 0; i < waypoints.Length; i++)
        {
            if (i > 0) sb.Append(",");
            var wp = waypoints[i];

            // Converte o enum para string para enviar ao Java
            string tipoStr = wp.tipo.ToString(); // "Especie", "AreaFoto" ou "AreaVideo"

            sb.Append($"{{" +
                $"\"lat\":{wp.lat.ToString(CultureInfo.InvariantCulture)}," +
                $"\"lng\":{wp.lng.ToString(CultureInfo.InvariantCulture)}," +
                $"\"radius\":{wp.radius.ToString(CultureInfo.InvariantCulture)}," +
                $"\"nome\":\"{wp.nome}\"," +
                $"\"tipo\":\"{tipoStr}\"" +
                $"}}");
        }
        sb.Append("]");
        return sb.ToString();
    }
}
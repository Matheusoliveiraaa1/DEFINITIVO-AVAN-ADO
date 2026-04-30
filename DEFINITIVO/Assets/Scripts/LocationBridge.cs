using UnityEngine;
using UnityEngine.Android;
using TMPro;
using System.Collections;
using System.Globalization;
using System.Text;

[System.Serializable]
public class Waypoint
{
    public string nome = "Novo Ponto";
    public double lat;
    public double lng;
    public float radius = 50f;
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
        // Garantindo que o nome do objeto está correto para receber mensagens do Java
        if (gameObject.name != "LocationManager")
        {
            gameObject.name = "LocationManager";
            Debug.Log($"[{UNITY_TAG}] Nome do GameObject ajustado para 'LocationManager'.");
        }

        if (locationText != null) locationText.text = "Checando permissões...";

        // Inicia a sequência que pede permissão e liga o serviço no final
        StartCoroutine(RequestPermissionsSequence());
    }

    IEnumerator RequestPermissionsSequence()
    {
        if (Application.platform != RuntimePlatform.Android) yield break;

        // 1. Localização Precisa (Obrigatório)
        if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
        {
            Debug.Log($"[{UNITY_TAG}] Solicitando FineLocation...");
            Permission.RequestUserPermission(Permission.FineLocation);
            yield return new WaitUntil(() => Permission.HasUserAuthorizedPermission(Permission.FineLocation));
        }

        // 2. Notificações (Android 13+)
        if (!Permission.HasUserAuthorizedPermission("android.permission.POST_NOTIFICATIONS"))
        {
            Debug.Log($"[{UNITY_TAG}] Solicitando permissão de Notificação...");
            Permission.RequestUserPermission("android.permission.POST_NOTIFICATIONS");
            yield return new WaitForSeconds(0.5f);
        }

        // 3. Localização em Segundo Plano (Android 11+)
        // Nota: Isso abre o menu de configurações do celular
        if (!Permission.HasUserAuthorizedPermission("android.permission.ACCESS_BACKGROUND_LOCATION"))
        {
            Debug.Log($"[{UNITY_TAG}] Solicitando Background Location (abrindo configurações)...");
            Permission.RequestUserPermission("android.permission.ACCESS_BACKGROUND_LOCATION");

            // Espera o usuário voltar para o app e decidir
            yield return new WaitUntil(() => Permission.HasUserAuthorizedPermission("android.permission.ACCESS_BACKGROUND_LOCATION"));
        }

        Debug.Log($"[{UNITY_TAG}] Todas as permissões concedidas! Iniciando serviço automaticamente...");

        // CHAMA O INÍCIO DO SERVIÇO AUTOMATICAMENTE AQUI
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
            sb.Append($"{{\"lat\":{wp.lat.ToString(CultureInfo.InvariantCulture)},\"lng\":{wp.lng.ToString(CultureInfo.InvariantCulture)},\"radius\":{wp.radius.ToString(CultureInfo.InvariantCulture)},\"nome\":\"{wp.nome}\"}}");
        }
        sb.Append("]");
        return sb.ToString();
    }
}
using UnityEngine;
using System.Collections.Generic;
using System.Text;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Collections;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif

[System.Serializable]
public enum GeofenceType
{
    Area = 0,
    Sticker = 1,
    VideoPoint = 2
}

[System.Serializable]
public class MyGeofence
{
    public string name;
    public double latitude;
    public double longitude;
    [Range(10, 5000)] public float radius = 50;
    public GeofenceType type;
}

public class LocationBackgroundManager : MonoBehaviour
{
    public List<MyGeofence> destinationPoints = new List<MyGeofence>();
    public static bool IsServiceRunning = false;

#if UNITY_IOS
    [DllImport("__Internal")] private static extern void StartNativeiOS(string data);
    [DllImport("__Internal")] private static extern void StopNativeiOS();
#endif

    // Substitua o Start antigo por este no LocationBackgroundManager.cs
    private IEnumerator Start()
    {
        this.gameObject.name = "LocationManager";

        // 1. Pede as permissões
        RequestPermissions();

        // 2. Aguarda até que a permissão de localização seja concedida
        // Sem isso, o StartTracking() roda antes do clique no "Permitir" e causa o Crash
        while (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
        {
            Debug.Log("Aguardando permissão do usuário...");
            yield return new WaitForSeconds(0.5f);
        }

        // 3. Agora que temos permissão, inicia o rastreio com segurança
        StartTracking();
    }

    private void RequestPermissions()
    {
#if UNITY_ANDROID
        if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
            Permission.RequestUserPermission(Permission.FineLocation);
        if (!Permission.HasUserAuthorizedPermission("android.permission.POST_NOTIFICATIONS"))
            Permission.RequestUserPermission("android.permission.POST_NOTIFICATIONS");
        if (!Permission.HasUserAuthorizedPermission("android.permission.VIBRATE"))
            Permission.RequestUserPermission("android.permission.VIBRATE");
#elif UNITY_IOS
        Input.location.Start(); Input.location.Stop();
#endif
    }

    // --- NOVO: Recebe o status vindo do clique na Notificação Android ---
    public void OnServiceStatusChanged(string status)
    {
        if (status == "Running")
        {
            IsServiceRunning = true;
            Debug.Log("Serviço está ATIVO e rastreando.");
        }
        else if (status == "Paused")
        {
            IsServiceRunning = false;
            Debug.Log("Serviço está PAUSADO (GPS desligado).");
        }
    }

    public void StartTracking()
    {
        if (destinationPoints.Count == 0) return;

        StringBuilder sb = new StringBuilder();
        foreach (var p in destinationPoints)
        {
            string safeName = p.name.Replace("|", "").Replace(";", "").Trim();
            if (string.IsNullOrEmpty(safeName)) safeName = "Local";

            sb.Append(
                $"{safeName}|" +
                $"{p.latitude.ToString(CultureInfo.InvariantCulture)}|" +
                $"{p.longitude.ToString(CultureInfo.InvariantCulture)}|" +
                $"{p.radius.ToString(CultureInfo.InvariantCulture)}|" +
                $"{(int)p.type};"
            );
        }
        string data = sb.ToString().TrimEnd(';');

#if UNITY_ANDROID && !UNITY_EDITOR
        StartAndroid(data);
#elif UNITY_IOS && !UNITY_EDITOR
        StartNativeiOS(data);
#endif

        IsServiceRunning = true;
        Input.location.Start();
    }

    // Este método agora apenas envia o comando "STOP_SERVICE", que no nosso novo Java PAUSA o serviço.
    public void StopTracking(string msg = "")
    {
        IsServiceRunning = false;
        Input.location.Stop();
#if UNITY_ANDROID && !UNITY_EDITOR
        StopAndroid();
#elif UNITY_IOS && !UNITY_EDITOR
        StopNativeiOS();
#endif
    }

    // --- NOVO: Caso você queira realmente FECHAR o app e matar a notificação ---
    public void KillServiceCompletely()
    {
        IsServiceRunning = false;
        Input.location.Stop();
#if UNITY_ANDROID && !UNITY_EDITOR
        using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        {
            AndroidJavaObject context = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity").Call<AndroidJavaObject>("getApplicationContext");
            AndroidJavaObject intent = new AndroidJavaObject("android.content.Intent", context, new AndroidJavaClass("com.unity.location.UnityLocationService"));
            context.Call<bool>("stopService", intent);
        }
#endif
    }

    private void StartAndroid(string data)
    {
        try
        {
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                AndroidJavaObject context = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity").Call<AndroidJavaObject>("getApplicationContext");
                using (AndroidJavaObject intent = new AndroidJavaObject("android.content.Intent", context, new AndroidJavaClass("com.unity.location.UnityLocationService")))
                {
                    intent.Call<AndroidJavaObject>("putExtra", "points_data", data);
                    using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
                    {
                        if (version.GetStatic<int>("SDK_INT") >= 26) context.Call<AndroidJavaObject>("startForegroundService", intent);
                        else context.Call<AndroidJavaObject>("startService", intent);
                    }
                }
            }
        }
        catch (System.Exception e) { Debug.LogError(e.Message); }
    }

    private void StopAndroid()
    {
        using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        {
            AndroidJavaObject context = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity").Call<AndroidJavaObject>("getApplicationContext");
            AndroidJavaObject intent = new AndroidJavaObject("android.content.Intent", context, new AndroidJavaClass("com.unity.location.UnityLocationService"));
            intent.Call<AndroidJavaObject>("setAction", "STOP_SERVICE");
            context.Call<AndroidJavaObject>("startService", intent);
        }
    }
}
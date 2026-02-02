using UnityEngine;
using UnityEngine.Android;
using System.Collections.Generic;
using System.Text;
using System.Globalization; // Importante para garantir que coordenadas usem ponto (.) e não vírgula

[System.Serializable]
public class MyGeofence 
{
    public string name;      // Ex: Casa
    public double latitude;  // Ex: -26.8912
    public double longitude; // Ex: -49.2231
    public float radius;     // Ex: 50 (metros)
}

public class AndroidForegroundLocation : MonoBehaviour
{
    // Cadastre seus pontos aqui pelo Inspector
    public List<MyGeofence> destinationPoints = new List<MyGeofence>();
    public static bool IsServiceRunning = false;

    void Start()
    {
        if (Application.platform == RuntimePlatform.Android)
        {
            // Pede permissão de localização
            if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
                Permission.RequestUserPermission(Permission.FineLocation);
            
            // Pede permissão de notificação (Android 13+)
            const string NOTIF_PERM = "android.permission.POST_NOTIFICATIONS";
            if (!Permission.HasUserAuthorizedPermission(NOTIF_PERM))
                Permission.RequestUserPermission(NOTIF_PERM);
        }
    }

    public void StartTracking()
    {
        if (Application.platform != RuntimePlatform.Android) return;
        if (destinationPoints.Count == 0) 
        {
            Debug.LogError("Nenhum ponto cadastrado na lista!");
            return;
        }

        // Formata a lista para string: Nome|Lat|Lon|Raio;Nome|Lat|Lon|Raio
        StringBuilder sb = new StringBuilder();
        foreach (var p in destinationPoints)
        {
            sb.Append($"{p.name}|{p.latitude.ToString(CultureInfo.InvariantCulture)}|{p.longitude.ToString(CultureInfo.InvariantCulture)}|{p.radius.ToString(CultureInfo.InvariantCulture)};");
        }
        string dataToSend = sb.ToString().TrimEnd(';');

        try 
        {
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                AndroidJavaObject context = activity.Call<AndroidJavaObject>("getApplicationContext");
                
                using (AndroidJavaObject intent = new AndroidJavaObject("android.content.Intent", context, new AndroidJavaClass("com.unity.location.UnityLocationService")))
                {
                    intent.Call<AndroidJavaObject>("putExtra", "points_data", dataToSend);
                    
                    if (GetSDKInt() >= 26)
                        context.Call<AndroidJavaObject>("startForegroundService", intent);
                    else
                        context.Call<AndroidJavaObject>("startService", intent);
                }
            }
            
            IsServiceRunning = true;
            // Iniciamos o GPS do Unity também só para garantir sincronia se app estiver aberto
            Input.location.Start(); 
        }
        catch (System.Exception e)
        {
            Debug.LogError("Erro ao iniciar serviço: " + e.Message);
        }
    }

    public void StopTracking()
    {
        // Lógica simples para parar o serviço se necessário
        if (Application.platform != RuntimePlatform.Android) return;
        
        using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        {
            AndroidJavaObject context = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity").Call<AndroidJavaObject>("getApplicationContext");
            AndroidJavaObject intent = new AndroidJavaObject("android.content.Intent", context, new AndroidJavaClass("com.unity.location.UnityLocationService"));
            context.Call<bool>("stopService", intent);
        }
        IsServiceRunning = false;
        Input.location.Stop();
    }

    private int GetSDKInt()
    {
        using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
            return version.GetStatic<int>("SDK_INT");
    }
}
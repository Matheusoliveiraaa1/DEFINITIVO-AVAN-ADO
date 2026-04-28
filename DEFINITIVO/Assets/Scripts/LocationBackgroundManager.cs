using UnityEngine;
using System.Collections.Generic;
using System.Text;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Collections;

#if UNITY_ANDROID || UNITY_EDITOR
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
    private void Awake()
    {
        this.gameObject.name = "BackgroundManager";
    }

        private static bool _isRequestingPermissions = false;

    private IEnumerator Start()
    {
        yield return new WaitForSeconds(1.0f);
        RequestPermissions();
        
        float timeout = 15f;
        float elapsed = 0f;

        while (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
        {
            elapsed += 0.5f;
            if (elapsed >= timeout)
            {
                Debug.LogWarning("Location permission denied or timed out.");
                yield break; 
            }
            yield return new WaitForSeconds(0.5f);
        }

        StartTracking();
    }

    private void RequestPermissions()
    {
#if UNITY_ANDROID
        if (_isRequestingPermissions) return;

        System.Collections.Generic.List<string> permissions = new System.Collections.Generic.List<string>();
        
        if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.FineLocation))
            permissions.Add(UnityEngine.Android.Permission.FineLocation);
            
        if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission("android.permission.POST_NOTIFICATIONS"))
            permissions.Add("android.permission.POST_NOTIFICATIONS");

        if (permissions.Count > 0)
        {
            _isRequestingPermissions = true;
            UnityEngine.Android.Permission.RequestUserPermissions(permissions.ToArray());
            Invoke(nameof(ResetPermissionFlag), 3f);
        }
#elif UNITY_IOS
        Input.location.Start(); Input.location.Stop();
#endif
    }

    private void ResetPermissionFlag() { _isRequestingPermissions = false; }
    // --- NOVO: Recebe o status vindo do clique na Notifica��o Android ---
    public void OnServiceStatusChanged(string status)
    {
        if (status == "Running")
        {
            IsServiceRunning = true;
            Debug.Log("Servi�o est� ATIVO e rastreando.");
        }
        else if (status == "Paused")
        {
            IsServiceRunning = false;
            Debug.Log("Servi�o est� PAUSADO (GPS desligado).");
        }
    }

    public void StartTracking()
    {
        if (destinationPoints.Count == 0) return;

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        foreach (var p in destinationPoints)
        {
            string safeName = p.name.Replace("|", "").Replace(";", "").Trim();
            if (string.IsNullOrEmpty(safeName)) safeName = "Local";

            sb.Append(
                $"{safeName}|" +
                $"{p.latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}|" +
                $"{p.longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}|" +
                $"{p.radius.ToString(System.Globalization.CultureInfo.InvariantCulture)}|" +
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
        
        try { 
            if (Input.location.status == LocationServiceStatus.Running)
                Input.location.Stop();
                
            Input.location.Start(); 
        } 
        catch (System.Exception e) { Debug.LogError("Error starting Input.location: " + e.Message); }
    }

    public void StopTracking(string msg = "")
    {
        IsServiceRunning = false;
        try { Input.location.Stop(); } catch {}
#if UNITY_ANDROID && !UNITY_EDITOR
        StopAndroid();
#elif UNITY_IOS && !UNITY_EDITOR
        StopNativeiOS();
#endif
    }

    // --- NOVO: Caso voc� queira realmente FECHAR o app e matar a notifica��o ---
    public void KillServiceCompletely()
    {
        IsServiceRunning = false;
        try { Input.location.Stop(); } catch {}
    #if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                using (AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    if (activity != null)
                    {
                        using (AndroidJavaObject context = activity.Call<AndroidJavaObject>("getApplicationContext"))
                        {
                            using (AndroidJavaClass serviceClass = new AndroidJavaClass("com.unity.location.UnityLocationService"))
                            using (AndroidJavaObject intent = new AndroidJavaObject("android.content.Intent", context, serviceClass))
                            {
                                intent.Call<AndroidJavaObject>("putExtra", "kill", true);
                                context.Call<bool>("stopService", intent);
                            }
                        }
                    }
                }
            }
        }
        catch (System.Exception e) { Debug.LogError("Error killing service: " + e.Message); }
    #endif
    }

    private void StartAndroid(string data)
    {
        try
        {
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                using (AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    if (activity == null) return;
                    using (AndroidJavaObject context = activity.Call<AndroidJavaObject>("getApplicationContext"))
                    {
                        using (AndroidJavaClass serviceClass = new AndroidJavaClass("com.unity.location.UnityLocationService"))
                        using (AndroidJavaObject intent = new AndroidJavaObject("android.content.Intent", context, serviceClass))
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
            }
        }
        catch (System.Exception e) { Debug.LogError("Error starting Android service: " + e.Message); }
    }

    private void StopAndroid()
    {
        try
        {
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                using (AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    if (activity == null) return;
                    using (AndroidJavaObject context = activity.Call<AndroidJavaObject>("getApplicationContext"))
                    {
                        using (AndroidJavaClass serviceClass = new AndroidJavaClass("com.unity.location.UnityLocationService"))
                        using (AndroidJavaObject intent = new AndroidJavaObject("android.content.Intent", context, serviceClass))
                        {
                            intent.Call<AndroidJavaObject>("setAction", "STOP_SERVICE");
                            context.Call<AndroidJavaObject>("startService", intent);
                        }
                    }
                }
            }
        }
        catch (System.Exception e) { Debug.LogError("Error stopping Android service: " + e.Message); }
    }
    
    private void OnApplicationQuit()
    {
        KillServiceCompletely();
    }
    
}
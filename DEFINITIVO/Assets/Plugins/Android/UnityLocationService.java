package com.unity.location;

import android.app.*;
import android.content.*;
import android.location.Location;
import android.location.LocationListener;
import android.location.LocationManager;
import android.os.Bundle;
import android.os.IBinder;
import androidx.core.app.NotificationCompat;
import com.unity3d.player.UnityPlayer;
import java.util.ArrayList;
import java.util.List;

import javax.management.Notification;

import android.os.PowerManager;


public class UnityLocationService extends Service implements LocationListener
{
    private LocationManager locationManager;

    private PowerManager.WakeLock wakeLock;

    

    
    // TWO CHANNELS: One for the silent persistent notification, one for the loud alert

    
    private static final String CHANNEL_ID_SERVICE = "LocationServiceChannel";
    private static final String CHANNEL_ID_ALERTS = "LocationAlertsChannel";
    private static final String ACTION_STOP = "STOP_SERVICE";
    
    private List<GeofencePoint> points = new ArrayList<>();
    
    // Helper class to store geofence data
    private static class GeofencePoint
    {
        String name;
        double lat, lon;
        float radius;
        boolean alerted = false;
        
        GeofencePoint(String n, double la, double lo, float r)
        {
            name = n;
            lat = la;
            lon = lo;
            radius = r;
        }
    }
    
    @Override
    public int onStartCommand(Intent intent, int flags, int startId)
    {
        // --- 1. HANDLE STOP BUTTON ---
        if (intent != null && ACTION_STOP.equals(intent.getAction()))
        {
            // Tell Unity C# that we are stopping
            // UnityPlayer.UnitySendMessage("LocationManager", "StopTracking", "");
            stopForeground(true);
            stopSelf();
            return START_NOT_STICKY;
        }
        
        // --- 2. PARSE DATA FROM UNITY ---
        if (intent != null && intent.hasExtra("points_data"))
        {
            parsePoints(intent.getStringExtra("points_data"));
        }
        
        // --- 3. CREATE CHANNELS ---
        createNotificationChannels();
        
        // --- 4. BUILD PERSISTENT NOTIFICATION ---
        // Dynamically get the intent to open the Unity App (Fixes Unity 6 error)
        Intent openAppIntent = getPackageManager().getLaunchIntentForPackage(getPackageName());
        int pendingFlags = android.os.Build.VERSION.SDK_INT >= 30 ? 
            PendingIntent.FLAG_IMMUTABLE : PendingIntent.FLAG_UPDATE_CURRENT;
        PendingIntent pendingOpenApp = null;
        
        if (openAppIntent != null)
        {
            pendingOpenApp = PendingIntent.getActivity(this, 0, openAppIntent, pendingFlags);
        }
        
        // Intent for the "Stop Tracking" button
        Intent stopIntent = new Intent(this, UnityLocationService.class);
        stopIntent.setAction(ACTION_STOP);
        PendingIntent pendingStop = PendingIntent.getService(this, 0, stopIntent, pendingFlags);
        
        // Build the silent notification that keeps the service alive
        NotificationCompat.Builder builder = new NotificationCompat.Builder(this, CHANNEL_ID_SERVICE)
            .setContentTitle("Rastreamento Ativo")
            .setContentText("Monitorando localização em segundo plano.")
            .setSmallIcon(android.R.drawable.ic_menu_mylocation)
            .setOngoing(true) // Cannot be swiped away
            .addAction(
                android.R.drawable.ic_menu_close_clear_cancel, 
                "Parar Rastreamento", 
                pendingStop
            )
            .setPriority(NotificationCompat.PRIORITY_LOW); // Low priority = no sound/popup
            
        if (pendingOpenApp != null)
        {
            builder.setContentIntent(pendingOpenApp); // Clicking body opens app
        }
        
        // Start Foreground Service (ID 12345)
        startForeground(12345, builder.build());

        PowerManager pm = (PowerManager) getSystemService(POWER_SERVICE);
wakeLock = pm.newWakeLock(
    PowerManager.PARTIAL_WAKE_LOCK,
    "UnityLocationService::WakeLock"
);

if (wakeLock != null && !wakeLock.isHeld())
{
    wakeLock.acquire();
}

        
        // --- 5. START GPS UPDATES ---
        locationManager = (LocationManager) getSystemService(Context.LOCATION_SERVICE);
        try
        {
            // Update every 5 seconds or 5 meters
            locationManager.requestLocationUpdates(
                LocationManager.GPS_PROVIDER, 
                5000, 
                5, 
                this
            );
        }
        catch (SecurityException e)
        {
            e.printStackTrace();
        }
        
        return START_STICKY;
    }
    
    // --- HELPER: PARSE STRING TO LIST ---
    private void parsePoints(String data)
    {
        points.clear();
        if (data == null || data.isEmpty()) return;
        
        String[] entries = data.split(";");
        for (String entry : entries)
        {
            String[] parts = entry.split("\\|");
            if (parts.length == 4)
            {
                try
                {
                    points.add(new GeofencePoint(
                        parts[0],
                        Double.parseDouble(parts[1]),
                        Double.parseDouble(parts[2]),
                        Float.parseFloat(parts[3])
                    ));
                }
                catch (Exception e)
                {
                    // Ignore parsing errors
                }
            }
        }
    }
    
    // --- HELPER: CHECK IF UNITY IS IN BACKGROUND ---
    private boolean isAppInBackground()
    {
        ActivityManager.RunningAppProcessInfo myProcess = new ActivityManager.RunningAppProcessInfo();
        ActivityManager.getMyMemoryState(myProcess);
        return myProcess.importance != ActivityManager.RunningAppProcessInfo.IMPORTANCE_FOREGROUND;
    }
    
    // --- GPS CALLBACK ---
    @Override
    public void onLocationChanged(Location location)
    {
        for (GeofencePoint p : points)
        {
            float[] results = new float[1];
            Location.distanceBetween(
                location.getLatitude(),
                location.getLongitude(),
                p.lat,
                p.lon,
                results
            );
            
            // Check if inside radius
            if (results[0] <= p.radius)
            {
                // Only alert if we haven't alerted yet AND app is in background
             if (!p.alerted)

                {
                    sendArrivalNotification(p.name);
                    p.alerted = true;
                }
            }
            // Reset alert if user moves 50m away from the radius edge
            else if (results[0] > p.radius + 50)
            {
                p.alerted = false;
            }
        }
    }
    
    // --- SEND VIBRATING NOTIFICATION ---
    private void sendArrivalNotification(String locationName)
    {
        NotificationManager manager = (NotificationManager) getSystemService(NOTIFICATION_SERVICE);
        
        Intent intent = getPackageManager().getLaunchIntentForPackage(getPackageName());
        PendingIntent pending = null;
        
        if (intent != null)
        {
            pending = PendingIntent.getActivity(
                this, 
                0, 
                intent, 
                PendingIntent.FLAG_IMMUTABLE
            );
        }
        
        // Vibration Pattern: Delay 0ms, Vibrate 500ms, Pause 200ms, Vibrate 500ms
        long[] vibrationPattern = { 0, 500, 200, 500 };
        
        NotificationCompat.Builder builder = new NotificationCompat.Builder(this, CHANNEL_ID_ALERTS)
            .setContentTitle("Você chegou!")
            .setContentText("Local: " + locationName)
            .setSmallIcon(android.R.drawable.ic_dialog_map)
            .setPriority(NotificationCompat.PRIORITY_HIGH) // High Priority = Heads up display
            .setVibrate(vibrationPattern) // Trigger Vibration
            .setDefaults(Notification.DEFAULT_ALL) // Trigger Default Sound
            .setAutoCancel(true); // Disappear when clicked
            
        if (pending != null)
        {
            builder.setContentIntent(pending);
        }
        
        // Send notification with unique ID based on location name hash
        manager.notify(locationName.hashCode(), builder.build());
    }
    
    // --- CREATE CHANNELS ---
    private void createNotificationChannels()
    {
        if (android.os.Build.VERSION.SDK_INT >= 26)
        {
            NotificationManager manager = getSystemService(NotificationManager.class);
            
            // 1. Silent Channel for the Foreground Service
            NotificationChannel serviceChannel = new NotificationChannel(
                CHANNEL_ID_SERVICE,
                "Rastreamento (Silencioso)",
                NotificationManager.IMPORTANCE_LOW
            );
            manager.createNotificationChannel(serviceChannel);
            
            // 2. Alert Channel for Geofence Hits (High Importance + Vibration)
            NotificationChannel alertChannel = new NotificationChannel(
                CHANNEL_ID_ALERTS,
                "Alertas de Chegada",
                NotificationManager.IMPORTANCE_HIGH
            );
            alertChannel.enableVibration(true);
            alertChannel.setVibrationPattern(new long[]{ 0, 500, 200, 500 });
            manager.createNotificationChannel(alertChannel);
        }
    }
    
    @Override
    public IBinder onBind(Intent intent)
    {
        return null;
    }
    
    @Override
    public void onStatusChanged(String provider, int status, Bundle extras)
    {
        // Empty implementation
    }
    
    @Override
    public void onProviderEnabled(String provider)
    {
        // Empty implementation
    }
    
    @Override
    public void onProviderDisabled(String provider)
    {
        // Empty implementation
    }


@Override
public void onDestroy()
{
    super.onDestroy();

    if (locationManager != null)
    {
        locationManager.removeUpdates(this);
    }

    if (wakeLock != null && wakeLock.isHeld())
    {
        wakeLock.release();
    }
}







} 
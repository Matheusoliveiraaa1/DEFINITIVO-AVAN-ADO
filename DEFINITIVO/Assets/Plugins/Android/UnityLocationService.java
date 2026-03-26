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

public class UnityLocationService extends Service implements LocationListener {
    private LocationManager locationManager;
    private static final String CHANNEL_ID_SERVICE = "LocationServiceChannel";
    private static final String CHANNEL_ID_ALERTS = "LocationAlertsChannel_v2";
    
    private static final String ACTION_STOP = "STOP_SERVICE";
    private static final String ACTION_START = "START_SERVICE";
    
    private List<GeofencePoint> points = new ArrayList<>();
    private String lastRawData = ""; 

    private static class GeofencePoint {
        String name;
        double lat, lon;
        float radius;
        int type;
        boolean alerted = false;

        GeofencePoint(String n, double la, double lo, float r, int t) {
            name = n; lat = la; lon = lo; radius = r; type = t;
        }
    }

    @Override
    public int onStartCommand(Intent intent, int flags, int startId) {
        if (intent == null) return START_STICKY;

        String action = intent.getAction();

        if (ACTION_STOP.equals(action)) {
            pauseLocationUpdates();
            return START_STICKY;
        }

        if (ACTION_START.equals(action) || intent.hasExtra("points_data")) {
            if (intent.hasExtra("points_data")) {
                lastRawData = intent.getStringExtra("points_data");
                parsePointsSafe(lastRawData);
            }
            resumeLocationUpdates();
        }

        return START_STICKY;
    }

    private void resumeLocationUpdates() {
        createNotificationChannels();
        updateForegroundNotification(true);

        try {
            locationManager = (LocationManager) getSystemService(Context.LOCATION_SERVICE);
            locationManager.requestLocationUpdates(LocationManager.GPS_PROVIDER, 5000, 5, this);
            
            // ATENÇÃO: Verifique se o nome aqui é BackgroundManager ou LocationManager conforme o Checkpoint anterior
            UnityPlayer.UnitySendMessage("BackgroundManager", "OnServiceStatusChanged", "Running");
        } catch (SecurityException e) { e.printStackTrace(); }
    }

    private void pauseLocationUpdates() {
        if (locationManager != null) {
            locationManager.removeUpdates(this);
        }
        updateForegroundNotification(false);
        try { UnityPlayer.UnitySendMessage("BackgroundManager", "OnServiceStatusChanged", "Paused"); } catch (Exception e) {}
    }

    private void updateForegroundNotification(boolean isRunning) {
        int pendingFlags = android.os.Build.VERSION.SDK_INT >= 30 ? PendingIntent.FLAG_IMMUTABLE : PendingIntent.FLAG_UPDATE_CURRENT;
        
        Intent openAppIntent = getPackageManager().getLaunchIntentForPackage(getPackageName());
        PendingIntent pendingOpenApp = PendingIntent.getActivity(this, 0, openAppIntent, pendingFlags);

        Intent actionIntent = new Intent(this, UnityLocationService.class);
        actionIntent.setAction(isRunning ? ACTION_STOP : ACTION_START);
        PendingIntent pendingAction = PendingIntent.getService(this, (int)System.currentTimeMillis(), actionIntent, pendingFlags);

        String title = isRunning ? "Rastreamento Ativo" : "Rastreamento Pausado";
        String content = isRunning ? "Monitorando localização..." : "Clique em iniciar para retomar.";
        int icon = isRunning ? android.R.drawable.ic_menu_mylocation : android.R.drawable.ic_media_play;
        String buttonText = isRunning ? "Parar" : "Iniciar";

        NotificationCompat.Builder builder = new NotificationCompat.Builder(this, CHANNEL_ID_SERVICE)
                .setContentTitle(title)
                .setContentText(content)
                .setSmallIcon(icon)
                .setOngoing(true)
                .setContentIntent(pendingOpenApp)
                .addAction(icon, buttonText, pendingAction)
                .setPriority(NotificationCompat.PRIORITY_LOW);

        startForeground(12345, builder.build());
    }

    // --- MÉTODOS DE NOTIFICAÇÃO DE ALERTA (RECOLOCADOS AQUI) ---

    private void sendAreaNotification(String locationName) {
        NotificationManager manager = (NotificationManager) getSystemService(NOTIFICATION_SERVICE);
        Intent intent = getPackageManager().getLaunchIntentForPackage(getPackageName());
        PendingIntent pending = null;
        if (intent != null) {
            int flags = android.os.Build.VERSION.SDK_INT >= 30 ? PendingIntent.FLAG_IMMUTABLE : PendingIntent.FLAG_UPDATE_CURRENT;
            pending = PendingIntent.getActivity(this, (int)System.currentTimeMillis(), intent, flags);
        }

        NotificationCompat.Builder builder = new NotificationCompat.Builder(this, CHANNEL_ID_ALERTS)
                .setContentTitle("Área próxima!")
                .setContentText("Você está se aproximando de uma área de observação!")
                .setSmallIcon(android.R.drawable.ic_dialog_map)
                .setPriority(NotificationCompat.PRIORITY_MAX)
                .setCategory(NotificationCompat.CATEGORY_EVENT)
                .setVisibility(NotificationCompat.VISIBILITY_PUBLIC)
                .setDefaults(NotificationCompat.DEFAULT_ALL)
                .setAutoCancel(true);

        if (pending != null) builder.setContentIntent(pending);
        manager.notify(("AREA_" + locationName).hashCode(), builder.build());
    }

    private void sendStickerNotification(String locationName) {
        NotificationManager manager = (NotificationManager) getSystemService(NOTIFICATION_SERVICE);
        Intent intent = getPackageManager().getLaunchIntentForPackage(getPackageName());
        PendingIntent pending = null;
        if (intent != null) {
            int flags = android.os.Build.VERSION.SDK_INT >= 30 ? PendingIntent.FLAG_IMMUTABLE : PendingIntent.FLAG_UPDATE_CURRENT;
            pending = PendingIntent.getActivity(this, (int)System.currentTimeMillis(), intent, flags);
        }

        long[] vibrationPattern = { 0, 800, 200, 800 };

        NotificationCompat.Builder builder = new NotificationCompat.Builder(this, CHANNEL_ID_ALERTS)
                .setContentTitle("Espécie Próxima!")
                .setContentText("Você está próximo de uma espécie...")
                .setSmallIcon(android.R.drawable.star_big_on)
                .setPriority(NotificationCompat.PRIORITY_MAX)
                .setVibrate(vibrationPattern)
                .setAutoCancel(true);

        if (pending != null) builder.setContentIntent(pending);
        manager.notify(("STICKER_" + locationName).hashCode(), builder.build());
    }

    private void sendVideoNotification(String locationName) {
        NotificationManager manager = (NotificationManager) getSystemService(NOTIFICATION_SERVICE);
        Intent intent = getPackageManager().getLaunchIntentForPackage(getPackageName());
        PendingIntent pending = null;
        if (intent != null) {
            int flags = android.os.Build.VERSION.SDK_INT >= 30 ? PendingIntent.FLAG_IMMUTABLE : PendingIntent.FLAG_UPDATE_CURRENT;
            pending = PendingIntent.getActivity(this, (int)System.currentTimeMillis(), intent, flags);
        }

        long[] vibrationPattern = {0, 500, 100, 500, 100, 500};

        NotificationCompat.Builder builder = new NotificationCompat.Builder(this, CHANNEL_ID_ALERTS)
                .setContentTitle("Vídeo disponível!")
                .setContentText("Vídeo de orientação próximo.")
                .setSmallIcon(android.R.drawable.ic_media_play)
                .setPriority(NotificationCompat.PRIORITY_MAX)
                .setVibrate(vibrationPattern)
                .setAutoCancel(true);

        if (pending != null) builder.setContentIntent(pending);
        manager.notify(("VIDEO_" + locationName).hashCode(), builder.build());
    }

    private void parsePointsSafe(String data) {
        points.clear();
        if (data == null || data.isEmpty()) return;
        String cleanData = data.replaceAll("[^a-zA-Z0-9|;.\\- ]", ""); 

        String[] entries = cleanData.split(";");
        for (String entry : entries) {
            String[] parts = entry.split("\\|");
            if (parts.length == 5) {
                try {
                    points.add(new GeofencePoint(parts[0], Double.parseDouble(parts[1]), 
                               Double.parseDouble(parts[2]), Float.parseFloat(parts[3]), Integer.parseInt(parts[4])));
                } catch (Exception e) { }
            }
        }
    }

    @Override
    public void onLocationChanged(Location location) {
        if (location == null) return;
        for (GeofencePoint p : points) {
            float[] results = new float[1];
            Location.distanceBetween(location.getLatitude(), location.getLongitude(), p.lat, p.lon, results);

            if (results[0] <= p.radius) {
                if (!p.alerted && isAppInBackground()) {
                    if (p.type == 0) sendAreaNotification(p.name);
                    else if (p.type == 1) sendStickerNotification(p.name);
                    else if (p.type == 2) sendVideoNotification(p.name);
                    p.alerted = true;
                }
            } else if (results[0] > p.radius + 50) {
                p.alerted = false;
            }
        }
    }

    private boolean isAppInBackground() {
        ActivityManager.RunningAppProcessInfo myProcess = new ActivityManager.RunningAppProcessInfo();
        ActivityManager.getMyMemoryState(myProcess);
        return myProcess.importance != ActivityManager.RunningAppProcessInfo.IMPORTANCE_FOREGROUND;
    }

    private void createNotificationChannels() {
        if (android.os.Build.VERSION.SDK_INT >= 26) {
            NotificationManager manager = getSystemService(NotificationManager.class);
            if (manager != null) {
                manager.createNotificationChannel(new NotificationChannel(CHANNEL_ID_SERVICE, "Rastreamento", NotificationManager.IMPORTANCE_LOW));
                NotificationChannel alertChannel = new NotificationChannel(CHANNEL_ID_ALERTS, "Alertas de Chegada", NotificationManager.IMPORTANCE_HIGH);
                alertChannel.enableVibration(true);
                manager.createNotificationChannel(alertChannel);
            }
        }
    }

    @Override
    public void onDestroy() {
        if (locationManager != null) locationManager.removeUpdates(this);
        super.onDestroy();
    }

    @Override public IBinder onBind(Intent intent) { return null; }
    @Override public void onStatusChanged(String p, int s, Bundle e) {}
    @Override public void onProviderEnabled(String p) {}
    @Override public void onProviderDisabled(String p) {}
}
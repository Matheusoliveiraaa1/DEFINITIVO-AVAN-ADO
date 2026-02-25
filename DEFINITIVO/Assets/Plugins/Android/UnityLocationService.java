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
    // We change the ID to force Android to recreate the channel with the vibration settings enabled
    private static final String CHANNEL_ID_ALERTS = "LocationAlertsChannel_v2";
    private static final String ACTION_STOP = "STOP_SERVICE";
    private List<GeofencePoint> points = new ArrayList<>();

    private static class GeofencePoint {
        String name;
        double lat, lon;
        float radius;
        int type;
        boolean alerted = false;

        GeofencePoint(String n, double la, double lo, float r, int t) {
            name = n;
            lat = la;
            lon = lo;
            radius = r;
            type = t;
        }
    }

    @Override
    public int onStartCommand(Intent intent, int flags, int startId) {
        if (intent == null) return START_STICKY;

        // --- BOTÃO PARAR ---
        if (ACTION_STOP.equals(intent.getAction())) {
            try { UnityPlayer.UnitySendMessage("LocationManager", "StopTracking", ""); } catch (Exception e) {}

            // CRITICAL FIX: Stop listening to GPS so the service can actually die
            if (locationManager != null) {
                locationManager.removeUpdates(this);
            }

            stopForeground(true);
            stopSelf();
            return START_NOT_STICKY;
        }

        // --- RECEBE DADOS ---
        if (intent.hasExtra("points_data")) {
            parsePointsSafe(intent.getStringExtra("points_data"));
        }

        createNotificationChannels();

        // --- NOTIFICAÇÃO PERSISTENTE (SILENCIOSA) ---
        Intent openAppIntent = getPackageManager().getLaunchIntentForPackage(getPackageName());
        PendingIntent pendingOpenApp = null;
        int pendingFlags = android.os.Build.VERSION.SDK_INT >= 30 ? PendingIntent.FLAG_IMMUTABLE : PendingIntent.FLAG_UPDATE_CURRENT;

        if (openAppIntent != null) {
            pendingOpenApp = PendingIntent.getActivity(this, 0, openAppIntent, pendingFlags);
        }

        Intent stopIntent = new Intent(this, UnityLocationService.class);
        stopIntent.setAction(ACTION_STOP);
        PendingIntent pendingStop = PendingIntent.getService(this, 0, stopIntent, pendingFlags);

        NotificationCompat.Builder builder = new NotificationCompat.Builder(this, CHANNEL_ID_SERVICE)
                .setContentTitle("Rastreamento Ativo")
                .setContentText("Monitorando localização em segundo plano.")
                .setSmallIcon(android.R.drawable.ic_menu_mylocation)
                .setOngoing(true)
                .addAction(android.R.drawable.ic_menu_close_clear_cancel, "Parar Rastreamento", pendingStop)
                .setPriority(NotificationCompat.PRIORITY_LOW);

        if (pendingOpenApp != null) builder.setContentIntent(pendingOpenApp);

        startForeground(12345, builder.build());

        // --- INICIA GPS ---
        try {
            locationManager = (LocationManager) getSystemService(Context.LOCATION_SERVICE);
            locationManager.requestLocationUpdates(LocationManager.GPS_PROVIDER, 5000, 5, this);
        } catch (SecurityException | IllegalArgumentException e) { e.printStackTrace(); }

        return START_STICKY;
    }

    private void parsePointsSafe(String data) {
        points.clear();
        if (data == null || data.isEmpty()) return;
        String cleanData = data.replaceAll("[^a-zA-Z0-9|;.\\- ]", ""); // Sanitização

        String[] entries = cleanData.split(";");
        for (String entry : entries) {
            String[] parts = entry.split("\\|");
            if (parts.length == 5) {
                try {
                    String name = parts[0];
                    double lat = Double.parseDouble(parts[1]);
                    double lon = Double.parseDouble(parts[2]);
                    float rad = Float.parseFloat(parts[3]);
                    int type = Integer.parseInt(parts[4]);

                    if (lat >= -90 && lat <= 90 && lon >= -180 && lon <= 180) {
                        points.add(new GeofencePoint(name, lat, lon, rad, type));
                    }

                } catch (Exception e) { }
            }
        }
    }

    private boolean isAppInBackground() {
        ActivityManager.RunningAppProcessInfo myProcess = new ActivityManager.RunningAppProcessInfo();
        ActivityManager.getMyMemoryState(myProcess);
        return myProcess.importance != ActivityManager.RunningAppProcessInfo.IMPORTANCE_FOREGROUND;
    }

    @Override
    public void onLocationChanged(Location location) {
        if (location == null) return;
        for (GeofencePoint p : points) {
            float[] results = new float[1];
            Location.distanceBetween(location.getLatitude(), location.getLongitude(), p.lat, p.lon, results);

            if (results[0] <= p.radius) {
                if (!p.alerted && isAppInBackground()) {

                    if (p.type == 0) {
                        sendAreaNotification(p.name);
                    }
                    else if (p.type == 1) {
                        sendStickerNotification(p.name);
                    }

                    p.alerted = true;
                }
            } else if (results[0] > p.radius + 50) {
                p.alerted = false;
            }
        }
    }

    private void sendAreaNotification(String locationName) {
        NotificationManager manager = (NotificationManager) getSystemService(NOTIFICATION_SERVICE);

        Intent intent = getPackageManager().getLaunchIntentForPackage(getPackageName());
        PendingIntent pending = null;
        if (intent != null) {
            int flags = android.os.Build.VERSION.SDK_INT >= 30 ? PendingIntent.FLAG_IMMUTABLE : PendingIntent.FLAG_UPDATE_CURRENT;
            // Using a unique request code to prevent intent caching issues
            pending = PendingIntent.getActivity(this, (int)System.currentTimeMillis(), intent, flags);
        }

        NotificationCompat.Builder builder = new NotificationCompat.Builder(this, CHANNEL_ID_ALERTS)
                .setContentTitle("Área próxima!")
                .setContentText("Você está se aproximando de uma área...")
                .setSmallIcon(android.R.drawable.ic_dialog_map)
                .setPriority(NotificationCompat.PRIORITY_MAX) // Max priority for screen-off bypass
                .setCategory(NotificationCompat.CATEGORY_EVENT) // Tells OS this is an important event
                .setVisibility(NotificationCompat.VISIBILITY_PUBLIC) // Show on lock screen
                .setDefaults(NotificationCompat.DEFAULT_ALL) // Applies default device vibration and sound
                .setAutoCancel(true);

        if (pending != null) builder.setContentIntent(pending);

        manager.notify(("AREA_NOTIFICATION").hashCode(), builder.build());
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
                .setPriority(NotificationCompat.PRIORITY_MAX) // Max priority
                .setCategory(NotificationCompat.CATEGORY_EVENT) // Important event categorization
                .setVisibility(NotificationCompat.VISIBILITY_PUBLIC) // Lock screen visibility
                .setVibrate(vibrationPattern)
                .setAutoCancel(true);

        if (pending != null) builder.setContentIntent(pending);

        manager.notify(("STICKER_NOTIFICATION").hashCode(), builder.build());
    }

    private void createNotificationChannels() {
        if (android.os.Build.VERSION.SDK_INT >= 26) {
            NotificationManager manager = getSystemService(NotificationManager.class);
            NotificationChannel serviceChannel = new NotificationChannel(CHANNEL_ID_SERVICE, "Rastreamento (Silencioso)", NotificationManager.IMPORTANCE_LOW);
            manager.createNotificationChannel(serviceChannel);

            NotificationChannel alertChannel = new NotificationChannel(CHANNEL_ID_ALERTS, "Alertas de Chegada", NotificationManager.IMPORTANCE_HIGH);
            alertChannel.enableVibration(true);
            alertChannel.setVibrationPattern(new long[]{ 0, 800, 200, 800 });
            manager.createNotificationChannel(alertChannel);
        }
    }

    // CRITICAL FIX: Ensure LocationManager is cleared if Android destroys the Service forcefully
    @Override
    public void onDestroy() {
        if (locationManager != null) {
            locationManager.removeUpdates(this);
        }
        super.onDestroy();
    }

    @Override public IBinder onBind(Intent intent) { return null; }
    @Override public void onStatusChanged(String p, int s, Bundle e) {}
    @Override public void onProviderEnabled(String p) {}
    @Override public void onProviderDisabled(String p) {}
}
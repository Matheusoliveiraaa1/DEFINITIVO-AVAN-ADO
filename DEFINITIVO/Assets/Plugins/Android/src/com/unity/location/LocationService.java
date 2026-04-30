package com.unity.location;

import android.app.Notification;
import android.app.NotificationChannel;
import android.app.NotificationManager;
import android.app.PendingIntent; 
import android.app.Service;
import android.content.Context;
import android.content.Intent;
import android.location.Location;
import android.location.LocationListener;
import android.location.LocationManager;
import android.os.Build;
import android.os.IBinder;
import android.os.VibrationEffect;
import android.os.Vibrator;
import android.util.Log; // Importante para os logs
import androidx.core.app.NotificationCompat;
import com.unity3d.player.UnityPlayer;
import org.json.JSONArray;
import org.json.JSONObject;

public class LocationService extends Service implements LocationListener {

    private static final String TAG = "GPS_JAVA";
    private static final String CHANNEL_ID          = "LocationChannel";
    private static final String CHANNEL_DISCOVERY_ID = "DiscoveryChannel";
    private static final int    NOTIF_FOREGROUND_ID  = 1001;
    private static final int    NOTIF_DISCOVERY_BASE = 2000;

    private LocationManager locationManager;
    private float[][]  waypointCoords = new float[0][3];
    private String[]   waypointNames  = new String[0];
    private boolean[] waypointTriggered;

    @Override
    public int onStartCommand(Intent intent, int flags, int startId) {
        Log.d(TAG, "onStartCommand disparado!");
        createNotificationChannels();

        if (intent != null && intent.hasExtra("waypoints")) {
            String json = intent.getStringExtra("waypoints");
            Log.d(TAG, "Recebendo waypoints via Intent: " + json);
            parseWaypoints(json);
        }

        Intent stopIntent  = new Intent(this, StopServiceReceiver.class);
        int pendingFlags   = (Build.VERSION.SDK_INT >= Build.VERSION_CODES.M)
                ? PendingIntent.FLAG_IMMUTABLE | PendingIntent.FLAG_UPDATE_CURRENT
                : PendingIntent.FLAG_UPDATE_CURRENT;
        PendingIntent stopPI = PendingIntent.getBroadcast(this, 0, stopIntent, pendingFlags);

        Notification notification = new NotificationCompat.Builder(this, CHANNEL_ID)
                .setContentTitle("Rastreio Ativo")
                .setContentText("Monitorando sua localização em busca de descobertas.")
                .setSmallIcon(android.R.drawable.ic_menu_mylocation)
                .addAction(android.R.drawable.ic_menu_close_clear_cancel, "Parar", stopPI)
                .setOngoing(true)
                .build();

        startForeground(NOTIF_FOREGROUND_ID, notification);
        startLocationUpdates();
        return START_STICKY;
    }

    private void parseWaypoints(String json) {
        try {
            JSONArray arr = new JSONArray(json);
            waypointCoords   = new float[arr.length()][3];
            waypointNames    = new String[arr.length()];
            waypointTriggered = new boolean[arr.length()];

            for (int i = 0; i < arr.length(); i++) {
                JSONObject obj = arr.getJSONObject(i);
                waypointCoords[i][0] = (float) obj.getDouble("lat");
                waypointCoords[i][1] = (float) obj.getDouble("lng");
                waypointCoords[i][2] = obj.has("radius") ? (float) obj.getDouble("radius") : 50f;
                waypointNames[i] = obj.has("nome") ? obj.getString("nome") : "Ponto " + i;
                waypointTriggered[i] = false;
            }
            Log.d(TAG, "Waypoints parseados com sucesso: " + arr.length() + " pontos.");
        } catch (Exception e) {
            Log.e(TAG, "Erro ao parsear JSON: " + e.getMessage());
        }
    }

    private void startLocationUpdates() {
        Log.d(TAG, "Iniciando requestLocationUpdates...");
        locationManager = (LocationManager) getSystemService(Context.LOCATION_SERVICE);
        try {
            // Tenta GPS e Network para maior garantia de sinal inicial
            locationManager.requestLocationUpdates(LocationManager.GPS_PROVIDER, 3000, 2, this);
            locationManager.requestLocationUpdates(LocationManager.NETWORK_PROVIDER, 3000, 2, this);
            Log.d(TAG, "Listeners de GPS e Network registrados.");
        } catch (SecurityException e) {
            Log.e(TAG, "ERRO DE PERMISSÃO: " + e.getMessage());
        }
    }

    @Override
    public void onLocationChanged(Location location) {
        String pos = location.getLatitude() + "," + location.getLongitude();
        Log.d(TAG, "NOVA POSIÇÃO NO JAVA: " + pos + " (Acurácia: " + location.getAccuracy() + "m)");

        // Envia para o Unity
        Log.d(TAG, "Tentando UnitySendMessage para o objeto 'LocationManager'...");
        UnityPlayer.UnitySendMessage("LocationManager", "UpdateLocation", pos);

        for (int i = 0; i < waypointCoords.length; i++) {
            float[] result = new float[1];
            Location.distanceBetween(
                    location.getLatitude(),  location.getLongitude(),
                    waypointCoords[i][0],    waypointCoords[i][1],
                    result);

            float distancia = result[0];
            float raio      = waypointCoords[i][2];

            if (distancia <= raio && !waypointTriggered[i]) {
                waypointTriggered[i] = true;
                Log.d(TAG, "ENTROU NO RAIO: " + waypointNames[i]);
                triggerDiscovery(i, distancia, waypointNames[i]);
            } else if (distancia > raio && waypointTriggered[i]) {
                waypointTriggered[i] = false;
                Log.d(TAG, "SAIU DO RAIO: " + waypointNames[i]);
            }
        }
    }

    private void triggerDiscovery(int index, float distance, String nome) {
        Log.d(TAG, "Disparando notificação de descoberta para: " + nome);
        Vibrator vibrator = (Vibrator) getSystemService(Context.VIBRATOR_SERVICE);
        if (vibrator != null && vibrator.hasVibrator()) {
            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
                vibrator.vibrate(VibrationEffect.createWaveform(new long[]{0, 400, 200, 400}, -1));
            } else {
                vibrator.vibrate(new long[]{0, 400, 200, 400}, -1);
            }
        }

        NotificationManager nm = (NotificationManager) getSystemService(Context.NOTIFICATION_SERVICE);
        Notification notif = new NotificationCompat.Builder(this, CHANNEL_DISCOVERY_ID)
                .setContentTitle("Descoberta Próxima!")
                .setContentText(nome + " está a " + (int) distance + "m!")
                .setSmallIcon(android.R.drawable.ic_dialog_info)
                .setPriority(NotificationCompat.PRIORITY_HIGH)
                .setAutoCancel(true)
                .build();

        nm.notify(NOTIF_DISCOVERY_BASE + index, notif);
        UnityPlayer.UnitySendMessage("LocationManager", "OnPointReached", index + "|" + nome);
    }

    private void createNotificationChannels() {
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
            NotificationManager nm = getSystemService(NotificationManager.class);
            NotificationChannel foreground = new NotificationChannel(CHANNEL_ID, "GPS Ativo", NotificationManager.IMPORTANCE_LOW);
            nm.createNotificationChannel(foreground);

            NotificationChannel discovery = new NotificationChannel(CHANNEL_DISCOVERY_ID, "Descobertas", NotificationManager.IMPORTANCE_HIGH);
            discovery.enableVibration(true);
            nm.createNotificationChannel(discovery);
        }
    }

    @Override public IBinder onBind(Intent intent) { return null; }
    @Override public void onStatusChanged(String s, int i, android.os.Bundle b) {}
    @Override public void onDestroy() {
        Log.d(TAG, "Serviço destruído (onDestroy)");
        super.onDestroy();
        if (locationManager != null) locationManager.removeUpdates(this);
    }
}
using UnityEngine;
using TMPro;

public class LocationDisplay : MonoBehaviour
{
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI coordsText;

    void Update() {
        // Se a flag for falsa, a UI PARA imediatamente, independente do que o GPS diz
        if (!AndroidForegroundLocation.IsServiceRunning) {
            statusText.text = "Status: PARADO (Notificação)";
            coordsText.text = "GPS Desligado";
            coordsText.color = Color.red;
            return;
        }

        // Se estiver ativo, mostra os dados
        statusText.text = "Status: " + Input.location.status;
        if (Input.location.status == LocationServiceStatus.Running) {
            coordsText.text = $"Lat: {Input.location.lastData.latitude:F6}\nLon: {Input.location.lastData.longitude:F6}";
            coordsText.color = Color.green;
        }
    }
}
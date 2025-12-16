using UnityEngine;

public class MapScreenListener : MonoBehaviour
{
    public MapPinsController mapController;

    private bool alreadyTriggered = false;

    void OnEnable()
    {
        if (alreadyTriggered) return;

        alreadyTriggered = true;
        mapController.OnMapScreenOpened();
    }
}

using UnityEngine;
using UnityEngine.Android;

public class LocationPermissionManager : MonoBehaviour
{
    void Awake()  // Mude de Start para Awake
    {
        if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
        {
            Permission.RequestUserPermission(Permission.FineLocation);
        }

        if (!Permission.HasUserAuthorizedPermission(Permission.CoarseLocation))
        {
            Permission.RequestUserPermission(Permission.CoarseLocation);
        }
    }
}
using UnityEngine;
using UnityEngine.Android;

public class LocationPermissionManager : MonoBehaviour
{
    void Start()
    {
        // Verificar se o dispositivo já tem permissão para acessar a localização precisa (GPS)
        if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
        {
            // Solicitar permissão para acessar a localização precisa (GPS)
            Permission.RequestUserPermission(Permission.FineLocation);
        }

        // Verificar se o dispositivo já tem permissão para acessar a localização aproximada
        if (!Permission.HasUserAuthorizedPermission(Permission.CoarseLocation))
        {
            // Solicitar permissão para acessar a localização aproximada
            Permission.RequestUserPermission(Permission.CoarseLocation);
        }
    }
}

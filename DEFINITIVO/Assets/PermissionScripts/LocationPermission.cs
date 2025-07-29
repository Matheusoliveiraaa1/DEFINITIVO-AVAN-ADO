using UnityEngine;
using UnityEngine.Android;

public class LocationPermissionManager : MonoBehaviour
{
    void Start()
    {
        // Verificar se o dispositivo j� tem permiss�o para acessar a localiza��o precisa (GPS)
        if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
        {
            // Solicitar permiss�o para acessar a localiza��o precisa (GPS)
            Permission.RequestUserPermission(Permission.FineLocation);
        }

        // Verificar se o dispositivo j� tem permiss�o para acessar a localiza��o aproximada
        if (!Permission.HasUserAuthorizedPermission(Permission.CoarseLocation))
        {
            // Solicitar permiss�o para acessar a localiza��o aproximada
            Permission.RequestUserPermission(Permission.CoarseLocation);
        }
    }
}

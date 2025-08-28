using UnityEngine;

public static class VideoPlayState
{
    // Se o usu�rio j� liberou o v�deo no overlay
    public static bool IsAuthorized = false;

    // Se o v�deo j� come�ou a tocar (para evitar tocar de novo)
    public static bool AlreadyPlayed = false;

    public static void Reset()
    {
        IsAuthorized = false;
        AlreadyPlayed = false;
    }
}

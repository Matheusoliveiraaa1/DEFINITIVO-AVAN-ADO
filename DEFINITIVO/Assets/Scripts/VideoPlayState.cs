using UnityEngine;

public static class VideoPlayState
{
    // Se o usuário já liberou o vídeo no overlay
    public static bool IsAuthorized = false;

    // Se o vídeo já começou a tocar (para evitar tocar de novo)
    public static bool AlreadyPlayed = false;

    public static void Reset()
    {
        IsAuthorized = false;
        AlreadyPlayed = false;
    }
}

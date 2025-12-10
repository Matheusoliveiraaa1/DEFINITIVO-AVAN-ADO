public static class VideoPlayState
{
    public static bool IsAuthorized = false;
    public static bool AlreadyPlayed = false;

    // ✅ NOVO: nome do vídeo atual da área
    public static string CurrentVideoFile = "";

    public static void Reset()
    {
        IsAuthorized = false;
        AlreadyPlayed = false;
        CurrentVideoFile = ""; // limpa o vídeo atual
    }
}

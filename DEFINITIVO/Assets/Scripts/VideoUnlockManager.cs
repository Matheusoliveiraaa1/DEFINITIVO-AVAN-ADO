using UnityEngine;

public static class VideoUnlockManager
{
    private const string KEY_PREFIX = "VideoUnlocked_";

    public static bool IsUnlocked(string areaName)
    {
        return PlayerPrefs.GetInt(KEY_PREFIX + areaName, 0) == 1;
    }

    public static void Unlock(string areaName)
    {
        PlayerPrefs.SetInt(KEY_PREFIX + areaName, 1);
        PlayerPrefs.Save();
    }
}

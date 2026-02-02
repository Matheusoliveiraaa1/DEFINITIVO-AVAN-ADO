using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;
using System.IO;

public class MapVideoPlayer : MonoBehaviour
{
    [Header("UI")]
    public GameObject overlayRoot;
    public RawImage videoRawImage;
    public Button closeButton;

    [Header("Video")]
    public VideoPlayer videoPlayer;

    private string currentArea;

    void Awake()
    {
        overlayRoot.SetActive(false);

        closeButton.onClick.RemoveAllListeners();
        closeButton.onClick.AddListener(Close);

        // 🔥 GARANTE que o callback está sempre registrado
        videoPlayer.loopPointReached -= OnVideoFinished;
        videoPlayer.loopPointReached += OnVideoFinished;
    }

    public void Play(string areaName, string videoFile)
    {
        currentArea = areaName;

        string path = Path.Combine(Application.streamingAssetsPath, videoFile);
        videoPlayer.url = path;

        overlayRoot.SetActive(true);
        videoPlayer.Play();
    }

    void OnVideoFinished(VideoPlayer vp)
    {
        Debug.Log("🎬 Vídeo do mapa terminou");

        // 🔓 desbloqueia a área
        VideoUnlockManager.Unlock(currentArea);

        // ❌ some com o overlay AUTOMATICAMENTE
        Close();
    }

    void Close()
    {
        if (videoPlayer.isPlaying)
            videoPlayer.Stop();

        overlayRoot.SetActive(false);
    }
}

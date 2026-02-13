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

    private void Awake()
    {
        overlayRoot.SetActive(false);

        closeButton.onClick.RemoveAllListeners();
        closeButton.onClick.AddListener(Close);

        videoPlayer.loopPointReached += OnVideoFinished;
    }

    public void Play(string areaName)
    {
        // 🔒 segurança extra (opcional, mas recomendado)
        if (!VideoUnlockManager.IsUnlocked(areaName))
        {
            Debug.Log("🔒 Vídeo ainda não desbloqueado: " + areaName);
            return;
        }

        string videoFile = GetVideoFileForArea(areaName);
        if (string.IsNullOrEmpty(videoFile))
            return;

        string path = Path.Combine(Application.streamingAssetsPath, videoFile);

        videoPlayer.Stop();
        videoPlayer.url = path;

        overlayRoot.SetActive(true);
        videoRawImage.gameObject.SetActive(true);

        videoPlayer.Play();
    }

    void OnVideoFinished(VideoPlayer vp)
    {
        Close();
    }

    void Close()
    {
        videoPlayer.Stop();
        overlayRoot.SetActive(false);
    }

    string GetVideoFileForArea(string areaName)
    {
        switch (areaName)
        {
            case "CursoDagua": return "CursoDagua.mp4";
            case "Subosque": return "Subosque.mp4";
            case "Dossel": return "Dossel.mp4";
            case "Epifitas": return "Epifitas.mp4";
            case "Serrapilheira": return "TESTE2.mp4";
            default:
                Debug.LogError("❌ Área desconhecida: " + areaName);
                return null;
        }
    }
}

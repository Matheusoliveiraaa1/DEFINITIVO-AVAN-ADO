using UnityEngine;
using UnityEngine.Video;
using System.IO;
using TMPro;

public class VideoAutoPlayer : MonoBehaviour
{
    private VideoPlayer videoPlayer;
    private bool callbackRegistered = false;

    [Header("REFERÊNCIA DIRETA")]
    public PostVideoImageController postImageController; // ✅ ARRASTAR NO INSPECTOR

    [Header("DEBUG MOBILE")]
    public TMP_Text debugText;

    void Awake()
    {
        videoPlayer = GetComponent<VideoPlayer>();

        if (debugText != null)
            debugText.text = "🎬 VideoAutoPlayer iniciado";

        Debug.Log("🎬 VideoAutoPlayer inicializado");
    }

    void OnEnable()
    {
        TryPlayVideo();
    }

    private void TryPlayVideo()
    {
        if (!callbackRegistered)
        {
            videoPlayer.loopPointReached += OnVideoFinished;
            callbackRegistered = true;

            if (debugText != null)
                debugText.text = "✅ Callback registrado";

            Debug.Log("✅ Callback do vídeo registrado");
        }

        if (VideoPlayState.AlreadyPlayed)
        {
            if (debugText != null)
                debugText.text = "⚠️ Vídeo já foi tocado";

            Debug.LogWarning("⚠️ Vídeo já havia sido tocado");
            return;
        }

        string fileName = VideoPlayState.CurrentVideoFile;

        if (string.IsNullOrEmpty(fileName))
            fileName = "TESTE2.mp4";

        string videoPath = Path.Combine(Application.streamingAssetsPath, fileName);
        videoPlayer.url = videoPath;

        videoPlayer.Play();
        VideoPlayState.AlreadyPlayed = true;

        if (debugText != null)
            debugText.text = "🎬 Tocando: " + fileName;

        Debug.Log("🎬 Tocando vídeo: " + fileName);
    }

    private void OnVideoFinished(VideoPlayer vp)
    {
        if (debugText != null)
            debugText.text = "✅ FIM DO VÍDEO DETECTADO";

        Debug.Log("✅ FIM DO VÍDEO DETECTADO");

        if (postImageController != null)
        {
            postImageController.ShowAfterVideo();

            if (debugText != null)
                debugText.text += "\n🖼️ Imagem exibida";

            Debug.Log("🖼️ Comando de exibição enviado para PostVideoImageController");
        }
        else
        {
            if (debugText != null)
                debugText.text += "\n❌ Referência do PostVideoImageController NÃO atribuída!";

            Debug.LogError("❌ PostVideoImageController NÃO atribuído no Inspector!");
        }
    }
}

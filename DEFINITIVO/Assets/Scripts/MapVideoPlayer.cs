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

        // Evento chamado quando o vídeo termina
        videoPlayer.loopPointReached += OnVideoFinished;

        // NOVO: Evento chamado quando o vídeo está preparado e pronto para dar o primeiro frame
        videoPlayer.prepareCompleted += OnVideoPrepared;
    }

    public void Play(string areaName)
    {
        if (!VideoUnlockManager.IsUnlocked(areaName))
        {
            Debug.Log("🔒 Vídeo ainda não desbloqueado: " + areaName);
            return;
        }

        string videoFile = GetVideoFileForArea(areaName);
        if (string.IsNullOrEmpty(videoFile))
            return;

        string path = Path.Combine(Application.streamingAssetsPath, videoFile);

        // 1. Para o vídeo atual
        videoPlayer.Stop();

        // 2. ESCONDE a RawImage para não mostrar o frame antigo (o "fantasma")
        videoRawImage.enabled = false;

        // 3. Configura o novo caminho
        videoPlayer.url = path;

        // 4. Prepara o vídeo (carrega em background) em vez de dar Play direto
        overlayRoot.SetActive(true);
        videoPlayer.Prepare();
    }

    // Chamado automaticamente quando o VideoPlayer terminar de carregar o vídeo novo
    void OnVideoPrepared(VideoPlayer vp)
    {
        // 5. Agora que o vídeo está pronto, mostramos a imagem e damos o Play
        videoRawImage.enabled = true;
        vp.Play();
    }

    void OnVideoFinished(VideoPlayer vp)
    {
        Close();
    }

    void Close()
    {
        videoPlayer.Stop();
        // Garantimos que a imagem suma ao fechar para não brilhar o frame antigo na próxima abertura
        videoRawImage.enabled = false;
        overlayRoot.SetActive(false);
    }

    string GetVideoFileForArea(string areaName)
    {
        return areaName switch
        {
            "CursoDagua" => "curso_dagua.mp4",
            "Subosque" => "subosque.mp4",
            "Dossel" => "dossel.mp4",
            "Epifitas" => "epifitas.mp4",
            "Serrapilheira" => "serrapilheira.mp4",
            _ => null
        };
    }
}
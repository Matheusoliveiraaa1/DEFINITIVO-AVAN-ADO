using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;
using System.Collections.Generic;

public class VideoManager : MonoBehaviour
{
    public static VideoManager Instance;

    [Header("UI References")]
    public GameObject upBar;
    public GameObject navBar;
    public VideoPlayer videoPlayer;
    public RawImage rawImage;
    public NavBarController navController;


    [Header("Ativar somente durante vídeo")]
    public GameObject[] activeWhileVideoPlaying;

    private string pendingVideoArea = null;
    private bool isVideoPlaying = false;
    public PostVideoImageManager postVideoImageManager;
    private string currentPlayingArea = null;



    // HashSet para rastrear quais vídeos já foram vistos nesta sessão
    private HashSet<string> videosVistosNaSessao = new HashSet<string>();

    private void Awake()
    {

        if (activeWhileVideoPlaying != null)
        {
            foreach (var obj in activeWhileVideoPlaying)
                if (obj != null)
                    obj.SetActive(false);
        }
        if (Instance != null && Instance != this)
            Destroy(this.gameObject);
        else
            Instance = this;

        if (navController == null)
            navController = FindObjectOfType<NavBarController>();

        if (rawImage != null)
            rawImage.gameObject.SetActive(false);
    }

    public void PrepareVideo(string areaName)
    {
        pendingVideoArea = areaName;
        TryPlayVideo();
    }

    public void TryPlayVideo()
    {
        if (isVideoPlaying || string.IsNullOrEmpty(pendingVideoArea))
            return;

        // Verifica se o vídeo desta área já foi visto nesta sessão
        if (videosVistosNaSessao.Contains(pendingVideoArea))
        {
            pendingVideoArea = null; // Limpa a área pendente
            return; // Não reproduz o vídeo novamente
        }

        NavigationManager nav = FindObjectOfType<NavigationManager>();
        if (nav == null || nav.currentState != NavigationManager.AppState.Exploracao)
            return;

        PlayVideoForArea(pendingVideoArea);
    }

    private void PlayVideoForArea(string areaName)
    {

        string videoPath = GetVideoPath(areaName);
        if (string.IsNullOrEmpty(videoPath))
            return;
        currentPlayingArea = areaName;


        // Marca que este vídeo foi visto na sessão atual
        videosVistosNaSessao.Add(areaName);

        // Oculta UI
        if (upBar != null)
            upBar.SetActive(false);
        if (navBar != null)
            navBar.SetActive(false);
        if (navController != null)
            navController.ignoreWhileVideoPlaying = true;

        if (rawImage != null)
        {
            rawImage.gameObject.SetActive(true);

            // 🔥 SEMPRE recria a textura (evita bug após reset)
            if (videoPlayer.targetTexture != null)
            {
                videoPlayer.targetTexture.Release();
            }

            // 🔥 Usa tamanho fixo (evita textura branca)
            RenderTexture rt = new RenderTexture(1920, 1080, 0);
            rt.Create();

            videoPlayer.targetTexture = rt;
            rawImage.texture = rt;
        }

        videoPlayer.url = videoPath;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.Play();
        isVideoPlaying = true;

        if (activeWhileVideoPlaying != null)
        {
            foreach (var obj in activeWhileVideoPlaying)
                if (obj != null)
                    obj.SetActive(true);
        }
        videoPlayer.loopPointReached += OnVideoEnd;
        pendingVideoArea = null;
    }

    private string GetVideoPath(string areaName)
    {
        switch (areaName)
        {
            case "Epifitas": return Application.streamingAssetsPath + "/epifitas.mp4";
            case "Serrapilheira": return Application.streamingAssetsPath + "/serrapilheira.mp4";
            case "CursoDagua": return Application.streamingAssetsPath + "/curso_dagua.mp4";
            case "Subosque": return Application.streamingAssetsPath + "/subosque.mp4";
            case "Dossel": return Application.streamingAssetsPath + "/dossel.mp4";
            default: return null;
        }
    }

    private void OnVideoEnd(VideoPlayer vp)
    {
        isVideoPlaying = false;

        if (activeWhileVideoPlaying != null)
        {
            foreach (var obj in activeWhileVideoPlaying)
                if (obj != null)
                    obj.SetActive(false);
        }

        // Reativa UI
        if (upBar != null)
            upBar.SetActive(true);
        if (navBar != null)
            navBar.SetActive(true);

        if (navController != null)
            navController.ignoreWhileVideoPlaying = false;

        // Para vídeo e limpa RawImage
        videoPlayer.Stop();
        if (rawImage != null)
        {
            rawImage.texture = null;
            rawImage.gameObject.SetActive(false);
        }

        // Limpa targetTexture
        if (videoPlayer.targetTexture != null)
        {
            videoPlayer.targetTexture.Release();
            videoPlayer.targetTexture = null;
        }

        vp.loopPointReached -= OnVideoEnd;

        // Dispara imagem pós-vídeo da área
        if (postVideoImageManager != null && !string.IsNullOrEmpty(currentPlayingArea))
        {
            postVideoImageManager.ShowForArea(currentPlayingArea);
        }
        // 🔓 desbloqueia a área permanentemente
        if (!string.IsNullOrEmpty(currentPlayingArea))
        {
            VideoUnlockManager.Unlock(currentPlayingArea);
        }


        currentPlayingArea = null;

    }

    // Método para limpar o histórico de vídeos vistos (opcional, se quiser resetar manualmente)
    public void ResetVideosVistosNaSessao()
    {
        videosVistosNaSessao.Clear();
    }

    // Método para verificar se um vídeo já foi visto nesta sessão
    public bool FoiVistoNaSessao(string areaName)
    {
        return videosVistosNaSessao.Contains(areaName);
    }
}
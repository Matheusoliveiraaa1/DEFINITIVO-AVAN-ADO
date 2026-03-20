using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;
using System.IO;
using TMPro;

public class GlobalVideoPlayer : MonoBehaviour
{
    public static GlobalVideoPlayer Instance;




    [Header("VIDEOS DO MENU")]

    public GameObject videoMenuPanel;
    public string videoMenu1 = "video1.mp4";
    public string videoMenu2 = "video2.mp4";
    public string videoMenu3 = "video3.mp4";

    public void PlayMenuVideo1()
    {
        string path = Path.Combine(Application.streamingAssetsPath, videoMenu1);
        PlayVideo(path);
    }

    public void PlayMenuVideo2()
    {
        string path = Path.Combine(Application.streamingAssetsPath, videoMenu2);
        PlayVideo(path);
    }

    public void PlayMenuVideo3()
    {
        string path = Path.Combine(Application.streamingAssetsPath, videoMenu3);
        PlayVideo(path);
    }






    [Header("DEBUG")]
    public TMP_Text debugText;


    [Header("UI")]
    public GameObject videoPanel;
    public RawImage videoRawImage;
    public VideoPlayer videoPlayer;

    private bool isPlaying = false;
    private RenderTexture renderTexture;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (videoPanel != null)
            videoPanel.SetActive(false);

        if (videoPlayer == null)
            Debug.LogError("VideoPlayer NÃO atribuído!");

        if (videoRawImage == null)
            Debug.LogError("RawImage NÃO atribuída!");
    }

    public void PlayVideo(string videoPath)
    {
        DebugLog("PlayVideo chamado.");
        DebugLog("isPlaying: " + isPlaying);

        if (isPlaying || string.IsNullOrEmpty(videoPath))
            return;

        SetupRenderTexture();

        videoPanel.SetActive(true);

        videoPlayer.source = VideoSource.Url; // 🔥 IMPORTANTE
        videoPlayer.url = videoPath;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;

        videoPlayer.errorReceived += OnVideoError;
        videoPlayer.prepareCompleted += OnVideoPrepared;
        videoPlayer.loopPointReached += OnVideoFinished;

        videoPlayer.Prepare();

        isPlaying = true;
    }

    private void OnVideoPrepared(VideoPlayer vp)
    {

        DebugLog("Video preparado. Iniciando Play.");
        videoPlayer.prepareCompleted -= OnVideoPrepared;
        videoPlayer.Play();
    }

    private void OnVideoFinished(VideoPlayer vp)
    {
        DebugLog("Video terminou.");

        videoPlayer.loopPointReached -= OnVideoFinished;
        Cleanup();
    }

    private void OnVideoError(VideoPlayer vp, string message)
    {

        DebugLog("Erro: " + message);
        Debug.LogError("Erro no vídeo: " + message);
        Cleanup();
    }

    public void StopVideo()
    {
        if (!isPlaying)
            return;

        videoPlayer.Stop();
        Cleanup();
    }

    private void SetupRenderTexture()
    {
        if (renderTexture == null)
        {
            renderTexture = new RenderTexture(Screen.width, Screen.height, 0);
            renderTexture.Create();
        }

        videoPlayer.targetTexture = renderTexture;
        videoRawImage.texture = renderTexture;
    }

    private void Cleanup()
    {
        isPlaying = false;

        videoPlayer.Stop();

        videoRawImage.texture = null;

        if (videoPlayer.targetTexture != null)
        {
            videoPlayer.targetTexture.Release();
            videoPlayer.targetTexture = null;
        }

        videoPanel.SetActive(false);
    }

    public bool IsPlaying()
    {
        return isPlaying;
    }

    //private void Start() { string path = Application.streamingAssetsPath + "/Teste.mp4"; PlayVideo(path); }

    private void DebugLog(string msg)
    {
        if (debugText != null)
        {
            debugText.text += "\n[Video] " + msg;
        }
    }


    public void OpenVideoMenu()
    {
        if (videoMenuPanel != null)
            videoMenuPanel.SetActive(true);
    }

    public void CloseVideoMenu()
    {
        if (videoMenuPanel != null)
            videoMenuPanel.SetActive(false);
    }





}
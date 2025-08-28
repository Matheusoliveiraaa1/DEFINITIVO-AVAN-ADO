using UnityEngine;
using UnityEngine.Video;

public class VideoAutoPlayer : MonoBehaviour
{
    private VideoPlayer videoPlayer;

    void Start()
    {
        videoPlayer = GetComponent<VideoPlayer>();
        string videoPath = System.IO.Path.Combine(Application.streamingAssetsPath, "TESTE2.mp4");
        videoPlayer.url = videoPath;
    }

    void OnEnable()
    {
        TryPlayVideo();
    }

    private void TryPlayVideo()
    {
        // S� toca se autorizado e ainda n�o rodou
        if (VideoPlayState.IsAuthorized && !VideoPlayState.AlreadyPlayed)
        {
            videoPlayer.Play();
            VideoPlayState.AlreadyPlayed = true;
        }
    }
}

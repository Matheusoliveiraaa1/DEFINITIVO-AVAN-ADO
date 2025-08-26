using UnityEngine;
using UnityEngine.Video;

public class VideoAutoPlayer : MonoBehaviour
{
    void Start()
    {
        VideoPlayer videoPlayer = GetComponent<VideoPlayer>();
        string videoPath = System.IO.Path.Combine(Application.streamingAssetsPath, "TESTE2.mp4");
        videoPlayer.url = videoPath;
        videoPlayer.Play();
    }


    //// 



}
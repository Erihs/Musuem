using UnityEngine;
using UnityEngine.Video;

public class VideoControls : MonoBehaviour
{
    public VideoPlayer player;

    void Awake()
    {
        if (!player) player = GetComponent<VideoPlayer>();
    }

    public void Play()  { if (player) player.Play(); }
    public void Pause() { if (player) player.Pause(); }
    public void Stop()  { if (player) player.Stop(); }

    // Use this to play a new local clip
    public void SetClip(VideoClip clip, bool autoPlay = true)
    {
        if (!player) return;
        player.source = VideoSource.VideoClip;
        player.clip = clip;
        if (autoPlay) player.Play();
    }

    // Use this to play a URL (http/https or file://)
    public void SetURL(string url, bool autoPlay = true)
    {
        if (!player) return;
        player.source = VideoSource.Url;
        player.url = url;
        if (autoPlay) player.Play();
    }
}
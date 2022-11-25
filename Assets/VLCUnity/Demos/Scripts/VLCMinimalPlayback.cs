using UnityEngine;
using System;
using LibVLCSharp;
using UnityEngine.UI;

public class VLCMinimalPlayback : MonoBehaviour
{
    LibVLC _libVLC;
    MediaPlayer _mediaPlayer;
    const int seekTimeDelta = 5000;
    Texture2D tex = null;
    private bool playing;

    public bool Playing { get => playing;}

    void Awake()
    {

        TextureHelper.FlipTextures(transform);

        Core.Initialize(Application.dataPath);

        _libVLC = new LibVLC(enableDebugLogs: true);

        Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
    }

    public void SeekForward()
    {
        Debug.Log("[VLC] Seeking forward !");
        _mediaPlayer.SetTime(_mediaPlayer.Time + seekTimeDelta);
    }

    public void SeekBackward()
    {
        Debug.Log("[VLC] Seeking backward !");
        _mediaPlayer.SetTime(_mediaPlayer.Time - seekTimeDelta);
    }

    void OnDisable() 
    {
        _mediaPlayer?.Stop();
        _mediaPlayer?.Dispose();
        _mediaPlayer = null;

        _libVLC?.Dispose();
        _libVLC = null;
    }

    public void Play(string uri)
    {
        if (_mediaPlayer == null)
        {
            _mediaPlayer = new MediaPlayer(_libVLC);
        }

        playing = true;

        _mediaPlayer.SetVolume(volume);
        _mediaPlayer.Play(new Media(new Uri(uri)));
        
    }

    public void Stop ()
    {
        playing = false;
        _mediaPlayer?.Stop();

        tex = null;
    }
    int volume;
    public void SetVolume(float value)
    {
        volume = Mathf.RoundToInt(value * 100);
        if (_mediaPlayer != null)
            _mediaPlayer.SetVolume(volume);
    }

    void Update()
    {
        if(!playing) return;

        if (tex == null)
        {
            // If received size is not null, it and scale the texture
            uint i_videoHeight = 0;
            uint i_videoWidth = 0;

            _mediaPlayer.Size(0, ref i_videoWidth, ref i_videoHeight);
            var texptr = _mediaPlayer.GetTexture(i_videoWidth, i_videoHeight, out bool updated);
            if (i_videoWidth != 0 && i_videoHeight != 0 && updated && texptr != IntPtr.Zero)
            {
                Debug.Log("Creating texture with height " + i_videoHeight + " and width " + i_videoWidth);
                tex = Texture2D.CreateExternalTexture((int)i_videoWidth,
                    (int)i_videoHeight,
                    TextureFormat.RGBA32,
                    true,
                    true,
                    texptr);
                var material = new Material(Shader.Find("Unlit/Texture"));
                material.mainTexture = tex;
                GetComponent<Image>().material = material;
                GetComponent<AspectRatioFitter>().aspectRatio = (float)i_videoWidth / i_videoHeight;
            }
        }
        else if (tex != null)
        {
            var texptr = _mediaPlayer.GetTexture((uint)tex.width, (uint)tex.height, out bool updated);
            if (updated)
            {
                tex.UpdateExternalTexture(texptr);
            }
        }
    }
}

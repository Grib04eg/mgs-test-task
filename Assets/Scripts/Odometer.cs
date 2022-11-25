using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;
using System.Text;
using System.IO;

public class Odometer : MonoBehaviour
{
    public GribSockets socket;
    string streamAddress;
    [SerializeField] Image statusImage;
    [SerializeField] TextMeshProUGUI OdometerCounter;
    [SerializeField] SoundsPlayer soundsPlayer;
    [SerializeField] AudioSource musicPlayer;

    [Header("Stream")]
    [SerializeField] VLCMinimalPlayback streamPlayer;
    [SerializeField] RectTransform screenTransform;
    [SerializeField] TextMeshProUGUI startButtonText;

    [Header("Menu")]
    [SerializeField] CanvasGroup menuGroup;
    [SerializeField] RectTransform menuTransform;

    [SerializeField] Toggle soundToggle;
    [SerializeField] Toggle musicToggle;
    [SerializeField] TMP_InputField ipField;
    [SerializeField] TMP_InputField portField;
    [SerializeField] TMP_InputField streamField;
    [SerializeField] Slider volumeSlider;

    void Start()
    {
        LoadSettings();
    }
    List<char> digits = new List<char>() { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };
    IEnumerator OdometerAnimation(string text)
    {
        var delay = new WaitForSeconds(0.02f);
        string currentText = OdometerCounter.text;
        while (currentText != text)
        {
            for (int i = text.Length - 1; i >= 0; i--)
            {
                if (currentText[i] == text[i])
                    continue;
                int digit = digits.FindIndex((a)=> { return a == currentText[i]; });
                if (++digit == 10)
                    digit = 0;
                
                StringBuilder sb = new StringBuilder(currentText);
                sb[i] = digits[digit];
                currentText = sb.ToString();
                break;
            }
            OdometerCounter.text = currentText;
            yield return delay;
        }
    }

    void LoadSettings()
    {
        bool sound = PlayerPrefs.GetInt("settings/sound", 1) == 1;
        soundToggle.isOn = sound;
        soundsPlayer.SetActive(sound);

        bool music = PlayerPrefs.GetInt("settings/music", 1) == 1;
        musicToggle.isOn = music;
        if (music)
            musicPlayer.Play();
        else
            musicPlayer.Stop();

        var address = ReadConfig();
        //string ip = PlayerPrefs.GetString("settings/serverip", "185.246.65.199");
        //ipField.text = ip;
        //string port = PlayerPrefs.GetString("settings/serverport", "9090/ws");
        //portField.text = port;
        Connect(address);


        streamAddress = PlayerPrefs.GetString("settings/stream", "rtsp://localhost:8554/stream");
        streamField.text = streamAddress;

        float volume = PlayerPrefs.GetFloat("settings/volume", 0.46f);
        volumeSlider.value = volume;
        soundsPlayer.SetVolume(volume);
        musicPlayer.volume = volume;
        streamPlayer.SetVolume(volume);
    }

    void Connect(string address)
    {
        socket = new GribSockets(address, OnConnected, OnDisconnected);
        socket.Subscribe("odometer_val", (msg) => { StartCoroutine(OdometerAnimation(msg.value.ToString("000000000000.0"))); });
        socket.Subscribe("currentOdometer", (msg) => { StartCoroutine(OdometerAnimation(msg.odometer.ToString("000000000000.0"))); });
    }

    string ReadConfig()
    {
        StreamReader inputStream = new StreamReader(Application.dataPath+"/config.txt");
        string address = "ws://";
        while (!inputStream.EndOfStream)
        {
            string line = inputStream.ReadLine();
            if (line.StartsWith("Адрес сервера: "))
            {
                string ip = line.Remove(0, 15);
                ipField.text = ip;
                address += ip+":";
                Debug.Log("("+ip+")");
            }
            if (line.StartsWith("Порт: "))
            {
                string port = line.Remove(0, 6);
                portField.text = port;
                address += port;
                Debug.Log("(" + port + ")");
            }
        }
        inputStream.Close();
        return address;
    }

    private void OnDisconnected(WebSocketSharp.CloseEventArgs e)
    {
        Debug.Log("Disconnected from server");
        statusImage.color = Color.red;
    }

    private void OnConnected(System.EventArgs e)
    {
        Debug.Log("Connected to server");
        statusImage.color = Color.green;
    }

    public void StartStopStream()
    {
        SoundsPlayer.PlaySound(SoundsPlayer.SoundType.Click);
        if (!streamPlayer.Playing)
        {
            screenTransform.DOScale(new Vector3(-1, -1, 1), 0.2f).SetEase(Ease.InCirc);
            streamPlayer.Play(streamAddress);
            startButtonText.text = "Stop";
            if (musicToggle.isOn)
                musicPlayer.Pause();
        } else
        {
            screenTransform.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InCirc);
            streamPlayer.Stop();
            startButtonText.text = "Start";
            if (musicToggle.isOn)
                musicPlayer.Play();
        }
    }

    public void ToggleMenu()
    {
        SoundsPlayer.PlaySound(SoundsPlayer.SoundType.Click);
        if (!menuGroup.interactable)
        {
            menuGroup.interactable = true;
            menuTransform.DOSizeDelta(new Vector2(320f, 400f), 0.2f).SetEase(Ease.OutCirc);
        } 
        else
        {
            menuGroup.interactable = false;
            menuTransform.DOSizeDelta(new Vector2(320f, 0), 0.2f).SetEase(Ease.OutCirc);
        }
    }

    public void ToggleSound()
    {
        var sound = soundToggle.isOn;
        PlayerPrefs.SetInt("settings/sound", sound ? 1 : 0);
        soundsPlayer.SetActive(sound);
    }

    public void ToggleMusic()
    {
        var music = musicToggle.isOn;
        PlayerPrefs.SetInt("settings/music", music ? 1 : 0);
        if (music)
            musicPlayer.Play();
        else
            musicPlayer.Stop();
    }

    public void EditServerAddress()
    {
        string ip = ipField.text;
        string port = portField.text;

        File.WriteAllText(Application.dataPath + "/config.txt", "Адрес сервера: " + ip + "\n" + "Порт: " + port);
        if (socket.IsConnected)
            socket.Disconnect();
        Connect("ws://" + ip + ":" + port);
    }

    public void EditStreamAddress()
    {
        streamAddress = streamField.text;
        PlayerPrefs.SetString("settings/stream", streamAddress);
    }

    public void VolumeChanged()
    {
        var volume = volumeSlider.value;
        PlayerPrefs.SetFloat("settings/volume", volume);
        soundsPlayer.SetVolume(volume);
        musicPlayer.volume = volume;
        streamPlayer.SetVolume(volume);
    }

    public void ManualRequestOdometer()
    {
        socket.SendMessage("getCurrentOdometer");
    }

    public void ToggleCheckbox()
    {
        SoundsPlayer.PlaySound(SoundsPlayer.SoundType.Click);
    }
}

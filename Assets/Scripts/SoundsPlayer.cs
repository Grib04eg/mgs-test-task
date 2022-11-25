using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundsPlayer : MonoBehaviour
{
    bool active;
    public enum SoundType
    {
        Click,

    }

    [SerializeField] AudioClip[] audioClips;

    static SoundsPlayer instance;
    AudioSource audioSource;
    private void Awake()
    {
        if (instance)
            Destroy(gameObject);
        else
        {
            instance = this;
            audioSource = GetComponent<AudioSource>();
        }
    }
    public static void PlaySound(SoundType type)
    {
        if (instance.active)
            instance.audioSource.PlayOneShot(instance.audioClips[(int)type]);
    }

    public void SetVolume(float value)
    {
        audioSource.volume = value;
    }

    public void SetActive(bool state)
    {
        active = state;
    }
}

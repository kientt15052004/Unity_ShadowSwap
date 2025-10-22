using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    // Variable to hold the audio source component
    public AudioSource audioSource;
    // Variable to hold the background music clip
    public AudioClip backgroundMusic;
    void Start()
    {
        audioSource.clip = backgroundMusic;
        audioSource.Play();
        audioSource.loop = true;
    }
    public void PlaySFX(AudioClip sfxClip)
    {
        audioSource.PlayOneShot(sfxClip);
    }
}

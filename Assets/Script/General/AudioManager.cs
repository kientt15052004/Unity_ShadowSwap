using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Music")]
    public AudioSource musicSource;
    public AudioClip backgroundMusic;

    [Header("SFX")]
    public AudioSource sfxSource;
    public AudioClip jumpClip;
    public AudioClip landClip;
    public AudioClip hurtClip;
    public AudioClip[] footstepClips;

    [Header("Volumes")]
    [Range(0f, 1f)] public float musicVolume = 1f;
    [Range(0f, 1f)] public float sfxVolume = 1f;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        PlayMusic(backgroundMusic);
    }

    // Phát nhạc nền (loop)
    public void PlayMusic(AudioClip clip)
    {
        if (clip == null) return;
        musicSource.clip = clip;
        musicSource.volume = musicVolume;
        musicSource.loop = true;
        musicSource.Play();
    }

    // Dừng nhạc nền
    public void StopMusic()
    {
        musicSource.Stop();
    }

    // Phát hiệu ứng âm thanh (1 lần)
    public void PlaySFX(AudioClip sfxClip)
    {
        if (sfxClip == null) return;
        sfxSource.volume = sfxVolume;
        sfxSource.PlayOneShot(sfxClip);
    }

    // Phát hiệu ứng âm thanh với pitch tùy chỉnh
    public void PlaySFX(AudioClip sfxClip, float pitch)
    {
        if (sfxClip == null) return;
        sfxSource.pitch = pitch;
        sfxSource.volume = sfxVolume;
        sfxSource.PlayOneShot(sfxClip);
        sfxSource.pitch = 1f; // Reset về 1
    }

    // Các method chuyên biệt cho từng loại âm thanh
    public void PlayFootstep()
    {
        if (footstepClips.Length > 0)
        {
            AudioClip clip = footstepClips[Random.Range(0, footstepClips.Length)];
            float randomPitch = Random.Range(0.9f, 1.1f);
            PlaySFX(clip, randomPitch);
        }
    }

    public void PlayJump()
    {
        PlaySFX(jumpClip);
    }

    public void PlayLand()
    {
        PlaySFX(landClip);
    }

    public void PlayHurt()
    {
        PlaySFX(hurtClip);
    }

    // Thay đổi âm lượng runtime
    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        musicSource.volume = musicVolume;
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
    }

    // Tắt/bật nhanh
    public void ToggleMusic(bool isOn)
    {
        musicSource.mute = !isOn;
    }

    public void ToggleSFX(bool isOn)
    {
        sfxSource.mute = !isOn;
    }
}
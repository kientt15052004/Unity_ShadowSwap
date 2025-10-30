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
    public AudioClip healClip;
    public AudioClip shadowSummonClip;
    public AudioClip shadowDisappearClip;
    public AudioClip shadowSwapClip;
    public AudioClip coinClip;
    public AudioClip keyClip;
    public AudioClip unlockChestClip;
    public AudioClip unlockGateClip;

    public AudioClip[] footstepClips;

    [Header("Volumes")]
    [Range(0f, 1f)] public float musicVolume = 0.5f;
    [Range(0f, 1f)] public float sfxVolume = 1f;

    private void Awake()
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

    private void Start()
    {
    // Load volume từ PlayerPrefs
    musicVolume = PlayerPrefs.GetFloat("MusicVolume", 0.5f);
    sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);

    // Load trạng thái âm thanh (1 = bật, 0 = tắt)
    bool musicEnabled = PlayerPrefs.GetInt("MusicEnabled", 1) == 1;
    bool sfxEnabled = PlayerPrefs.GetInt("SFXEnabled", 1) == 1;

    // Áp dụng âm lượng & trạng thái mute
    ApplyVolumes();
    musicSource.mute = !musicEnabled;
    sfxSource.mute = !sfxEnabled;

    // Phát nhạc nền
    PlayMusic(backgroundMusic);
    }


    private void ApplyVolumes()
    {
        musicSource.volume = musicVolume;
        sfxSource.volume = sfxVolume;
    }

    // Phát nhạc nền (loop)
    public void PlayMusic(AudioClip clip)
    {
        if (clip == null) return;
        musicSource.clip = clip;
        musicSource.loop = true;
        musicSource.volume = musicVolume;
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
        sfxSource.PlayOneShot(sfxClip, sfxVolume);
    }

    // Phát hiệu ứng âm thanh với pitch tùy chỉnh
    public void PlaySFX(AudioClip sfxClip, float pitch)
    {
        if (sfxClip == null) return;
        float originalPitch = sfxSource.pitch;
        sfxSource.pitch = pitch;
        sfxSource.PlayOneShot(sfxClip, sfxVolume);
        sfxSource.pitch = originalPitch;
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

    public void PlayJump() => PlaySFX(jumpClip);
    public void PlayLand() => PlaySFX(landClip);
    public void PlayHurt() => PlaySFX(hurtClip);
    public void PlayHeal() => PlaySFX(healClip);
    public void PlayShadowSummon() => PlaySFX(shadowSummonClip);
    public void PlayShadowDisappear() => PlaySFX(shadowDisappearClip);
    public void PlayShadowSwap() => PlaySFX(shadowSwapClip);
    public void PlayCoin() => PlaySFX(coinClip);
    public void PlayKey() => PlaySFX(keyClip);
    public void PlayUnlockChest() => PlaySFX(unlockChestClip);
    public void PlayUnlockGate() => PlaySFX(unlockGateClip);

    // Thay đổi âm lượng runtime
    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        musicSource.volume = musicVolume;
        PlayerPrefs.SetFloat("MusicVolume", musicVolume);
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
    }

    // Tắt/bật nhanh
    public void ToggleMusic(bool isOn)
    {
        musicSource.mute = !isOn;
        PlayerPrefs.SetInt("MusicEnabled", isOn ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void ToggleSFX(bool isOn)
    {
        sfxSource.mute = !isOn;
        PlayerPrefs.SetInt("SFXEnabled", isOn ? 1 : 0);
        PlayerPrefs.Save();
    }   

    private void OnDestroy()
    {
        // Lưu lại khi thoát scene
        PlayerPrefs.Save();
    }
}

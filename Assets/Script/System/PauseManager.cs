using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] GameObject pauseCanvasRoot;   // PauseCanvas (root)
    [SerializeField] GameObject pausePanel;        // Panel chính có 4 nút: Resume, Restart, Options, Quit
    [SerializeField] GameObject optionsPanel;      // Panel Options chứa thanh trượt âm lượng
    [SerializeField] Image overlayBlocker;         // Image full-screen chặn raycast

    [Header("Options UI")]
    [SerializeField] Slider musicSlider;
    [SerializeField] Slider sfxSlider;
    [SerializeField] Toggle musicToggle;
    [SerializeField] Toggle sfxToggle;    

    [Header("Behavior")]
    [SerializeField] KeyCode toggleKey = KeyCode.Escape;
    [SerializeField] string menuSceneName = "LevelSelectScreen";
    [SerializeField] bool pauseAudio = true;

    [Header("Scripts cần khóa khi Pause (kéo thả vào đây)")]
    [SerializeField] List<Behaviour> disableOnPause = new List<Behaviour>();

    float _prevTimeScale = 1f;
    float _prevFixedDelta;
    bool _isPaused = false;

    void Awake()
    {
        _prevFixedDelta = Time.fixedDeltaTime;
        SetPauseUI(false);

        if (overlayBlocker) overlayBlocker.transform.SetAsFirstSibling();

        // Gắn sự kiện cho thanh trượt âm thanh nếu có
        if (musicSlider)
        {
            musicSlider.onValueChanged.AddListener(value =>
            {
                if (AudioManager.Instance)
                    AudioManager.Instance.SetMusicVolume(value);
            });
        }

        if (sfxSlider)
        {
            sfxSlider.onValueChanged.AddListener(value =>
            {
                if (AudioManager.Instance)
                    AudioManager.Instance.SetSFXVolume(value);
            });
        }
        if (musicToggle)
        {
            musicToggle.onValueChanged.AddListener(isOn =>
            {
                if (AudioManager.Instance)
                    AudioManager.Instance.ToggleMusic(isOn);
            });
        }

        if (sfxToggle)
        {
            sfxToggle.onValueChanged.AddListener(isOn =>
            {
                if (AudioManager.Instance)
                    AudioManager.Instance.ToggleSFX(isOn);
            });
        }
    }

    void Start()
    {
        // Gán giá trị thanh trượt bằng giá trị hiện tại trong AudioManager
        if (AudioManager.Instance)
        {
            if (musicSlider) musicSlider.value = AudioManager.Instance.musicVolume;
            if (sfxSlider) sfxSlider.value = AudioManager.Instance.sfxVolume;
            if (musicToggle) musicToggle.isOn = !AudioManager.Instance.musicSource.mute;
            if (sfxToggle) sfxToggle.isOn = !AudioManager.Instance.sfxSource.mute;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
            TogglePause();
    }

    public void TogglePause()
    {
        if (_isPaused) Resume();
        else Pause();
    }

    public void Pause()
    {
        if (_isPaused) return;
        _isPaused = true;

        _prevTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        Time.fixedDeltaTime = 0f;

        foreach (var b in disableOnPause) if (b) b.enabled = false;

        SetPauseUI(true);

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void Resume()
    {
        if (!_isPaused) return;
        _isPaused = false;

        Time.timeScale = _prevTimeScale <= 0f ? 1f : _prevTimeScale;
        Time.fixedDeltaTime = _prevFixedDelta;

        foreach (var b in disableOnPause) if (b) b.enabled = true;

        SetPauseUI(false);
        // Cursor.visible = false;
        // Cursor.lockState = CursorLockMode.Locked;
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f;
        Time.fixedDeltaTime = _prevFixedDelta;
        if (pauseAudio) AudioListener.pause = false;

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void OpenOptions()
    {
        if (!optionsPanel || !pausePanel) return;
        optionsPanel.SetActive(true);
        pausePanel.SetActive(false);

        // Cập nhật lại slider khi mở options
        if (AudioManager.Instance)
        {
            if (musicSlider) musicSlider.value = AudioManager.Instance.musicVolume;
            if (sfxSlider) sfxSlider.value = AudioManager.Instance.sfxVolume;
            if (musicToggle) musicToggle.isOn = !AudioManager.Instance.musicSource.mute;
            if (sfxToggle) sfxToggle.isOn = !AudioManager.Instance.sfxSource.mute;
        }
    }

    public void CloseOptions()
    {
        if (!optionsPanel || !pausePanel) return;
        optionsPanel.SetActive(false);
        pausePanel.SetActive(true);
    }

    public void QuitToMenu()
    {
        Time.timeScale = 1f;
        Time.fixedDeltaTime = _prevFixedDelta;
        if (pauseAudio) AudioListener.pause = false;

        SceneManager.LoadScene(menuSceneName);
    }

    void SetPauseUI(bool show)
    {
        if (pauseCanvasRoot) pauseCanvasRoot.SetActive(show);
        if (overlayBlocker) overlayBlocker.raycastTarget = show;

        if (pausePanel) pausePanel.SetActive(show);
        if (show && optionsPanel) optionsPanel.SetActive(false);
    }

    void OnDestroy()
    {
        if (_isPaused)
        {
            Time.timeScale = 1f;
            Time.fixedDeltaTime = _prevFixedDelta;
            if (pauseAudio) AudioListener.pause = false;
        }
    }
}

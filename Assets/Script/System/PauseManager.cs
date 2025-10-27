using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] GameObject pauseCanvasRoot;   // PauseCanvas (root)
    [SerializeField] GameObject pausePanel;        // Panel có 3 nút
    [SerializeField] GameObject optionsPanel;      // Panel Options (nếu dùng)
    [SerializeField] Image overlayBlocker;         // Image full-screen chặn raycast

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

        // (An toàn) Đảm bảo OverlayBlocker nằm first-sibling
        if (overlayBlocker) overlayBlocker.transform.SetAsFirstSibling();
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
        if (pauseAudio) AudioListener.pause = true;

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
        if (pauseAudio) AudioListener.pause = false;

        foreach (var b in disableOnPause) if (b) b.enabled = true;

        SetPauseUI(false);
        // Nếu game cần lock chuột trở lại, bật 2 dòng sau:
        // Cursor.visible = false;
        // Cursor.lockState = CursorLockMode.Locked;
    }

    public void OpenOptions()
    {
        if (!optionsPanel || !pausePanel) return;
        optionsPanel.SetActive(true);
        pausePanel.SetActive(false);
    }

    public void CloseOptions()
    {
        if (!optionsPanel || !pausePanel) return;
        optionsPanel.SetActive(false);
        pausePanel.SetActive(true);
    }

    public void QuitToMenu()
    {
        // Khôi phục thời gian trước khi rời scene
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

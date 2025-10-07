using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class SceneNavigationManager : MonoBehaviour
{
    [Header("Buttons Reference")]
    public Button startButton;
    public Button optionsButton;
    public Button quitButton;

    [Header("Scene Names")]
    public string levelSelectScene = "LevelSelect";
    public string optionsScene = "Options";

    [Header("Fade Settings")]
    public CanvasGroup fadePanel;
    public float fadeDuration = 1f;

    [Header("Audio (Optional)")]
    public AudioSource buttonClickSound;

    void Start()
    {
        // Gắn functions vào buttons
        if (startButton != null)
        {
            startButton.onClick.AddListener(OnStartGameClick);
        }
        else
        {
            Debug.LogWarning("Start Button chưa được gắn!");
        }

        if (optionsButton != null)
        {
            optionsButton.onClick.AddListener(OnOptionsClick);
        }

        if (quitButton != null)
        {
            quitButton.onClick.AddListener(OnQuitClick);
        }
        else
        {
            Debug.LogWarning("Quit Button chưa được gắn!");
        }

        // Fade in khi vào scene
        if (fadePanel != null)
        {
            StartCoroutine(FadeIn());
        }
    }

    // Khi click START GAME
    public void OnStartGameClick()
    {
        Debug.Log("Loading Level Select...");
        PlayClickSound();
        StartCoroutine(LoadSceneWithFade(levelSelectScene));
    }

    // Khi click OPTIONS
    public void OnOptionsClick()
    {
        Debug.Log("Loading Options...");
        PlayClickSound();

        // Tạm thời show message vì chưa làm scene Options
        Debug.Log("Options scene đang được phát triển!");

        // Uncomment dòng này khi đã làm xong scene Options
        // StartCoroutine(LoadSceneWithFade(optionsScene));
    }

    // Khi click QUIT
    public void OnQuitClick()
    {
        Debug.Log("Quitting game...");
        PlayClickSound();
        StartCoroutine(QuitGameWithFade());
    }

    // Fade in khi vào scene
    IEnumerator FadeIn()
    {
        fadePanel.alpha = 1f;
        fadePanel.blocksRaycasts = true;

        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            fadePanel.alpha = 1f - (timer / fadeDuration);
            yield return null;
        }

        fadePanel.alpha = 0f;
        fadePanel.blocksRaycasts = false;
    }

    // Fade out và load scene mới
    IEnumerator LoadSceneWithFade(string sceneName)
    {
        fadePanel.blocksRaycasts = true;
        float timer = 0f;

        // Fade to black
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            fadePanel.alpha = timer / fadeDuration;
            yield return null;
        }

        fadePanel.alpha = 1f;

        // Load scene
        SceneManager.LoadScene(sceneName);
    }

    // Quit game với fade
    IEnumerator QuitGameWithFade()
    {
        fadePanel.blocksRaycasts = true;
        float timer = 0f;

        // Fade to black
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            fadePanel.alpha = timer / fadeDuration;
            yield return null;
        }

        // Quit
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // Play sound effect (optional)
    void PlayClickSound()
    {
        if (buttonClickSound != null)
        {
            buttonClickSound.Play();
        }
    }
}
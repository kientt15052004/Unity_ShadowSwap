using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("Portal UI")]
    public GameObject portalOptionsPanel;
    public Portal currentPortal;

    [Header("System Warning UI")] // MỚI
    public TextMeshProUGUI warningText;
    public CanvasGroup warningCanvasGroup;
    [SerializeField] private float warningDuration = 2.0f; // Thời gian hiển thị (s)
    [SerializeField] private float warningFadeTime = 0.5f; // Thời gian Fade In/Out (s) 


    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    // --- CÁC HÀM HIỂN THỊ/ẨN UI ---
    // Thay thế logic này trong UIManager.cs:
    public void ShowPortalOptions(Portal portal)
    {
        currentPortal = portal;
        // CHỈ BẬT PANEL QUYẾT ĐỊNH
        portalOptionsPanel.SetActive(true);

        // Đảm bảo PausePanel của PauseManager bị tắt nếu nó đang bật (để tránh trùng lặp UI)
        // Tùy chọn: Thêm một tham chiếu đến PauseManager để tắt PausePanel nếu cần
        // Ví dụ: if (PauseManager.Instance.pausePanel.activeSelf) PauseManager.Instance.pausePanel.SetActive(false);
    }
    // HÀM MỚI: HIỂN THỊ CẢNH BÁO
    public void ShowWarning(string message)
    {
        if (warningText == null || warningCanvasGroup == null)
        {
            Debug.LogError("Chưa gán WarningText hoặc CanvasGroup trong UIManager!");
            return;
        }

        // Dừng coroutine cũ nếu có (để tránh lỗi khi cảnh báo liên tục)
        StopAllCoroutines();

        warningText.text = message;
        warningText.gameObject.SetActive(true);
        warningCanvasGroup.alpha = 0f;

        StartCoroutine(FadeWarningText());
    }

    private IEnumerator FadeWarningText()
    {
        // 1. FADE IN
        float timer = 0f;
        while (timer < warningFadeTime)
        {
            // Sử dụng Time.unscaledDeltaTime để cảnh báo vẫn hiển thị khi game bị Pause
            warningCanvasGroup.alpha = Mathf.Lerp(0f, 1f, timer / warningFadeTime);
            timer += Time.unscaledDeltaTime;
            yield return null;
        }
        warningCanvasGroup.alpha = 1f;

        // 2. STAY (Giữ nguyên độ sáng)
        yield return new WaitForSecondsRealtime(warningDuration);

        // 3. FADE OUT
        timer = 0f;
        while (timer < warningFadeTime)
        {
            warningCanvasGroup.alpha = Mathf.Lerp(1f, 0f, timer / warningFadeTime);
            timer += Time.unscaledDeltaTime;
            yield return null;
        }
        warningCanvasGroup.alpha = 0f;
        warningText.gameObject.SetActive(false);
    }
    public void HidePortalOptions()
    {
        portalOptionsPanel.SetActive(false);
        currentPortal = null;
    }

    // --- HÀM TRUNG GIAN (CHO NÚT BẤM) ---
    public void CallContinueToNextLevel()
    {
        if (currentPortal != null)
            currentPortal.ContinueToNextLevel();
        else { Time.timeScale = 1f; HidePortalOptions(); }
    }
    public void CallGoToMainMenu()
    {
        if (currentPortal != null)
            currentPortal.GoToMainMenu();
        else { Time.timeScale = 1f; UnityEngine.SceneManagement.SceneManager.LoadScene("LevelSelectScreen"); HidePortalOptions(); }
    }

}
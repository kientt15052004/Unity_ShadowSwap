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

    [Header("End Game UI")]
    public GameObject winScreenPanel;


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

    public void HidePortalOptions()
    {
        portalOptionsPanel.SetActive(false);
        currentPortal = null;
    }

    public void ShowWinScreen() { winScreenPanel.SetActive(true); }

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
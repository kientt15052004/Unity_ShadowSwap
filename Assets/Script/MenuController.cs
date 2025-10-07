using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Controller đơn giản cho Start Screen
/// Các hàm PUBLIC để gắn vào button qua Inspector
/// </summary>
public class MenuController : MonoBehaviour
{
    [Header("Scene Names")]
    public string levelSelectScene = "LevelSelectScreen";
    public string optionsScene = "Options";

    void Start()
    {
        Debug.Log("=== MENU CONTROLLER START ===");
    }

    // ============================================
    // CÁC HÀM NÀY SẼ ĐƯỢC GỌI TỪ BUTTONS
    // ============================================

    /// <summary>
    /// Gọi khi click button START GAME
    /// </summary>
    public void StartGame()
    {
        Debug.Log("Loading Level Select Screen...");
        LoadScene(levelSelectScene);
    }

    /// <summary>
    /// Gọi khi click button OPTIONS
    /// </summary>
    public void OpenOptions()
    {
        Debug.Log("Opening Options...");
        // Tạm thời chưa làm scene Options
        Debug.Log("Options đang được phát triển!");

        // Uncomment khi đã có scene Options:
        // LoadScene(optionsScene);
    }

    /// <summary>
    /// Gọi khi click button QUIT
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("Quitting game...");

#if UNITY_EDITOR
            // Trong Unity Editor: Stop play mode
            UnityEditor.EditorApplication.isPlaying = false;
#else
        // Trong build: Thoát game
        Application.Quit();
#endif
    }

    // ============================================
    // HÀM PHỤ TRỢ
    // ============================================

    /// <summary>
    /// Load scene với kiểm tra
    /// </summary>
    void LoadScene(string sceneName)
    {
        // Kiểm tra scene có trong Build Settings không
        if (Application.CanStreamedLevelBeLoaded(sceneName))
        {
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            Debug.LogError($"KHÔNG thể load scene '{sceneName}'!");
            Debug.LogError("Hãy thêm scene vào Build Settings: File → Build Settings");
        }
    }
}
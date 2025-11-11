using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Portal : MonoBehaviour
{
    [Header("Tên Scene sẽ chuyển đến")]
    public string targetScene;

    [Header("Tên điểm spawn trong Scene mới")]
    public string targetSpawnPoint;

    [Header("Cần Key Đỏ để đi qua?")]
    public bool requiresRedKey = false;

    [Header("Đây có phải là Cổng Kết thúc Game?")]
    public bool isFinalLevel = false;

    [Header("World mở khóa sau khi đi qua portal này")]
    public int worldIndexToUnlock = 0;


    private bool isTransitioning = false;
    private GameObject playerObject;
    private PlayerMove playerMoveScript;
    private PauseManager pauseManager; // MỚI: Dùng để kiểm soát Pause

    void Start()
    {
        // Lấy tham chiếu đến PauseManager (Giả sử có 1 instance trong Scene)
        pauseManager = FindObjectOfType<PauseManager>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && !isTransitioning)
        {
            playerMoveScript = collision.GetComponent<PlayerMove>();
            if (playerMoveScript == null) return;

            // 1. KIỂM TRA CHÌA KHÓA
            if (requiresRedKey && playerMoveScript.keyRedCollected <= 0)
            {
                Debug.Log("Cổng đã bị khóa! Cần Chìa khóa Đỏ.");
                return;
            }

            isTransitioning = true;
            playerObject = collision.gameObject;

            // 2. KHÓA INPUT CỦA PLAYER (Chỉ khóa PlayerMove, PauseManager sẽ lo TimeScale)
            Rigidbody2D playerRb = playerObject.GetComponent<Rigidbody2D>();
            if (playerRb != null) playerRb.velocity = Vector2.zero;
            playerMoveScript.enabled = false;

            // 3. LOGIC CỔNG CUỐI HAY CỔNG BÌNH THƯỜNG
            if (isFinalLevel)
            {
                StartCoroutine(FinalLevelStop(playerObject, playerMoveScript));
            }
            else
            {
                // LOGIC CỔNG CHUYỂN MÀN BÌNH THƯỜNG (PAUSE & OPTIONS)

                if (pauseManager != null)
                {
                    pauseManager.Pause(); // DỪNG GAME BẰNG PAUSE MANAGER
                }
                else
                {
                    Time.timeScale = 0f; // Dự phòng
                }

                if (UIManager.Instance != null)
                {
                    UIManager.Instance.ShowPortalOptions(this); // HIỂN THỊ MENU OPTIONS
                }
                else
                {
                    Debug.LogError("UIManager.Instance không tìm thấy!");
                }
            }
        }
    }

    // -----------------------------------------------------------
    // HÀM CÔNG KHAI DÀNH CHO BUTTON CỦA UI
    // -----------------------------------------------------------

    public void ContinueToNextLevel()
    {
        if (UIManager.Instance != null) UIManager.Instance.HidePortalOptions();

        // Mở khóa game bằng Resume
        if (pauseManager != null)
        {
            pauseManager.Resume();
        }
        else
        {
            Time.timeScale = 1f; // Dự phòng
        }

        if (requiresRedKey) playerMoveScript?.UseRedKey();
        // === MỞ KHÓA MÀN CHƠI TIẾP THEO ===
        if (worldIndexToUnlock > 0)
        {
            int currentUnlocked = PlayerPrefs.GetInt("WorldUnlocked", 1);
            if (worldIndexToUnlock > currentUnlocked)
                PlayerPrefs.SetInt("WorldUnlocked", worldIndexToUnlock);
        }
        StartCoroutine(Transition(playerObject));
    }

    public void GoToMainMenu()
    {
        // Về menu bằng hàm của PauseManager
        if (pauseManager != null)
        {
            pauseManager.QuitToMenu();
        }
        else
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("LevelSelectScreen");
        }
    }

    // -----------------------------------------------------------
    // COROUTINES
    // -----------------------------------------------------------

    private IEnumerator Transition(GameObject player)
    {
        DontDestroyOnLoad(player);
        SceneManager.LoadScene(targetScene);
        yield return null;

        GameObject spawnPoint = GameObject.Find(targetSpawnPoint);
        if (spawnPoint != null)
        {
            player.transform.position = spawnPoint.transform.position;
        }

        // Bật lại input Player (Cần thiết vì PlayerMove bị tắt thủ công)
        if (playerMoveScript != null)
        {
            playerMoveScript.enabled = true;
        }

        isTransitioning = false;
    }

    private IEnumerator FinalLevelStop(GameObject player, PlayerMove moveScript)
    {
        if (requiresRedKey) moveScript?.UseRedKey();

        yield return new WaitForSeconds(0.5f);

        // HIỂN THỊ MÀN HÌNH HOÀN THÀNH
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowWinScreen();
        }

        isTransitioning = false;
        yield break;
    }
}
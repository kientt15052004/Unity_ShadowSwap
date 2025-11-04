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
    public bool isFinalLevel = false; // Đánh dấu nếu đây là map cuối

    private bool isTransitioning = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && !isTransitioning)
        {
            // Kiểm tra Key Đỏ nếu yêu cầu
            if (requiresRedKey)
            {
                PlayerMove playerMove = collision.GetComponent<PlayerMove>();

                if (playerMove != null)
                {
                    // Kiểm tra số lượng Key Đỏ
                    if (playerMove.keyRedCollected > 0)
                    {
                        // Đủ key: Sử dụng 1 Key Đỏ và tiếp tục dịch chuyển
                        playerMove.UseRedKey();
                        isTransitioning = true;
                        StartCoroutine(Transition(collision.gameObject));
                        return;
                    }
                    else
                    {
                        // Không đủ key
                        Debug.Log("Cổng đã bị khóa! Cần Chìa khóa Đỏ.");
                        return;
                    }
                }
            }

            // Dịch chuyển nếu không cần Key hoặc kiểm tra Key không được bật
            isTransitioning = true;
            StartCoroutine(Transition(collision.gameObject));
        }
    }

    private IEnumerator Transition(GameObject player)
    {
        // (Tuỳ chọn) Hiệu ứng fade hoặc delay
        yield return new WaitForSeconds(0.5f);

        // LOGIC MỚI: DỪNG KHI ĐẾN CỔNG CUỐI
        if (isFinalLevel)
        {
            Debug.Log("🎉 HOÀN THÀNH TRÒ CHƠI! Dừng dịch chuyển tại cổng cuối.");

            // 1. Tạm dừng Player (Ngăn chuyển động vật lý)
            Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
            if (playerRb != null)
            {
                playerRb.velocity = Vector2.zero;
                // Tùy chọn: Đảm bảo trọng lực không kéo nhân vật đi
                playerRb.isKinematic = true;
            }

            // 2. KHÓA INPUT (Vô hiệu hóa script PlayerMove) <<< BỔ SUNG QUAN TRỌNG
            PlayerMove playerMove = player.GetComponent<PlayerMove>();
            if (playerMove != null)
            {
                playerMove.enabled = false;
            }

            // Dừng coroutine tại đây, ngăn không cho LoadScene được gọi.
            isTransitioning = false;
            yield break;
        }

        // GIỮ LOGIC CHUYỂN SCENE THÔNG THƯỜNG

        // Giữ lại Player khi chuyển Scene
        DontDestroyOnLoad(player);

        // Tải Scene mới
        SceneManager.LoadScene(targetScene);

        // Chờ 1 frame để Scene load xong
        yield return null;

        // Tìm điểm spawn
        GameObject spawnPoint = GameObject.Find(targetSpawnPoint);
        if (spawnPoint != null)
        {
            player.transform.position = spawnPoint.transform.position;
        }

        // Cho phép Player trở lại trạng thái bình thường
        isTransitioning = false;
    }
}
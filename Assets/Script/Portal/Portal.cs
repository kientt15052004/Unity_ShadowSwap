using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Portal : MonoBehaviour
{
    [Header("Tên Scene sẽ chuyển đến")]
    public string targetScene;

    [Header("Tên điểm spawn trong Scene mới")]
    public string targetSpawnPoint;

    private bool isTransitioning = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && !isTransitioning)
        {
            isTransitioning = true;
            StartCoroutine(Transition(collision.gameObject));
        }
    }

    private IEnumerator Transition(GameObject player)
    {
        // (Tuỳ chọn) Hiệu ứng fade hoặc delay
        yield return new WaitForSeconds(0.5f);

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
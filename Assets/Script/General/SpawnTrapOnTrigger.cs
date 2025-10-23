using UnityEngine;
using System.Collections;

public class SpawnTrapOnTrigger : MonoBehaviour
{
    [Header("Trap Prefab")]
    public GameObject trapPrefab;

    [Header("Spawn Settings")]
    public int trapCount = 3;          // Số lượng trap sinh ra
    public float verticalSpacing = 1f; // Khoảng cách giữa các trap theo trục Y
    public float yOffset = 1f;         // Khoảng cách bắt đầu trên trigger
    public float spawnDelay = 2f;      // Thời gian trễ (giây) sau khi chạm trigger

    private bool hasTriggered = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasTriggered) return;

        if (other.CompareTag("Player"))
        {
            hasTriggered = true;
            StartCoroutine(SpawnTrapWithDelay());
        }
    }

    private IEnumerator SpawnTrapWithDelay()
    {
        // Đợi 2 giây trước khi sinh bẫy
        yield return new WaitForSeconds(spawnDelay);

        Vector2 triggerPos = transform.position;

        for (int i = 0; i < trapCount; i++)
        {
            Vector2 spawnPos = new Vector2(triggerPos.x, triggerPos.y + yOffset + i * verticalSpacing);
            Instantiate(trapPrefab, spawnPos, Quaternion.identity);
        }
    }
}

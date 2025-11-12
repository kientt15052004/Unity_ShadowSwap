using UnityEngine;
// Yêu cầu (RequireComponent) đã được loại bỏ ở đây vì nó đã có trong EnemyCore

public class HealthBarOverlay : MonoBehaviour
{
    private EnemyCore enemyCore;


    public float barHeightOffset = 1.2f;
    public float baseBarLength = 0.05f;   // Chiều dài chuẩn (cho quái 100 máu)
    public float barHeight = 0.15f;


    public Color regularEnemyColor = Color.red;
    public Color bossColor = new Color(0.5f, 0f, 0.5f); // Màu Tím
    public bool isBoss = false;

    void Awake()
    {
        enemyCore = GetComponent<EnemyCore>();
        if (enemyCore == null)
        {
            Debug.LogError("HealthBarOverlay requires an EnemyCore component!");
        }
    }

    void OnDrawGizmosSelected()
    {
        // Yêu cầu (RequireComponent) đã được loại bỏ ở đây vì nó đã có trong EnemyCore
        // Đã đảm bảo rằng EnemyCore được gọi
        if (enemyCore == null) return;
        if (enemyCore.CurrentHealth <= 0) return;

        float currentHealth = enemyCore.CurrentHealth;
        float maxHealth = enemyCore.maxHealth;

        // 1. Xác định Màu
        // SỬ DỤNG THUỘC TÍNH MỚI ĐỂ XÁC ĐỊNH VAI TRÒ
        Color barColor = enemyCore.IsBossType ? bossColor : regularEnemyColor;

        // 2. TÍNH TOÁN CHIỀU DÀI CHUẨN
        float lengthReference = 100f;
        float adjustedLength;

        // Nếu là Boss, thanh máu dài hơn, nếu là quái thường, thanh máu chuẩn
        if (enemyCore.IsBossType)
        {
            adjustedLength = baseBarLength * (maxHealth / lengthReference);
        }
        else
        {
            adjustedLength = baseBarLength;
        }


        // 3. Tính toán tỷ lệ và Vị trí trung tâm thanh máu
        float healthRatio = currentHealth / maxHealth;
        Vector3 barPosition = transform.position + Vector3.up * barHeightOffset;

        // 4. Vẽ Thanh máu Đã mất (Background)
        Gizmos.color = Color.black;
        Gizmos.DrawCube(barPosition, new Vector3(adjustedLength, barHeight, 0.01f));

        // 5. Vẽ Thanh máu Hiện tại (Foreground - Căn lề trái)
        float currentBarLength = adjustedLength * healthRatio;
        float offset = adjustedLength * 0.5f - currentBarLength * 0.5f;

        Vector3 currentBarPosition = barPosition - Vector3.right * offset;

        Gizmos.color = barColor;
        Gizmos.DrawCube(currentBarPosition, new Vector3(currentBarLength, barHeight, 0.01f));

        // 6. Hiển thị Số máu (chỉ trong Editor)
#if UNITY_EDITOR
        UnityEditor.Handles.color = Color.white;
        UnityEditor.Handles.Label(barPosition + Vector3.left * (adjustedLength * 0.5f + 0.1f),
                                    $"{currentHealth}",
                                    UnityEditor.EditorStyles.boldLabel);
#endif
    }
}
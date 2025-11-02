using UnityEngine;

// Đảm bảo Player có Collider 2D để xác định vị trí tấn công và LayerMask để tìm kẻ địch

public class PlayerAttackController : MonoBehaviour
{
    // Sát thương cơ bản cho mỗi đòn đánh

    public float attackRadius = 1.0f; // Phạm vi quét hitbox
    public int attackDamage = 15;     // Sát thương cơ bản cho Attack1/Attack2
    public LayerMask enemyLayer;      // Layer chứa tất cả kẻ địch và các đối tượng có thể bị tấn công

    // Bộ đệm để lưu trữ kết quả va chạm
    private Collider2D[] _hitResults = new Collider2D[10];

    // Chức năng này được gọi bằng Animation Event tại frame hit
    // Nó quét Hitbox và gây sát thương lên kẻ địch.
    public void PerformHitCheck(int damageOverride = 0)
    {
        // 1. Xác định vị trí và hướng tấn công
        // TÙY CHỌN: Nếu Hitbox bị lệch, hãy dùng: Vector2 attackOrigin = transform.position + transform.right * offset;
        Vector2 attackOrigin = transform.position;

        // 2. Sử dụng OverlapCircleNonAlloc để quét Hitbox
        // Đảm bảo chỉ quét các Collider trên Enemy Layer
        int hitCount = Physics2D.OverlapCircleNonAlloc(
      attackOrigin,
      attackRadius,
      _hitResults,
      enemyLayer
    );

        if (hitCount > 0)
        {
            int finalDamage = (damageOverride > 0) ? damageOverride : attackDamage;

            for (int i = 0; i < hitCount; i++)
            {
                Collider2D hitCollider = _hitResults[i];

                // Tránh tấn công chính mình hoặc các vật thể không phải kẻ địch
                if (hitCollider.gameObject == gameObject) continue;

                // 3. Tìm interface IDamageable
                IDamageable damageableTarget = hitCollider.GetComponent<IDamageable>();

                // Nếu không tìm thấy IDamageable trực tiếp, tìm trong Parent 
                if (damageableTarget == null)
                {
                    damageableTarget = hitCollider.GetComponentInParent<IDamageable>();
                }

                if (damageableTarget != null)
                {
                    // 4. Gây sát thương
                    DamageInfo info = new DamageInfo(
            amount: finalDamage,
            origin: transform.position,
            source: gameObject,
            isCritical: false
          );

                    damageableTarget.TakeDamage(info);
                    Debug.Log($"Player hit {hitCollider.gameObject.name} for {finalDamage} damage.");

                    // Chỉ tấn công đối tượng này một lần trong đợt quét hiện tại
                    // Sau khi tấn công, bạn có thể break khỏi vòng lặp nếu không muốn xuyên mục tiêu
                    // break; 
                }

                // Đặt lại slot buffer để chuẩn bị cho lần quét tiếp theo
                _hitResults[i] = null;
            }
        }
    }

    // Hàm này phải được gọi bởi Animation Event tại frame hit
    public void OnPlayerAttackHit()
    {
        // Gọi hàm kiểm tra Hitbox với sát thương cơ bản
        PerformHitCheck(attackDamage);
    }

    // --- NEW: VISUAL DEBUGGING ---
    void OnDrawGizmosSelected()
    {
        // Vẽ vòng tròn Hitbox màu đỏ khi Player được chọn trong Editor
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRadius);
    }
}

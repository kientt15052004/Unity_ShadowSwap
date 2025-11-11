using System.Collections.Generic;
using UnityEngine;

// Đảm bảo Player có Collider 2D để xác định vị trí tấn công và LayerMask để tìm kẻ địch

public class PlayerAttackController : MonoBehaviour
{
    // Sát thương cơ bản cho mỗi đòn đánh

    public float attackRadius = 1.0f; // Phạm vi quét hitbox
    public int attackDamage = 10;     // Sát thương cơ bản cho Attack1/Attack2
    public LayerMask enemyLayer;      // Layer chứa tất cả kẻ địch và các đối tượng có thể bị tấn công
    private float _lastAttackEventTime = -999f;
    // Bộ đệm để lưu trữ kết quả va chạm
    private Collider2D[] _hitResults = new Collider2D[10];

    private float _lastHitTime = -1f;
    [SerializeField] private float hitCooldown = 0.05f; // 50ms tránh double hit

    // Chức năng này được gọi bằng Animation Event tại frame hit
    // Nó quét Hitbox và gây sát thương lên kẻ địch.
    public void PerformHitCheck(int damageOverride = 0)
    {
        if (Time.time < _lastHitTime + hitCooldown) return;
        _lastHitTime = Time.time;

        Vector2 attackOrigin = transform.position;
        int hitCount = Physics2D.OverlapCircleNonAlloc(attackOrigin, attackRadius, _hitResults, enemyLayer);
        if (hitCount <= 0) return;

        int finalDamage = (damageOverride > 0) ? damageOverride : attackDamage;

        HashSet<int> hitRoots = new HashSet<int>(hitCount);

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hitCollider = _hitResults[i];
            if (hitCollider == null) continue;

            if (hitCollider.gameObject == gameObject) { _hitResults[i] = null; continue; }

            GameObject rootGO = hitCollider.attachedRigidbody != null ? hitCollider.attachedRigidbody.gameObject : hitCollider.gameObject;
            int id = rootGO.GetInstanceID();
            if (!hitRoots.Add(id))
            {
                _hitResults[i] = null;
                continue;
            }

            IDamageable damageableTarget = rootGO.GetComponent<IDamageable>() ?? rootGO.GetComponentInChildren<IDamageable>();
            if (damageableTarget != null)
            {
                DamageInfo info = new DamageInfo(finalDamage, transform.position, gameObject, false);
                damageableTarget.TakeDamage(info);
                Debug.Log($"Player hit {rootGO.name} for {finalDamage} damage. (collider: {hitCollider.name})");
            }
            else
            {
                var hm = rootGO.GetComponentInChildren<HealthManager>();
                if (hm != null)
                {
                    hm.TakeDamage(finalDamage);
                    Debug.Log($"Player hit (fallback) {rootGO.name} for {finalDamage} damage (HealthManager).");
                }
            }
            _hitResults[i] = null;
        }
    }

    // Called by animation event (attack hit frame)
    public void OnPlayerAttackHit()
    {
        // Optional safeguard: prevent animation event being accidentally called twice in the same frame.
        // If you're sure animation events are correct, you can remove the timestamp guard.
        if (Time.time < _lastAttackEventTime + 0.05f) // 50ms debounce
        {
            Debug.Log("OnPlayerAttackHit ignored (debounced).");
            return;
        }
        _lastAttackEventTime = Time.time;

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

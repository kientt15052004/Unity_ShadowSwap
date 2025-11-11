using System.Collections;
using UnityEngine;

// ------------------- FireBall -------------------
[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class FireBall : MonoBehaviour
{
    [Header("Projectile Settings")]
    public float baseSpeed = 8f;          // tăng tốc độ mặc định
    public int damage = 12;
    public bool destroyOnHit = true;
    public LayerMask hitLayers = ~0;
    public GameObject hitVFX;

    private Vector2 velocity;
    private GameObject source;
    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.bodyType = RigidbodyType2D.Kinematic;

        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    /// <summary>
    /// Khởi tạo projectile
    /// </summary>
    /// <param name="direction">hướng bay</param>
    /// <param name="speed">tốc độ</param>
    /// <param name="damage">sát thương</param>
    /// <param name="source">boss hoặc minion tạo ra</param>
    /// <param name="targetDistance">khoảng cách đến mục tiêu (tự tính lifeTime)</param>
    public void Initialize(Vector2 direction, float speed, int damage, GameObject source = null, float? targetDistance = null)
    {
        this.damage = damage;
        this.source = source;
        this.velocity = direction.normalized * speed;

        float life = 6f; // default
        if (targetDistance.HasValue)
            life = targetDistance.Value / speed + 0.5f; // thêm buffer nhỏ

        Destroy(gameObject, life);
    }

    void FixedUpdate()
    {
        rb.MovePosition(rb.position + velocity * Time.fixedDeltaTime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject == source) return;
        if (((1 << other.gameObject.layer) & hitLayers.value) == 0) return;

        IDamageable dmg = other.GetComponent<IDamageable>();
        if (dmg == null)
        {
            foreach (var m in other.GetComponentsInParent<MonoBehaviour>(true))
                if (m is IDamageable) { dmg = (IDamageable)m; break; }
        }

        if (dmg != null)
        {
            dmg.TakeDamage(new DamageInfo(damage, transform.position, source, false));
            if (hitVFX != null)
                Instantiate(hitVFX, transform.position, Quaternion.identity);
        }

        if (destroyOnHit) Destroy(gameObject);
    }
}
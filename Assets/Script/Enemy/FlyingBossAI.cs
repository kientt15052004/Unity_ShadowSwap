using UnityEngine;
using System.Collections;

public class FlyingBossAI : MonoBehaviour
{
    [Header("Damage")]
    public Transform attackPoint;
    public float attackRange = 1.2f;
    public int damage = 15;
    public LayerMask playerLayer;

    public Transform player;
    public float moveSpeed = 3f;
    public float stopDistance = 3f;
    public float attackCooldown = 2f;
    public float retreatHeight = 4f; // boss sẽ bay lên cao bao nhiêu tại retreat

    private float attackTimer;
    private Animator anim;
    private bool facingRight = true;
    private bool isRetreating = false; // chặn di chuyển & tấn công khi rút lui

    void Start()
    {
        anim = GetComponent<Animator>();
        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player").transform;
    }

    void Update()
    {
        if (!player || isRetreating) return;

        Flip();

        float dist = Vector2.Distance(transform.position, player.position);

        if (dist > stopDistance)
        {
            MoveTowardPlayer();
        }
        else
        {
            attackTimer -= Time.deltaTime;
            if (attackTimer <= 0)
            {
                StartCoroutine(AttackAndRetreat());
                attackTimer = attackCooldown;
            }
        }
    }

    IEnumerator AttackAndRetreat()
    {
        PlayAttack();

        // 🔹 Sau khi tấn công, chờ ngẫu nhiên 2 - 4 giây rồi retreat
        yield return new WaitForSeconds(Random.Range(2f, 4f));

        StartCoroutine(RetreatSequence());
    }

    IEnumerator RetreatSequence()
    {
        isRetreating = true;

        // 🔼 BAY LÊN
        Vector3 retreatTarget = new Vector3(transform.position.x, transform.position.y + retreatHeight, transform.position.z);

        while (Vector2.Distance(transform.position, retreatTarget) > 0.1f)
        {
            transform.position = Vector2.MoveTowards(transform.position, retreatTarget, moveSpeed * Time.deltaTime);
            yield return null;
        }

        // ⏸ ĐỨNG TRÊN CAO 2 GIÂY
        yield return new WaitForSeconds(2f);

        // 🦅 LAO XUỐNG THEO ĐƯỜNG CONG
        yield return StartCoroutine(DiveArcAttack());

        // ✅ Reset để boss tiếp tục tấn công bình thường
        attackTimer = attackCooldown;
        isRetreating = false;
    }

    void MoveTowardPlayer()
    {
        transform.position = Vector2.MoveTowards(transform.position, player.position, moveSpeed * Time.deltaTime);
    }

    void PlayAttack()
    {
        int atk = Random.Range(1, 3);
        anim.SetTrigger(atk == 1 ? "Attack1" : "Attack2");
    }

    void Flip()
    {
        if (player.position.x > transform.position.x && !facingRight)
        {
            transform.localScale = new Vector3(+Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
            facingRight = true;
        }
        else if (player.position.x < transform.position.x && facingRight)
        {
            transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
            facingRight = false;
        }
    }
    IEnumerator DiveArcAttack()
    {
        Vector3 start = transform.position;

        // ⬆️ Boss sẽ lao đến vị trí ngay phía trên Player, không chạm đất
        Vector3 end = new Vector3(player.position.x, player.position.y + 1.5f, player.position.z);

        float t = 0;
        float diveDuration = 1.2f;
        float arcHeight = 4f;

        while (t < 1)
        {
            t += Time.deltaTime / diveDuration;

            float heightOffset = Mathf.Sin(t * Mathf.PI) * arcHeight;
            transform.position = Vector3.Lerp(start, end, t) + new Vector3(0, heightOffset, 0);

            Flip();
            yield return null;
        }

        // Đánh khi lao xuống xong
        PlayAttack();
        yield return new WaitForSeconds(0.4f);
       
    }

    void DealDamage()
    {
        if (attackPoint == null)
        {
            Debug.LogError("⚠️ AttackPoint CHƯA ĐƯỢC GÁN!");
            return;
        }

        Collider2D hit = Physics2D.OverlapCircle(attackPoint.position, attackRange, playerLayer);

        if (hit != null)
        {
            hit.GetComponent<HealthManager>()?.TakeDamage(damage);
            Debug.Log("✅ Boss đánh trúng Player!");
        }
        else
        {
            Debug.Log("❌ Boss đánh hụt (hitbox không chạm)");

            // --- THAY ĐỔI THEO YÊU CẦU ---
            // Nếu đánh hụt, ngay lập tức bắt đầu lại chuỗi "Rút lui" (bay lên)

            // 1. Dừng tất cả các hành động (coroutine) hiện tại để tránh xung đột
            StopAllCoroutines();

            // 2. Bắt đầu một chuỗi RetreatSequence mới
            StartCoroutine(RetreatSequence());
            // --- KẾT THÚC THAY ĐỔI ---
        }
    }

    void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }



}

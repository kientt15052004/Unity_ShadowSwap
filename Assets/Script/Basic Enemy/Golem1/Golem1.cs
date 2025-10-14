using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Golem1 : MonoBehaviour
{
    [Header("Patrol Settings")]
    [SerializeField] private float patrolDistance = 5f;
    [SerializeField] private float patrolSpeed = 2f;

    [Header("Chase Settings")]
    [SerializeField] private float chaseDistance = 8f;
    [SerializeField] private float chaseSpeed = 4f;
    private Transform player; // Sẽ được tìm tự động

    [Header("Attack Settings")]
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private int damage = 10;
    [SerializeField] private float attackCooldown = 2f;
    private float lastAttackTime;

    [Header("Health & Combat")]
    [SerializeField] private int maxHealth = 100;
    private int currentHealth;
    private bool isDead = false;
    private bool isHurt = false;

    [Header("Jumping Settings")]
    [SerializeField] private float wallCheckDistance = 0.6f;
    [SerializeField] private float jumpForce = 7f;
    [SerializeField] private LayerMask groundLayer; // Layer của nền đất và tường
    [SerializeField] private Transform groundCheck; // Điểm để kiểm tra mặt đất
    [SerializeField] private float groundCheckRadius = 0.2f;
    // Components
    private Rigidbody2D rb;
    private Animator anim;

    // State Variables
    private Vector2 leftPatrolPoint, rightPatrolPoint, currentTarget, initialPosition;
    private bool isFacingRight = true;
    private bool isChasing = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        currentHealth = maxHealth;

        // Tự động tìm Player
        FindPlayer();

        // Thiết lập điểm tuần tra
        initialPosition = transform.position;
        leftPatrolPoint = new Vector2(initialPosition.x - patrolDistance, initialPosition.y);
        rightPatrolPoint = new Vector2(initialPosition.x + patrolDistance, initialPosition.y);
        currentTarget = rightPatrolPoint;
    }

    void Update()
    {
        if (isDead || player == null) return; // Nếu đã chết hoặc không tìm thấy player thì không làm gì

        // Nếu đang trong trạng thái bị đau, sẽ không làm gì cả
        if (isHurt) return;

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        isChasing = distanceToPlayer <= chaseDistance;
        // Ưu tiên: Tấn công nếu player trong tầm
        if (distanceToPlayer <= attackRange)
        {
            Attack();
        }
        // Tiếp theo: Đuổi theo nếu player trong tầm nhìn
        else if (distanceToPlayer <= chaseDistance)
        {
            ChasePlayer();
        }
        // Cuối cùng: Tuần tra
        else
        {
            Patrol();
        }

        FlipBasedOnVelocity();
    }

    private void FixedUpdate()
    {
        // Kiểm tra tường chỉ khi đang di chuyển (tuần tra hoặc đuổi theo)
        if (isChasing && IsGrounded())
        {
            CheckForWallAndJump();
        }
    }

    // --- CÁC HÀM HÀNH VI ---

    void Patrol()
    {
        anim.SetBool("Run", true);
        if (Vector2.Distance(transform.position, currentTarget) < 0.2f)
        {
            currentTarget = (currentTarget == rightPatrolPoint) ? leftPatrolPoint : rightPatrolPoint;
        }
        MoveTowards(currentTarget, patrolSpeed);
    }

    void ChasePlayer()
    {
        anim.SetBool("Run", true);
        MoveTowards(player.position, chaseSpeed);
    }

    void Attack()
    {
        rb.velocity = Vector2.zero; // Dừng lại để tấn công
        anim.SetBool("Run", false);

        // Quay mặt về phía player
        if (player.position.x > transform.position.x && !isFacingRight) Flip();
        if (player.position.x < transform.position.x && isFacingRight) Flip();

        if (Time.time > lastAttackTime + attackCooldown)
        {
            lastAttackTime = Time.time;
            anim.SetTrigger("Attack"); // Kích hoạt animation tấn công
        }
    }

    // --- CÁC HÀM HỖ TRỢ ---

    void MoveTowards(Vector2 target, float speed)
    {
        Vector2 direction = (target - (Vector2)transform.position).normalized;
        rb.velocity = new Vector2(direction.x * speed, rb.velocity.y);
    }

    void CheckForWallAndJump()
    {
        Vector2 rayDirection = isFacingRight ? Vector2.right : Vector2.left;
        RaycastHit2D hit = Physics2D.Raycast(transform.position, rayDirection, wallCheckDistance, groundLayer);

        // Nếu thấy tường và người chơi ở trên cao -> Nhảy
        if (hit.collider != null && player.position.y > transform.position.y + 0.5f)
        {
            Jump();
        }
    }

    bool IsGrounded()
    {
        // Vẽ một vòng tròn nhỏ ở dưới chân để kiểm tra, đáng tin cậy hơn Raycast
        return Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
    }

    void Jump()
    {
        rb.velocity = new Vector2(rb.velocity.x, jumpForce);
    }

    void FlipBasedOnVelocity()
    {
        if (rb.velocity.x > 0.1f && !isFacingRight) Flip();
        else if (rb.velocity.x < -0.1f && isFacingRight) Flip();
    }

    void Flip()
    {
        isFacingRight = !isFacingRight;
        transform.localScale = new Vector3(transform.localScale.x * -1, transform.localScale.y, transform.localScale.z);
    }

    void FindPlayer()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null) player = playerObject.transform;
        else Debug.LogError("GolemAI: Không tìm thấy đối tượng Player! Hãy chắc chắn Player có tag là 'Player'.");
    }

    // --- HỆ THỐNG MÁU VÀ COMBAT ---

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            // Bị đau, kích hoạt animation và trạng thái "hurt"
            anim.SetTrigger("Hurt");
            StartCoroutine(HurtRoutine());
        }
    }

    IEnumerator HurtRoutine()
    {
        isHurt = true; // Bật trạng thái "bị đau" -> không thể di chuyển hay tấn công
        rb.velocity = Vector2.zero; // Dừng di chuyển
        yield return new WaitForSeconds(0.5f); // Thời gian bị choáng, có thể chỉnh
        isHurt = false; // Tắt trạng thái "bị đau"
    }

    void Die()
    {
        isDead = true;
        anim.SetTrigger("Death");
        rb.velocity = Vector2.zero;
        GetComponent<Collider2D>().enabled = false; // Tắt va chạm để không cản đường
        this.enabled = false; // Tắt script này đi
        Destroy(gameObject, 3f); // Hủy đối tượng sau 3 giây để animation chạy xong
    }

    // HÀM NÀY SẼ ĐƯỢC GỌI BẰNG ANIMATION EVENT
    public void DealDamageToPlayer()
    {
        // Tạo một vùng kiểm tra nhỏ phía trước mặt Golem
        float checkRadius = 0.5f;
        Vector2 attackPos = (Vector2)transform.position + (isFacingRight ? Vector2.right : Vector2.left) * (attackRange - 0.5f);
        Collider2D[] hitPlayers = Physics2D.OverlapCircleAll(attackPos, checkRadius);

        foreach (Collider2D playerCollider in hitPlayers)
        {
            if (playerCollider.CompareTag("Player"))
            {
                // Giả sử Player có script PlayerHealth
                //PlayerHealth playerHealth = playerCollider.GetComponent<PlayerHealth>();
                //if (playerHealth != null)
                //{
                //    playerHealth.TakeDamage(damage);
                //    // Dừng vòng lặp sau khi gây sát thương cho 1 người chơi
                //    break;
                //}
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, chaseDistance);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Vẽ đường raycast kiểm tra tường
        Gizmos.color = Color.blue;
        Vector2 rayDirection = isFacingRight ? Vector2.right : Vector2.left;
        Gizmos.DrawLine(transform.position, (Vector2)transform.position + rayDirection * wallCheckDistance);
    }
}

using System.Collections;
using UnityEngine;

// ------------------------ Shared types (can be moved to separate files) ------------------------
public struct DamageInfo
{
    public int amount;
    public Vector2 origin;
    public GameObject source;
    public bool isCritical;
    public DamageInfo(int amount, Vector2 origin = default, GameObject source = null, bool isCritical = false)
    {
        this.amount = amount; this.origin = origin; this.source = source; this.isCritical = isCritical;
    }
}

public interface IDamageable { void TakeDamage(DamageInfo info); }

[RequireComponent(typeof(Collider2D))]
public class Health : MonoBehaviour, IDamageable
{
    [Header("Health")]
    public int maxHealth = 100;
    public float invincibilitySeconds = 0.2f;

    public System.Action<DamageInfo> OnDamaged;
    public System.Action OnDied;

    private int currentHealth;
    private float lastHitTime = -999f;

    void Awake() { currentHealth = maxHealth; }

    public int Current => currentHealth;
    public int Max => maxHealth;

    public void TakeDamage(DamageInfo info)
    {
        if (Time.time < lastHitTime + invincibilitySeconds) return;
        lastHitTime = Time.time;
        currentHealth -= info.amount;
        OnDamaged?.Invoke(info);
        if (currentHealth <= 0) OnDied?.Invoke();
    }

    public void Heal(int amount) { currentHealth = Mathf.Min(maxHealth, currentHealth + amount); }
}

[CreateAssetMenu(menuName = "Enemy/AttackData")]
public class AttackData : ScriptableObject
{
    public int damage = 10;
    public float range = 0.6f;
    public float cooldown = 1f;
    public LayerMask hitLayers = ~0;
    public GameObject hitVFX;
}

[RequireComponent(typeof(Collider2D))]
public class AttackHitbox : MonoBehaviour
{
    public LayerMask hitLayers = ~0;

    // REFACTOR: Tạo một bộ đệm (buffer) để tái sử dụng, tránh tạo mảng mới mỗi lần.
    // Con số 10 có nghĩa là nó có thể phát hiện tối đa 10 colliders 1 lúc.
    private Collider2D[] _hitsBuffer = new Collider2D[10];

    public void DoAttack(AttackData data, Transform origin, bool isFacingRight)
    {
        if (data == null || origin == null) return;

        Vector2 center = (Vector2)origin.position + (isFacingRight ? Vector2.right : Vector2.left) * (data.range * 0.5f);

        // REFACTOR: Sử dụng OverlapCircleNonAlloc để không tạo rác (GC)
        int hitCount = Physics2D.OverlapCircleNonAlloc(center, data.range, _hitsBuffer, data.hitLayers);

        for (int i = 0; i < hitCount; i++)
        {
            var col = _hitsBuffer[i];

            IDamageable d = col.GetComponent<IDamageable>();
            if (d == null)
            {
                var monos = col.GetComponentsInParent<MonoBehaviour>(true);
                foreach (var m in monos) if (m is IDamageable) { d = (IDamageable)m; break; }
            }
            if (d != null)
            {
                d.TakeDamage(new DamageInfo(data.damage, origin.position, origin.gameObject, false));
                if (data.hitVFX != null) Instantiate(data.hitVFX, col.transform.position, Quaternion.identity);
            }
        }
    }
}
public static class JumpMathUtility
{
    // Simulate a jump (forward integration) to estimate reachability.
    public static bool CanReachByJump(Vector2 start, Vector2 target, float horizontalSpeed, float jumpForce, float gravity, LayerMask groundLayer, float timeStep = 0.02f, float maxTime = 2f, float groundCheckRadius = 0.15f)
    {
        if (gravity <= 0f) gravity = 9.81f;
        float dir = Mathf.Sign(target.x - start.x);
        float vx = Mathf.Abs(horizontalSpeed) * dir;
        float vy = jumpForce;
        Vector2 pos = start;
        float t = 0f;
        while (t < maxTime)
        {
            vy -= gravity * timeStep;
            pos += new Vector2(vx * timeStep, vy * timeStep);
            if (pos.y <= target.y + 0.2f)
            {
                Collider2D ground = Physics2D.OverlapCircle(pos, groundCheckRadius, groundLayer);
                if (ground != null) { if (Mathf.Abs(pos.x - target.x) <= 0.6f) return true; }
            }
            if (pos.y < start.y - 5f) break;
            t += timeStep;
        }
        return false;
    }
}

// ------------------------ EnemyCore (main file) ------------------------
[RequireComponent(typeof(Rigidbody2D), typeof(Animator))]
public class EnemyCore : MonoBehaviour
{
    [Header("Movement")]
    public float patrolDistance = 4f;
    public float patrolSpeed = 2f;
    public float chaseDistance = 8f;
    public float chaseSpeed = 4f;

    [Header("Jumping/Wall")]
    public float wallCheckDistance = 0.6f;
    public float ledgeCheckDistance = 0.6f;
    public float jumpForce = 7f;
    public LayerMask groundLayer;
    public Transform groundCheck;
    public float groundCheckRadius = 0.12f;
    public float maxJumpableGap = 1.2f;

    [Header("Attack")]
    public AttackData attackData;
    public AttackHitbox attackHitbox;
    public float attackRange = 1f;
    public float attackCooldown = 1f;

    [Header("Health")]
    public int maxHealth = 100;
    public float invincibilityTime = 0.15f;

    // internals
    [HideInInspector] public Rigidbody2D rb;
    [HideInInspector] public Animator anim;
    [HideInInspector] public Transform player;

    [HideInInspector] public Vector2 startPos;
    [HideInInspector] public Vector2 leftPoint;
    [HideInInspector] public Vector2 rightPoint;
    [HideInInspector] public Vector2 targetPoint;
    [HideInInspector] public bool isFacingRight = true;

    protected int currentHealth;
    protected float lastHitAt = -999f;
    protected float lastAttackAt = -999f;
    protected bool isHurt = false;
    protected bool isDead = false;

    // NEW: track attack state (using bool-based animator)
    protected bool isAttacking = false;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        // FIX 2: Thêm kiểm tra null cho Animator để tránh lỗi UnassignedReferenceException
        anim = GetComponent<Animator>();
        if (anim == null)
        {
            Debug.LogError("EnemyCore on " + gameObject.name + " requires an Animator component!");
        }
        currentHealth = maxHealth;
        EnsureGroundCheck();
    }

    protected virtual void Start()
    {
        if (player == null) FindPlayer();
        startPos = transform.position;
        leftPoint = new Vector2(startPos.x - patrolDistance, startPos.y);
        rightPoint = new Vector2(startPos.x + patrolDistance, startPos.y);
        targetPoint = rightPoint;
    }

    protected virtual void Update()
    {
        if (isDead) return;
        if (player == null) FindPlayer();
        if (isHurt) { UpdateAnimationFlags(); return; }
        // Note: behaviors (Golem classes) typically call SimplePatrol/SimpleChase/TryAttack when appropriate.
        UpdateAnimationFlags();
        FlipByVelocity();
    }

    protected float lastJumpAt = -999f; // thời điểm nhảy gần nhất
    [SerializeField] private float jumpCooldown = 0.5f; // thời gian chờ giữa 2 lần nhảy


    protected virtual void FixedUpdate()
    {
        if (isDead || isHurt || isAttacking)
        {
            if (isAttacking)
            {
                rb.velocity = new Vector2(0f, rb.velocity.y);
            }
            return;
        }

        // FIX 1: Đã xóa dòng chặn di chuyển: if (Mathf.Abs(rb.velocity.x) < 0.05f) return;
        // Giúp logic kiểm tra tường/rìa luôn chạy, ngăn kẹt ở trạng thái đứng yên sau khi đảo hướng.

        // --- Chuẩn bị raycast ---
        Vector2 dir = isFacingRight ? Vector2.right : Vector2.left;

        // Lấy vị trí gốc (chân)
        Vector2 baseOrigin = groundCheck != null ? (Vector2)groundCheck.position : (Vector2)transform.position;

        // SỬA LỖI: Bắt đầu Raycast từ một điểm hơi CAO HƠN mặt đất (ví dụ: 0.1f)
        // để tránh việc raycast bắt đầu TỪ BÊN TRONG collider của mặt đất.
        Vector2 rayOrigin = baseOrigin + (Vector2.up * 0.1f);

        // Chiều dài raycast xuống đất cần tăng thêm tương ứng (1.2f ban đầu + 0.1f nâng lên)
        float groundCheckRayLength = 1.2f + 0.1f;

        // --- Kiểm tra tường ---
        // Dùng rayOrigin (cao hơn 0.1f)
        RaycastHit2D wall = Physics2D.Raycast(rayOrigin, dir, wallCheckDistance, groundLayer);

        // --- Kiểm tra mặt đất phía trước ---
        // Dùng điểm bắt đầu ở phía trước VÀ cao hơn 0.1f
        Vector2 ledgeCheckStart = rayOrigin + dir * ledgeCheckDistance;
        bool groundAhead = Physics2D.Raycast(ledgeCheckStart, Vector2.down, groundCheckRayLength, groundLayer);

        // --- Nếu gặp tường ---
        if (wall.collider != null)
        {
            // Nếu player cao hơn enemy một chút → thử nhảy
            if (player != null && player.position.y > transform.position.y + 0.4f && IsGrounded())
            {
                // Kiểm tra không nhảy quá dày
                if (Time.time > lastHitAt + 0.3f)
                {
                    TryJump();
                    lastHitAt = Time.time; // dùng tạm lastHitAt như cooldown nhảy
                }
            }
            else
            {
                ReverseDirection();
            }
        }
        // --- Nếu sắp hết đường ---
        // Logic này giờ sẽ chỉ kích hoạt khi THỰC SỰ ở rìa
        else if (!groundAhead)
        {
            // Nếu player ở cao hơn, trong phạm vi gap cho phép → nhảy theo
            if (player != null && player.position.y > transform.position.y + 0.3f &&
        Mathf.Abs(player.position.x - transform.position.x) <= maxJumpableGap && IsGrounded())
            {
                if (Time.time > lastHitAt + 0.3f)
                {
                    JumpTowardsPlayer();
                    lastHitAt = Time.time;
                }
            }
            else
            {
                ReverseDirection();
            }
        }
    }


    // -- movement helpers --
    public void SimplePatrol()
    {
        if (isAttacking) return; // guard
        if (anim != null) anim.SetBool("Run", true); // FIX 2: Thêm kiểm tra null
        if (Vector2.Distance(transform.position, targetPoint) < 0.2f) targetPoint = (targetPoint == rightPoint) ? leftPoint : rightPoint;
        MoveTowards(targetPoint, patrolSpeed);
    }

    public void SimpleChase()
    {
        if (isAttacking) return; // guard
        if (anim != null) anim.SetBool("Run", true); // FIX 2: Thêm kiểm tra null
        if (player != null) MoveTowards(player.position, chaseSpeed);
    }

    public void MoveTowards(Vector2 t, float speed)
    {
        Vector2 dir = (t - (Vector2)transform.position).normalized;
        rb.velocity = new Vector2(dir.x * speed, rb.velocity.y);
    }

    // -- Attack control (bool-based animation) --
    public virtual void TryAttack()
    {
        if (Time.time < lastAttackAt + attackCooldown) return;
        if (isAttacking) return; // already attacking
        lastAttackAt = Time.time;

        // start attack: stop movement and set bool
        rb.velocity = new Vector2(0f, rb.velocity.y);
        isAttacking = true;
        if (anim != null) anim.SetBool("IsAttacking", true); // FIX 2: Thêm kiểm tra null
    }

    // Animation event at hit frame should call this to apply damage
    public void OnAttackHit()
    {
        if (attackHitbox != null && attackData != null) attackHitbox.DoAttack(attackData, transform, isFacingRight);
    }

    // Animation event at the END of the attack animation should call this to finish attack state
    public void EndAttack()
    {
        isAttacking = false;
        if (anim != null) anim.SetBool("IsAttacking", false); // FIX 2: Thêm kiểm tra null
    }

    public void ReverseDirection()
    {
        rb.velocity = Vector2.zero;
        Flip();
        targetPoint = (targetPoint == rightPoint) ? leftPoint : rightPoint;
    }

    public void TryJump() { if (IsGrounded()) rb.velocity = new Vector2(rb.velocity.x, jumpForce); }
    public void JumpTowardsPlayer() { if (IsGrounded() && player != null) { Vector2 dir = ((Vector2)player.position - (Vector2)transform.position).normalized; rb.velocity = new Vector2(dir.x * patrolSpeed, jumpForce); } }

    // -- damage / health handling --
    public virtual void TakeDamage(DamageInfo info)
    {
        if (isDead) return;
        if (Time.time < lastHitAt + invincibilityTime) return;
        lastHitAt = Time.time;
        currentHealth -= info.amount;
        OnTakeDamageLocal(info);
        if (currentHealth <= 0) OnDieLocal();
    }

    protected virtual void OnTakeDamageLocal(DamageInfo info)
    {
        if (isDead) return;
        isHurt = true;
        // cancel attack if being hurt
        if (isAttacking)
        {
            isAttacking = false;
            if (anim != null) anim.SetBool("IsAttacking", false); // FIX 2: Thêm kiểm tra null
        }
        rb.velocity = Vector2.zero;
        StartCoroutine(HurtRoutine());
    }

    protected IEnumerator HurtRoutine()
    {
        if (anim != null) anim.SetBool("IsHurt", true); // FIX 2: Thêm kiểm tra null
        yield return new WaitForSeconds(0.45f);
        if (anim != null) anim.SetBool("IsHurt", false); // FIX 2: Thêm kiểm tra null
        isHurt = false;
    }

    protected virtual void OnDieLocal()
    {
        isDead = true;
        if (anim != null) anim.SetBool("IsDead", true); // FIX 2: Thêm kiểm tra null
        rb.velocity = Vector2.zero;
        var c = GetComponent<Collider2D>();
        if (c) c.enabled = false;
        this.enabled = false;
        Destroy(gameObject, 3f);
    }

    // -- utilities --
    // Make FindPlayer public so other classes can call safely
    public void FindPlayer() { var go = GameObject.FindGameObjectWithTag("Player"); if (go != null) player = go.transform; }
    public void FlipByVelocity() { if (rb.velocity.x > 0.1f && !isFacingRight) Flip(); else if (rb.velocity.x < -0.1f && isFacingRight) Flip(); }
    public void Flip() { isFacingRight = !isFacingRight; var s = transform.localScale; s.x *= -1f; transform.localScale = s; }
    public void EnsureGroundCheck() { if (groundCheck != null) return; GameObject go = new GameObject("GroundCheck"); go.transform.parent = transform; go.transform.localPosition = Vector3.zero; groundCheck = go.transform; groundCheckRadius = 0.12f; }
    public bool IsGrounded() { if (groundCheck == null) return false; return Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer); }
    public void UpdateAnimationFlags()
    {
        if (anim == null) return; // FIX 2: Thêm kiểm tra null 
        anim.SetBool("IsAttacking", isAttacking);
        anim.SetBool("IsHurt", isHurt);
        anim.SetBool("IsDead", isDead);
        anim.SetBool("IsJumping", !IsGrounded());
    }

    // Expose helper: simulate jump arc to decide if enemy should attempt jump
    public bool CanReachByJump(Vector2 target)
    {
        return JumpMathUtility.CanReachByJump(transform.position, target, patrolSpeed, jumpForce, Mathf.Abs(Physics2D.gravity.y * rb.gravityScale), groundLayer, 0.02f, 2f, groundCheckRadius);
    }

    // Debug gizmos
    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
        Vector2 dir = isFacingRight ? Vector2.right : Vector2.left;
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + (Vector3)dir * wallCheckDistance);
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine((Vector2)transform.position + dir * ledgeCheckDistance, (Vector2)transform.position + dir * ledgeCheckDistance + Vector2.down * 1.2f);
    }
}
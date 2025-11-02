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


public class AttackData : ScriptableObject
{
    public int damage = 10;
    public float range = 0.6f;
    public float cooldown = 1f;
    public LayerMask hitLayers = ~0;
    public GameObject hitVFX;
}


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
            // Clear the buffer slot after use
            _hitsBuffer[i] = null;
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

public class EnemyCore : MonoBehaviour, IDamageable
{
    [Header("Movement")]
    public float patrolDistance = 4f;
    public float patrolSpeed = 2f;
    public float chaseDistance = 8f;
    public float chaseSpeed = 4f;


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


    public float directionChangeCooldown = 0.4f;

    // internals
    [HideInInspector] public Rigidbody2D rb;
    [HideInInspector] public Animator anim;
    [HideInInspector] public Transform player;

    [HideInInspector] public Vector2 startPos;
    [HideInInspector] public Vector2 leftPoint;
    [HideInInspector] public Vector2 rightPoint;
    [HideInInspector] public Vector2 targetPoint;
    [HideInInspector] public bool isFacingRight = true;

    // Các trường protected cũ:
    protected int currentHealth;
    protected float lastHitAt = -999f;
    protected float lastAttackAt = -999f;
    protected bool isHurt = false;
    protected bool isDead = false;
    protected bool isAttacking = false;
    protected float lastDirectionChangeAt = -999f;
    protected float lastJumpAt = -999f; // Đã chuyển lên đây để dùng nhất quán
    private float jumpCooldown = 0.5f;

    // KHẮC PHỤC LỖI CS0122: Thêm Public Read-Only Properties
    public bool IsDead => isDead;
    public bool IsHurt => isHurt;
    public bool IsAttacking => isAttacking;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        if (anim == null)
        {
            Debug.LogError("EnemyCore on " + gameObject.name + " requires an Animator component!");
        }
        if (GetComponent<IDamageable>() == null) gameObject.AddComponent<Health>();

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

        UpdateAnimationFlags();

        // AI Golem1 sẽ quản lý việc quay mặt (FaceTarget)
    }

    protected virtual void FixedUpdate()
    {
        if (isDead || isHurt || isAttacking)
        {
            rb.velocity = new Vector2(0f, rb.velocity.y);
            return;
        }

        if (Time.time < lastDirectionChangeAt + directionChangeCooldown)
        {
            rb.velocity = new Vector2(0f, rb.velocity.y);
            return;
        }


        Vector2 dir = isFacingRight ? Vector2.right : Vector2.left;

        Vector2 baseOrigin = groundCheck != null ? (Vector2)groundCheck.position : (Vector2)transform.position;
        Vector2 rayOrigin = baseOrigin + (Vector2.up * 0.1f);
        float groundCheckRayLength = 1.2f + 0.1f;

        RaycastHit2D wall = Physics2D.Raycast(rayOrigin, dir, wallCheckDistance, groundLayer);

        Vector2 ledgeCheckStart = rayOrigin + dir * ledgeCheckDistance;
        bool groundAhead = Physics2D.Raycast(ledgeCheckStart, Vector2.down, groundCheckRayLength, groundLayer);

        // --- XỬ LÝ KHI GẶP TƯỜNG ---
        if (wall.collider != null)
        {
            // Điều kiện nhảy tường:
            // 1. Player phải ở vị trí cao hơn đáng kể (0.4f)
            // 2. Golem phải đang đứng trên đất
            // 3. Phải qua cooldown nhảy
            if (player != null && player.position.y > transform.position.y + 0.4f && IsGrounded())
            {
                if (Time.time > lastJumpAt + jumpCooldown)
                {
                    TryJump();
                    lastJumpAt = Time.time; // Cập nhật thời gian nhảy
                }
            }
            else
            {
                // Nếu không thể/không cần nhảy, đảo hướng
                ReverseDirection();
            }
        }
        // --- XỬ LÝ KHI GẶP RÌA VỰC ---
        else if (!groundAhead && IsGrounded())
        {
            // Điều kiện nhảy qua khe hở:
            // 1. Player phải ở vị trí cao hơn (0.3f)
            // 2. Player phải trong phạm vi khe hở cho phép (maxJumpableGap)
            // 3. Phải qua cooldown nhảy
            if (player != null && player.position.y > transform.position.y + 0.3f &&
            Mathf.Abs(player.position.x - transform.position.x) <= maxJumpableGap &&
            Time.time > lastJumpAt + jumpCooldown) // Dùng jumpCooldown
            {
                JumpTowardsPlayer();
                lastJumpAt = Time.time; // Cập nhật thời gian nhảy
            }
            else
            {
                // Nếu không thể/không cần nhảy, đảo hướng
                ReverseDirection();
            }
        }
    }


    // -- movement helpers --
    public void SimplePatrol()
    {
        if (isAttacking || Time.time < lastDirectionChangeAt + directionChangeCooldown) return;

        if (anim != null) anim.SetBool("Run", true);
        if (Vector2.Distance(transform.position, targetPoint) < 0.2f) targetPoint = (targetPoint == rightPoint) ? leftPoint : rightPoint;
        MoveTowards(targetPoint, patrolSpeed);
    }

    public void SimpleChase()
    {
        if (isAttacking || Time.time < lastDirectionChangeAt + directionChangeCooldown) return;

        if (anim != null) anim.SetBool("Run", true);
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
        if (isAttacking) return;
        lastAttackAt = Time.time;

        rb.velocity = new Vector2(0f, rb.velocity.y);
        isAttacking = true;
        if (anim != null) anim.SetBool("IsAttacking", true);
    }

    // Animation event at hit frame should call this to apply damage
    public void OnAttackHit()
    {
        if (attackHitbox != null && attackData != null)
        {
            attackHitbox.DoAttack(attackData, transform, isFacingRight);
        }
    }

    // Animation event at the END of the attack animation should call this to finish attack state
    public void EndAttack()
    {
        isAttacking = false;
        if (anim != null) anim.SetBool("IsAttacking", false);
    }

    public void ReverseDirection()
    {
        lastDirectionChangeAt = Time.time;

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

        if (isAttacking)
        {
            isAttacking = false;
            if (anim != null) anim.SetBool("IsAttacking", false);
        }
        rb.velocity = Vector2.zero;
        StartCoroutine(HurtRoutine());
    }

    protected IEnumerator HurtRoutine()
    {
        if (anim != null) anim.SetBool("IsHurt", true);
        yield return new WaitForSeconds(0.45f);
        if (anim != null) anim.SetBool("IsHurt", false);
        isHurt = false;
    }

    protected virtual void OnDieLocal()
    {
        isDead = true;
        if (anim != null) anim.SetBool("IsDead", true);
        rb.velocity = Vector2.zero;
        var c = GetComponent<Collider2D>();
        if (c) c.enabled = false;
        this.enabled = false;
        Destroy(gameObject, 3f);
    }

    // -- utilities --
    public void FindPlayer() { var go = GameObject.FindGameObjectWithTag("Player"); if (go != null) player = go.transform; }
    public void FlipByVelocity() { if (rb.velocity.x > 0.1f && !isFacingRight) Flip(); else if (rb.velocity.x < -0.1f && isFacingRight) Flip(); }

    /// <summary> Quay mặt về hướng ngược lại. </summary>
    public void Flip() { isFacingRight = !isFacingRight; var s = transform.localScale; s.x *= -1f; transform.localScale = s; }

    /// <summary> Buộc Golem quay mặt về phía vị trí mục tiêu. </summary>
    public void FaceTarget(Vector2 targetPosition)
    {
        float direction = targetPosition.x - transform.position.x;

        if (direction > 0 && !isFacingRight)
        {
            Flip();
        }
        else if (direction < 0 && isFacingRight)
        {
            Flip();
        }
    }

    public void EnsureGroundCheck() { if (groundCheck != null) return; GameObject go = new GameObject("GroundCheck"); go.transform.parent = transform; go.transform.localPosition = Vector3.zero; groundCheck = go.transform; groundCheckRadius = 0.12f; }
    public bool IsGrounded() { if (groundCheck == null) return false; return Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer); }
    public void UpdateAnimationFlags()
    {
        if (anim == null) return;
        anim.SetBool("IsAttacking", isAttacking);
        anim.SetBool("IsHurt", isHurt);
        anim.SetBool("IsDead", isDead);
        anim.SetBool("IsJumping", !IsGrounded() && rb.velocity.y > 0.05f);
        anim.SetBool("Run", IsGrounded() && Mathf.Abs(rb.velocity.x) > 0.1f);
    }

    // Expose helper: simulate jump arc to decide if enemy should attempt jump
    public bool CanReachByJump(Vector2 target)
    {
        return JumpMathUtility.CanReachByJump(transform.position, target, patrolSpeed, jumpForce, Mathf.Abs(Physics2D.gravity.y * rb.gravityScale), groundLayer, 0.02f, 2f, groundCheckRadius);
    }

    // Debug gizmos (Đã chỉnh lại để Gizmo vẽ đúng vị trí kiểm tra)
    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }

        Vector2 dir = isFacingRight ? Vector2.right : Vector2.left;

        Vector2 baseOrigin = groundCheck != null ? (Vector2)groundCheck.position : (Vector2)transform.position;
        Vector2 rayOrigin = baseOrigin + (Vector2.up * 0.1f);
        float groundCheckRayLength = 1.3f;

        // Wall Check Gizmo (Horizontal)
        Gizmos.color = Color.red;
        Gizmos.DrawLine(rayOrigin, rayOrigin + dir * wallCheckDistance);

        // Ledge Check Gizmo (Vertical)
        Gizmos.color = Color.yellow;
        Vector2 ledgeCheckStart = rayOrigin + dir * ledgeCheckDistance;
        Gizmos.DrawLine(ledgeCheckStart, ledgeCheckStart + Vector2.down * groundCheckRayLength);
    }
}

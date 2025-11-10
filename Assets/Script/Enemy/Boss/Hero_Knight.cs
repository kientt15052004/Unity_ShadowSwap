using UnityEngine;
using System.Collections;

public class Hero_Knight : EnemyCore
{
    [Header("Boss Stats")]
    public int bossMaxHealth = 150;
    public int bossDamage = 15;
    public override bool IsBossType => true;

    [Header("Boss Behavior")]
    public float patrolRadius = 10f;
    public float dodgeDistance = 2f;
    public float dodgeDuration = 0.2f;
    public float blockDuration = 1.0f;
    [Range(0f, 1f)] public float blockChance = 0.3f;
    public float actionCooldown = 0.5f;
    private float lastActionTime = -999f;

    [Header("Attack Fallback")]
    [Tooltip("Nếu Animator không có event OnAttackHit, sử dụng fallback coroutine với delay này (số giây từ bắt đầu animation đến hit frame).")]
    public float attackHitDelay = 0.22f;
    [Tooltip("Nếu Animator không có event EndAttack, kết thúc attack sau attackDuration (giây).")]
    public float attackDuration = 0.6f;
    [Tooltip("Nếu true, sẽ tự động fallback khi không thấy animation event (debug).")]
    public bool allowAttackFallback = true;
    public Animator animator;
    private Vector2 initialPosition;

    // cache for whether animator events are expected (manually set to true if you added events)
    [Header("Animator event flags")]
    [Tooltip("Bật true nếu animation attack của bạn có event gọi OnAttackHit() và EndAttack() chính xác.")]
    public bool animatorHasAttackEvents = false;

    protected override void Awake()
    {
        base.Awake(); // đảm bảo EnemyCore khởi tạo anim, rb, healthComp, v.v.

        // --- Ensure animator assigned ---
        if (anim == null)
        {
            anim = GetComponent<Animator>();
            if (anim == null)
            {
                // try children
                anim = GetComponentInChildren<Animator>(true);
            }
            if (anim == null)
            {
                Debug.LogWarning($"[{name}] Animator not found on GameObject or children. Please add an Animator or assign it in Inspector.");
            }
            else
            {
                Debug.Log($"[{name}] Auto-assigned Animator: {anim.gameObject.name}");
            }
        }

        // --- Ensure attackHitbox assigned ---
        if (attackHitbox == null)
        {
            attackHitbox = GetComponentInChildren<AttackHitbox>(true);
            if (attackHitbox != null) Debug.Log($"[{name}] Auto-assigned AttackHitbox from children.");
        }

        // --- Ensure attackData assigned ---
        if (attackData == null)
        {
            // create lightweight runtime AttackData as fallback (still recommend assigning asset in Inspector)
            attackData = ScriptableObject.CreateInstance<AttackData>();
            attackData.damage = bossDamage;
            attackData.range = Mathf.Max(0.6f, attackRange);
            attackData.cooldown = Mathf.Max(0.2f, attackCooldown);
            attackData.hitLayers = attackData.hitLayers == 0 ? ~0 : attackData.hitLayers;
            Debug.LogWarning($"[{name}] attackData missing -> runtime AttackData created as fallback. Assign real asset in Inspector.");
        }
    }

    protected override void Start()
    {
        base.Start();
        initialPosition = transform.position;
        patrolDistance = patrolRadius;
        maxHealth = bossMaxHealth;
        currentHealth = maxHealth;

        Debug.Log($"[{name}] started. attackData.damage={attackData?.damage ?? -1}, attackHitbox={(attackHitbox != null)}");
    }

    // SAFE Update override (uses EnemyCore helpers)
    protected override void Update()
    {
        // guard: ensure anim exists before calling SetBool directly
        if (anim == null && rb == null)
        {
            // try minimal recovery: get components
            rb = GetComponent<Rigidbody2D>();
            anim = GetComponent<Animator>();
        }

        // protect against animator null when calling animation methods
        // use the protected helper SetAnimatorBoolSafe if available in EnemyCore (preferred)
        // but also guard direct anim calls
        if (IsDead)
        {
            base.Update();
            return;
        }

        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (anim == null) anim = GetComponent<Animator>(); // last attempt

        if (IsHurt || IsBlocking)
        {
            UpdateAnimationFlags();
            return;
        }

        if (player == null)
        {
            FindPlayer();
            if (player == null)
            {
                UpdateAnimationFlags();
                return;
            }
        }

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        FaceTarget(player.position);

        if (distanceToPlayer <= chaseDistance)
        {
            if (distanceToPlayer <= attackRange)
            {
                if (rb != null) rb.velocity = new Vector2(0f, rb.velocity.y);
                TryAttack();
                // fallback start if no animation events (allowAttackFallback controls this)
                if (allowAttackFallback && !animatorHasAttackEvents && isAttacking && !IsInvoking(nameof(StartAttackFallback)))
                {
                    Invoke(nameof(StartAttackFallback), 0f);
                }
            }
            else
            {
                if (!IsAttacking)
                {
                    SimpleChase();
                }
            }
        }
        else
        {
            if (rb != null) rb.velocity = new Vector2(0f, rb.velocity.y);
            // safe set animator flag
            try
            {
                // Prefer protected helper if available
                animator.SetBool("Run", false);
            }
            catch
            {
                if (anim != null) anim.SetBool("Run", false);
            }
        }

        UpdateAnimationFlags();
        base.Update();
    }

    // If animator events missing, fallback will call this to time the hit and end attack
    private void StartAttackFallback()
    {
        // only start fallback when isAttacking true and an attack isn't already scheduled
        if (!isAttacking) return;
        StartCoroutine(AttackFallbackCoroutine());
    }

    private IEnumerator AttackFallbackCoroutine()
    {
        // wait until hit frame
        yield return new WaitForSeconds(attackHitDelay);

        // call same method animation event would call
        OnAttackHit(); // this will use attackHitbox or fallback damage

        // wait rest of animation then end attack
        yield return new WaitForSeconds(Mathf.Max(0f, attackDuration - attackHitDelay));

        EndAttack();
    }

    // Override OnAttackHit to ensure hitbox is used and fallback damage applied if needed
    public override void OnAttackHit()
    {
        // 1) If attackHitbox present and attackData present -> use it (this covers normal case)
        if (attackHitbox != null && attackData != null)
        {
            attackHitbox.DoAttack(attackData, transform, isFacingRight);
        }
        else
        {
            Debug.LogWarning($"[{name}] OnAttackHit: attackHitbox or attackData missing. Applying fallback direct damage to player if possible.");
            // fallback: direct damage to player if available
            if (player != null)
            {
                var playerGO = player.gameObject;
                // prefer IDamageable
                var id = playerGO.GetComponent<IDamageable>();
                if (id != null)
                {
                    id.TakeDamage(new DamageInfo(attackData != null ? attackData.damage : bossDamage, transform.position, gameObject, false));
                }
                else
                {
                    var hm = playerGO.GetComponent<HealthManager>();
                    if (hm != null)
                        hm.TakeDamage(attackData != null ? attackData.damage : bossDamage);
                    else
                        Debug.LogWarning($"[{name}] Fallback: player has no IDamageable nor HealthManager -> cannot apply damage.");
                }
            }
        }
    }

    // When boss dies, ensure attack fallback coroutines are stopped and healthbar updated
    protected override void OnDieLocal()
    {
        // stop fallback coroutine if running
        StopAllCoroutines();

        // ensure the healthbar and UI get updated to zero if needed
        if (healthBarInstance != null)
        {
            healthBarInstance.UpdateHealth(0);
        }

        base.OnDieLocal();
    }

    // override damage handler to maybe perform block/dodge
    protected override void OnTakeDamageLocal(DamageInfo info)
    {
        if (IsDead) return;

        if (Time.time < lastActionTime + actionCooldown)
        {
            base.OnTakeDamageLocal(info);
            return;
        }

        if (Random.value < blockChance)
        {
            StartCoroutine(BlockRoutine());
        }
        else
        {
            StartCoroutine(DodgeRoutine(info.origin));
        }

        lastActionTime = Time.time;
    }

    private IEnumerator BlockRoutine()
    {
        isBlocking = true;
        rb.velocity = Vector2.zero;
        animator.SetBool("Block", true);
        yield return new WaitForSeconds(blockDuration);
        isBlocking = false;
        animator.SetBool("Block", false);
    }

    private IEnumerator DodgeRoutine(Vector2 attackOrigin)
    {
        isHurt = true;
        float directionToDodge = Mathf.Sign(transform.position.x - attackOrigin.x);
        rb.velocity = new Vector2(directionToDodge * (chaseSpeed * 1.5f), rb.velocity.y);
        // use trigger safely
        if (anim != null) anim.SetTrigger("Dodge");
        yield return new WaitForSeconds(dodgeDuration);
        Vector2 clampedPos = new Vector2(
            Mathf.Clamp(transform.position.x, initialPosition.x - patrolRadius, initialPosition.x + patrolRadius),
            transform.position.y
        );
        transform.position = clampedPos;
        rb.velocity = new Vector2(0f, rb.velocity.y);
        isHurt = false;
    }
}

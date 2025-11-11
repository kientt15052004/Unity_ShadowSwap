using UnityEngine;

public class Hero_Knight : EnemyCore
{
    [Header("Boss Stats")]
    public int bossMaxHealth = 150;
    public int bossDamage = 15;
    public override bool IsBossType => true;

    [Header("Boss Behavior")]
    public float blockDuration = 1.0f;
    [Range(0f, 1f)] public float blockChance = 0.3f;
    public float actionCooldown = 0.5f;
    private float lastActionTime = -999f;

    [Header("Animator")]
    public Animator animator;

    [Header("Health Bar")]
    public HealthBarUI healthBarUI;

    private Vector2 initialPosition;

    [Header("Animator event flags")]
    public bool animatorHasAttackEvents = false;

    protected override void Awake()
    {
        base.Awake();

        // Animator
        if (anim == null)
        {
            anim = animator ?? GetComponent<Animator>() ?? GetComponentInChildren<Animator>(true);
            if (anim != null) animator = anim;
            else Debug.LogWarning($"[{name}] Animator not found.");
        }

        // AttackHitbox
        if (attackHitbox == null)
            attackHitbox = GetComponentInChildren<AttackHitbox>(true);

        // AttackData fallback
        if (attackData == null)
        {
            attackData = ScriptableObject.CreateInstance<AttackData>();
            attackData.damage = bossDamage;
            attackData.range = Mathf.Max(0.6f, attackRange);
            attackData.cooldown = Mathf.Max(0.2f, attackCooldown);
            attackData.hitLayers = attackData.hitLayers == 0 ? ~0 : attackData.hitLayers;
        }

        initialPosition = transform.position;
    }

    protected override void Start()
    {
        base.Start();

        initialPosition = transform.position;
        maxHealth = bossMaxHealth;
        currentHealth = GetComponent<Health>()?.Current ?? maxHealth;

        if (healthBarUI != null)
            healthBarUI.Initialize(transform, maxHealth, true);
    }

    protected override void Update()
    {
        if (IsDead) { base.Update(); return; }
        if (IsBlocking) { UpdateAnimationFlags(); return; }

        if (player == null)
        {
            FindPlayer();
            if (player == null) { UpdateAnimationFlags(); return; }
        }

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        FaceTarget(player.position);

        if (distanceToPlayer <= chaseDistance)
        {
            if (distanceToPlayer <= attackRange)
            {
                rb.velocity = new Vector2(0f, rb.velocity.y);
                TryAttack();
            }
            else
            {
                if (!isAttacking) SimpleChase();
            }
        }
        else
        {
            rb.velocity = new Vector2(0f, rb.velocity.y);
            if (anim != null) SetAnimatorBoolSafe("Run", false);
        }

        UpdateAnimationFlags();
        base.Update();
    }

    // Called by animation event
    public override void OnAttackHit()
    {
        if (attackHitbox != null && attackData != null)
        {
            FaceTarget(player != null ? (Vector2)player.position : (Vector2)transform.position);
            attackHitbox.DoAttack(attackData, transform, isFacingRight);
        }
        else if (player != null)
        {
            int dmg = attackData != null ? attackData.damage : bossDamage;
            if (player.TryGetComponent<IDamageable>(out var id))
                id.TakeDamage(new DamageInfo(dmg, transform.position, gameObject));
            else if (player.TryGetComponent<HealthManager>(out var hm))
                hm.TakeDamage(dmg);
        }
    }

    public void Attack()
    {
        OnAttackHit();
    }

    // Called by animation event
    public new void EndAttack()
    {
        isAttacking = false;
        if (anim != null) SetAnimatorBoolSafe("Attack", false);
    }

    protected override void OnTakeDamageLocal(DamageInfo info)
    {
        if (IsDead) return;

        if (Time.time < lastActionTime + actionCooldown)
        {
            base.OnTakeDamageLocal(info);
            return;
        }

        lastActionTime = Time.time;

        if (Random.value < blockChance)
        {
            // Block only
            isBlocking = true;
            rb.velocity = Vector2.zero;
            if (anim != null) SetAnimatorBoolSafe("Block", true);
            // Animation event EndBlock() will reset isBlocking
        }

        // Update health
        currentHealth = Mathf.Max(currentHealth - info.amount, 0);
        if (healthBarUI != null)
            healthBarUI.OnDamagedShow(currentHealth);
    }
    // Called by animation event
    public void EndBlock()
    {
        isBlocking = false;
        if (anim != null) SetAnimatorBoolSafe("Block", false);
    }

    protected override void OnDieLocal()
    {
        StopAllCoroutines();
        if (healthBarUI != null)
            healthBarUI.UpdateHealth(0);

        base.OnDieLocal();
        if (anim != null) SetAnimatorBoolSafe("Die", true);
    }
}

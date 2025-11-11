using System.Collections;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;

[RequireComponent(typeof(EnemyCore))]
public class Hero_Knight : GolemAIBase
{
    public int baseDamage = 20;
    public int comboStep = 5;
    int comboBonus = 0;

    protected override void Awake()
    {
        base.Awake();
    }

    protected override void OnExitAttackRange()
    {
        comboBonus = 0;
    }

    public override void OnAttackHit()
    {
        if (core == null || core.player == null) return;

        var playerGO = core.player.gameObject;
        var id = playerGO.GetComponent<IDamageable>();

        int totalDamage = baseDamage + comboBonus;

        if (id == null)
        {
            var hm = playerGO.GetComponent<HealthManager>();
            if (hm != null && core.IsAttacking)
            {
                hm.TakeDamage(totalDamage);
                comboBonus += comboStep;
            }
        }
        else
        {
            // If IDamageable exists, assume base AttackHitbox does baseDamage.
            // Apply only the combo bonus via IDamageable if desired:
            if (core.IsAttacking && comboBonus > 0)
            {
                id.TakeDamage(new DamageInfo(comboBonus, core.transform.position, gameObject, false));
                comboBonus += comboStep;
            }
        }
    }

    public override void OnDamaged(DamageInfo info)
    {
        base.OnDamaged(info);
        comboBonus = 0;
    }

    public override void OnDied()
    {
        base.OnDied();
    }
    //[Header("Stats")]
    //public int maxHealth = 300;
    //public int attackDamage = 20;
    //public int bonusDamage = 5;
    //public float moveSpeed = 6f;
    //[Range(0f, 1f)] public float blockChance = 0.3f;

    //[Header("Attack")]
    //public float attackRange = 2f;
    //public float chaseDistance = 10f;
    //public float stopDistance = 1.5f;  // Khoảng cách tối thiểu giữ với player
    //public float attackCooldown = 1f;
    //public float hitDelay = 0.25f;
    //public float attackTolerance = 0.15f;

    //[Header("Combo")]
    //public float comboResetTime = 2f;
    //private int comboCounter = 0;
    //private float lastComboHitTime = -999f;

    //[Header("Runtime flags")]
    //public bool IsAttacking = false;
    //public bool IsBlocking = false;
    //public bool IsHurt = false;
    //public bool IsDead = false;

    //private Transform player;
    //private int currentHealth;
    //private float lastAttackTime = -999f;

    //[Header("Health UI")]
    //public HealthBarUI healthBar;

    //[Header("Visual")]
    //public Transform visual;
    //private Vector3 originalScale;

    //private Animator anim;
    //private int runHash, attackHash, hurtHash, blockHash, deadHash;
    //private float squaredAttackRange;

    //[Header("Ground Check (Optional)")]
    //public LayerMask groundLayer;
    //public Transform groundCheck;
    //public float groundCheckRadius = 0.2f;

    //private void Awake()
    //{
    //    anim = GetComponent<Animator>();
    //    currentHealth = maxHealth;

    //    if (healthBar != null)
    //        healthBar.Initialize(transform, maxHealth, true);

    //    var p = GameObject.FindWithTag("Player");
    //    if (p != null) player = p.transform;

    //    if (visual == null) visual = transform;
    //    originalScale = visual.localScale;

    //    // Animator hashes
    //    runHash = Animator.StringToHash("Run");
    //    attackHash = Animator.StringToHash("IsAttacking");
    //    hurtHash = Animator.StringToHash("IsHurt");
    //    blockHash = Animator.StringToHash("IsBlocking");
    //    deadHash = Animator.StringToHash("IsDead");

    //    squaredAttackRange = (attackRange + attackTolerance) * (attackRange + attackTolerance);
    //}

    //private void Update()
    //{
    //    if (IsDead || player == null) return;

    //    // Combo reset
    //    if (Time.time - lastComboHitTime > comboResetTime)
    //        comboCounter = 0;

    //    float dist = Vector2.Distance(transform.position, player.position);

    //    if (dist <= attackRange + attackTolerance)
    //    {
    //        // Player trong tầm tấn công → tấn công
    //        FaceTarget(player.position);
    //        SafeAnimSetBool(runHash, false);
    //        TryAttack();
    //    }
    //    else if (dist > stopDistance && dist <= chaseDistance)
    //    {
    //        // Chase nhưng giữ khoảng cách stopDistance
    //        FaceTarget(player.position);
    //        ChasePlayerWithStopDistance(dist);
    //        SafeAnimSetBool(runHash, true);
    //    }
    //    else
    //    {
    //        // Player quá gần hoặc quá xa → dừng lại
    //        SafeAnimSetBool(runHash, false);
    //    }
    //}

    //private void ChasePlayerWithStopDistance(float currentDistance)
    //{
    //    if (player == null) return;

    //    Vector3 dir = (player.position - transform.position).normalized;

    //    // Boss chỉ di chuyển nếu còn xa hơn stopDistance
    //    if (currentDistance > stopDistance)
    //    {
    //        transform.position += dir * moveSpeed * Time.deltaTime;
    //    }
    //}

    //private bool IsGrounded()
    //{
    //    if (groundCheck == null) return true;
    //    return Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
    //}

    //private void TryAttack()
    //{
    //    if (IsAttacking || IsHurt || IsDead) return;
    //    if (Time.time < lastAttackTime + attackCooldown) return;

    //    lastAttackTime = Time.time;
    //    IsAttacking = true;
    //    SafeAnimSetBool(attackHash, true);

    //    StartCoroutine(PerformAttack());
    //}

    //private IEnumerator PerformAttack()
    //{
    //    yield return new WaitForSeconds(hitDelay);

    //    if (player != null)
    //    {
    //        float sqrDist = ((Vector2)player.position - (Vector2)transform.position).sqrMagnitude;
    //        if (sqrDist <= squaredAttackRange)
    //        {
    //            int totalDamage = attackDamage;
    //            comboCounter = (Time.time - lastComboHitTime <= comboResetTime) ? comboCounter + 1 : 1;
    //            lastComboHitTime = Time.time;
    //            totalDamage += comboCounter * bonusDamage;

    //            IDamageable dmg = player.GetComponent<IDamageable>()
    //                            ?? player.GetComponentInChildren<IDamageable>();
    //            dmg?.TakeDamage(new DamageInfo(totalDamage, transform.position, gameObject, false));
    //        }
    //    }

    //    float remaining = attackCooldown - hitDelay;
    //    if (remaining > 0f) yield return new WaitForSeconds(remaining);

    //    IsAttacking = false;
    //    SafeAnimSetBool(attackHash, false);
    //}

    //public void TakeDamage(int damage)
    //{
    //    if (IsDead || currentHealth <= 0) return;

    //    if (Random.value < blockChance)
    //    {
    //        IsBlocking = true;
    //        SafeAnimSetBool(blockHash, true);
    //        Invoke(nameof(EndBlock), 0.5f);
    //        return;
    //    }

    //    currentHealth -= damage;
    //    currentHealth = Mathf.Max(0, currentHealth);
    //    healthBar?.OnDamagedShow(currentHealth);

    //    IsHurt = true;
    //    SafeAnimSetBool(hurtHash, true);
    //    Invoke(nameof(EndHurt), 0.5f);

    //    if (currentHealth == 0) Die();
    //}

    //private void EndBlock()
    //{
    //    IsBlocking = false;
    //    SafeAnimSetBool(blockHash, false);
    //}

    //private void EndHurt()
    //{
    //    IsHurt = false;
    //    SafeAnimSetBool(hurtHash, false);
    //}

    //private void Die()
    //{
    //    IsDead = true;
    //    SafeAnimSetBool(deadHash, true);
    //    IsAttacking = false;
    //    SafeAnimSetBool(attackHash, false);
    //    SafeAnimSetBool(runHash, false);
    //    enabled = false;
    //}

    //private void FaceTarget(Vector2 target)
    //{
    //    if (visual == null) return;
    //    float dx = target.x - transform.position.x;
    //    Vector3 s = originalScale;
    //    s.x = dx >= 0 ? Mathf.Abs(originalScale.x) : -Mathf.Abs(originalScale.x);
    //    visual.localScale = s;
    //}

    //private void SafeAnimSetBool(int hash, bool value)
    //{
    //    anim?.SetBool(hash, value);
    //}

    //private void OnDrawGizmosSelected()
    //{
    //    Gizmos.color = Color.red;
    //    Gizmos.DrawWireSphere(transform.position, attackRange);
    //    Gizmos.color = Color.yellow;
    //    Gizmos.DrawWireSphere(transform.position, attackRange + attackTolerance);
    //    if (groundCheck != null)
    //    {
    //        Gizmos.color = Color.blue;
    //        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    //    }
    //}
}

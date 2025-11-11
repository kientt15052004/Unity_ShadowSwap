using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(BossHealth))]
[RequireComponent(typeof(Rigidbody2D))]
public class MartinalHero : MonoBehaviour
{
    [Header("Boss Stats")]
    public int baseDamage = 40;
    public float moveSpeed = 6f;
    public float attackRange = 2f;
    public float chaseDistance = 10f;
    public float stopDistance = 1.5f;

    [Header("Health & UI")]
    public BossHealth bossHealth;
    public HealthBarUI healthBarUI;

    [Header("AI")]
    public Transform player;

    private bool isAttacking = false;

    [Header("Animator")]
    public Animator anim;
    private int attackHash, deadHash, hurtHash, runHash;

    private Rigidbody2D rb;

    // Buff
    private float tempDamageMultiplier = 0f;
    private int tempExtraHP = 0;

    // Events
    public Action OnBossDamaged;
    public Action OnBossDied;

    // Thời gian giả định animation attack (nên khớp với clip)
    public float attackDuration = 1f;

    private void Awake()
    {
        if (bossHealth == null) bossHealth = GetComponent<BossHealth>();

        if (bossHealth != null)
        {
            bossHealth.OnHealthChanged += BossTookDamage;
            bossHealth.OnHealthDied += BossDied;
        }

        if (healthBarUI != null)
            healthBarUI.Initialize(transform, bossHealth.MaxHealth, true);

        if (player == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null) player = p.transform;
        }

        rb = GetComponent<Rigidbody2D>();

        if (anim == null)
            anim = GetComponent<Animator>();

        attackHash = Animator.StringToHash("IsAttacking");
        deadHash = Animator.StringToHash("IsDead");
        hurtHash = Animator.StringToHash("IsHurt");
        runHash = Animator.StringToHash("Run");
    }

    private void Update()
    {
        if (player == null || bossHealth.IsDead) return;

        FacePlayer();
        float dist = Vector2.Distance(transform.position, player.position);

        // Nếu player trong tầm attack → dừng chạy và tấn công
        if (!isAttacking && dist <= attackRange)
        {
            anim.SetBool(runHash, false);
            StartCoroutine(PerformAttack());
            return;
        }

        // Nếu player ngoài tầm attack nhưng trong tầm chase → chase
        if (!isAttacking && dist > stopDistance && dist <= chaseDistance)
        {
            Vector2 dir = (player.position - transform.position).normalized;
            rb.MovePosition(rb.position + dir * moveSpeed * Time.deltaTime);
            anim.SetBool(runHash, true);
        }
        else
        {
            anim.SetBool(runHash, false);
        }
    }

    private void FacePlayer()
    {
        if (player == null) return;
        Vector3 euler = transform.eulerAngles;
        euler.y = (player.position.x >= transform.position.x) ? 0f : 180f;
        transform.eulerAngles = euler;
    }

    private IEnumerator PerformAttack()
    {
        isAttacking = true;
        anim.SetBool(attackHash, true);

        // Đợi animation kết thúc (1s hoặc đúng clip length)
        yield return new WaitForSeconds(attackDuration);

        // Reset attack
        anim.SetBool(attackHash, false);
        isAttacking = false;
    }

    // Animation Event
    public void DealDamage()
    {
        if (player == null) return;

        float dist = Vector2.Distance(transform.position, player.position);
        if (dist <= attackRange)
        {
            int damage = GetDamage();
            IDamageable dmg = player.GetComponent<IDamageable>() ?? player.GetComponentInChildren<IDamageable>();
            dmg?.TakeDamage(new DamageInfo(damage, transform.position, gameObject, false));
        }
    }

    private void BossTookDamage(int currentHp)
    {
        OnBossDamaged?.Invoke();
        healthBarUI?.UpdateHealth(currentHp);
        anim.SetBool(hurtHash, true);
    }

    public void OnHurtAnimationEnd()
    {
        anim.SetBool(hurtHash, false);
    }

    private void BossDied()
    {
        OnBossDied?.Invoke();
        StopAllCoroutines();
        anim.SetBool(deadHash, true);
        anim.SetBool(attackHash, false);
        anim.SetBool(runHash, false);
        rb.velocity = Vector2.zero;
    }

    public void ApplyTemporaryBuff(float damagePercent, int extraHP, float duration)
    {
        tempDamageMultiplier = damagePercent;
        tempExtraHP = extraHP;
        bossHealth.Heal(extraHP);
        Invoke(nameof(RemoveTempBuff), duration);
    }

    private void RemoveTempBuff()
    {
        tempDamageMultiplier = 0f;
        tempExtraHP = 0;
    }

    public int GetDamage()
    {
        return Mathf.RoundToInt(baseDamage * (1f + tempDamageMultiplier));
    }
}

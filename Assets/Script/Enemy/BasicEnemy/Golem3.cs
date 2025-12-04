using UnityEngine;

[RequireComponent(typeof(EnemyCore))]
public class Golem3 : GolemAIBase
{
    [Header("Damage Settings")]
    public int baseDamage = 40;
    public int comboStep = 2;
    private int comboBonus = 0;

    public int fallbackDamage = 10;

    [Header("Player Speed Settings")]
    public float playerCloseSpeed = 3f;
    public float playerFarSpeed = 5f;
    private bool playerSpeedReduced = false;

    [Header("References")]
    public HealthBarUI playerHealthBar; // gán HealthBarUI của player nếu có

    protected override void Awake()
    {
        base.Awake();
    }

    protected override void OnEnterAttackRange()
    {
        ApplyPlayerSpeed(true);
    }

    protected override void OnExitAttackRange()
    {
        comboBonus = 0;
        ApplyPlayerSpeed(false);
    }

    private void ApplyPlayerSpeed(bool inRange)
    {
        if (core == null || core.player == null) return;
        if (core.player.gameObject.TryGetComponent<PlayerMove>(out PlayerMove pm))
        {
            if (inRange && !playerSpeedReduced)
            {
                pm.SetupMove(playerCloseSpeed, 5f);
                playerSpeedReduced = true;
            }
            else if (!inRange && playerSpeedReduced)
            {
                pm.SetupMove(playerFarSpeed, 8f);
                playerSpeedReduced = false;
            }
        }
    }

    public override void OnAttackHit()
    {
        if (core == null || core.player == null) return;

        // 1. Trigger attack animation
        base.OnAttackHit();

        var playerGO = core.player.gameObject;
        int totalDamage = baseDamage + comboBonus;

        var id = playerGO.GetComponent<IDamageable>();
        if (id != null)
        {
            // Player là IDamageable, attack đã xử lý
            if (core.IsAttacking && comboBonus > 0)
            {
                id.TakeDamage(new DamageInfo(comboBonus, core.transform.position, gameObject, false));
                comboBonus += comboStep;
            }
        }
        else
        {
            var hm = playerGO.GetComponent<HealthManager>();
            if (hm != null)
            {
                // Gây damage + combo
                int damageToApply = Mathf.Max(fallbackDamage, totalDamage);
                hm.TakeDamage(damageToApply);

                // Update HealthBar nếu có
                if (playerHealthBar != null)
                    playerHealthBar.OnDamagedShow(hm.currentHealth);

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
        comboBonus = 0;


        ApplyPlayerSpeed(false);
    }
}

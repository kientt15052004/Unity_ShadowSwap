using UnityEngine;

[RequireComponent(typeof(EnemyCore))]
public class Golem3 : GolemAIBase
{
    public int baseDamage = 10;
    public int comboStep = 2;
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
}

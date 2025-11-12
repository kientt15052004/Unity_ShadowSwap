using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(EnemyCore))]
public class MartinalHero : GolemAIBase
{
    public int baseDamage = 40;
    public int comboStep = 2;
    int comboBonus = 0;

    public int fallbackDamage = 10;
    public float playerCloseSpeed = 3f;
    public float playerFarSpeed = 5f;
    bool playerSpeedReduced = false;

    protected override void Awake()
    {
        base.Awake();
    }

    protected override void OnExitAttackRange()
    {
        comboBonus = 0;
        ApplyPlayerSpeed(false);
    }

    protected override void OnEnterAttackRange()
    {
        ApplyPlayerSpeed(true);
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

using UnityEngine;

[RequireComponent(typeof(EnemyCore))]
public class Golem2 : GolemAIBase
{
    public int fallbackDamage = 10;

    protected override void Awake()
    {
        base.Awake();
    }

    public override void OnAttackHit()
    {
        if (core == null || core.player == null) return;

        var playerGO = core.player.gameObject;
        var id = playerGO.GetComponent<IDamageable>();
        if (id != null) return; // assume AttackHitbox handled it

        var hm = playerGO.GetComponent<HealthManager>();
        if (hm != null) hm.TakeDamage(fallbackDamage);
    }

    public override void OnDamaged(DamageInfo info)
    {
        base.OnDamaged(info);
    }

    public override void OnDied()
    {
        base.OnDied();
    }
}

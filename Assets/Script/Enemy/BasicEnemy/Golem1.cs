using UnityEngine;

[RequireComponent(typeof(EnemyCore))]
public class Golem1 : GolemAIBase
{
    public int fallbackDamage = 10;
    public float playerCloseSpeed = 3f;
    public float playerFarSpeed = 5f;
    bool playerSpeedReduced = false;

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
        ApplyPlayerSpeed(false);
    }

    public override void OnAttackHit()
    {
        if (core == null || core.player == null) return;

        var playerGO = core.player.gameObject;
        var id = playerGO.GetComponent<IDamageable>();
        if (id != null)
        {
            // nếu player là IDamageable thì AttackHitbox (core) đã xử lý; không làm gì thêm
            return;
        }

        var hm = playerGO.GetComponent<HealthManager>();
        if (hm != null)
        {
            hm.TakeDamage(fallbackDamage);
        }
    }

    public override void OnDamaged(DamageInfo info)
    {
        base.OnDamaged(info);
        // thêm hiệu ứng nếu cần
    }

    public override void OnDied()
    {
        base.OnDied();
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
}

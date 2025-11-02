using UnityEngine;

[RequireComponent(typeof(EnemyCore))]
public class GolemBase : MonoBehaviour
{
    protected EnemyCore core;

    protected virtual void Awake()
    {
        core = GetComponent<EnemyCore>();
    }

    protected virtual void Start()
    {
        // nothing by default
    }

    protected virtual void Update()
    {
        // fallback behavior: patrol/chase/attack
        if (core.player == null) core.FindPlayer();
        if (core.player == null) return;
        float d = Vector2.Distance(core.transform.position, core.player.position);
        if (d <= core.attackRange) core.TryAttack();
        else if (d <= core.chaseDistance) core.SimpleChase();
        else core.SimplePatrol();
    }

    protected virtual void FixedUpdate()
    {
        // default: call core's default FixedUpdate checks through core (which already runs them)
    }

    // animation event hook
    public virtual void OnAttackHit() { core.OnAttackHit(); }

    // damage hooks
    public virtual void OnDamaged(DamageInfo info) { /* optional override */ }
    public virtual void OnDied() { /* optional override */ }
}
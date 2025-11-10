using UnityEngine;

[RequireComponent(typeof(EnemyCore))]
public abstract class GolemAIBase : MonoBehaviour
{
    protected EnemyCore core;
    [Header("AI Tunables")]
    public float detectionRange = 8f; // when to start chasing (not strictly needed if using core.chaseDistance)
    public float stopDistance = 0.9f;

    protected bool prevInRange = false;

    protected virtual void Awake()
    {
        if (core == null) core = GetComponent<EnemyCore>();
        if (core == null) Debug.LogError($"{name} GolemAIBase requires an EnemyCore on the same GameObject.");
    }

    protected virtual void Update()
    {
        if (core == null) return;

        core.UpdateAnimationFlags();

        if (core.IsDead || core.IsHurt) return;

        if (core.player == null)
        {
            core.FindPlayer();
            if (core.player == null) return;
        }

        float dist = Vector2.Distance(core.transform.position, core.player.position);
        bool inRange = dist <= core.attackRange;
        bool withinChase = dist <= core.chaseDistance;

        if (inRange && !prevInRange) OnEnterAttackRange();
        else if (!inRange && prevInRange) OnExitAttackRange();
        prevInRange = inRange;

        if (inRange)
        {
            core.FaceTarget(core.player.position);
            OnAttackTriggered();
            core.TryAttack();
        }
        else
        {
            if (core.IsAttacking) core.EndAttack();

            if (withinChase)
            {
                core.FaceTarget(core.player.position);
                core.SimpleChase();
                core.FlipByVelocity();
            }
            else
            {
                core.SimplePatrol();
                core.FlipByVelocity();
            }
        }
    }

    // Hooks
    protected virtual void OnEnterAttackRange() { }
    protected virtual void OnExitAttackRange() { }
    protected virtual void OnAttackTriggered() { }
    public virtual void OnAttackHit() { }
    public virtual void OnDamaged(DamageInfo info) { }
    public virtual void OnDied() { }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(GolemBase))]
public class Golem1 : GolemBase
{
    private bool prevAttack = false;


    protected override void Update()
    {
        if (core.player == null) core.FindPlayer();
        if (core.player == null) return;


        float distanceToPlayer = Vector2.Distance(core.transform.position, core.player.position);
        bool isAttacking = distanceToPlayer <= core.attackRange;


        if (isAttacking) core.TryAttack();
        else if (distanceToPlayer <= core.chaseDistance) core.SimpleChase();
        else core.SimplePatrol();


        // preserve original prevAttack behavior modifying PlayerMove
        var pm = core.player.GetComponent<PlayerMove>();
        if (pm != null && prevAttack != isAttacking)
        {
            if (!isAttacking) pm.SetupMove(5f, 7f);
            else pm.SetupMove(4f, 6f);
            prevAttack = isAttacking;
        }
    }


    public override void OnAttackHit()
    {
        base.OnAttackHit();
        // additional effects if needed
    }


    public override void OnDamaged(DamageInfo info)
    {
        // preserve hurt routine (core handles animations)
    }


    public override void OnDied()
    {
        base.OnDied();
        // restore player move as in original
        if (core.player != null)
        {
            var pm = core.player.GetComponent<PlayerMove>();
            if (pm != null) pm.SetupMove(5f, 7f);
        }
    }
}

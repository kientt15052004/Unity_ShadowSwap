using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(GolemBase))]
public class Golem2 : GolemBase
{
    protected override void Update()
    {
        if (core.player == null) core.FindPlayer();
        if (core.player == null) return;


        float distanceToPlayer = Vector2.Distance(core.transform.position, core.player.position);
        if (distanceToPlayer <= core.attackRange) core.TryAttack();
        else if (distanceToPlayer <= core.chaseDistance) core.SimpleChase();
        else core.SimplePatrol();
    }


    // Golem2 uses core's FixedUpdate wall/ledge logic by default. Additional overrides can be added.
}

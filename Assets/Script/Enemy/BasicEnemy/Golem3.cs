using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(GolemBase))]
public class Golem3 : GolemBase
{
    private int baseDamage = -1;


    //protected virtual void Start()
    //{
    //    base.Start();
    //    if (core.attackData != null) baseDamage = core.attackData.damage;
    //}


    protected virtual void Update()
    {
        if (core.player == null) core.FindPlayer();
        if (core.player == null) return;


        float d = Vector2.Distance(core.transform.position, core.player.position);
        if (d <= core.attackRange)
        {
            if (core.attackData != null && baseDamage > 0) core.attackData.damage = Mathf.CeilToInt(baseDamage * 1.5f);
            core.TryAttack();
            if (core.attackData != null && baseDamage > 0) core.attackData.damage = baseDamage; // restore
            return;
        }
        if (d <= core.chaseDistance) core.SimpleChase(); else core.SimplePatrol();
    }
}

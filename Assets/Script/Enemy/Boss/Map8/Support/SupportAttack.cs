using System.Collections;
using UnityEngine;

public class SupportAttack : MonoBehaviour
{
    public Transform player;
    public int damage = 10;
    public float shootInterval = 1.5f;

    private void Start()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null) player = p.transform;
        }
        StartCoroutine(ShootLoop());
    }

    private IEnumerator ShootLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(shootInterval);
            TryShootAtPlayer();
        }
    }

    private void TryShootAtPlayer()
    {
        if (player == null) return;

        IDamageable dmg = player.GetComponent<IDamageable>() ?? player.GetComponentInChildren<IDamageable>();
        if (dmg != null)
        {
            dmg.TakeDamage(new DamageInfo(damage, transform.position, gameObject, false));
        }
    }
}

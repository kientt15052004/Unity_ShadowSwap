using System.Collections;
using UnityEngine;

public class SupportHealth : MonoBehaviour
{
    //[Header("References")]
    //public MartinalHero ownerBoss;

    //[Header("Buff parameters")]
    //public float buffDamagePct = 0.25f;
    //public int buffExtraHP = 50;
    //public float buffDuration = 10f;

    //[Header("Timing")]
    //public float buffInterval = 12f;
    //public float firstBuffDelay = 2f;

    //protected float lastBuffAt = -999f;

    //private void Start()
    //{
    //    if (ownerBoss == null)
    //        ownerBoss = GetComponentInParent<MartinalHero>();

    //    if (ownerBoss != null)
    //    {
    //        // subscribe để tự hủy khi boss chết
    //        ownerBoss.OnBossDied += OnOwnerDied;
    //    }

    //    StartCoroutine(BuffLoop());
    //}

    //private void OnDestroy()
    //{
    //    if (ownerBoss != null)
    //        ownerBoss.OnBossDied -= OnOwnerDied;
    //}

    //private void OnOwnerDied()
    //{
    //    Destroy(gameObject);
    //}

    //public void SetOwner(MartinalHero boss)
    //{
    //    if (ownerBoss == boss) return;

    //    if (ownerBoss != null)
    //        ownerBoss.OnBossDied -= OnOwnerDied;

    //    ownerBoss = boss;
    //    if (ownerBoss != null)
    //        ownerBoss.OnBossDied += OnOwnerDied;
    //}

    //IEnumerator BuffLoop()
    //{
    //    yield return new WaitForSeconds(firstBuffDelay);
    //    while (true)
    //    {
    //        TryEmitBuff();
    //        yield return new WaitForSeconds(buffInterval);
    //    }
    //}

    //public void TryEmitBuff()
    //{
    //    if (ownerBoss == null) return;
    //    ownerBoss.ApplyTemporaryBuff(buffDamagePct, buffExtraHP, buffDuration);
    //    lastBuffAt = Time.time;
    //}

    //public void OnOwnerDamaged()
    //{
    //    if (Time.time > lastBuffAt + Mathf.Max(3f, buffInterval * 0.4f))
    //        TryEmitBuff();
    //}
}

using UnityEngine;

// Đảm bảo EnemyCore luôn được thêm vào GameObject này
[RequireComponent(typeof(EnemyCore))]
public class GolemBase : MonoBehaviour
{
    // Biến core được protected để Golem1 có thể truy cập
    protected EnemyCore core;

    protected virtual void Awake()
    {
        // KHẮC PHỤC GỐC RỄ CỦA LỖI NRE: Gán giá trị cho 'core'
        core = GetComponent<EnemyCore>();

        if (core == null)
        {
            Debug.LogError($"GolemBase: Không tìm thấy component EnemyCore trên {gameObject.name}. Vui lòng kiểm tra lại cài đặt component.");
        }
    }

    public virtual void OnAttackHit() { }
    public virtual void OnDamaged(DamageInfo info) { }
    public virtual void OnDied() { }
}
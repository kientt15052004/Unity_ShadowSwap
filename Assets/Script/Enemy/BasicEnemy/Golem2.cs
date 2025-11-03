using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(GolemBase))]
public class Golem2 : GolemBase
{
    // Dùng để theo dõi sự thay đổi trạng thái tấn công nhằm điều chỉnh tốc độ Player
    private bool prevAttack = false;

    // ĐÃ LOẠI BỎ TỪ KHÓA 'override' để khắc phục lỗi biên dịch.
    // Golem1.Update() giờ đây là một phương thức Update tiêu chuẩn của MonoBehaviour,
    // được gọi bởi Unity Engine.
    protected void Update()
    {
        // --- 1. Kiểm tra An Toàn và Trạng Thái ---
        if (core == null || core.IsDead || core.IsHurt)
        {
            return;
        }

        if (core.player == null)
        {
            core.FindPlayer();
            if (core.player == null) return;
        }

        // --- 2. Xác định Khoảng cách và Phạm vi ---
        float distanceToPlayer = Vector2.Distance(core.transform.position, core.player.position);
        bool isInAttackRange = distanceToPlayer <= core.attackRange;

        if (isInAttackRange)
        {
            // --- HÀNH VI 1: TẤN CÔNG (trong tầm) ---
            // BẮT BUỘC: Quay mặt về phía Player trước khi tấn công
            core.FaceTarget(core.player.position);
            core.TryAttack();
        }
        else // Player đã ra khỏi tầm tấn công
        {
            // Buộc thoát khỏi trạng thái tấn công nếu Player ra khỏi tầm
            if (core.IsAttacking)
            {
                core.EndAttack();
            }

            // --- HÀNH VI 2: ĐUỔI THEO hoặc TUẦN TRA ---
            if (distanceToPlayer <= core.chaseDistance)
            {
                // Đuổi theo
                // BẮT BUỘC: Quay mặt về phía Player khi đuổi theo
                core.FaceTarget(core.player.position);
                core.SimpleChase();
            }
            else
            {
                // Tuần tra
                core.SimplePatrol();
            }
        }
    }

    // --- Các hàm Override (được gọi từ EnemyCore) ---

    public override void OnAttackHit()
    {
        base.OnAttackHit();
        HealthManager hm = core.player.GetComponent<HealthManager>();
        if (hm != null)
        {
            hm.TakeDamage(10);
        }
    }


    public override void OnDamaged(DamageInfo info)
    {
        // Gọi hành vi bị thương mặc định
        base.OnDamaged(info);

    }


    public override void OnDied()
    {
        base.OnDied();
        // Khôi phục PlayerMove khi kẻ địch chết
    }
}

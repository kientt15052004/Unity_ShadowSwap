using UnityEngine;

// Golem3 kế thừa từ GolemBase, chứa logic AI cụ thể cho Golem này.
public class Golem3 : GolemBase
{
    // Dùng để theo dõi sự thay đổi trạng thái tấn công nhằm điều chỉnh tốc độ Player
    private bool prevAttack = false;

    // Biến lưu trữ sát thương cộng thêm cho combo
    private int damageInterval = 0;

    protected void Update()
    {
        // --- 1. Kiểm tra An Toàn và Trạng Thái ---
        if (core != null)
        {
            core.UpdateAnimationFlags();
        }
        if (core.IsDead || core.IsHurt)
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

            // LƯU Ý: Sát thương thực tế được xử lý trong OnAttackHit()
        }
        else // Player đã ra khỏi tầm tấn công
        {
            // Buộc thoát khỏi trạng thái tấn công nếu Player ra khỏi tầm
            if (core.IsAttacking)
            {
                core.EndAttack();
                // Khôi phục combo damage khi Golem dừng tấn công (hoặc Player thoát tầm)
                damageInterval = 0;
            }

            // --- HÀNH VI 2: ĐUỔI THEO hoặc TUẦN TRA ---
            if (distanceToPlayer <= core.chaseDistance)
            {
                // Đuổi theo
                // BẮT BUỘC: Quay mặt về phía Player khi đuổi theo
                core.SimpleChase();
                core.FlipByVelocity();
                // Khôi phục combo damage khi Golem chuyển sang đuổi theo
                damageInterval = 0;
            }
            else
            {
                // Tuần tra
                core.SimplePatrol();
                core.FlipByVelocity();
                damageInterval = 0;
            }
        }

    }

    // --- Các hàm Override (được gọi từ EnemyCore) ---

    // Hàm này được gọi khi Animation Event tại frame hit xảy ra
    public override void OnAttackHit()
    {
        // 1. Gây sát thương mặc định (sử dụng logic hitbox của core)
        base.OnAttackHit();

        // 2. Gây sát thương bổ sung theo Combo
        HealthManager hm = core.player.GetComponent<HealthManager>();
        if (hm != null)
        {
            // Sát thương cơ bản (giả định 10) + Sát thương cộng thêm
            int totalBonusDamage = damageInterval+10;

            // Chỉ gây sát thương combo nếu Golem đang tấn công (ngăn chặn bug)
            if (core.IsAttacking)
            {
                hm.TakeDamage(totalBonusDamage);

                // Tăng sát thương cho lần tấn công tiếp theo
                damageInterval += 2;
                Debug.Log($"Golem hit! Bonus Damage: {totalBonusDamage}. Next Bonus: {damageInterval}");
            }
        }
    }


    public override void OnDamaged(DamageInfo info)
    {
        base.OnDamaged(info);

        // Đặt lại sát thương combo khi Golem bị thương
        damageInterval = 0;

    }


    public override void OnDied()
    {
        base.OnDied();
    }
}

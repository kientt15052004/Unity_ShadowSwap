using UnityEngine;

// Golem1 kế thừa từ GolemBase, chứa logic AI cụ thể cho Golem này.
public class Golem1 : GolemBase
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

        // --- 3. Logic Phụ: Điều chỉnh tốc độ Player (Giả định PlayerMove tồn tại) ---
        PlayerMove pm = null;
        if (core.player != null && core.player.gameObject.TryGetComponent(out pm))
        {
            if (prevAttack != isInAttackRange)
            {
                // Điều chỉnh tốc độ chạy của Player dựa trên việc Golem có đang trong tầm tấn công không
                if (!isInAttackRange) pm.SetupMove(5f, 8f);
                else pm.SetupMove(3f, 5f);

                prevAttack = isInAttackRange;
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
        if (core.player != null && core.player.gameObject.TryGetComponent<PlayerMove>(out PlayerMove pm))
        {
            pm.SetupMove(5f, 8f);
        }
    }
}

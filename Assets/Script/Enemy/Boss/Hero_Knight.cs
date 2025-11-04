using UnityEngine;
using System.Collections;

// BossSentinel kế thừa từ EnemyCore
public class Hero_Knight : EnemyCore
{
    [Header("Boss Stats")]
    // Tăng máu và sát thương (Cần thiết lập lại trong Inspector)
    public int bossMaxHealth = 150;
    public int bossDamage = 15;

    [Header("Boss Behavior")]
    public float patrolRadius = 10f; // Phạm vi hoạt động tối đa
    public float dodgeDistance = 2f; // Khoảng cách lùi lại khi né
    public float dodgeDuration = 0.2f;
    public float blockDuration = 1.0f;
    public float blockChance = 0.3f; // Tỉ lệ Boss sẽ chặn thay vì lùi lại
    public float actionCooldown = 0.5f;
    private float lastActionTime = -999f;

    [Header("Defense Detection")]
    public float playerAttackPredictionRange = 1.5f; // Phạm vi Boss dự đoán Player sắp tấn công
    // public float playerAttackCooldownTime = 0.5f; // (Thêm nếu có thể truy cập cooldown của Player)


    private Vector2 initialPosition;

    protected override void Awake()
    {
        base.Awake();
        // Áp dụng chỉ số Boss
        maxHealth = bossMaxHealth;
        // Kiểm tra null cho attackData trước khi truy cập
        if (attackData != null)
        {
            attackData.damage = bossDamage;
        }
        currentHealth = maxHealth;

        initialPosition = transform.position;
    }

    protected override void Start()
    {
        base.Start();
        // Thay đổi phạm vi Patrol để giới hạn phạm vi hoạt động (Arena)
        patrolDistance = patrolRadius;
    }

    protected override void Update()
    {
        // Bắt buộc gọi base.Update() để EnemyCore xử lý các cờ chung
        base.Update();

        // --- 1. Kiểm tra Phòng thủ Chủ động (Smart Defense) ---
        CheckPreemptiveDefense();

        // --- 2. Kiểm tra An Toàn và Trạng Thái ---
        if (IsDead || IsHurt || IsBlocking || IsAttacking)
        {
            // Nếu chết, bị thương, đang chặn hoặc đang tấn công, dừng AI
            return;
        }

        if (player == null)
        {
            FindPlayer();
            if (player == null) return;
        }

        // --- 3. Logic AI Chính (Tấn công / Chase) ---
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        // A. QUAY MẶT VÀO PLAYER
        FaceTarget(player.position);

        // B. HÀNH VI TẤN CÔNG / CHASE
        if (distanceToPlayer <= chaseDistance)
        {
            // B1. Trong tầm tấn công (sát thương)
            if (distanceToPlayer <= attackRange)
            {
                // Ngừng di chuyển khi ở đủ gần để tấn công
                rb.velocity = new Vector2(0f, rb.velocity.y);
                TryAttack(); // Kích hoạt tấn công
            }
            // B2. Đuổi theo
            else
            {
                SimpleChase();
            }
        }
        else
        {
            // C. HÀNH VI ĐỨNG YÊN
            rb.velocity = new Vector2(0f, rb.velocity.y);
            if (anim != null) anim.SetBool("Run", false);
        }
    }

    // HÀM MỚI: Kiểm tra và kích hoạt phòng thủ chủ động
    private void CheckPreemptiveDefense()
    {
        if (player == null || IsDead || IsHurt || IsBlocking || IsAttacking) return;
        if (Time.time < lastActionTime + actionCooldown) return;

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        // Điều kiện dự đoán: Player ở rất gần (có khả năng sắp tấn công)
        if (distanceToPlayer <= playerAttackPredictionRange)
        {
            // Đặt cooldown hành động ngay lập tức để tránh lặp lại
            lastActionTime = Time.time;

            if (Random.value < blockChance)
            {
                // Trigger Block
                StartCoroutine(BlockRoutine());
            }
            else // Trigger Dodge
            {
                // Dodge based on player's position
                StartCoroutine(DodgeRoutine(player.position));
            }
        }
    }

    // --- Coroutine Đỡ Đòn ---
    private IEnumerator BlockRoutine()
    {
        isBlocking = true;
        rb.velocity = Vector2.zero;

        // Phát animation chặn (Giả sử có trigger "Block" trong Animator)
        if (anim != null) anim.SetTrigger("Block");

        yield return new WaitForSeconds(blockDuration);

        isBlocking = false;
        if (anim != null) anim.SetBool("IsBlocking", false); // Tắt cờ nếu animation không tự tắt
    }

    // --- Coroutine Né Đòn ---
    private IEnumerator DodgeRoutine(Vector2 attackOrigin)
    {
        isHurt = true; // Dùng cờ Hurt để ngăn AI di chuyển trong thời gian né
        if (anim != null) anim.SetTrigger("Dodge"); // Phát animation né (Giả sử có trigger "Dodge")

        // Xác định hướng lùi lại (ngược lại với hướng tấn công)
        float directionToDodge = Mathf.Sign(transform.position.x - attackOrigin.x);

        // Lùi lại
        rb.velocity = new Vector2(directionToDodge * (chaseSpeed * 1.5f), rb.velocity.y);

        yield return new WaitForSeconds(dodgeDuration);

        // Đảm bảo Boss không bị đẩy ra khỏi khu vực Arena
        Vector2 clampedPos = new Vector2(
            Mathf.Clamp(transform.position.x, initialPosition.x - patrolRadius, initialPosition.x + patrolRadius),
            transform.position.y
        );
        transform.position = clampedPos;

        rb.velocity = new Vector2(0f, rb.velocity.y);
        isHurt = false;
        if (anim != null) anim.SetBool("IsHurt", false); // Tắt cờ Hurt nếu nó được dùng cho Dodge
    }


    // --- Override cho OnTakeDamageLocal (Xử lý khi sát thương thực sự được nhận) ---
    protected override void OnTakeDamageLocal(DamageInfo info)
    {
        if (IsDead) return;

        // Nếu isBlocking là TRUE, điều đó có nghĩa là:
        // 1. Boss đã kích hoạt BlockRoutine (Phòng thủ chủ động) HOẶC
        // 2. Boss đã bị đánh trong khi đang Block (Block thành công)
        // Trong cả hai trường hợp, không muốn gọi HurtRoutine của base
        if (isBlocking)
        {
            // Do EnemyCore.TakeDamage đã giảm sát thương, ở đây ta chỉ cần log và không làm gì thêm
            Debug.Log($"[{gameObject.name}] Block successful! Reduced {info.amount} damage.");
            // Có thể thêm hiệu ứng/âm thanh chặn ở đây
            return;
        }

        // Nếu không chặn/né (isBlocking = false), gọi base để xử lý sát thương bình thường (HurtRoutine)
        base.OnTakeDamageLocal(info);
    }

    // --- Override cho OnDied (Xử lý cổng khi Boss chết) ---
    protected override void OnDieLocal()
    {
        // Khi Boss chết, khôi phục tốc độ Player (nếu có)
        // Lưu ý: Cần có script PlayerMove để đoạn code này hoạt động
        /*
        if (player != null && player.gameObject.TryGetComponent<PlayerMove>(out PlayerMove pm))
        {
            // Giả định PlayerMove có phương thức SetupMove(float speed, float jumpForce)
            pm.SetupMove(5f, 8f);
        }
        */

        // Thực hiện logic chết của EnemyCore (Set isDead, Animation, v.v.)
        base.OnDieLocal();
    }
}

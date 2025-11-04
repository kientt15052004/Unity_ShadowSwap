using UnityEngine;
using System.Collections; // Cần thiết cho Coroutine

// BossSentinel kế thừa từ EnemyCore (thay vì GolemBase, giả định GolemBase là lớp rỗng)
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

    private Vector2 initialPosition;

    protected override void Awake()
    {
        base.Awake();
        // Áp dụng chỉ số Boss
        maxHealth = bossMaxHealth;
        attackData.damage = bossDamage;
        currentHealth = maxHealth;

        initialPosition = transform.position;
    }

    protected override void Start()
    {
        base.Start();
        // Thay đổi phạm vi Patrol để giới hạn phạm vi hoạt động (Arena)
        patrolDistance = patrolRadius;
    }


    protected void Update()
    {
        // --- 1. Kiểm tra An Toàn và Trạng Thái ---
        if (IsDead || IsHurt || IsBlocking)
        {
            // Nếu chết, bị thương hoặc đang đỡ đòn, không thực hiện AI
            UpdateAnimationFlags();
            return;
        }

        if (player == null)
        {
            FindPlayer();
            if (player == null) return;
        }

        // --- 2. Logic AI Chính ---
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        // A. QUAY MẶT VÀO PLAYER (Boss luôn nhìn Player)
        FaceTarget(player.position);

        // B. HÀNH VI TẤN CÔNG / CHASE
        if (distanceToPlayer <= chaseDistance)
        {
            // B1. Trong tầm tấn công
            if (distanceToPlayer <= attackRange)
            {
                // Ngừng di chuyển khi ở đủ gần để tấn công
                rb.velocity = new Vector2(0f, rb.velocity.y);
                TryAttack();
            }
            // B2. Đuổi theo / Giữ khoảng cách
            else
            {
                // Nếu không đang tấn công, đuổi theo
                if (!IsAttacking)
                {
                    SimpleChase();
                }
            }
        }
        else
        {
            // C. HÀNH VI ĐỨNG YÊN (KHÔNG TUẦN TRA)
            // Nếu Player quá xa, Boss đứng yên và reset vị trí gần tâm Arena (nếu cần)
            rb.velocity = new Vector2(0f, rb.velocity.y);
            // Giữ trạng thái Run = false khi đứng yên
            anim.SetBool("Run", false);
        }

        // --- 3. Cập nhật Animation ---
        UpdateAnimationFlags();
    }

    // HÀM XỬ LÝ NÉ ĐÒN VÀ ĐỠ ĐÒN (Được gọi khi bị tấn công)
    protected override void OnTakeDamageLocal(DamageInfo info)
    {
        if (IsDead) return;

        // --- LOGIC DI CHUYỂN LINH HOẠT VÀ ĐỠ ĐÒN ---
        if (Time.time < lastActionTime + actionCooldown)
        {
            // Nếu đang trong cooldown, chỉ chịu sát thương bình thường
            base.OnTakeDamageLocal(info);
            return;
        }

        // Tỉ lệ chặn
        if (Random.value < blockChance)
        {
            StartCoroutine(BlockRoutine());
        }
        else // Tỉ lệ né
        {
            StartCoroutine(DodgeRoutine(info.origin));
        }

        lastActionTime = Time.time;
    }

    // Coroutine Đỡ Đòn
    private IEnumerator BlockRoutine()
    {
        isBlocking = true;
        rb.velocity = Vector2.zero;

        // Phát animation chặn
        anim.SetTrigger("Block");

        yield return new WaitForSeconds(blockDuration);

        isBlocking = false;
        // Bắt đầu lại Coroutine Hurt thông thường sau khi chặn xong nếu vẫn bị thương.
        // Tuy nhiên, vì logic TakeDamage đã xử lý sát thương giảm, ta chỉ cần thoát
    }

    // Coroutine Né Đòn
    private IEnumerator DodgeRoutine(Vector2 attackOrigin)
    {
        isHurt = true; // Sử dụng cờ Hurt để ngăn AI di chuyển

        // Xác định hướng lùi lại (ngược lại với hướng tấn công)
        float directionToDodge = Mathf.Sign(transform.position.x - attackOrigin.x);

        // Lùi lại
        rb.velocity = new Vector2(directionToDodge * (chaseSpeed * 1.5f), rb.velocity.y);

        // Phát animation né (ví dụ: Hurt)
        anim.SetTrigger("Dodge");

        yield return new WaitForSeconds(dodgeDuration);

        // Đảm bảo Boss không bị đẩy ra khỏi khu vực Arena
        Vector2 clampedPos = new Vector2(
            Mathf.Clamp(transform.position.x, initialPosition.x - patrolRadius, initialPosition.x + patrolRadius),
            transform.position.y
        );
        transform.position = clampedPos;

        rb.velocity = new Vector2(0f, rb.velocity.y);
        isHurt = false;
    }

    // --- LOGIC CỔNG (Gate Blocking) ---

    // HÀM MỚI: Dùng để ngăn Player di chuyển đến cổng
    // Cần gọi hàm này từ một script quản lý cổng hoặc đặt cổng
    // (Giả sử bạn có một script GateManager)
    public void PreventPlayerMovement(GameObject gate)
    {
        // Tìm PlayerMove script
        if (player != null && player.TryGetComponent<PlayerMove>(out PlayerMove pm))
        {
            // Nếu PlayerMove có hàm để vô hiệu hóa di chuyển
            // pm.DisableMovement(); 
            // Hoặc đơn giản là giảm tốc độ Player về 0 khi ở gần cổng
        }

        // Tùy chọn: Có thể spawn một bức tường vô hình (Invisible Wall) tại vị trí cổng
        // Collider2D gateCollider = gate.GetComponent<Collider2D>();
        // if (gateCollider != null) gateCollider.enabled = true;
    }

    // --- Override cho OnDied ---
    protected override void OnDieLocal()
    {
        // Khi Boss chết, khôi phục tốc độ Player (nếu có)
        if (player != null && player.gameObject.TryGetComponent<PlayerMove>(out PlayerMove pm))
        {
            pm.SetupMove(5f, 8f);
        }

        // Thực hiện logic chết của EnemyCore
        base.OnDieLocal();
    }
}
using TMPro;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;
using UnityEngine.UI; // Cần thiết cho Image Icons

public class PlayerMove : MonoBehaviour
{
    // Cài đặt di chuyển và nhảy
    [SerializeField] private float speed = 5f;
    [SerializeField] private float jumpHeight = 7f;
    // Thêm vào đầu class PlayerMove (phần khai báo biến):
    [SerializeField] private float attackCooldown = 0.5f; // thời gian giữa 2 lần chém
    private float attackTimer = 0f;

    private Rigidbody2D body;
    private Animator anim;
    private bool grounded;
    private bool isBusy = false;
    private bool wasGrounded = true;

    // Kiểm tra mặt đất (Thiết lập trong Inspector)
    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;

    // Footstep settings
    [Header("Footstep Settings")]
    [SerializeField] private float footstepInterval = 0.3f; // khoảng thời gian giữa 2 bước
    private float footstepTimer;

    // Hurt sound timer
    [Header("Hurt Sound Settings")]
    [SerializeField] private float hurtSoundInterval = 1f;
    private float hurtSoundTimer = 0f;


    // SHADOW SKILL COOLDOWN
    [Header("Shadow Skill Cooldown")]
    [SerializeField] private float shadowCooldown = 10f;
    private float shadowCooldownTimer = 0f;

    [Header("Shadow Skill UI")]
    [SerializeField] private Image shadowSkillIcon;
    [SerializeField] private TextMeshProUGUI shadowCooldownText;

    // Item Prefabs
    [Header("Item Prefabs")]
    [SerializeField] private GameObject redKeyPrefab; // Prefab của Key Đỏ để Instantiate

    private ShadowManager shadowManager;
    private HealthManager healthManager;
    private PlayerAttackController attackController;
    private float horizontalInput;

    // BIẾN THỐNG KÊ
    private int coinCount;
    private int keyCount;
    private int keyRedCount;
    private int coinScore = 0;
    private int coinCollected = 0;
    private int keyGoldCollected = 0;
    public int keyRedCollected = 0; // PUBLIC để script Portal có thể kiểm tra

    // UI ICONS & TEXT
    [Header("Score UI (Text Only)")]
    public TextMeshProUGUI scoreText;            // Text cho Score (ví dụ: Score: 0)

    [Header("Key UI (Icon + Text)")]
    [SerializeField] private Image keyGoldIconImage; // Icon Key Vàng
    public TextMeshProUGUI keyGoldText;          // Text cho số lượng Key Vàng
    [SerializeField] private Image keyRedIconImage;  // Icon Key Đỏ
    public TextMeshProUGUI keyRedText;           // Text cho số lượng Key Đỏ

    public bool isBlocking = false;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        shadowManager = FindObjectOfType<ShadowManager>();
        healthManager = GetComponent<HealthManager>();
        attackController = GetComponent<PlayerAttackController>();

        // KHỞI TẠO CÁC BIẾN TỔNG SỐ ITEM TẠI ĐÂY
        coinCount = GameObject.FindGameObjectsWithTag("Coin").Length;
        keyCount = GameObject.FindGameObjectsWithTag("Key").Length;
        keyRedCount = GameObject.FindGameObjectsWithTag("KeyRed").Length;
    }

    public void SetupMove(float s, float jh)
    {
        speed = s;
        jumpHeight = jh;
    }


    private void Update()
    {
        // 1. CẬP NHẬT TRẠNG THÁI GROUNDED 
        grounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        horizontalInput = Input.GetAxis("Horizontal");
        // Cập nhật timer cho attack cooldown
        if (attackTimer > 0f)
            attackTimer -= Time.deltaTime;
        // 2. XỬ LÝ NHẢY
        if (Input.GetKeyDown(KeyCode.Space) && grounded)
        {
            body.velocity = new Vector2(body.velocity.x, jumpHeight);
        }





        // Phát âm thanh khi vừa chạm đất
        if (!wasGrounded && grounded)
        {
            AudioManager.Instance?.PlayLand();
        }



        // 3. XỬ LÝ SHADOW SWAP
        // ======== SHADOW SKILL COOLDOWN ========
        if (shadowCooldownTimer > 0f)
        {
            shadowCooldownTimer -= Time.deltaTime;

            // UI hiển thị cooldown
            if (shadowCooldownText != null)
                shadowCooldownText.text = Mathf.Ceil(shadowCooldownTimer).ToString();

            if (shadowSkillIcon != null)
                shadowSkillIcon.color = new Color(1, 1, 1, 0.4f); // icon mờ khi đang hồi
        }
        else
        {
            // UI khi hồi xong
            if (shadowCooldownText != null)
                shadowCooldownText.text = "";

            if (shadowSkillIcon != null)
                shadowSkillIcon.color = new Color(1, 1, 1, 1f); // icon sáng trở lại

            // Chỉ cho dùng khi hồi xong
            if (Input.GetKeyDown(KeyCode.E))
            {
                if (shadowManager != null)
                {
                    shadowManager.CreateShadow(transform.position, transform.rotation, transform.localScale);
                    AudioManager.Instance?.PlayShadowSummon();
                }

                shadowCooldownTimer = shadowCooldown; // Bắt đầu hồi chiêu
            }
        }
        // =======================================

        if (Input.GetKeyDown(KeyCode.Q))
        {
            if (shadowManager != null)
                // Dịch chuyển đến bóng
                shadowManager.TeleportToShadow(transform);
            AudioManager.Instance?.PlayShadowSwap();
        }

        // 8.XỬ LÝ TẤN CÔNG & PHÒNG THỦ

        // Attack 1 (Đánh từ dưới lên) - Chỉ khi trên mặt đất
        // Attack 1 (trên mặt đất)
        if (Input.GetKeyDown(KeyCode.J) && grounded && attackTimer <= 0f)
        {
            isBusy = true;
            anim.SetTrigger("Attack1");
            AudioManager.Instance?.PlayAttack();
            attackTimer = attackCooldown; // bắt đầu hồi đòn
        }
        // Attack 2 (trên không)
        else if (Input.GetKeyDown(KeyCode.K) && !grounded && attackTimer <= 0f)
        {
            isBusy = true;
            anim.SetTrigger("Attack2");
            AudioManager.Instance?.PlayAttack();
            attackTimer = attackCooldown;
        }
        // Block - Chỉ khi trên mặt đất
        else if (Input.GetKeyDown(KeyCode.L) && grounded)
        {
            isBusy = true;
            anim.SetTrigger("Block");
        }


        // 4. XỬ LÝ HÌNH ẢNH (Animation & Xoay)
        anim.SetBool("Grounded", grounded); // Grounded nên cập nhật liên tục

        anim.SetBool("Run", horizontalInput != 0 && grounded);

        if (horizontalInput > 0.01f)
        {
            transform.localScale = new Vector3(2, 2, 1);
        }
        else if (horizontalInput < -0.01f)
        {
            transform.localScale = new Vector3(-2, 2, 1);
        }

        wasGrounded = grounded;
        HandleFootsteps();
        CheckHurtSound();
    }

    private void HandleFootsteps()
    {
        if (grounded && Mathf.Abs(horizontalInput) > 0.1f)
        {
            footstepTimer -= Time.deltaTime;
            if (footstepTimer <= 0f)
            {
                AudioManager.Instance?.PlayFootstep();
                footstepTimer = footstepInterval;
            }
        }
        else
        {
            footstepTimer = 0f;
        }
    }

    private void CheckHurtSound()
    {
        // Kiểm tra xem có đang trong trap không
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, 0.5f);
        bool inTrap = false;

        foreach (Collider2D col in colliders)
        {
            if (col.CompareTag("Trap") && col.isTrigger)
            {
                inTrap = true;
                break;
            }
        }

        if (inTrap && hurtSoundTimer <= 0f)
        {
            AudioManager.Instance?.PlayHurt();
            hurtSoundTimer = hurtSoundInterval;
        }
    }

    private void FixedUpdate()
    {
        if (isBusy)
        {
            body.velocity = new Vector2(0, body.velocity.y); // Dừng di chuyển ngang
            return;
        }
        // Di chuyển ngang (Cho phép di chuyển trên không)
        body.velocity = new Vector2(horizontalInput * speed, body.velocity.y);

        // Xử lý hurt sound timer
        if (hurtSoundTimer > 0f)
        {
            hurtSoundTimer -= Time.fixedDeltaTime;
        }
    }

    // 5. XỬ LÝ VA CHẠM CỨNG (TRAP)
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Trap"))
        {
            if (healthManager != null)
            {
                healthManager.TakeDamage(5); // Sát thương tức thời 5
            }
            AudioManager.Instance?.PlayHurt();
        }
    }

    // 6. XỬ LÝ SÁT THƯƠNG LIÊN TỤC (OnTriggerStay2D)
    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("Trap"))
        {
            if (healthManager != null)
            {
                healthManager.StartContinuousDamage(5); // Sát thương 5 mỗi giây
            }
        }
    }

    // 7. DỪNG SÁT THƯƠNG LIÊN TỤC (OnTriggerExit2D)
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("Trap"))
        {
            if (healthManager != null)
            {
                healthManager.StopContinuousDamage();
            }
        }
    }
    // ----------------------------------------------------------------------
    // 8.ANIMATION EVENTS
    // Hàm này được gọi bởi Animation Event từ Animator (tại frame hit)
    public void OnPlayerAttackHit()
    {
        if (attackController != null)
        {
            attackController.OnPlayerAttackHit();
        }
    }


    // XỬ LÝ TẤT CẢ ITEM PICKUP (OnTriggerEnter2D)
    private void OnTriggerEnter2D(Collider2D other)
    {
        // XỬ LÝ COIN
        if (other.CompareTag("Coin"))
        {
            coinScore += 1; // 1 điểm mỗi Coin
            coinCollected += 1;
            Destroy(other.gameObject);
            UpdateScoreUI();
            AudioManager.Instance?.PlayCoin();
        }
        // XỬ LÝ KEY VÀNG (Tag: "Key")
        else if (other.CompareTag("Key"))
        {
            keyGoldCollected += 1;  // Tăng số lượng Key Vàng hiện có
            coinScore += 10;        // +10 điểm Coin
            Destroy(other.gameObject);
            UpdateScoreUI();
            UpdateKeyGoldUI();
            AudioManager.Instance?.PlayKey();

        }
        // XỬ LÝ KEY ĐỎ (Tag: "KeyRed")
        else if (other.CompareTag("KeyRed"))
        {
            keyRedCollected += 1;  // Tăng số lượng Key Đỏ hiện có
            // Key Đỏ không tăng điểm
            Destroy(other.gameObject);
            UpdateScoreUI();
            UpdateKeyRedUI();
            AudioManager.Instance?.PlayKey();
        }
        // XỬ LÝ HEAL
        else if (other.CompareTag("Heal"))
        {
            if (healthManager != null)
            {
                healthManager.Heal(20); // Hồi cố định 20 máu
                AudioManager.Instance?.PlayHeal();

            }
            Destroy(other.gameObject);
        }
        // XỬ LÝ CHEST
        else if (other.CompareTag("Chest"))
        {
            // Dùng Key Vàng để mở
            if (keyGoldCollected > 0)
            {
                if (redKeyPrefab != null) // Kiểm tra Prefab Key Đỏ
                {
                    keyGoldCollected--;
                    coinScore += 20; // +20 điểm Coin khi mở Rương

                    // TẠO (INSTANTIATE) KEY ĐỎ TẠI VỊ TRÍ RƯƠNG
                    Instantiate(redKeyPrefab, other.transform.position, Quaternion.identity);

                    AudioManager.Instance?.PlayUnlockChest();

                    // Giả định rương biến mất sau khi mở và nhận thưởng
                    Destroy(other.gameObject);
                    UpdateScoreUI();
                    UpdateKeyGoldUI();
                }
                else
                {
                    Debug.LogError("Red Key Prefab is not assigned in the Inspector!");
                }
            }
            else
            {
                // TRƯỜNG HỢP RƯƠNG BỊ KHÓA
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.ShowWarning("Rương bị khóa! Cần Chìa khóa Vàng."); // Cảnh báo
                }
            }
        }
    }

    // HÀM CẬP NHẬT UI
    // Cập nhật Score (Giữ nguyên Text)
    private void UpdateScoreUI()
    {
        // Quay lại cấu trúc Text ban đầu: "Score: [số điểm]"
        scoreText.text = "Score: " + coinScore;
    }

    // Cập nhật Key Vàng (Key Vàng Icon + Key Vàng Text)
    private void UpdateKeyGoldUI()
    {
        if (keyGoldText != null)
        {
            keyGoldText.text = keyGoldCollected.ToString();
        }
        if (keyGoldIconImage != null)
        {
            keyGoldIconImage.gameObject.SetActive(true);
        }
    }

    public void StartBlock()
    {
        isBlocking = true;
    }

    // Tắt block (gọi từ Animation Event frame cuối)
    public void EndBlock()
    {
        isBlocking = false;
        isBusy = false; // Kết thúc trạng thái bận
    }

    public bool IsBlocking()
    {
        return isBlocking;
    }

    // Cập nhật Key Đỏ (Key Đỏ Icon + Key Đỏ Text)
    private void UpdateKeyRedUI()
    {
        if (keyRedText != null)
        {
            keyRedText.text = keyRedCollected.ToString();
        }
        if (keyRedIconImage != null)
        {
            keyRedIconImage.gameObject.SetActive(true);
        }
    }

    // HÀM MỚI: DÙNG KEY ĐỎ (GỌI BỞI PORTAL)
    public void UseRedKey()
    {
        if (keyRedCollected > 0)
        {
            keyRedCollected--;
            UpdateKeyRedUI();
            // Thêm AudioManager.Instance?.Play...() cho âm thanh sử dụng Key nếu cần
        }
    }


    // 9.ANIMATION EVENTS
    // Hàm này phải được gọi bằng Animation Event 
    // tại FRAME CUỐI CÙNG của các animation "Attack1", "Attack2", và "Block"
    public void AnimationFinished()
    {
        isBusy = false;
    }
}
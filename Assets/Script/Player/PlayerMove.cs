using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    // Cài đặt di chuyển và nhảy
    [SerializeField] private float speed = 5f;
    [SerializeField] private float jumpHeight = 7f;

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

    private ShadowManager shadowManager;
    private HealthManager healthManager;
    private float horizontalInput;

    // BIẾN THỐNG KÊ
    private int coinCount;      // Tổng số Coin trong màn chơi
    private int keyCount;       // Tổng số Key trong màn chơi
    private int coinScore = 0;  // Điểm số từ Coin (và Key, Chest)
    private int keyScore = 0;   // Điểm số từ Key (Hiện chưa dùng)
    private int coinCollected = 0; // Số Coin đã nhặt
    private int keyCollected = 0;  // Số Key hiện có (Dùng để mở rương)


    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        shadowManager = FindObjectOfType<ShadowManager>();
        healthManager = GetComponent<HealthManager>();

        // KHỞI TẠO CÁC BIẾN TỔNG SỐ ITEM TẠI ĐÂY
        coinCount = GameObject.FindGameObjectsWithTag("Coin").Length;
        keyCount = GameObject.FindGameObjectsWithTag("Key").Length;

        // Log để kiểm tra tổng số Item đã tìm thấy
        Debug.Log("Total Coins in level: " + coinCount);
        Debug.Log("Total Keys in level: " + keyCount);
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
        
        if (!isBusy)
        {
            // 2. XỬ LÝ NHẢY
            if (Input.GetKeyDown(KeyCode.Space) && grounded)
            {
                body.velocity = new Vector2(body.velocity.x, jumpHeight);
            }

            // 3. XỬ LÝ SHADOW SWAP
            if (Input.GetKeyDown(KeyCode.E))
            {
                if (shadowManager != null)
                    // Tạo bóng, truyền kèm localScale để lật đúng chiều
                    shadowManager.CreateShadow(transform.position, transform.rotation, transform.localScale);
            }

            if (Input.GetKeyDown(KeyCode.Q))
            {
                if (shadowManager != null)
                    // Dịch chuyển đến bóng
                    shadowManager.TeleportToShadow(transform);
            }

        // Phát âm thanh khi vừa chạm đất
        if (!wasGrounded && grounded)
        {
            AudioManager.Instance?.PlayLand();
        }

        // 2. XỬ LÝ NHẢY
        if (Input.GetKeyDown(KeyCode.Space) && grounded)
        {
            body.velocity = new Vector2(body.velocity.x, jumpHeight);
            AudioManager.Instance?.PlayJump();
        }

        // 3. XỬ LÝ SHADOW SWAP
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (shadowManager != null)
                // Tạo bóng, truyền kèm localScale để lật đúng chiều
                shadowManager.CreateShadow(transform.position, transform.rotation, transform.localScale);
                AudioManager.Instance?.PlayShadowSummon();
        }

        if (Input.GetKeyDown(KeyCode.Q))
        {
            if (shadowManager != null)
                // Dịch chuyển đến bóng
                shadowManager.TeleportToShadow(transform);
                AudioManager.Instance?.PlayShadowSwap();
        }

            // 8.XỬ LÝ TẤN CÔNG & PHÒNG THỦ

            // Attack 1 (Đánh từ dưới lên) - Chỉ khi trên mặt đất
            if (Input.GetKeyDown(KeyCode.J) && grounded)
            {
                isBusy = true;
                anim.SetTrigger("Attack1");
            }
            // Attack 2 (Đánh từ trên xuống) - Chỉ khi ở trên không
            else if (Input.GetKeyDown(KeyCode.K) && !grounded)
            {
                isBusy = true;
                anim.SetTrigger("Attack2");
            }
            // Block - Chỉ khi trên mặt đất
            else if (Input.GetKeyDown(KeyCode.L) && grounded)
            {
                isBusy = true;
                anim.SetTrigger("Block");
            }
        }

        // 4. XỬ LÝ HÌNH ẢNH (Animation & Xoay)
        anim.SetBool("Grounded", grounded); // Grounded nên cập nhật liên tục
        if (!isBusy)
        {
            anim.SetBool("Run", horizontalInput != 0 && grounded);

            if (horizontalInput > 0.01f)
            {
                transform.localScale = new Vector3(2, 2, 1);
            }
            else if (horizontalInput < -0.01f)
            {
                transform.localScale = new Vector3(-2, 2, 1);
            }
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

    // XỬ LÝ TẤT CẢ ITEM PICKUP (OnTriggerEnter2D)
    private void OnTriggerEnter2D(Collider2D other)
    {
        // XỬ LÝ COIN
        if (other.CompareTag("Coin"))
        {
            coinScore += 1; // 1 điểm mỗi Coin
            coinCollected += 1;
            Debug.Log("Coin Collected! Current Score: " + coinScore);
            Destroy(other.gameObject);
            AudioManager.Instance?.PlayCoin();
        }
        // XỬ LÝ KEY
        else if (other.CompareTag("Key"))
        {
            keyCollected += 1;  // Tăng số lượng Key hiện có
            coinScore += 10;    // +10 điểm Coin

            Debug.Log("Key Collected! Total Keys: " + keyCollected + ". Coin Score: " + coinScore);
            Destroy(other.gameObject);
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
            if (keyCollected > 0)
            {
                keyCollected--;
                coinScore += 20; // +20 điểm Coin khi mở Rương

                Debug.Log("Chest Opened! Keys remaining: " + keyCollected + ". Coin Score: " + coinScore);
                AudioManager.Instance?.PlayUnlockChest();
                // Giả định rương biến mất sau khi mở và nhận thưởng
                Destroy(other.gameObject);
            }
            else
            {
                Debug.Log("Chest is locked! Need a key.");
            }
        }
    }

    // 8. (MỚI) ANIMATION EVENTS
    // Hàm này phải được gọi bằng Animation Event 
    // tại FRAME CUỐI CÙNG của các animation "Attack1", "Attack2", và "Block"
    public void AnimationFinished()
    {
        isBusy = false;
    }
}
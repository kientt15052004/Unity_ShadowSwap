using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    // Cài đặt di chuyển và nhảy
    [SerializeField] private float speed = 5f;
    [SerializeField] private float jumpHeight = 7f;

    private Rigidbody2D body;
    private Animator anim;
    private bool grounded;

    // Kiểm tra mặt đất (Thiết lập trong Inspector)
    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;

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

        // 4. XỬ LÝ HÌNH ẢNH (Animation & Xoay)
        anim.SetBool("Run", horizontalInput != 0 && grounded);
        anim.SetBool("Grounded", grounded);

        if (horizontalInput > 0.01f)
        {
            transform.localScale = new Vector3(2, 2, 1);
        }
        else if (horizontalInput < -0.01f)
        {
            transform.localScale = new Vector3(-2, 2, 1);
        }
    }

    private void FixedUpdate()
    {
        // Di chuyển ngang (Cho phép di chuyển trên không)
        body.velocity = new Vector2(horizontalInput * speed, body.velocity.y);
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
        }
        // XỬ LÝ KEY
        else if (other.CompareTag("Key"))
        {
            keyCollected += 1;  // Tăng số lượng Key hiện có
            coinScore += 10;    // +10 điểm Coin

            Debug.Log("Key Collected! Total Keys: " + keyCollected + ". Coin Score: " + coinScore);
            Destroy(other.gameObject);
        }
        // XỬ LÝ HEAL
        else if (other.CompareTag("Heal"))
        {
            if (healthManager != null)
            {
                healthManager.Heal(20); // Hồi cố định 20 máu
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

                // Giả định rương biến mất sau khi mở và nhận thưởng
                Destroy(other.gameObject);
            }
            else
            {
                Debug.Log("Chest is locked! Need a key.");
            }
        }
    }
}
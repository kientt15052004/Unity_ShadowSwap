using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    // Cài đặt di chuyển và nhảy
    [SerializeField] private float speed = 5f;
    [SerializeField] private float jumpHeight = 7f;

    private Rigidbody2D body;
    private Animator anim;
    private bool grounded;
    private bool wasGrounded = true;
    // Kiểm tra mặt đất (Thiết lập trong Inspector)
    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;
    // Âm thanh 
    [Header("Audio Settings")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip[] footstepClips;
    [SerializeField] private AudioClip jumpClip;
    [SerializeField] private AudioClip landClip;
    [SerializeField] private AudioClip hurtClip;
    [SerializeField] private float footstepInterval = 0.4f; // khoảng thời gian giữa 2 bước
    private float hurtSoundTimer = 0f;
    public float hurtSoundInterval = 1f;
    private float footstepTimer;
    
    private ShadowManager shadowManager;
    private HealthManager healthManager;
    private float horizontalInput;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        shadowManager = FindObjectOfType<ShadowManager>();
        healthManager = GetComponent<HealthManager>();
    }

    private void Update()
    {
        // 1. CẬP NHẬT TRẠNG THÁI GROUNDED 
        grounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        horizontalInput = Input.GetAxis("Horizontal");

        // Phát âm thanh khi vừa chạm đất
        if (!wasGrounded && grounded)
        {
            PlayLandSound();
        }

        // 2. XỬ LÝ NHẢY
        if (Input.GetKeyDown(KeyCode.Space) && grounded)
        {
            body.velocity = new Vector2(body.velocity.x, jumpHeight);
            PlayJumpSound();
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
                PlayFootstepSound();
                footstepTimer = footstepInterval;
            }
        }
        else footstepTimer = 0f;
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
            PlayHurtSound();
            hurtSoundTimer = hurtSoundInterval;
        }
    }
    private void FixedUpdate()
    {
        // Di chuyển ngang (Cho phép di chuyển trên không)
        body.velocity = new Vector2(horizontalInput * speed, body.velocity.y);
        // Xử lý hurt sound timer
        if (hurtSoundTimer > 0f)
        {
            hurtSoundTimer -= Time.fixedDeltaTime; // Dùng fixedDeltaTime thay vì deltaTime
        }
    }

    // 5. XỬ LÝ SÁT THƯƠNG TỨC THỜI (OnCollisionEnter2D)
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Trap"))
        {
            if (healthManager != null)
            {
                healthManager.TakeDamage(5); // Sát thương tức thời 5
            }
                PlayHurtSound();
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

    private void PlayFootstepSound()
    {
        if (footstepClips.Length > 0 && audioSource != null)
        {
            AudioClip clip = footstepClips[Random.Range(0, footstepClips.Length)];
            audioSource.pitch = Random.Range(0.9f, 1.1f);
            audioSource.PlayOneShot(clip);
        }
    }
    private void PlayJumpSound()
    {
        if (jumpClip != null && audioSource != null)
        {
            audioSource.pitch = 1f;
            audioSource.PlayOneShot(jumpClip);
        }
    }
    private void PlayLandSound()
    {
        if (landClip != null && audioSource != null)
            audioSource.PlayOneShot(landClip);
    }

    private void PlayHurtSound()
    {
        if (hurtClip != null && audioSource != null)
            audioSource.PlayOneShot(hurtClip);
    }


}
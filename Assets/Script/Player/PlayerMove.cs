using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    [SerializeField] private float speed;
    [SerializeField] private float jumpHeight;
    private Rigidbody2D body;
    private Animator anim;
    private bool grounded;

    // Thêm một biến để tham chiếu đến ShadowManager
    private ShadowManager shadowManager;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        // Tìm và lấy đối tượng ShadowManager trong scene
        shadowManager = FindObjectOfType<ShadowManager>();
    }

    private void Update()
    {
        float horizontalInput = Input.GetAxis("Horizontal");
        body.velocity = new Vector2(horizontalInput * speed, body.velocity.y);

        if (horizontalInput > 0.01f)
        {
            transform.localScale = new Vector3(2, 2, 1);
        }
        else if (horizontalInput < -0.01f)
        {
            transform.localScale = new Vector3(-2, 2, 1);
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (grounded == true)
            {
                body.velocity = new Vector2(body.velocity.x, jumpHeight);
                grounded = false;
            }
        }

        // --- Thêm logic cho Shadow Swap ---
        // Nhấn E để tạo bản thể
        if (Input.GetKeyDown(KeyCode.E))
        {
            shadowManager.CreateShadow(transform.position, transform.rotation);
        }

        // Nhấn Q để dịch chuyển đến bản thể
        if (Input.GetKeyDown(KeyCode.Q))
        {
            shadowManager.TeleportToShadow(transform);
        }
        // --- Kết thúc logic cho Shadow Swap ---

        anim.SetBool("Run", horizontalInput != 0);
        anim.SetBool("Grounded", grounded);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            grounded = true;
        }
    }
}
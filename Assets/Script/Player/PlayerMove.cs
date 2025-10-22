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
        // Xử lý Input (nhận tín hiệu từ bàn phím) ở đây
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (grounded)
            {
                body.velocity = new Vector2(body.velocity.x, jumpHeight);
                grounded = false;
            }
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            shadowManager.CreateShadow(transform.position, transform.rotation);
        }

        if (Input.GetKeyDown(KeyCode.Q))
        {
            shadowManager.TeleportToShadow(transform);
        }

        // Xử lý Animation và xoay nhân vật
        float horizontalInput = Input.GetAxis("Horizontal");
        anim.SetBool("Run", horizontalInput != 0);
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
        // Xử lý vật lý (di chuyển, nhảy) ở đây
        float horizontalInput = Input.GetAxis("Horizontal");
        body.velocity = new Vector2(horizontalInput * speed, body.velocity.y);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            grounded = true;
        }
    }
}
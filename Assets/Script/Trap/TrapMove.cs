using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class TrapMove : MonoBehaviour
{
    [Header("Trap Settings")]
    [SerializeField] private float moveSpeed = 5f;           // Tốc độ di chuyển
    [SerializeField] private float moveDistance = 1.5f;      // Khoảng cách di chuyển (lên/xuống từ vị trí giữa)
    [SerializeField] private float waitTime = 0.3f;          // Thời gian đợi ở đỉnh/đáy
    [SerializeField] private float cycleTime = 2.5f;         // Thời gian mỗi nửa chu kỳ (lên hoặc xuống)

    [Header("Damage Settings")]
    [SerializeField] private int damage = 5;
    [SerializeField] private bool canDamage = true;          // Luôn có thể gây damage

    private Vector3 centerPosition;      // Vị trí giữa (vị trí ban đầu)
    private Vector3 topPosition;         // Vị trí trên cùng
    private Vector3 bottomPosition;      // Vị trí dưới cùng
    private float timer = 0f;
    private TrapState currentState = TrapState.MovingDown;

    private enum TrapState
    {
        MovingDown,    // Đang di chuyển xuống
        WaitingBottom, // Đang đợi ở dưới
        MovingUp,      // Đang di chuyển lên
        WaitingTop     // Đang đợi ở trên
    }

    void Start()
    {
        // Vị trí ban đầu là vị trí giữa
        centerPosition = transform.position;

        // Tính vị trí trên và dưới
        topPosition = centerPosition + Vector3.up * moveDistance;
        bottomPosition = centerPosition + Vector3.down * moveDistance;

        // Bắt đầu chu kỳ - đi xuống trước
        timer = 0f;
        currentState = TrapState.MovingDown;
    }

    void Update()
    {
        timer += Time.deltaTime;

        switch (currentState)
        {
            case TrapState.MovingDown:
                // Di chuyển xuống
                transform.position = Vector3.MoveTowards(
                    transform.position,
                    bottomPosition,
                    moveSpeed * Time.deltaTime
                );

                // Kiểm tra đã chạm đáy chưa
                if (Vector3.Distance(transform.position, bottomPosition) < 0.01f)
                {
                    transform.position = bottomPosition;
                    currentState = TrapState.WaitingBottom;
                    timer = 0f;
                }
                break;

            case TrapState.WaitingBottom:
                // Đợi ở dưới
                if (timer >= waitTime)
                {
                    currentState = TrapState.MovingUp;
                    timer = 0f;
                }
                break;

            case TrapState.MovingUp:
                // Di chuyển lên
                transform.position = Vector3.MoveTowards(
                    transform.position,
                    topPosition,
                    moveSpeed * Time.deltaTime
                );

                // Kiểm tra đã lên đỉnh chưa
                if (Vector3.Distance(transform.position, topPosition) < 0.01f)
                {
                    transform.position = topPosition;
                    currentState = TrapState.WaitingTop;
                    timer = 0f;
                }
                break;

            case TrapState.WaitingTop:
                // Đợi ở trên
                if (timer >= waitTime)
                {
                    currentState = TrapState.MovingDown;
                    timer = 0f;
                }
                break;
        }
    }

    // Gây damage khi chạm player
    void OnTriggerEnter2D(Collider2D other)
    {
        if (canDamage && other.CompareTag("Player"))
        {
            HealthManager health = other.GetComponent<HealthManager>();
            if (health != null)
            {
                health.TakeDamage(damage);
                AudioManager.Instance?.PlayHurt();
                Debug.Log("Trap hit player! Damage: " + damage);
            }
        }
    }

    // Vẽ Gizmos để dễ hình dung trong Scene View
    void OnDrawGizmos()
    {
        Vector3 center = Application.isPlaying ? centerPosition : transform.position;
        Vector3 top = center + Vector3.up * moveDistance;
        Vector3 bottom = center + Vector3.down * moveDistance;

        // Vẽ vị trí giữa (màu xanh dương)
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(center, 0.2f);

        // Vẽ vị trí trên cùng (màu xanh lá)
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(top, 0.3f);

        // Vẽ vị trí dưới cùng (màu đỏ)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(bottom, 0.3f);

        // Vẽ đường đi
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(top, bottom);

        // Vẽ vị trí hiện tại (màu trắng)
        if (Application.isPlaying)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(transform.position, 0.25f);
        }
    }
}

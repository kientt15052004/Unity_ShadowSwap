using UnityEngine;

public class FallingTrap : MonoBehaviour
{
    [Header("Trap Settings")]
    [SerializeField] private float fallSpeed = 8f;           // Tốc độ rơi
    [SerializeField] private float riseSpeed = 3f;           // Tốc độ kéo lên (chậm hơn)
    [SerializeField] private float fallDistance = 3f;        // Khoảng cách rơi xuống
    [SerializeField] private float waitAtBottom = 0.5f;      // Thời gian đợi ở dưới
    [SerializeField] private float cycleTime = 5f;           // Chu kỳ 5 giây

    private Vector3 originalPosition;
    private Vector3 targetPosition;
    private float timer = 0f;
    private TrapState currentState = TrapState.Waiting;

    private enum TrapState
    {
        Waiting,    // Đang đợi ở trên
        Falling,    // Đang rơi xuống
        AtBottom,   // Đang ở dưới
        Rising      // Đang kéo lên
    }

    void Start()
    {
        // Lưu vị trí ban đầu
        originalPosition = transform.position;
        targetPosition = originalPosition + Vector3.down * fallDistance;

        // Bắt đầu chu kỳ
        timer = 0f;
        currentState = TrapState.Waiting;
    }

    void Update()
    {
        timer += Time.deltaTime;

        switch (currentState)
        {
            case TrapState.Waiting:
                // Đợi đủ thời gian chu kỳ thì bắt đầu rơi
                if (timer >= cycleTime)
                {
                    currentState = TrapState.Falling;
                    timer = 0f;
                }
                break;

            case TrapState.Falling:
                // Rơi xuống
                transform.position = Vector3.MoveTowards(
                    transform.position,
                    targetPosition,
                    fallSpeed * Time.deltaTime
                );

                // Kiểm tra đã chạm đáy chưa
                if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
                {
                    transform.position = targetPosition;
                    currentState = TrapState.AtBottom;
                    timer = 0f;
                }
                break;

            case TrapState.AtBottom:
                // Đợi ở dưới một chút
                if (timer >= waitAtBottom)
                {
                    currentState = TrapState.Rising;
                    timer = 0f;
                }
                break;

            case TrapState.Rising:
                // Kéo lên
                transform.position = Vector3.MoveTowards(
                    transform.position,
                    originalPosition,
                    riseSpeed * Time.deltaTime
                );

                // Kiểm tra đã về vị trí ban đầu chưa
                if (Vector3.Distance(transform.position, originalPosition) < 0.01f)
                {
                    transform.position = originalPosition;
                    currentState = TrapState.Waiting;
                    timer = 0f;
                }
                break;
        }
    }

    // Vẽ Gizmos để dễ hình dung trong Scene View
    void OnDrawGizmos()
    {
        if (Application.isPlaying)
        {
            // Vẽ vị trí ban đầu (màu xanh lá)
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(originalPosition, 0.3f);

            // Vẽ vị trí đích (màu đỏ)
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(targetPosition, 0.3f);

            // Vẽ đường đi
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(originalPosition, targetPosition);
        }
        else
        {
            // Trong Edit Mode, vẽ preview
            Vector3 startPos = transform.position;
            Vector3 endPos = startPos + Vector3.down * fallDistance;

            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(startPos, 0.3f);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(endPos, 0.3f);

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(startPos, endPos);
        }
    }
}
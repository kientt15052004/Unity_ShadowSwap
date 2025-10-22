using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowCamera : MonoBehaviour
{
    public Transform player;    // đối tượng Player
    public float smoothSpeed = 0.125f;
    public Vector3 offset;      // khoảng cách giữa camera và player
    private float minY;         // giới hạn thấp nhất (camera ko theo mãi khi rơi)

    void Start()
    {
        // ban đầu cho phép camera theo player nhưng giữ giới hạn Y
        minY = player.position.y - 5; // ví dụ camera chỉ theo xuống thêm 5 đơn vị
    }

    void LateUpdate()
    {
        if (player != null)
        {
            // vị trí mong muốn = vị trí player + offset
            Vector3 desiredPosition = player.position + offset;

            // kiểm tra nếu player rơi quá thấp thì giữ camera tại minY
            if (desiredPosition.y < minY)
                desiredPosition.y = minY;

            // di chuyển mượt
            Vector3 smoothed = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
            transform.position = smoothed;
        }
    }
}
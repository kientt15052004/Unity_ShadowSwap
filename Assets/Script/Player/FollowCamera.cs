using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowCamera : MonoBehaviour
{
    public Transform player;    // đối tượng Player
    public float smoothSpeed = 0.125f;

    // Gán giá trị mặc định Z = -10.0f ngay tại đây
    public Vector3 offset = new Vector3(0f, 0f, -10f);

    void Start()
    {
        // Hàm Start trống
    }

    void LateUpdate()
    {
        if (player != null)
        {
            // Vị trí mong muốn = vị trí player + offset
            Vector3 desiredPosition = player.position + offset;

            // Di chuyển mượt đến vị trí mong muốn
            Vector3 smoothed = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
            transform.position = smoothed;
        }
    }
}
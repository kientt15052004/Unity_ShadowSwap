using UnityEngine;

public class MiniMapFollow : MonoBehaviour
{
    public Transform player;

    void LateUpdate()
    {
        Vector3 newPosition = player.position;
        newPosition.z = transform.position.z; // giữ nguyên độ cao camera minimap
        transform.position = newPosition;
    }
}

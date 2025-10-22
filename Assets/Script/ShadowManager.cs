using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShadowManager : MonoBehaviour
{
    // Kéo thả prefab của bản thể vào đây trong Inspector
    public GameObject shadowPrefab;
    // Thời gian bản thể tồn tại
    public float shadowDuration = 5f;
    // Biến lưu trữ bản thể hiện tại
    private GameObject currentShadow;

    // Hàm tạo bản thể tại vị trí và góc quay của người chơi
    public void CreateShadow(Vector3 position, Quaternion rotation)
    {
        // Nếu có bản thể cũ, hủy nó đi trước khi tạo cái mới
        if (currentShadow != null)
        {
            Destroy(currentShadow);
        }

        // Tạo bản thể mới từ prefab
        currentShadow = Instantiate(shadowPrefab, position, rotation);
        // Hủy bản thể sau thời gian quy định
        Destroy(currentShadow, shadowDuration);
    }

    // Hàm dịch chuyển người chơi đến vị trí của bản thể
    public void TeleportToShadow(Transform playerTransform)
    {
        // Kiểm tra xem bản thể có tồn tại không
        if (currentShadow != null)
        {
            // Dịch chuyển người chơi
            playerTransform.position = currentShadow.transform.position;
            // Hủy bản thể sau khi dịch chuyển
            Destroy(currentShadow);
        }
    }
}
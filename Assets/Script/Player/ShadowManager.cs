using UnityEngine;

public class ShadowManager : MonoBehaviour
{
    public GameObject shadowPrefab;
    public float shadowDuration = 10f;
    private GameObject currentShadow;

    // Hàm tạo bản thể
    public void CreateShadow(Vector3 position, Quaternion rotation, Vector3 scale)
    {
        if (currentShadow == null)
        {
            currentShadow = Instantiate(shadowPrefab, position, rotation);

            // Giữ lại chiều lật (Scale) của người chơi
            currentShadow.transform.localScale = scale;

            Destroy(currentShadow, shadowDuration);
        }
    }

    public void TeleportToShadow(Transform playerTransform)
    {
        if (currentShadow != null)
        {
            playerTransform.position = currentShadow.transform.position;

            Destroy(currentShadow);
            currentShadow = null;
        }
    }
}
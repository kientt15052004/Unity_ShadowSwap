using UnityEngine;

public class BossTrigger : MonoBehaviour
{
    public GameObject trapLeft;
    public GameObject trapRight;
    public GameObject boss;

    bool activated = false;

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (activated) return;
        if (!collision.CompareTag("Player")) return;

        activated = true;

        // Đóng cửa -> bật trap
        trapLeft.SetActive(true);
        trapRight.SetActive(true);

        // Bật boss nếu ban đầu ẩn
        if (boss != null)
            boss.SetActive(true);
    }

    void Update()
    {
        // Boss bị phá hủy → mở cửa
        if (activated && boss == null)
        {
            trapLeft.SetActive(false);
            trapRight.SetActive(false);
        }
    }
}

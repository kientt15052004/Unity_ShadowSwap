using UnityEngine;

public class BossRoomTrigger : MonoBehaviour
{
    public GameObject trapLeft;
    public GameObject trapRight;
    public GameObject boss;

    public GameObject bossHP_Panel; // <-- thêm cái này

    bool activated = false;

    void Start()
    {
        if (bossHP_Panel != null)
            bossHP_Panel.SetActive(false); // Ẩn panel máu khi chưa đánh
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (activated) return;
        if (!collision.CompareTag("Player")) return;

        activated = true;

        trapLeft.SetActive(true);
        trapRight.SetActive(true);

        if (boss != null)
            boss.SetActive(true);

        if (bossHP_Panel != null)
            bossHP_Panel.SetActive(true); // ✅ Hiện thanh máu khi bắt đầu
    }

    void Update()
    {
        if (activated && boss == null) // Boss bị chết (destroy)
        {
            trapLeft.SetActive(false);
            trapRight.SetActive(false);

            if (bossHP_Panel != null)
                bossHP_Panel.SetActive(false); // ✅ Tắt thanh máu khi boss chết
        }
    }
}

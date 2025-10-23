using UnityEngine;

public class DeathZone : MonoBehaviour
{
    [Header("Death Zone Settings")]
    [SerializeField] private bool killInstantly = true; // Chết ngay hay trừ dần
    [SerializeField] private int damageAmount = 100;    // Sát thương (nếu không chết ngay)

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Kiểm tra có phải Player không
        if (other.CompareTag("Player"))
        {
            HealthManager health = other.GetComponent<HealthManager>();

            if (health != null)
            {
                if (killInstantly)
                {
                    // Chết ngay lập tức
                    health.Die();
                }
                else
                {
                    // Trừ máu thông thường
                    health.TakeDamage(damageAmount);
                }
            }
        }
    }
}
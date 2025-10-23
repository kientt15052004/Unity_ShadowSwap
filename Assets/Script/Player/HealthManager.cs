using UnityEngine;
using UnityEngine.UI;
using TMPro; // QUAN TRỌNG: Cần thư viện này cho Text hiển thị số máu
using System.Collections;

public class HealthManager : MonoBehaviour
{
    // Cài đặt Máu và Bất tử
    public int maxHealth = 100;
    public int currentHealth = 100;
    public float invincibilityDuration = 1f;

    // Tham chiếu UI & Animation (Cần kéo thả trong Inspector)
    public Slider healthBarSlider;     // Thanh máu (Slider)
    public Animator anim;              // Component Animator của Player
    public TextMeshProUGUI healthText; // Text hiển thị số máu (ví dụ: 100/100)

    private bool isInvincible = false;
    private Coroutine continuousDamageCoroutine;
    private bool isTakingContinuousDamage = false;

    private void Start()
    {
        // 1. Lấy Animator nếu chưa gán thủ công
        if (anim == null)
        {
            anim = GetComponent<Animator>();
        }

        // 2. Khởi tạo thanh máu Slider
        if (healthBarSlider != null)
        {
            healthBarSlider.maxValue = maxHealth;
            healthBarSlider.value = currentHealth;
        }

        // 3. Khởi tạo Text hiển thị máu
        UpdateHealthText();
    }

    // Hàm cập nhật Text hiển thị máu (100/100)
    private void UpdateHealthText()
    {
        if (healthText != null)
        {
            healthText.text = currentHealth.ToString() + "/" + maxHealth.ToString();
        }
    }

    // Hàm tổng hợp cập nhật UI (Slider và Text) và Animation Hurt
    private void UpdateUIAndAnimation()
    {
        // Cập nhật thanh máu Slider
        if (healthBarSlider != null)
        {
            healthBarSlider.value = currentHealth;
        }

        // Cập nhật Text hiển thị máu
        UpdateHealthText();

        // Kích hoạt animation Hurt
        if (anim != null)
        {
            anim.SetTrigger("Hurt");
        }

        Debug.Log("Health: " + currentHealth);
    }

    // Hàm gọi khi nhân vật bị sát thương tức thời
    public void TakeDamage(int damage)
    {
        if (isInvincible) return;

        currentHealth -= damage;

        UpdateUIAndAnimation();

        isInvincible = true;
        // Kích hoạt hàm DisableInvincibility sau một khoảng thời gian
        Invoke("DisableInvincibility", invincibilityDuration);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void DisableInvincibility()
    {
        isInvincible = false;
    }

    private void Die()
    {
        Debug.Log("Nhân vật đã chết!");

        StopContinuousDamage();

        // Tắt điều khiển và Collider
        PlayerMove playerMove = GetComponent<PlayerMove>();
        if (playerMove != null) playerMove.enabled = false;

        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;
    }

    // Xử lý sát thương liên tục (Damage Over Time - DOT)

    public void StartContinuousDamage(int damagePerSecond)
    {
        // Tránh chạy nhiều Coroutine DOT cùng lúc
        if (isTakingContinuousDamage) return;

        isTakingContinuousDamage = true;
        continuousDamageCoroutine = StartCoroutine(DamageOverTime(damagePerSecond));
    }

    public void StopContinuousDamage()
    {
        if (!isTakingContinuousDamage) return;

        isTakingContinuousDamage = false;
        if (continuousDamageCoroutine != null)
        {
            StopCoroutine(continuousDamageCoroutine);
        }
    }

    // Coroutine trừ máu mỗi giây
    private IEnumerator DamageOverTime(int damage)
    {
        while (currentHealth > 0)
        {
            currentHealth -= damage;

            // Cập nhật UI và Animation
            UpdateUIAndAnimation();

            if (currentHealth <= 0)
            {
                Die();
                yield break;
            }

            yield return new WaitForSeconds(1f);
        }
    }
}
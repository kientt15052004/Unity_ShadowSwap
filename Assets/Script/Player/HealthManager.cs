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

        //PlayerMove pm = GetComponent<PlayerMove>();
        //if (pm != null && pm.IsBlocking())
        //{
        //    Debug.Log("Blocked damage!");
        //    return; // Chặn sát thương
        //}

        currentHealth -= damage;

        UpdateUIAndAnimation();

        isInvincible = true;
        // Kích hoạt hàm DisableInvincibility sau một khoảng thời gian
        Invoke("DisableInvincibility", invincibilityDuration);

        if (currentHealth <= 0)
        {
            currentHealth = 0;
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
        AudioManager.Instance.PlayDie();
        // Kích hoạt animation chết
        if (anim != null)
        {
            anim.SetBool("IsDead", true);
            anim.SetTrigger("Die");
        }

        // Tắt điều khiển
        PlayerMove playerMove = GetComponent<PlayerMove>();
        if (playerMove != null) playerMove.enabled = false;

        // Tắt Rigidbody2D (ngừng vật lý)
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.simulated = false; // Tắt hoàn toàn vật lý
        }

        // Tắt Collider
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        // Respawn sau 2 giây (hoặc reload scene)
        Invoke("Respawn", 2f);
    }

    private void Respawn()
    {
        // Reload scene
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
        );

        // Hồi sinh tại vị trí checkpoint
        // currentHealth = maxHealth;
        // UpdateUIAndAnimation();
        // transform.position = checkpointPosition; // Cần thêm biến checkpointPosition
        // GetComponent<PlayerMove>().enabled = true;
        // GetComponent<Rigidbody2D>().simulated = true;
        // GetComponent<Collider2D>().enabled = true;
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
    
    public void InstantDeath()
    {
        currentHealth = 0;
        UpdateUIAndAnimation();
        Die();
    }

    // HÀM HEAL (HỒI MÁU) ĐÃ ĐƯỢC THÊM VÀO
    public void Heal(int amount)
    {
        currentHealth += amount;
        if (currentHealth > maxHealth)
        {
            currentHealth = maxHealth;
        }

        // Cập nhật UI sau khi hồi máu (không cần animation Hurt)
        UpdateHealthText();
        if (healthBarSlider != null)
        {
            healthBarSlider.value = currentHealth;
        }

        Debug.Log("Player healed for: " + amount + ". Current Health: " + currentHealth);
    }
}
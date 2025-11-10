using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HealthBarUI : MonoBehaviour
{
    [Header("UI refs (assign in prefab)")]
    public Canvas worldCanvas;
    public Image fillImage;          // MUST support Filled (preferred). Fallback uses Rect width.
    public Image backgroundImage;
    public TextMeshProUGUI healthText;

    [Header("Loss Effect")] // Thêm phần này
    public Image lossImage; // Thanh màu trắng (hoặc màu khác) hiển thị lượng máu sắp mất
    public float lossSpeed = 0.5f; // Tốc độ thanh mất máu chạy

    [Header("Positioning")]
    public Vector3 localOffset = new Vector3(0f, 1.5f, 0f); // mặc định cao hơn 1.2 -> 1.5
    public bool faceCamera = true;

    [Header("Show / Hide")]
    public bool showOnlyOnDamage = true;      // nếu true -> ẩn ban đầu, chỉ hiện khi có damage
    public float visibleDuration = 3.0f;     // hiển trong bao nhiêu giây sau khi nhận damage
    public Color inactiveBackgroundColor = Color.white; // màu background khi ẩn (phần mất đi hiện màu trắng)
    public Color activeBackgroundColor = new Color(0f, 0f, 0f, 0.6f); // mặc định

    // internals
    private int maxHealth;
    private int currentHealth;
    private Transform target;
    private bool isChildOfTarget = false;
    private float lastHitTime = -999f;
    private float maxBarWidth = 100f;
    private float lossFillAmount;

    public void Initialize(Transform targetTransform, int maxHp, bool isBoss)
    {
        target = targetTransform;
        maxHealth = Mathf.Max(1, maxHp);
        currentHealth = maxHealth;

        // determine if this healthbar is child of enemy
        isChildOfTarget = (target != null && transform.parent == target);

        // set fill color (boss vs normal)
        if (fillImage != null)
        {
            fillImage.color = isBoss ? new Color(0.5f, 0f, 0.5f) : Color.red;
        }

        // compute default max width from background if available
        if (backgroundImage != null)
        {
            var bgRT = backgroundImage.GetComponent<RectTransform>();
            if (bgRT != null && bgRT.rect.width > 0f) maxBarWidth = bgRT.rect.width;
        }

        // initial background color
        if (backgroundImage != null) backgroundImage.color = activeBackgroundColor;

        UpdateUI();

        // initial visibility
        if (showOnlyOnDamage)
        {
            // start hidden
            gameObject.SetActive(false);
            // set inactive appearance (background white) so when visible later you can toggle
            if (backgroundImage != null) backgroundImage.color = inactiveBackgroundColor;
        }
        else
        {
            gameObject.SetActive(true);
        }

        if (isChildOfTarget)
        {
            transform.localPosition = localOffset;
            if (faceCamera && Camera.main != null) transform.rotation = Camera.main.transform.rotation;
        }
    }

    void LateUpdate()
    {
        // auto-hide timer
        if (showOnlyOnDamage && gameObject.activeSelf && Time.time > lastHitTime + visibleDuration)
        {
            // hide and set inactive background color (so when shown again it will show white bg if you want)
            gameObject.SetActive(false);
            if (backgroundImage != null) backgroundImage.color = inactiveBackgroundColor;
            return;
        }

        // Hiệu ứng mất máu từ từ
        if (lossImage != null && lossImage.type == Image.Type.Filled)
        {
            // Giảm dần fillAmount của thanh loss Image về giá trị máu hiện tại (lossFillAmount)
            if (lossImage.fillAmount > lossFillAmount)
            {
                lossImage.fillAmount = Mathf.Max(lossFillAmount, lossImage.fillAmount - Time.deltaTime * lossSpeed);
            }
            // Đảm bảo không bị lỗi float
            else if (lossImage.fillAmount < lossFillAmount)
            {
                lossImage.fillAmount = lossFillAmount;
            }
        }

        if (target == null)
        {
            if (!isChildOfTarget) Destroy(gameObject);
            return;
        }

        if (isChildOfTarget)
        {
            transform.localPosition = localOffset;
            if (faceCamera && Camera.main != null) transform.rotation = Camera.main.transform.rotation;
            return;
        }

        transform.position = target.position + localOffset;
        if (faceCamera && Camera.main != null) transform.rotation = Camera.main.transform.rotation;
    }

    /// <summary>
    /// Gọi khi enemy bị damage (EnemyCore sẽ gọi).
    /// Điều này đảm bảo healthbar hiển và reset timer.
    /// </summary>
    public void OnDamagedShow(int newHp)
    {
        currentHealth = Mathf.Clamp(newHp, 0, maxHealth);

        // show bar when damaged
        if (showOnlyOnDamage)
        {
            gameObject.SetActive(true);
            if (backgroundImage != null) backgroundImage.color = activeBackgroundColor;
        }

        // set last hit time for auto-hide
        lastHitTime = Time.time;

        UpdateUI();
    }

    public void UpdateHealth(int hp)
    {
        currentHealth = Mathf.Clamp(hp, 0, maxHealth);
        UpdateUI();
    }

    void UpdateUI()
    {
        float ratio = (float)currentHealth / Mathf.Max(1, maxHealth);

        // 1. Cập nhật thanh Fill (màu đỏ) ngay lập tức
        if (fillImage != null)
        {
            if (fillImage.type == Image.Type.Filled)
            {
                fillImage.fillAmount = Mathf.Clamp01(ratio);
            }
            else
            {
                var rt = fillImage.GetComponent<RectTransform>();
                if (rt != null) rt.sizeDelta = new Vector2(maxBarWidth * ratio, rt.sizeDelta.y);
            }
        }

        // 2. Thiết lập mục tiêu cho thanh Loss (màu trắng)
        lossFillAmount = ratio;
        if (lossImage != null)
        {
            // Ngay khi nhận damage, đặt thanh Loss bằng máu cũ (máu tối đa nếu chưa bị mất)
            // và để LateUpdate() giảm dần về fillImage.fillAmount
            if (lossImage.type == Image.Type.Filled)
            {
                // Nếu đây là lần đầu hoặc máu chưa mất (lossFillAmount = 1)
                if (lossImage.fillAmount < ratio) lossImage.fillAmount = ratio;
            }
            else
            {
                // Logic cho Rect width (ít dùng hơn)
                var rt = lossImage.GetComponent<RectTransform>();
                if (rt != null) rt.sizeDelta = new Vector2(maxBarWidth * lossFillAmount, rt.sizeDelta.y);
            }
        }

        if (healthText != null) healthText.text = $"{currentHealth}/{maxHealth}";
    }

    public void SetMaxWidth(float pixelWidth)
    {
        if (pixelWidth > 0f) maxBarWidth = pixelWidth;
    }

    public void Hide()
    {
        gameObject.SetActive(false);
        if (backgroundImage != null) backgroundImage.color = inactiveBackgroundColor;
    }
}

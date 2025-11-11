using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HealthBarUI : MonoBehaviour
{
    [Header("UI refs")]
    public Canvas worldCanvas;
    public Image fillImage;             // máu hiện tại
    public Image lossImage;             // phần máu đã mất
    public Image backgroundImage;
    public TextMeshProUGUI healthText;

    [Header("Loss Effect")]
    public float lossSpeed = 1.5f;     // tốc độ thu nhỏ lossImage

    [Header("Fill Animation")]
    public float fillAnimationSpeed = 5f;

    [Header("Positioning")]
    public Vector3 localOffset = new Vector3(0f, 1.5f, 0f);
    public bool faceCamera = true;

    [Header("Show / Hide")]
    public bool showOnlyOnDamage = true;
    public float visibleDuration = 3.0f;
    public Color inactiveBackgroundColor = Color.clear;
    public Color activeBackgroundColor = new Color(0f, 0f, 0f, 0.6f);

    [Header("Behavior tweaks")]
    public bool freezeFillUntilDead = false;

    // Internal
    private int maxHealth = 100;
    private int currentHealth = 100;
    private Transform target;
    private bool isChildOfTarget = false;
    private float lastHitTime = -999f;
    private float maxBarWidth = 100f;

    private float targetFillAmount = 1f;
    private float lossCurrentAmount = 0f;

    private Camera mainCamera;
    private Vector3 _initialLocalScale = Vector3.one;
    private Transform _parentTransformForScale = null;
    private bool _neutralizeParentScale = true;
    private bool isBoss = false;

    //---------------- Initialize ----------------
    public void Initialize(Transform targetTransform, int maxHp, bool isBossFlag)
    {
        mainCamera = Camera.main;
        isBoss = isBossFlag;

        target = targetTransform;
        maxHealth = Mathf.Max(1, maxHp);
        currentHealth = maxHealth;
        targetFillAmount = 1f;

        isChildOfTarget = (target != null && transform.parent == target);
        if (isChildOfTarget) _parentTransformForScale = transform.parent;
        _initialLocalScale = transform.localScale;

        // Fill color
        if (fillImage != null)
        {
            fillImage.color = isBoss ? new Color(0.6f, 0.15f, 0.6f, 1f) : Color.red;
            fillImage.type = Image.Type.Filled;
            fillImage.fillAmount = 1f;
        }

        // Loss image = trắng nhạt
        if (lossImage != null)
        {
            lossImage.color = new Color(1f, 1f, 1f, 0.8f);
            lossImage.type = Image.Type.Filled;
            lossImage.fillAmount = 0f;
            lossCurrentAmount = 0f;
            lossImage.gameObject.SetActive(true);
            lossImage.raycastTarget = false;
        }

        if (backgroundImage != null)
        {
            backgroundImage.color = activeBackgroundColor;
            var bgRT = backgroundImage.GetComponent<RectTransform>();
            if (bgRT != null && bgRT.rect.width > 0f) maxBarWidth = bgRT.rect.width;
        }

        transform.localScale = Vector3.one * 0.0085f;

        UpdateTextUI();

        if (showOnlyOnDamage)
        {
            gameObject.SetActive(false);
            if (backgroundImage != null) backgroundImage.color = inactiveBackgroundColor;
        }
    }

    void LateUpdate()
    {
        // 1. Smooth fill
        if (fillImage != null)
        {
            if (freezeFillUntilDead && currentHealth > 0)
                fillImage.fillAmount = 1f;
            else
                fillImage.fillAmount = Mathf.MoveTowards(fillImage.fillAmount, targetFillAmount, Time.deltaTime * fillAnimationSpeed);
        }

        // 2. Animate loss image
        if (lossImage != null && lossCurrentAmount > targetFillAmount)
        {
            lossCurrentAmount = Mathf.MoveTowards(lossCurrentAmount, targetFillAmount, Time.deltaTime * lossSpeed);
            lossImage.fillAmount = lossCurrentAmount;
        }

        // 3. Auto-hide
        if (showOnlyOnDamage && gameObject.activeSelf && Time.time > lastHitTime + visibleDuration)
        {
            if (Mathf.Approximately(fillImage.fillAmount, targetFillAmount))
            {
                gameObject.SetActive(false);
                if (backgroundImage != null) backgroundImage.color = inactiveBackgroundColor;
            }
        }

        // 4. Position & scale
        if (target == null)
        {
            if (!isChildOfTarget) Destroy(gameObject);
            return;
        }

        if (isChildOfTarget)
        {
            transform.localPosition = localOffset;
            if (faceCamera && mainCamera != null) transform.rotation = mainCamera.transform.rotation;

            if (_neutralizeParentScale && _parentTransformForScale != null)
            {
                Vector3 p = _parentTransformForScale.localScale;
                Vector3 inv = new Vector3(
                    Mathf.Abs(p.x) > 0.0001f ? 1f / p.x : 1f,
                    Mathf.Abs(p.y) > 0.0001f ? 1f / p.y : 1f,
                    Mathf.Abs(p.z) > 0.0001f ? 1f / p.z : 1f
                );
                transform.localScale = Vector3.Scale(inv, _initialLocalScale);
            }
            else transform.localScale = _initialLocalScale;

            if (fillImage != null)
            {
                int origin = (_parentTransformForScale.localScale.x < 0f) ? 1 : 0;
                fillImage.fillOrigin = origin;
                if (lossImage != null) lossImage.fillOrigin = 1 - origin;
            }
        }
        else
        {
            transform.position = target.position + localOffset;
            if (faceCamera && mainCamera != null) transform.rotation = mainCamera.transform.rotation;
            transform.localScale = _initialLocalScale;
        }
    }

    //---------------- Methods ----------------
    public void OnDamagedShow(int newHp)
    {
        float prevFill = fillImage != null ? fillImage.fillAmount : 1f;
        currentHealth = Mathf.Clamp(newHp, 0, maxHealth);
        lastHitTime = Time.time;

        float newRatio = (float)currentHealth / maxHealth;
        newRatio = Mathf.Clamp01(newRatio);
        targetFillAmount = newRatio;

        // Loss image = phần máu mất
        if (lossImage != null)
        {
            lossCurrentAmount = Mathf.Max(lossCurrentAmount, prevFill);
            lossImage.fillAmount = lossCurrentAmount;
            lossImage.gameObject.SetActive(true);
        }

        // Show bar
        if (showOnlyOnDamage) gameObject.SetActive(true);

        UpdateTextUI();
    }

    // Cập nhật ngay, không animation
    public void UpdateHealth(int hp)
    {
        currentHealth = Mathf.Clamp(hp, 0, maxHealth);
        targetFillAmount = (float)currentHealth / maxHealth;

        if (fillImage != null) fillImage.fillAmount = targetFillAmount;
        if (lossImage != null)
        {
            lossCurrentAmount = 1f - targetFillAmount;
            lossImage.fillAmount = lossCurrentAmount;
            lossImage.gameObject.SetActive(true);
        }

        UpdateTextUI();
    }

    private void UpdateTextUI()
    {
        if (healthText != null)
        {
            healthText.text = $"{currentHealth}/{maxHealth}";
            if (currentHealth <= 0)
                healthText.color = Color.yellow;
            else if (isBoss)
                healthText.color = new Color(0.9f, 0.8f, 1f);
            else
                healthText.color = Color.black;
        }
    }

    public void SetMaxWidth(float pixelWidth)
    {
        if (pixelWidth > 0f)
            maxBarWidth = pixelWidth;
    }

    public void Hide()
    {
        gameObject.SetActive(false);
        if (backgroundImage != null) backgroundImage.color = inactiveBackgroundColor;
    }
}

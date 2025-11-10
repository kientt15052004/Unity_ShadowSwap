using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HealthBarUI : MonoBehaviour
{
    [Header("UI refs (assign in prefab)")]
    public Canvas worldCanvas;
    public Image fillImage;          // colored bar underlay (red / purple)
    public Image backgroundImage;    // dùng để lấy kích thước toàn thanh
    public TextMeshProUGUI healthText;

    [Header("Loss Effect")]
    public Image lossImage;          // white overlay on top that represents only the lost chunk (we position/size in pixels)
    public float lossSpeed = 200f;   // speed in pixels/second (tăng để nhanh hơn)

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

    // internals
    private int maxHealth = 100;
    private int currentHealth = 100;
    private Transform target;
    private bool isChildOfTarget = false;
    private float lastHitTime = -999f;

    private float maxBarWidth = 100f; // pixel width of backgroundRect

    // loss overlay in pixel coordinates
    private float lossCurrentWidthPx = 0f;   // current visible white width in px
    private float lossTargetWidthPx = 0f;    // usually 0, we animate current -> target
    private float lossLeftInsetPx = 0f;      // left inset in px (where white piece starts)

    [SerializeField] private Color lossImageColor = new Color(1f, 1f, 1f, 0.85f); // white
    [SerializeField] private float scaleFactor = 0.0085f;
    [SerializeField] private Vector3 offset = new Vector3(0, 1.2f, 0);

    private Vector3 _initialLocalScale = Vector3.one;
    private Transform _parentTransformForScale = null;
    private bool _neutralizeParentScale = true;

    // cached rects
    private RectTransform _bgRect;
    private RectTransform _lossRect;
    private RectTransform _fillRect;

    // ---------------- Initialize ----------------
    public void Initialize(Transform targetTransform, int maxHp, bool isBoss)
    {
        // cache rects
        _bgRect = backgroundImage != null ? backgroundImage.GetComponent<RectTransform>() : null;
        _lossRect = lossImage != null ? lossImage.GetComponent<RectTransform>() : null;
        _fillRect = fillImage != null ? fillImage.GetComponent<RectTransform>() : null;

        // compute bar width (pixel) from background if available
        if (_bgRect != null)
        {
            // Note: rect.width is in local space pixels
            maxBarWidth = Mathf.Max(1f, _bgRect.rect.width);
        }

        EnsureLossImageSetup();

        target = targetTransform;
        maxHealth = Mathf.Max(1, maxHp);
        currentHealth = maxHealth;

        isChildOfTarget = (target != null && transform.parent == target);
        if (isChildOfTarget) _parentTransformForScale = transform.parent;
        _initialLocalScale = transform.localScale;

        // set fill color depending on boss/normal
        if (fillImage != null)
        {
            fillImage.color = isBoss ? new Color(0.55f, 0.15f, 0.55f, 1f) : Color.red;
            if (fillImage.type == Image.Type.Filled) fillImage.fillAmount = 1f;
        }

        // lossImage default hidden
        if (lossImage != null)
        {
            lossImage.color = lossImageColor;
            lossImage.gameObject.SetActive(false);
            lossCurrentWidthPx = 0f;
            lossTargetWidthPx = 0f;
            lossLeftInsetPx = 0f;
        }

        if (backgroundImage != null)
        {
            backgroundImage.color = activeBackgroundColor;
            if (_bgRect != null) maxBarWidth = Mathf.Max(1f, _bgRect.rect.width);
        }

        // scale & offset setup
        localOffset = offset;
        _initialLocalScale = Vector3.one * scaleFactor;
        transform.localScale = _initialLocalScale;

        UpdateUI();

        if (showOnlyOnDamage)
        {
            gameObject.SetActive(false);
            if (backgroundImage != null) backgroundImage.color = inactiveBackgroundColor;
        }
    }

    void LateUpdate()
    {
        // hide logic
        if (showOnlyOnDamage && gameObject.activeSelf && Time.time > lastHitTime + visibleDuration)
        {
            gameObject.SetActive(false);
            if (backgroundImage != null) backgroundImage.color = inactiveBackgroundColor;
            return;
        }

        // animate loss overlay width toward target (pixel-based)
        if (_lossRect != null && lossImage != null && lossImage.gameObject.activeSelf)
        {
            if (!Mathf.Approximately(lossCurrentWidthPx, lossTargetWidthPx))
            {
                lossCurrentWidthPx = Mathf.MoveTowards(lossCurrentWidthPx, lossTargetWidthPx, Time.deltaTime * lossSpeed);
                // update rect
                UpdateLossRectTransform(lossLeftInsetPx, lossCurrentWidthPx);
                if (Mathf.Approximately(lossCurrentWidthPx, lossTargetWidthPx))
                {
                    // finished shrinking -> hide
                    if (Mathf.Approximately(lossTargetWidthPx, 0f))
                        lossImage.gameObject.SetActive(false);
                }
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

            if (_neutralizeParentScale && _parentTransformForScale != null)
            {
                Vector3 p = _parentTransformForScale.localScale;
                Vector3 inv = new Vector3(
                    (Mathf.Abs(p.x) > 0.0001f) ? 1f / p.x : 1f,
                    (Mathf.Abs(p.y) > 0.0001f) ? 1f / p.y : 1f,
                    (Mathf.Abs(p.z) > 0.0001f) ? 1f / p.z : 1f
                );
                transform.localScale = Vector3.Scale(inv, _initialLocalScale);
            }
            else transform.localScale = _initialLocalScale;

            return;
        }

        // world follow
        transform.position = target.position + localOffset;
        if (faceCamera && Camera.main != null) transform.rotation = Camera.main.transform.rotation;
        transform.localScale = _initialLocalScale;
    }

    // ---------------- Core: called when enemy is damaged ----------------
    // newHp = enemy.currentHealth AFTER damage applied
    public void OnDamagedShow(int newHp)
    {
        if (_bgRect == null)
        {
            if (backgroundImage != null) _bgRect = backgroundImage.GetComponent<RectTransform>();
            if (_bgRect != null) maxBarWidth = Mathf.Max(1f, _bgRect.rect.width);
        }

        // prev fill fraction (trước khi thay đổi)
        float prevFill = 1f;
        if (fillImage != null && fillImage.type == Image.Type.Filled)
            prevFill = fillImage.fillAmount;
        else if (_fillRect != null && _bgRect != null)
            prevFill = Mathf.Clamp01(_fillRect.rect.width / Mathf.Max(1f, _bgRect.rect.width));

        // clamp new health
        currentHealth = Mathf.Clamp(newHp, 0, maxHealth);

        if (showOnlyOnDamage)
        {
            gameObject.SetActive(true);
            if (backgroundImage != null) backgroundImage.color = activeBackgroundColor;
        }
        lastHitTime = Time.time;

        float newRatio = (float)currentHealth / Mathf.Max(1, maxHealth);
        newRatio = Mathf.Clamp01(newRatio);

        // update colored fill
        if (fillImage != null)
        {
            if (fillImage.type == Image.Type.Filled)
                fillImage.fillAmount = newRatio;
            else if (_fillRect != null)
                _fillRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, newRatio * maxBarWidth);
        }

        // --- Setup lossImage để luôn hiển thị phần máu đã mất ---
        if (_lossRect != null && _bgRect != null && lossImage != null)
        {
            float fullW = Mathf.Max(1f, _bgRect.rect.width);
            float lostDeltaFrac = Mathf.Clamp01(prevFill - newRatio);

            if (lostDeltaFrac > 0.0001f)
            {
                float leftInsetPx = newRatio * fullW;
                float widthPx = lostDeltaFrac * fullW;

                lossLeftInsetPx = leftInsetPx;
                lossCurrentWidthPx = widthPx;

                _lossRect.anchorMin = new Vector2(0f, 0f);
                _lossRect.anchorMax = new Vector2(0f, 1f);
                _lossRect.pivot = new Vector2(0f, 0.5f);

                UpdateLossRectTransform(lossLeftInsetPx, lossCurrentWidthPx);

                lossImage.color = new Color(lossImageColor.r, lossImageColor.g, lossImageColor.b, Mathf.Clamp01(lossImageColor.a));
                lossImage.gameObject.SetActive(true);
            }
        }

        UpdateUI();
    }

    // Update loss rect position/size using parent left inset + width
    private void UpdateLossRectTransform(float leftInsetPx, float widthPx)
    {
        if (_lossRect == null || _bgRect == null) return;

        // we'll set left inset relative to background's left edge
        // We set anchor to left (0) so SetInsetAndSizeFromParentEdge operates as expected
        _lossRect.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, leftInsetPx, widthPx);

        // vertically stretch to parent height
        _lossRect.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, 0f, _bgRect.rect.height);
        // ensure y inset is zero with top anchored; using Top/Left dual to stretch
        // Alternatively you can set anchorMin/Max to stretch, but SetInset... suffices here.
    }

    // direct update (no loss animation)
    public void UpdateHealth(int hp)
    {
        currentHealth = Mathf.Clamp(hp, 0, maxHealth);
        UpdateUI();
    }

    void UpdateUI()
    {
        float ratio = (float)currentHealth / Mathf.Max(1, maxHealth);
        ratio = Mathf.Clamp01(ratio);

        // colored fill
        if (fillImage != null)
        {
            if (freezeFillUntilDead && currentHealth > 0)
            {
                if (fillImage.type == Image.Type.Filled) fillImage.fillAmount = 1f;
            }
            else
            {
                if (fillImage.type == Image.Type.Filled) fillImage.fillAmount = ratio;
                else if (_fillRect != null && _bgRect != null)
                {
                    _fillRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, ratio * maxBarWidth);
                }
            }
        }

        // health text
        if (healthText != null)
        {
            healthText.text = $"{currentHealth}/{maxHealth}";
            healthText.color = (ratio <= 0.15f) ? Color.black : Color.white;
            if (currentHealth == 0) healthText.color = Color.yellow;
        }
    }

    // ensure loss image set up correctly
    private void EnsureLossImageSetup()
    {
        if (lossImage == null) return;

        // prefer Simple/Sliced so we can set size by pixels
        if (lossImage.type == Image.Type.Filled)
        {
            // we will not use fillAmount for loss overlay in final solution; ensure sprite supports simple rendering
            lossImage.type = Image.Type.Simple;
        }

        lossImage.color = lossImageColor;
        lossImage.gameObject.SetActive(false);

        // cache rect if possible
        _lossRect = lossImage.GetComponent<RectTransform>();

        // bring to front if sibling same parent
        if (fillImage != null && _lossRect != null && fillImage.transform.parent == _lossRect.transform.parent)
        {
            int fi = fillImage.transform.GetSiblingIndex();
            _lossRect.transform.SetSiblingIndex(Mathf.Max(0, fi + 1));
        }
        else if (_lossRect != null)
        {
            _lossRect.transform.SetAsLastSibling();
        }

        // cache background rect
        if (backgroundImage != null) _bgRect = backgroundImage.GetComponent<RectTransform>();
        if (fillImage != null) _fillRect = fillImage.GetComponent<RectTransform>();
        if (_bgRect != null) maxBarWidth = Mathf.Max(1f, _bgRect.rect.width);
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

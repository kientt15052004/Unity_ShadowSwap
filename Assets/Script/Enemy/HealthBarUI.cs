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

    [Header("Loss Effect")]
    public Image lossImage;
    public float lossSpeed = 0.5f;

    [Header("Positioning")]
    public Vector3 localOffset = new Vector3(0f, 1.5f, 0f);
    public bool faceCamera = true;

    [Header("Show / Hide")]
    public bool showOnlyOnDamage = true;
    public float visibleDuration = 3.0f;
    public Color inactiveBackgroundColor = Color.white;
    public Color activeBackgroundColor = new Color(0f, 0f, 0f, 0.6f);

    [Header("Behavior tweaks")]
    public bool freezeFillUntilDead = true;
    public bool showHealthTextEvenWhenFrozen = false;

    // internals
    private int maxHealth;
    private int currentHealth;
    private Transform target;
    private bool isChildOfTarget = false;
    private float lastHitTime = -999f;
    private float maxBarWidth = 100f;
    private float lossFillAmount;
    private float previousLossFillAmount;

    // ------------------ NEW: Auto-assign fields ------------------
    /// <summary>
    /// Try to auto-assign missing UI references by scanning children.
    /// Best results if child names include keywords: "fill", "loss", "bg", "hptext".
    /// Call this from Initialize() or right after Instantiating the prefab.
    /// </summary>
    public void AutoAssignFields()
    {
        // If everything already assigned, skip
        if (worldCanvas != null && fillImage != null && lossImage != null && backgroundImage != null && healthText != null)
            return;

        // Cache children components
        Image[] images = GetComponentsInChildren<Image>(includeInactive: true);
        TextMeshProUGUI[] tmps = GetComponentsInChildren<TextMeshProUGUI>(includeInactive: true);
        UnityEngine.UI.Text[] uis = GetComponentsInChildren<UnityEngine.UI.Text>(includeInactive: true);
        Canvas foundCanvas = GetComponentInChildren<Canvas>(includeInactive: true);

        // Assign canvas if missing
        if (worldCanvas == null && foundCanvas != null) worldCanvas = foundCanvas;

        // Helper local functions for finding by name
        System.Func<string, Image> FindImageByName = (key) =>
        {
            foreach (var img in images)
            {
                if (img == null) continue;
                if (img.gameObject.name.ToLower().Contains(key.ToLower())) return img;
            }
            return null;
        };

        System.Func<string, TextMeshProUGUI> FindTMPByName = (key) =>
        {
            foreach (var t in tmps)
            {
                if (t == null) continue;
                if (t.gameObject.name.ToLower().Contains(key.ToLower())) return t;
            }
            return null;
        };

        // 1) Try by conventional names
        if (fillImage == null)
        {
            // prefer "fill" or "hp" in name, and prefer Image.Type.Filled
            Image candidate = FindImageByName("fill") ?? FindImageByName("hp") ?? FindImageByName("bar");
            if (candidate == null)
            {
                // fallback: pick first filled image
                foreach (var img in images) if (img != null && img.type == Image.Type.Filled) { candidate = img; break; }
            }
            if (candidate == null && images.Length > 0) candidate = images[0];
            fillImage = candidate;
        }

        if (lossImage == null)
        {
            Image candidate = FindImageByName("loss") ?? FindImageByName("white") ?? FindImageByName("flash");
            // prefer a different image than fillImage and prefer Filled
            if (candidate == null)
            {
                foreach (var img in images)
                {
                    if (img == null) continue;
                    if (img == fillImage) continue;
                    if (img.type == Image.Type.Filled) { candidate = img; break; }
                }
            }
            if (candidate == null)
            {
                foreach (var img in images) if (img != null && img != fillImage) { candidate = img; break; }
            }
            lossImage = candidate;
        }

        if (backgroundImage == null)
        {
            Image candidate = FindImageByName("bg") ?? FindImageByName("back") ?? FindImageByName("background");
            if (candidate == null)
            {
                // pick first image that is not fillImage and not lossImage
                foreach (var img in images) if (img != null && img != fillImage && img != lossImage) { candidate = img; break; }
            }
            backgroundImage = candidate;
        }

        if (healthText == null)
        {
            // try TMP by name
            TextMeshProUGUI tCandidate = FindTMPByName("hp") ?? FindTMPByName("health") ?? FindTMPByName("text");
            if (tCandidate == null && tmps.Length > 0) tCandidate = tmps[0];
            if (tCandidate != null) healthText = tCandidate;
            else if (uis.Length > 0)
            {
                // fallback: if only UnityEngine.UI.Text exists, create a simple TMP wrapper if possible
                // but by default assign null and user can use UpdateHealth via other means
            }
        }

        // Final sanity: if fillImage exists but not set to Filled and there is a filled alternative, prefer filled
        if (fillImage != null && fillImage.type != Image.Type.Filled)
        {
            foreach (var img in images)
            {
                if (img == null) continue;
                if (img.type == Image.Type.Filled && img != lossImage)
                {
                    fillImage = img;
                    break;
                }
            }
        }

        // Set reasonable defaults if still null (avoid null refs)
        // backgroundImage optional, fillImage ideally present
        if (backgroundImage == null && images.Length > 0) backgroundImage = images[0];
        if (fillImage == null && images.Length > 0) fillImage = images[0];
        // lossImage optional — ok if null
        // healthText optional — ok if null

        // Compute maxBarWidth if backgroundImage found
        if (backgroundImage != null)
        {
            var bgRT = backgroundImage.GetComponent<RectTransform>();
            if (bgRT != null && bgRT.rect.width > 0f) maxBarWidth = bgRT.rect.width;
        }
    }
    // ------------------ END auto-assign ------------------

    public void Initialize(Transform targetTransform, int maxHp, bool isBoss)
    {
        // First attempt auto- assign so Initialize can work even if prefab missing refs
        AutoAssignFields();

        target = targetTransform;
        maxHealth = Mathf.Max(1, maxHp);
        currentHealth = maxHealth;

        isChildOfTarget = (target != null && transform.parent == target);

        if (fillImage != null)
        {
            fillImage.color = isBoss ? new Color(0.5f, 0f, 0.5f) : Color.red;
        }

        if (backgroundImage != null)
        {
            var bgRT = backgroundImage.GetComponent<RectTransform>();
            if (bgRT != null && bgRT.rect.width > 0f) maxBarWidth = bgRT.rect.width;
            backgroundImage.color = activeBackgroundColor;
        }

        UpdateUI();

        if (showOnlyOnDamage)
        {
            gameObject.SetActive(false);
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
        if (showOnlyOnDamage && gameObject.activeSelf && Time.time > lastHitTime + visibleDuration)
        {
            gameObject.SetActive(false);
            if (backgroundImage != null) backgroundImage.color = inactiveBackgroundColor;
            return;
        }

        if (lossImage != null && lossImage.type == Image.Type.Filled)
        {
            if (lossImage.fillAmount > lossFillAmount)
            {
                float dec = Time.deltaTime * lossSpeed;
                lossImage.fillAmount = Mathf.Max(lossFillAmount, lossImage.fillAmount - dec);
            }
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

    public void OnDamagedShow(int newHp)
    {
        if (lossImage != null && lossImage.type == Image.Type.Filled) previousLossFillAmount = lossImage.fillAmount;
        else previousLossFillAmount = 1.0f;

        currentHealth = Mathf.Clamp(newHp, 0, maxHealth);

        if (showOnlyOnDamage)
        {
            gameObject.SetActive(true);
            if (backgroundImage != null) backgroundImage.color = activeBackgroundColor;
        }

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
        bool isAlive = currentHealth > 0;
        float desiredFill = (!freezeFillUntilDead || !isAlive) ? Mathf.Clamp01(ratio) : 1f;

        lossFillAmount = ratio;
        if (lossImage != null && lossImage.type == Image.Type.Filled)
        {
            if (ratio < previousLossFillAmount)
            {
                lossImage.fillAmount = previousLossFillAmount;
            }
            else
            {
                lossImage.fillAmount = ratio;
            }

            if (freezeFillUntilDead && isAlive)
            {
                lossImage.fillAmount = 1f;
            }
        }

        if (fillImage != null)
        {
            if (fillImage.type == Image.Type.Filled)
            {
                fillImage.fillAmount = desiredFill;
            }
            else
            {
                var rt = fillImage.GetComponent<RectTransform>();
                if (rt != null) rt.sizeDelta = new Vector2(maxBarWidth * desiredFill, rt.sizeDelta.y);
            }
        }

        if (healthText != null)
        {
            if (freezeFillUntilDead && isAlive && !showHealthTextEvenWhenFrozen)
            {
                healthText.text = $"{maxHealth}/{maxHealth}";
            }
            else
            {
                healthText.text = $"{currentHealth}/{maxHealth}";
            }
        }
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
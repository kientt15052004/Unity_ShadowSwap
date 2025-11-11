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

        // Find fillImage
        if (fillImage == null)
        {
            Image candidate = FindImageByName("UISprite") ?? FindImageByName("fill") ?? FindImageByName("hp") ?? FindImageByName("bar");
            if (candidate == null)
            {
                foreach (var img in images) if (img != null && img.type == Image.Type.Filled) { candidate = img; break; }
            }
            if (candidate == null && images.Length > 0) candidate = images[0];
            fillImage = candidate;
        }

        // Find lossImage (prefer different from fill)
        if (lossImage == null)
        {
            Image candidate = FindImageByName("UISprite") ?? FindImageByName("loss") ?? FindImageByName("white") ?? FindImageByName("flash");
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

        // backgroundImage
        if (backgroundImage == null)
        {
            Image candidate = FindImageByName("bg") ?? FindImageByName("back") ?? FindImageByName("background");
            if (candidate == null)
            {
                foreach (var img in images) if (img != null && img != fillImage && img != lossImage) { candidate = img; break; }
            }
            backgroundImage = candidate;
        }

        // healthText
        if (healthText == null)
        {
            TextMeshProUGUI tCandidate = FindTMPByName("hp") ?? FindTMPByName("health") ?? FindTMPByName("text");
            if (tCandidate == null && tmps.Length > 0) tCandidate = tmps[0];
            if (tCandidate != null) healthText = tCandidate;
        }

        // If both images are filled-type, sync their fill method and origin so they animate together
        if (fillImage != null && lossImage != null)
        {
            // prefer fillImage to be the one configured; if it's not Filled, try pick a Filled one
            if (fillImage.type != Image.Type.Filled)
            {
                foreach (var img in images) if (img != null && img.type == Image.Type.Filled && img != lossImage) { fillImage = img; break; }
            }

            // If lossImage not Filled but fillImage is, prefer making lossImage Filled (so fillAmount works)
            if (fillImage != null && fillImage.type == Image.Type.Filled)
            {
                // ensure lossImage uses same fill settings
                if (lossImage != null)
                {
                    lossImage.type = Image.Type.Filled;
                    lossImage.fillMethod = fillImage.fillMethod;
                    lossImage.fillOrigin = fillImage.fillOrigin;
                    lossImage.fillClockwise = fillImage.fillClockwise;
                }
            }

            // Ensure sibling order: lossImage must be rendered ON TOP of fillImage
            if (fillImage != null && lossImage != null && fillImage.transform.parent == lossImage.transform.parent)
            {
                int fi = fillImage.transform.GetSiblingIndex();
                int desired = Mathf.Max(0, fi + 1);
                lossImage.transform.SetSiblingIndex(desired);
            }
            else if (lossImage != null)
            {
                // put lossImage as last sibling in its parent so it renders top-most in that canvas group
                lossImage.transform.SetAsLastSibling();
            }
        }

        // Set reasonable defaults if still null
        if (backgroundImage == null && images.Length > 0) backgroundImage = images[0];
        if (fillImage == null && images.Length > 0) fillImage = images[0];

        // Compute maxBarWidth if backgroundImage found
        if (backgroundImage != null)
        {
            var bgRT = backgroundImage.GetComponent<RectTransform>();
            if (bgRT != null && bgRT.rect.width > 0f) maxBarWidth = bgRT.rect.width;
        }

        // Canvas safety: ensure world canvas is world-space and has sorting enabled
        if (worldCanvas != null)
        {
            if (worldCanvas.renderMode != RenderMode.WorldSpace) worldCanvas.renderMode = RenderMode.WorldSpace;
            worldCanvas.overrideSorting = true;
            worldCanvas.sortingOrder = 1000; // high so it's visible above most UI
            if (Camera.main != null) worldCanvas.worldCamera = Camera.main;
        }
    }
    // ------------------ END auto-assign ------------------

    public void Initialize(Transform targetTransform, int maxHp, bool isBoss)
    {
        AutoAssignFields();

        target = targetTransform;
        maxHealth = Mathf.Max(1, maxHp);
        currentHealth = maxHealth;

        isChildOfTarget = (target != null && transform.parent == target);

        if (fillImage != null)
        {
            fillImage.color = isBoss ? new Color(0.5f, 0f, 0.5f) : Color.red;
            if (fillImage.type == Image.Type.Filled) fillImage.fillAmount = 1f;
        }

        if (lossImage != null)
        {
            // ensure lossImage visible and above
            lossImage.gameObject.SetActive(true);
            // if lossImage is filled type, set to full initially
            if (lossImage.type == Image.Type.Filled) lossImage.fillAmount = 1f;
            // make sure alpha not zero
            var c = lossImage.color; if (c.a <= 0f) c.a = 1f; lossImage.color = c;
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

        // IMPORTANT: don't force lossImage.fillAmount to 1 if you want loss effect to show previous value.
        // We only set lossImage.fillAmount = previousLossFillAmount on damage (OnDamagedShow saved it).
        if (lossImage != null && lossImage.type == Image.Type.Filled)
        {
            // If this is initialization (previousLossFillAmount is 0) ensure lossImage at least matches desiredFill
            if (previousLossFillAmount <= 0f)
                previousLossFillAmount = Mathf.Clamp01(desiredFill);

            // If health decreased: keep lossImage at previous value so LateUpdate can lerp it down
            if (ratio < previousLossFillAmount)
            {
                lossImage.fillAmount = previousLossFillAmount;
            }
            else
            {
                // health increased or equal: sync loss image immediately
                lossImage.fillAmount = ratio;
            }

            // DO NOT override with 1f here; freeze behavior should only affect visible fill (fillImage)
            // If user wants freeze until dead, we keep loss image as previous value so it won't "reveal" the new fill
        }

        if (fillImage != null)
        {
            if (freezeFillUntilDead && isAlive)
            {
                // freeze visual fill (show full) until dead
                if (fillImage.type == Image.Type.Filled) fillImage.fillAmount = 1f;
                else
                {
                    var rt = fillImage.GetComponent<RectTransform>();
                    if (rt != null) rt.sizeDelta = new Vector2(maxBarWidth * 1f, rt.sizeDelta.y);
                }
            }
            else
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
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if TMP_PRESENT
using TMPro;
#endif

// ------------------------ Shared types (can be moved to separate files) ------------------------
// (LƯU Ý: Nếu bạn đã tạo IDamageable_Structs.cs, bạn có thể xóa phần này)
public struct DamageInfo
{
    public int amount;
    public Vector2 origin;
    public GameObject source;
    public bool isCritical;
    public DamageInfo(int amount, Vector2 origin = default, GameObject source = null, bool isCritical = false)
    {
        this.amount = amount; this.origin = origin; this.source = source; this.isCritical = isCritical;
    }
}

public interface IDamageable { void TakeDamage(DamageInfo info); }


public class Health : MonoBehaviour, IDamageable
{
    [Header("Health")]
    public int maxHealth = 100;
    public float invincibilitySeconds = 0.2f;

    public System.Action<DamageInfo> OnDamaged;
    public System.Action OnDied;

    private int currentHealth;
    private float lastHitTime = -999f;

    void Awake() { currentHealth = maxHealth; }

    public int Current => currentHealth;
    public int Max => maxHealth;

    public void TakeDamage(DamageInfo info)
    {
        if (Time.time < lastHitTime + invincibilitySeconds) return;
        lastHitTime = Time.time;
        currentHealth -= info.amount;
        OnDamaged?.Invoke(info);
        if (currentHealth <= 0) OnDied?.Invoke();
    }

    public void Heal(int amount) { currentHealth = Mathf.Min(maxHealth, currentHealth + amount); }
}


public class AttackData : ScriptableObject
{
    public int damage = 10;
    public float range = 0.6f;
    public float cooldown = 1f;
    public LayerMask hitLayers = ~0;
    public GameObject hitVFX;
}


public class AttackHitbox : MonoBehaviour
{
    public LayerMask hitLayers = ~0;

    // REFACTOR: Tạo một bộ đệm (buffer) để tái sử dụng, tránh tạo mảng mới mỗi lần.
    // Con số 10 có nghĩa là nó có thể phát hiện tối đa 10 colliders 1 lúc.
    private Collider2D[] _hitsBuffer = new Collider2D[10];

    public void DoAttack(AttackData data, Transform origin, bool isFacingRight)
    {
        if (data == null || origin == null) return;

        Vector2 center = (Vector2)origin.position + (isFacingRight ? Vector2.right : Vector2.left) * (data.range * 0.5f);
        int hitCount = Physics2D.OverlapCircleNonAlloc(center, data.range, _hitsBuffer, data.hitLayers);

        Debug.Log($"[AttackHitbox] origin={origin.name} center={center} range={data.range} hits={hitCount}");

        for (int i = 0; i < hitCount; i++)
        {
            var col = _hitsBuffer[i];
            if (col == null) continue;
            Debug.Log($"[AttackHitbox] hit collider: {col.name}, go={col.gameObject.name}");

            IDamageable d = col.GetComponent<IDamageable>();
            if (d == null)
            {
                var monos = col.GetComponentsInParent<MonoBehaviour>(true);
                foreach (var m in monos) if (m is IDamageable) { d = (IDamageable)m; break; }
            }

            if (d != null)
            {
                Debug.Log($"[AttackHitbox] applying {data.damage} damage to {((MonoBehaviour)d).gameObject.name}");
                d.TakeDamage(new DamageInfo(data.damage, origin.position, origin.gameObject, false));
                if (data.hitVFX != null) Instantiate(data.hitVFX, col.transform.position, Quaternion.identity);
            }
            else
            {
                Debug.LogWarning($"[AttackHitbox] HIT but target is NOT IDamageable: {col.gameObject.name}");
            }

            // Clear buffer slot
            _hitsBuffer[i] = null;
        }
    }
}
public static class JumpMathUtility
{
    // Simulate a jump (forward integration) to estimate reachability.
    public static bool CanReachByJump(Vector2 start, Vector2 target, float horizontalSpeed, float jumpForce, float gravity, LayerMask groundLayer, float timeStep = 0.02f, float maxTime = 2f, float groundCheckRadius = 0.15f)
    {
        if (gravity <= 0f) gravity = 9.81f;
        float dir = Mathf.Sign(target.x - start.x);
        float vx = Mathf.Abs(horizontalSpeed) * dir;
        float vy = jumpForce;
        Vector2 pos = start;
        float t = 0f;
        while (t < maxTime)
        {
            vy -= gravity * timeStep;
            pos += new Vector2(vx * timeStep, vy * timeStep);
            if (pos.y <= target.y + 0.2f)
            {
                Collider2D ground = Physics2D.OverlapCircle(pos, groundCheckRadius, groundLayer);
                if (ground != null) { if (Mathf.Abs(pos.x - target.x) <= 0.6f) return true; }
            }
            if (pos.y < start.y - 5f) break;
            t += timeStep;
            // Dừng nếu đi quá xa theo chiều ngang
            if (Mathf.Abs(pos.x - start.x) > 10f) break;
        }
        return false;
    }
}

// ------------------------ EnemyCore (main file) ------------------------

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyCore : MonoBehaviour, IDamageable
{
    [Header("Movement")]
    public float patrolDistance = 4f;
    public float patrolSpeed = 2f;
    public float chaseDistance = 8f;
    public float chaseSpeed = 4f;

    [Header("Wall/Jump")]
    public float wallCheckDistance = 0.6f;
    public float ledgeCheckDistance = 0.6f;
    public float jumpForce = 7f;
    public LayerMask groundLayer;
    public Transform groundCheck;
    public float groundCheckRadius = 0.12f;
    public float maxJumpableGap = 1.2f;

    [Header("Attack")]
    public AttackData attackData;
    public AttackHitbox attackHitbox;
    public float attackRange = 1f;
    public float attackCooldown = 1f;

    [Header("Health")]
    public int maxHealth = 100;
    public float invincibilityTime = 0.15f;

    [Header("UI")]
    [Tooltip("Optional: assign a HealthBarUI prefab (world-space). If left null, runtime bar will be created.")]
    public GameObject healthBarPrefab;
    public Transform uiRoot; // optional world-space canvas parent
    public Vector3 healthBarOffset = new Vector3(0f, 1.2f, 0f);

    // internals
    [HideInInspector] public Rigidbody2D rb;
    [HideInInspector] public Animator anim;
    [HideInInspector] public Transform player;

    [HideInInspector] public Vector2 startPos;
    [HideInInspector] public Vector2 leftPoint;
    [HideInInspector] public Vector2 rightPoint;
    [HideInInspector] public Vector2 targetPoint;
    [HideInInspector] public bool isFacingRight = true;

    protected int currentHealth;
    protected float lastHitAt = -999f;
    public float lastAttackAt = -999f;
    protected bool isHurt = false;
    protected bool isDead = false;
    protected bool isAttacking = false;
    protected float lastDirectionChangeAt = -999f;
    protected float lastJumpAt = -999f;
    protected float jumpCooldown = 0.5f;
    protected bool isBlocking = false;

    // exposed read-only properties
    public bool IsDead => isDead;
    public bool IsHurt => isHurt;
    public bool IsAttacking => isAttacking;
    public int CurrentHealth => currentHealth;
    public bool IsBlocking => isBlocking;
    public virtual bool IsBossType => false;

    // internal UI instance
    public HealthBarUI healthBarInstance;

    // small cooldown guard
    public float directionChangeCooldown = 0.4f;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        if (anim == null)
        {
            Debug.LogWarning($"[{name}] Animator missing on EnemyCore — animations will be skipped.");
        }

        currentHealth = maxHealth;
        EnsureGroundCheck();
        var healthComp = GetComponent<Health>();
        if (healthComp != null)
        {
            // sync currentHealth with Health component
            currentHealth = healthComp.Current;
            healthComp.OnDamaged += (DamageInfo info) =>
            {
                // Đồng bộ máu:
                currentHealth = healthComp.Current;

                // **QUAN TRỌNG:** Phải gọi OnDamagedShow để kích hoạt hiển thị và cập nhật giá trị
                if (healthBarInstance != null)
                {
                    healthBarInstance.OnDamagedShow(currentHealth);
                }

                // Kích hoạt logic hurt của EnemyCore (ví dụ: animation, dừng di chuyển)
                OnTakeDamageLocal(info);

                if (currentHealth <= 0) OnDieLocal();
            };
            healthComp.OnDied += () => { OnDieLocal(); };
        }
    }

    protected virtual void Start()
    {
        if (player == null) FindPlayer();
        startPos = transform.position;
        leftPoint = new Vector2(startPos.x - patrolDistance, startPos.y);
        rightPoint = new Vector2(startPos.x + patrolDistance, startPos.y);
        targetPoint = rightPoint;
        currentHealth = maxHealth;

        // Try resolve uiRoot automatically (prefer existing world-space Canvas)
        if (uiRoot == null)
        {
            Canvas c = FindObjectOfType<Canvas>();
            if (c != null && c.renderMode == RenderMode.WorldSpace) uiRoot = c.transform;
        }

        // 1) Try prefab (if assigned)
        if (healthBarPrefab != null)
        {
            Debug.Log($"{name}: healthBarPrefab assigned -> attempting Instantiate...");
            if (healthBarPrefab.GetComponent<EnemyCore>() != null)
            {
                Debug.LogWarning($"{name}: healthBarPrefab accidentally contains EnemyCore — skipping to avoid recursion.");
            }
            else
            {
                GameObject go = Instantiate(healthBarPrefab);
                if (uiRoot != null) go.transform.SetParent(uiRoot, worldPositionStays: true);
                // find HealthBarUI on root or children
                HealthBarUI ui = go.GetComponent<HealthBarUI>() ?? go.GetComponentInChildren<HealthBarUI>();
                if (ui != null)
                {
                    healthBarInstance = ui;
                    PostInitHealthbar(go);
                    Debug.Log($"{name}: healthbar instantiated from prefab -> OK. instance={go.name}");
                }
                else
                {
                    Debug.LogWarning($"{name}: Instantiated prefab but no HealthBarUI found on it or children. Prefab root: {go.name}");
                    // keep instance for inspection but continue to fallback creation below
                    Destroy(go); // remove incorrect prefab to avoid clutter
                }
            }
        }

        // 2) try find child in case healthbar was added manually to enemy
        if (healthBarInstance == null)
        {
            HealthBarUI existing = GetComponentInChildren<HealthBarUI>(true);
            if (existing != null)
            {
                healthBarInstance = existing;
                PostInitHealthbar(healthBarInstance.gameObject);
                Debug.Log($"{name}: Found child HealthBarUI and initialized it.");
            }
        }

        // 3) fallback: create runtime healthbar (guaranteed to include HealthBarUI)
        if (healthBarInstance == null)
        {
            Debug.LogWarning($"{name}: healthBarInstance IS NULL after prefab/child checks. Creating runtime fallback healthbar now...");
            GameObject go = CreateRuntimeHealthBar();
            if (go != null)
            {
                HealthBarUI ui = go.GetComponent<HealthBarUI>() ?? go.GetComponentInChildren<HealthBarUI>();
                if (ui != null)
                {
                    healthBarInstance = ui;
                    PostInitHealthbar(go);
                    Debug.Log($"{name}: Runtime healthbar created and initialized. instance={go.name}");
                }
                else
                {
                    Debug.LogError($"{name}: Runtime healthbar created BUT HealthBarUI component NOT FOUND.");
                    Destroy(go);
                }
            }
        }

        if (healthBarInstance == null)
        {
            Debug.LogWarning($"{name}: Final check -> healthBarInstance is STILL NULL. Enemy will run without healthbar.");
        }
    }

    // Helper called any time after instantiating/locating the healthbar root
    private void PostInitHealthbar(GameObject hbRoot)
    {
        HealthBarUI ui = hbRoot.GetComponent<HealthBarUI>() ?? hbRoot.GetComponentInChildren<HealthBarUI>();
        if (ui == null) return;

        // compute vertical offset from renderer or collider
        float spriteHeight = 0f;
        var rend = GetComponentInChildren<Renderer>();
        if (rend != null) spriteHeight = rend.bounds.size.y;
        else
        {
            var col = GetComponent<Collider2D>();
            if (col != null) spriteHeight = col.bounds.size.y;
        }

        // user asked: "muon thanh mau hien cao hon mot ti" -> add extraPadding
        float extraPadding = 0.85f; // <-- bạn có thể chỉnh value này (đơn vị world units)
        float yOffset = Mathf.Max(0.8f, spriteHeight * 0.6f) + extraPadding;
        Vector3 computedOffset = new Vector3(0f, yOffset, 0f);

        // parent to enemy so it follows exactly; use localPosition offset
        hbRoot.transform.SetParent(this.transform, worldPositionStays: false);
        hbRoot.transform.localPosition = computedOffset;
        hbRoot.transform.localRotation = Quaternion.identity;

        // set reasonable scale (tăng chút để dễ thấy)
        float defaultScale = 0.04f; // tăng lên so với trước
        hbRoot.transform.localScale = Vector3.one * defaultScale;

        // ensure canvas settings
        Canvas canvas = hbRoot.GetComponent<Canvas>() ?? hbRoot.GetComponentInChildren<Canvas>();
        if (canvas != null)
        {
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 1000;
            if (Camera.main != null) canvas.worldCamera = Camera.main;
        }

        // init UI
        ui.localOffset = computedOffset;
        ui.faceCamera = true;

        // pass measured background width if available
        float bgWidth = 0f;
        if (ui.backgroundImage != null)
        {
            var bgRT = ui.backgroundImage.GetComponent<RectTransform>();
            if (bgRT != null) bgWidth = bgRT.rect.width;
        }
        if (bgWidth <= 0f) bgWidth = 120f;

        ui.SetMaxWidth(bgWidth);
        ui.Initialize(this.transform, maxHealth, IsBossType);
    }

    public void SpawnOrInitHealthBarNow()
    {
        if (healthBarInstance != null)
        {
            Debug.Log($"{name}: healthbar already present.");
            return;
        }
        // Re-run the same init logic quickly (simple)
        // Try prefab first
        if (healthBarPrefab != null && healthBarPrefab.GetComponent<EnemyCore>() == null)
        {
            GameObject go = Instantiate(healthBarPrefab);
            if (uiRoot != null) go.transform.SetParent(uiRoot, worldPositionStays: true);
            HealthBarUI ui = go.GetComponent<HealthBarUI>() ?? go.GetComponentInChildren<HealthBarUI>();
            if (ui != null)
            {
                healthBarInstance = ui;
                healthBarInstance.localOffset = healthBarOffset;
                healthBarInstance.Initialize(transform, maxHealth, IsBossType);
                Debug.Log($"{name}: SpawnOrInit -> prefab instantiated and initialized.");
                return;
            }
        }

        // Try to find existing child
        HealthBarUI existing = GetComponentInChildren<HealthBarUI>(true);
        if (existing != null)
        {
            healthBarInstance = existing;
            healthBarInstance.localOffset = healthBarOffset;
            healthBarInstance.Initialize(transform, maxHealth, IsBossType);
            Debug.Log($"{name}: SpawnOrInit -> found child HealthBarUI and initialized.");
            return;
        }

        // Fallback runtime
        GameObject fallback = CreateRuntimeHealthBar();
        if (fallback != null)
        {
            if (uiRoot != null) fallback.transform.SetParent(uiRoot, worldPositionStays: true);
            HealthBarUI ui = fallback.GetComponent<HealthBarUI>() ?? fallback.GetComponentInChildren<HealthBarUI>();
            if (ui != null)
            {
                healthBarInstance = ui;
                healthBarInstance.localOffset = healthBarOffset;
                healthBarInstance.Initialize(transform, maxHealth, IsBossType);
                Debug.Log($"{name}: SpawnOrInit -> runtime fallback created and initialized.");
                return;
            }
        }

        Debug.LogWarning($"{name}: SpawnOrInitHealthBarNow failed to create healthbar.");
    }
    protected virtual void Update()
    {
        if (isDead) return;
        if (player == null) FindPlayer();

        UpdateAnimationFlags();
    }

    protected virtual void FixedUpdate()
    {
        if (isDead || isHurt || isAttacking)
        {
            rb.velocity = new Vector2(0f, rb.velocity.y);
            return;
        }

        if (Time.time < lastDirectionChangeAt + directionChangeCooldown)
        {
            rb.velocity = new Vector2(0f, rb.velocity.y);
            return;
        }

        Vector2 dir = isFacingRight ? Vector2.right : Vector2.left;
        Vector2 baseOrigin = groundCheck != null ? (Vector2)groundCheck.position : (Vector2)transform.position;
        Vector2 rayOrigin = baseOrigin + (Vector2.up * 0.1f);
        float groundCheckRayLength = 1.2f + 0.1f;

        RaycastHit2D wall = Physics2D.Raycast(rayOrigin, dir, wallCheckDistance, groundLayer);
        Vector2 ledgeCheckStart = rayOrigin + dir * ledgeCheckDistance;
        bool groundAhead = Physics2D.Raycast(ledgeCheckStart, Vector2.down, groundCheckRayLength, groundLayer);

        if (wall.collider != null)
        {
            if (player != null && player.position.y > transform.position.y + 0.4f && IsGrounded())
            {
                if (Time.time > lastJumpAt + jumpCooldown)
                {
                    TryJump();
                    lastJumpAt = Time.time;
                }
            }
            else
            {
                ReverseDirection();
            }
        }
        else if (!groundAhead && IsGrounded())
        {
            if (player != null && player.position.y > transform.position.y + 0.3f &&
                Mathf.Abs(player.position.x - transform.position.x) <= maxJumpableGap &&
                Time.time > lastJumpAt + jumpCooldown)
            {
                JumpTowardsPlayer();
                lastJumpAt = Time.time;
            }
            else
            {
                ReverseDirection();
            }
        }
    }

    // movement helpers
    public void SimplePatrol()
    {
        if (isAttacking || Time.time < lastDirectionChangeAt + directionChangeCooldown) return;
        SetAnimatorBoolSafe("Run", true);
        if (Vector2.Distance(transform.position, targetPoint) < 0.2f) targetPoint = (targetPoint == rightPoint) ? leftPoint : rightPoint;
        MoveTowards(targetPoint, patrolSpeed);
    }

    public void SimpleChase()
    {
        if (isAttacking || Time.time < lastDirectionChangeAt + directionChangeCooldown) return;
        SetAnimatorBoolSafe("Run", true);
        if (player != null) MoveTowards(player.position, chaseSpeed);
    }

    public void MoveTowards(Vector2 t, float speed)
    {
        Vector2 dir = (t - (Vector2)transform.position).normalized;
        rb.velocity = new Vector2(dir.x * speed, rb.velocity.y);
    }

    // Attack control
    public virtual void TryAttack()
    {
        if (Time.time < lastAttackAt + attackCooldown) return;
        if (isAttacking || isHurt || isDead) return;

        lastAttackAt = Time.time;
        isAttacking = true;
        rb.velocity = new Vector2(0f, rb.velocity.y);
        SetAnimatorBoolSafe("IsAttacking", true);
        // If you use animation events, they should call OnAttackHit() and EndAttack()
        // Optionally you can implement a coroutine fallback in subclass.
    }

    // Called from animation event at the hit frame
    public virtual void OnAttackHit()
    {
        if (attackHitbox != null && attackData != null)
            attackHitbox.DoAttack(attackData, transform, isFacingRight);
    }

    // Called from animation event at the end of the attack animation
    public virtual void EndAttack()
    {
        isAttacking = false;
        SetAnimatorBoolSafe("IsAttacking", false);
    }

    public void ReverseDirection()
    {
        lastDirectionChangeAt = Time.time;
        rb.velocity = Vector2.zero;
        Flip();
        targetPoint = (targetPoint == rightPoint) ? leftPoint : rightPoint;
    }

    public void TryJump() { if (IsGrounded()) rb.velocity = new Vector2(rb.velocity.x, jumpForce); }
    public void JumpTowardsPlayer() { if (IsGrounded() && player != null) { Vector2 dir = ((Vector2)player.position - (Vector2)transform.position).normalized; rb.velocity = new Vector2(dir.x * patrolSpeed, jumpForce); } }

    // Damage / health handling (IDamageable)
    public virtual void TakeDamage(DamageInfo info)
    {
        Debug.Log($"[EnemyCore.TakeDamage] {name} taking {info.amount} damage from {(info.source != null ? info.source.name : "unknown")} (isBlocking={isBlocking})");

        if (isDead) return;
        if (Time.time < lastHitAt + invincibilityTime) { Debug.Log("[EnemyCore] ignore due to invincibility"); return; }

        if (isBlocking)
        {
            info.amount = Mathf.RoundToInt(info.amount * 0.2f);
        }

        lastHitAt = Time.time;
        currentHealth -= info.amount;
        if (currentHealth < 0) currentHealth = 0;

        Debug.Log($"[EnemyCore] {name} HP now {currentHealth}/{maxHealth}");

        OnTakeDamageLocal(info); // current behavior, animations...

        // **ĐẢM BẢO cập nhật UI ở đây**
        if (healthBarInstance != null)
        {
            // gọi OnDamagedShow để đảm bảo healthbar hiện khi bị đòn và reset auto-hide timer
            healthBarInstance.OnDamagedShow(currentHealth);
            Debug.Log($"{name}: healthBarInstance.OnDamagedShow({currentHealth}) called");
        }
        else
        {
            Debug.LogWarning($"{name}: healthBarInstance is NULL when taking damage!");
        }

        if (currentHealth <= 0) OnDieLocal();
    }

    public virtual void OnTakeDamageLocal(DamageInfo info)
    {
        if (isDead) return;
        isHurt = true;
        Debug.Log($"[Enemy Health] {gameObject.name} took {info.amount} damage from {(info.source != null ? info.source.name : "unknown")}. Remaining: {currentHealth}/{maxHealth}");

        if (isAttacking)
        {
            isAttacking = false;
            SetAnimatorBoolSafe("IsAttacking", false);
        }

        rb.velocity = Vector2.zero;
        StartCoroutine(HurtRoutine());
    }

    protected IEnumerator HurtRoutine()
    {
        SetAnimatorBoolSafe("IsHurt", true);
        yield return new WaitForSeconds(0.45f);
        SetAnimatorBoolSafe("IsHurt", false);
        isHurt = false;
    }

    public virtual void OnDieLocal()
    {
        isDead = true;
        SetAnimatorBoolSafe("IsDead", true);

        // disable physics interaction but keep object alive so death animation and events can run
        rb.velocity = Vector2.zero;
        var c = GetComponent<Collider2D>();
        if (c) c.enabled = false;
        if (rb) rb.bodyType = RigidbodyType2D.Kinematic;

        // hide healthbar
        if (healthBarInstance != null) healthBarInstance.Hide();

        Debug.Log($"[{gameObject.name}] died (OnDieLocal). Waiting for DestroySelf() animation event if any.");
    }

    // Animation event helpers (call from animation)
    public void DisableCollidersAndPhysics()
    {
        var c = GetComponent<Collider2D>();
        if (c) c.enabled = false;
        if (rb) rb.bodyType = RigidbodyType2D.Kinematic;
        Debug.Log($"[{gameObject.name}] Physics disabled by animation event.");
    }

    public void DestroySelf()
    {
        Debug.Log($"[{gameObject.name}] DestroySelf called by animation event.");
        Destroy(gameObject);
    }

    // Utilities
    public void FindPlayer() { var go = GameObject.FindGameObjectWithTag("Player"); if (go != null) player = go.transform; }
    public void FlipByVelocity() { if (rb != null && rb.velocity.x > 0.1f && !isFacingRight) Flip(); else if (rb != null && rb.velocity.x < -0.1f && isFacingRight) Flip(); }
    public void Flip() { isFacingRight = !isFacingRight; var s = transform.localScale; s.x *= -1f; transform.localScale = s; }
    public void FaceTarget(Vector2 targetPosition) { float direction = targetPosition.x - transform.position.x; if (direction > 0 && !isFacingRight) Flip(); else if (direction < 0 && isFacingRight) Flip(); }

    private bool HasAnimatorParameter(string paramName)
    {
        if (anim == null) return false;
        var pars = anim.parameters;
        for (int i = 0; i < pars.Length; i++) if (pars[i].name == paramName) return true;
        return false;
    }

    public void SetAnimatorBoolSafe(string paramName, bool value)
    {
        if (anim == null) return;
        if (HasAnimatorParameter(paramName))
        {
            anim.SetBool(paramName, value);
        }
    }

    public void UpdateAnimationFlags()
    {
        if (anim == null) return;

        SetAnimatorBoolSafe("IsAttacking", isAttacking);
        SetAnimatorBoolSafe("IsHurt", isHurt);
        SetAnimatorBoolSafe("IsDead", isDead);
        SetAnimatorBoolSafe("IsBlocking", isBlocking);

        // common movement flags
        bool isJumping = !IsGrounded() && rb != null && rb.velocity.y > 0.05f;
        SetAnimatorBoolSafe("IsJumping", isJumping);

        bool isRunning = IsGrounded() && rb != null && Mathf.Abs(rb.velocity.x) > 0.1f;
        SetAnimatorBoolSafe("Run", isRunning);
        SetAnimatorBoolSafe("IsMoving", isRunning);
    }

    // Groundhelpers
    public void EnsureGroundCheck() { if (groundCheck != null) return; GameObject go = new GameObject("GroundCheck"); go.transform.parent = transform; go.transform.localPosition = Vector3.zero; groundCheck = go.transform; groundCheckRadius = 0.12f; }
    public bool IsGrounded() { if (groundCheck == null) return false; return Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer); }

    // Create a simple runtime healthbar (fallback)
    private GameObject CreateRuntimeHealthBar()
    {
        GameObject root = new GameObject($"{name}_HPBar_Runtime");

        // Canvas
        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 1000;
        if (Camera.main != null) canvas.worldCamera = Camera.main;

        // Scaler & Raycaster
        var cs = root.AddComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ConstantPhysicalSize;
        cs.dynamicPixelsPerUnit = 10f;
        root.AddComponent<GraphicRaycaster>();

        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(200f, 32f);

        // 1. Background (Lớp dưới cùng)
        GameObject bg = new GameObject("BG");
        bg.transform.SetParent(root.transform, false);
        Image bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0f, 0f, 0f, 0.6f);
        RectTransform bgRect = bg.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero; bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero; bgRect.offsetMax = Vector2.zero;

        // 2. Loss Fill (Thanh trắng/vàng, chạy chậm - Lớp giữa)
        GameObject loss = new GameObject("LossFill");
        loss.transform.SetParent(bg.transform, false);
        Image lossImg = loss.AddComponent<Image>();
        lossImg.type = Image.Type.Filled; // Đặt kiểu Filled
        lossImg.fillMethod = Image.FillMethod.Horizontal;
        lossImg.fillOrigin = (int)Image.OriginHorizontal.Left;
        lossImg.fillAmount = 1f;
        lossImg.color = Color.white; // Màu trắng cho hiệu ứng mất máu
        RectTransform lossRect = loss.GetComponent<RectTransform>();
        lossRect.anchorMin = new Vector2(0f, 0f); lossRect.anchorMax = new Vector2(1f, 1f);
        lossRect.offsetMin = new Vector2(4f, 4f); lossRect.offsetMax = new Vector2(-4f, -4f);

        // 3. Fill (Thanh đỏ, cập nhật ngay lập tức - Lớp trên cùng)
        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(bg.transform, false);
        Image fillImg = fill.AddComponent<Image>();
        fillImg.type = Image.Type.Filled; // Đặt kiểu Filled
        fillImg.fillMethod = Image.FillMethod.Horizontal;
        fillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
        fillImg.fillAmount = 1f;
        fillImg.color = Color.red;
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0f, 0f); fillRect.anchorMax = new Vector2(1f, 1f);
        fillRect.offsetMin = new Vector2(4f, 4f); fillRect.offsetMax = new Vector2(-4f, -4f);

        // Text (optional)
        GameObject txtGO = new GameObject("HPText");
        txtGO.transform.SetParent(root.transform, false);
        // SỬA LỖI: Dùng TextMeshProUGUI thay vì UnityEngine.UI.Text
        TextMeshProUGUI healthText = txtGO.AddComponent<TextMeshProUGUI>();
        healthText.text = "100/100";
        healthText.fontSize = 18f;
        healthText.alignment = TextAlignmentOptions.Center;
        RectTransform txtRect = healthText.GetComponent<RectTransform>();
        txtRect.anchorMin = Vector2.zero; txtRect.anchorMax = Vector2.one;
        txtRect.offsetMin = Vector2.zero; txtRect.offsetMax = Vector2.zero;

        // Add HealthBarUI và GÁN CÁC THAM CHIẾU
        HealthBarUI ui = root.AddComponent<HealthBarUI>();
        ui.worldCanvas = canvas;
        ui.fillImage = fillImg;
        ui.backgroundImage = bgImg;
        ui.lossImage = lossImg; // Gán Loss Image
        ui.healthText = healthText; // Gán TextMeshProUGUI

        // default transform (will be adjusted by PostInitHealthbar)
        root.transform.localScale = Vector3.one * 0.04f;

        return root;
    }


    // Debug gizmos
    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }

        Vector2 dir = isFacingRight ? Vector2.right : Vector2.left;
        Vector2 baseOrigin = groundCheck != null ? (Vector2)groundCheck.position : (Vector2)transform.position;
        Vector2 rayOrigin = baseOrigin + (Vector2.up * 0.1f);
        float groundCheckRayLength = 1.3f;

        Gizmos.color = Color.red;
        Gizmos.DrawLine(rayOrigin, rayOrigin + dir * wallCheckDistance);

        Gizmos.color = Color.yellow;
        Vector2 ledgeCheckStart = rayOrigin + dir * ledgeCheckDistance;
        Gizmos.DrawLine(ledgeCheckStart, ledgeCheckStart + Vector2.down * groundCheckRayLength);
    }
}

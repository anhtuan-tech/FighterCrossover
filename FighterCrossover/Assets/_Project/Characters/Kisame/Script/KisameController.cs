using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Controller nhân vật Kisame - kế thừa FighterBase.
///
/// Keybindings mặc định (override qua settings.json):
///   A/D – Di chuyển  |  K – Nhảy / Double Jump  |  S – Guard (giữ)
///   L   – Dash       |  J – Combo 4 hit          |  U – Skill1 WaterSlash
///   I   – Skill2 Shark Ultimate
///
/// Animator Parameters:
///   Float  : Speed, VelocityY
///   Bool   : IsGrounded, IsBlocking, IsMoving
///   Trigger: Jump, DoubleJump, Fall, Dash,
///            Attack1..4, SkillRanged, SkillUltimate,
///            Hit, Knockback, BlockHit, Die
/// </summary>
public class KisameController : FighterBase
{
    // ────────────────────────────────────────────────────────────────────────────
    //  INSPECTOR FIELDS
    // ────────────────────────────────────────────────────────────────────────────
    [Header("─── KISAME SKILLS ───")]
    [Tooltip("Prefab vùng sóng nước AoE (Skill 1 – phím U). Gắn WaterSlashArea.cs.")]
    public GameObject waterSlashPrefab;

    [Tooltip("Prefab cá mập nước khổng lồ (Skill 2 – phím I). Gắn SharkUltimate.cs.")]
    public GameObject sharkUltimatePrefab;

    [Tooltip("Điểm spawn sóng nước (empty child). Để trống → dùng offset mặc định.")]
    public Transform waterSlashSpawnPoint;

    [Header("─── TIMING ───")]
    [Tooltip("Thời gian lock sau Skill1 – nên bằng độ dài clip Skill1 (giây).")]
    public float rangedLockDuration = 1.2f;

    [Tooltip("Thời gian lock sau Skill2 – nên bằng độ dài clip Skill2 (giây).")]
    public float ultimateLockDuration = 3.0f;

    // ────────────────────────────────────────────────────────────────────────────
    //  PRIVATE – KEYBINDINGS
    // ────────────────────────────────────────────────────────────────────────────
    private AnimeFighter.UI.KeybindingsData _keys;
    private bool _bindingsReady = false;

    // ────────────────────────────────────────────────────────────────────────────
    //  PRIVATE – ANIMATOR HASHES
    // ────────────────────────────────────────────────────────────────────────────
    private static readonly int HashSpeed      = Animator.StringToHash("Speed");
    private static readonly int HashVelocityY  = Animator.StringToHash("VelocityY");
    private static readonly int HashIsGrounded = Animator.StringToHash("IsGrounded");
    private static readonly int HashIsBlocking = Animator.StringToHash("IsBlocking");
    private static readonly int HashIsMoving   = Animator.StringToHash("IsMoving");
    private static readonly int HashFall       = Animator.StringToHash("Fall");
    private static readonly int HashDash       = Animator.StringToHash("Dash");

    // ── Fall tracking (chỉ set trigger 1 lần khi bắt đầu rơi) ──────────────────
    private bool _wasFalling = false;

    // ── Combo chain: cho phép nhận input khi đang trong Attacking state ─────────
    // Thời điểm animation attack bắt đầu (để tính cửa sổ chain)
    private float _attackStartTime = 0f;
    // Thời gian animation 1 hit (giây). Phải khớp với độ dài clip Attack.
    [Header("─── COMBO SETTINGS ───")]
    [Tooltip("Độ dài trung bình 1 animation hit (giây). Sau thời gian này mới nhận combo tiếp theo.")]
    public float singleHitDuration = 0.35f;

    // ────────────────────────────────────────────────────────────────────────────
    //  INITIALIZATION
    // ────────────────────────────────────────────────────────────────────────────
    #region Initialization
    protected override void Awake()
    {
        base.Awake();
        LoadKeybindings();
    }

    private void LoadKeybindings()
    {
        string path = System.IO.Path.Combine(Application.persistentDataPath, "settings.json");
        AnimeFighter.UI.GameSettingsData data = null;

        if (System.IO.File.Exists(path))
        {
            try
            {
                data = JsonUtility.FromJson<AnimeFighter.UI.GameSettingsData>(
                    System.IO.File.ReadAllText(path));
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[KisameController] settings.json parse error: {ex.Message}");
            }
        }

        if (data == null)
        {
            data = new AnimeFighter.UI.GameSettingsData();
            data.SetDefaultValues();
        }

        _keys = (playerNumber == 1) ? data.player1Keys : data.player2Keys;
        _bindingsReady = true;
        Debug.Log($"[KisameController] Keybindings loaded for Player {playerNumber}.");
    }
    #endregion

    // ────────────────────────────────────────────────────────────────────────────
    //  GAME LOOP
    // ────────────────────────────────────────────────────────────────────────────
    #region GameLoop
    protected override void Update()
    {
        if (CurrentState == FighterState.Dead) return;

        CheckGrounded();
        HandleStateLogic();
        HandleStaminaRegen();
        UpdateAnimations();

        if (!_bindingsReady) return;

        bool defenseHeld = GetKey(_keys.defense);

        if (defenseHeld)
        {
            if (GetKeyDown(_keys.jump) || GetKeyDown(_keys.dodge)) TryDropDown();
        }
        else
        {
            // Jump: hoạt động ngay cả khi đang Attacking (không block jump)
            if (GetKeyDown(_keys.jump))  ExecuteJump();
            if (GetKeyDown(_keys.dodge)) TriggerDash();
        }

        // Attack: xử lý combo chain riêng (bypass CanAct khi đang Attacking)
        if (GetKeyDown(_keys.attack))       TriggerAttack();

        // Skill chỉ hoạt động khi thực sự rảnh (CanAct)
        if (GetKeyDown(_keys.rangedAttack)) TriggerRanged();
        if (GetKeyDown(_keys.specialMove))  TriggerUltimate();
    }

    protected override void HandleStateLogic()
    {
        // Reset combo nếu đã quá lâu không attack
        if (comboStep > 0
            && Time.time - lastAttackTime > comboResetTime
            && CurrentState != FighterState.Attacking)
        {
            comboStep = 0;
        }

        // Đọc input di chuyển ngang
        float horizontal = 0f;
        if (_bindingsReady)
        {
            if (GetKey(_keys.moveLeft))  horizontal -= 1f;
            if (GetKey(_keys.moveRight)) horizontal += 1f;
        }
        else if (playerInput?.actions != null)
        {
            var moveAct = playerInput.actions.FindAction("Move");
            if (moveAct != null) horizontal = moveAct.ReadValue<Vector2>().x;
        }

        moveInput = new Vector2(horizontal, 0f);

        if (!CanAct()) return;

        bool blocking = _bindingsReady && GetKey(_keys.defense);

        if (blocking && isGrounded)
            ChangeState(FighterState.Blocking);
        else if (Mathf.Abs(moveInput.x) > 0.1f)
            ChangeState(FighterState.Moving);
        else
            ChangeState(isGrounded ? FighterState.Idle : FighterState.Jumping);
    }

    protected override void UpdateAnimations()
    {
        if (anim == null) return;

        anim.SetFloat(HashSpeed,      Mathf.Abs(moveInput.x));
        anim.SetFloat(HashVelocityY,  rb.linearVelocity.y);
        anim.SetBool(HashIsGrounded,  isGrounded);
        anim.SetBool(HashIsBlocking,  CurrentState == FighterState.Blocking);
        anim.SetBool(HashIsMoving,    CurrentState == FighterState.Moving);

        // Fall Trigger: chỉ set 1 lần khi bắt đầu rơi
        bool isFallingNow = !isGrounded
                         && rb.linearVelocity.y < -1.0f
                         && CurrentState != FighterState.Attacking
                         && CurrentState != FighterState.Dashing
                         && CurrentState != FighterState.Stunned;

        if (isFallingNow && !_wasFalling)
            anim.SetTrigger(HashFall);

        _wasFalling = isFallingNow;
    }
    #endregion

    // ────────────────────────────────────────────────────────────────────────────
    //  JUMP OVERRIDE – fix double jump khi rơi từ platform
    // ────────────────────────────────────────────────────────────────────────────
    #region JumpOverride

    /// <summary>
    /// Override CheckGrounded: thực hiện raycast riêng để reset cả currentJumps lẫn
    /// _airJumpCount khi tiếp đất, tránh phụ thuộc vào base reset.
    /// </summary>
    private int _airJumpCount = 0; // Số lần nhảy trong không khí (reset khi tiếp đất)

    protected override void CheckGrounded()
    {
        bool wasGrounded = isGrounded;

        // Thực hiện raycast giống FighterBase
        Vector2 rayStart = (Vector2)transform.position + Vector2.up * 0.1f;
        RaycastHit2D[] hits = Physics2D.RaycastAll(rayStart, Vector2.down, 0.25f, groundLayer);

        isGrounded     = false;
        groundCollider = null;

        foreach (var hit in hits)
        {
            if (hit.collider != null && hit.collider.gameObject != gameObject)
            {
                isGrounded     = true;
                groundCollider = hit.collider;
                break;
            }
        }

        // Reset counter khi tiếp đất (velocity đi xuống hoặc đứng yên)
        if (isGrounded && rb.linearVelocity.y <= 0.01f)
        {
            currentJumps  = 0;
            _airJumpCount = 0;
        }
    }

    /// <summary>
    /// Override ExecuteJump: phân biệt Jump/DoubleJump bằng _airJumpCount.
    ///
    /// Khi rơi xuống platform (chưa nhảy lần nào), _airJumpCount = 0:
    ///   - Bấm K lần 1 (đang rơi): _airJumpCount = 0 → nhảy → trigger "Jump",  _airJumpCount = 1.
    ///   - Bấm K lần 2 (vẫn trên không): _airJumpCount = 1 → nhảy → trigger "DoubleJump".
    ///   - Chạm đất: _airJumpCount = 0 (reset).
    /// </summary>
    protected override void ExecuteJump()
    {
        if (!CanAct()) return;

        // Kiểm tra còn lượt nhảy không
        bool canJump = isGrounded || _airJumpCount < (maxJumps - 1);
        if (!canJump) return;

        ChangeState(FighterState.Jumping);
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);

        if (_airJumpCount == 0)
        {
            // Lần nhảy đầu tiên (từ đất hoặc rơi xuống chưa nhảy lần nào)
            _airJumpCount = 1;
            currentJumps  = 1;
            anim?.SetTrigger("Jump");
        }
        else
        {
            // Double jump (đã nhảy 1 lần từ trước)
            _airJumpCount++;
            currentJumps++;
            anim?.SetTrigger("DoubleJump");
        }
    }
    #endregion

    // ────────────────────────────────────────────────────────────────────────────
    //  COMBAT TRIGGERS
    // ────────────────────────────────────────────────────────────────────────────
    #region CombatTriggers

    /// <summary>
    /// TriggerAttack hỗ trợ combo chain:
    ///   - Nếu đang Idle/Moving/Jumping → kích hoạt bình thường.
    ///   - Nếu đang Attacking → nhận input trong "cửa sổ chain" để queue hit tiếp theo.
    ///
    /// Cửa sổ chain mở sau singleHitDuration giây kể từ khi bắt đầu hit.
    /// </summary>
    private void TriggerAttack()
    {
        if (!isGrounded) return;
        if (CurrentState == FighterState.Stunned || CurrentState == FighterState.Dead) return;
        if (CurrentState == FighterState.Dashing) return;

        // Nếu đang Attacking: chỉ chain khi đã qua cửa sổ thời gian tối thiểu
        if (CurrentState == FighterState.Attacking)
        {
            bool inChainWindow = (Time.time - _attackStartTime) >= singleHitDuration;
            if (!inChainWindow) return;
        }

        ExecuteAttack();
    }

    private void TriggerRanged()
    {
        if (!CanAct() || !isGrounded) return;
        ExecuteRanged();
    }

    private void TriggerUltimate()
    {
        if (!CanAct() || !isGrounded) return;
        ExecuteUltimate();
    }
    #endregion

    // ────────────────────────────────────────────────────────────────────────────
    //  COMBAT EXECUTION
    // ────────────────────────────────────────────────────────────────────────────
    #region CombatExecution

    /// <summary>
    /// Combo 4 đòn. Hỗ trợ chain: có thể gọi khi đang Attacking.
    /// Fallback Invoke đảm bảo state luôn được mở khóa dù AnimEvent không được gán.
    /// </summary>
    protected override void ExecuteAttack()
    {
        ChangeState(FighterState.Attacking);
        rb.linearVelocity = Vector2.zero;
        lastAttackTime    = Time.time;
        _attackStartTime  = Time.time; // Ghi thời điểm bắt đầu hit này

        comboStep = (comboStep >= 4) ? 1 : comboStep + 1;

        // Reset mọi trigger Attack đang pending để tránh animation bị queue chồng
        for (int i = 1; i <= 4; i++)
            anim?.ResetTrigger("Attack" + i);

        anim?.SetTrigger("Attack" + comboStep);

        // Fallback unlock: gọi AnimationEvent_EndAttack sau khi animation hit xong
        // Dùng singleHitDuration * 2 để có đủ thời gian animation play và nhận input chain
        CancelInvoke(nameof(AnimationEvent_EndAttack));
        Invoke(nameof(AnimationEvent_EndAttack), singleHitDuration * 2f);
    }

    /// <summary>Skill 1 – Water Slash. AnimEvent SpawnWaterSlash ở ~65-75% clip.</summary>
    private void ExecuteRanged()
    {
        ChangeState(FighterState.Attacking);
        rb.linearVelocity = Vector2.zero;
        lastAttackTime    = Time.time;

        anim?.ResetTrigger("SkillRanged");
        anim?.SetTrigger("SkillRanged");

        CancelInvoke(nameof(AnimationEvent_EndAttack));
        Invoke(nameof(AnimationEvent_EndAttack), rangedLockDuration);
    }

    /// <summary>Skill 2 – Shark Ultimate. AnimEvent SpawnShark ở ~80-85% clip.</summary>
    private void ExecuteUltimate()
    {
        ChangeState(FighterState.Attacking);
        rb.linearVelocity = Vector2.zero;
        lastAttackTime    = Time.time;

        anim?.ResetTrigger("SkillUltimate");
        anim?.SetTrigger("SkillUltimate");

        CancelInvoke(nameof(AnimationEvent_EndAttack));
        Invoke(nameof(AnimationEvent_EndAttack), ultimateLockDuration);
    }

    /// <summary>
    /// Override Dash: teleport-style, tránh xuyên tường bằng Raycast.
    /// </summary>
    protected override IEnumerator DashRoutine()
    {
        ChangeState(FighterState.Dashing);
        stats.stamina     -= 20;
        lastStaminaUseTime = Time.time;

        anim?.ResetTrigger("Dash");
        anim?.SetTrigger(HashDash);

        float dir      = transform.localScale.x;
        float dashDist = 5.0f;

        rb.gravityScale   = 0f;
        rb.linearVelocity = Vector2.zero;

        Vector2 startPos  = rb.position;
        Vector2 targetPos = startPos + new Vector2(dir * dashDist, 0f);

        RaycastHit2D[] hits = Physics2D.RaycastAll(startPos, new Vector2(dir, 0f), dashDist, groundLayer);
        foreach (var h in hits)
        {
            if (h.collider != null && h.collider.gameObject != gameObject)
            {
                targetPos = h.point - new Vector2(dir * 0.4f, 0f);
                break;
            }
        }

        rb.position = targetPos;

        yield return new WaitForSeconds(dashDuration);

        rb.gravityScale = 3.5f;
        ChangeState(FighterState.Idle);
    }
    #endregion

    // ────────────────────────────────────────────────────────────────────────────
    //  OVERRIDE TAKE DAMAGE & KNOCKBACK
    // ────────────────────────────────────────────────────────────────────────────
    #region TakeDamageOverride

    public override void TakeDamage(float damage, float attackerPosX, bool isHeavyAttack = false)
    {
        base.TakeDamage(damage, attackerPosX, isHeavyAttack);
    }

    protected override void ApplyKnockback(float attackerPosX)
    {
        float knockbackDir = transform.position.x > attackerPosX ? 1f : -1f;
        rb.linearVelocity  = Vector2.zero;
        rb.AddForce(new Vector2(knockbackDir * knockbackForce.x, knockbackForce.y), ForceMode2D.Impulse);

        anim?.SetTrigger("Knockback");

        CancelInvoke(nameof(ResetStun));
        Invoke(nameof(ResetStun), 0.8f);
    }
    #endregion

    // ────────────────────────────────────────────────────────────────────────────
    //  ANIMATION EVENTS
    // ────────────────────────────────────────────────────────────────────────────
    #region AnimationEvents

    /// <summary>
    /// ★ ANIMATION EVENT – clip Skill1, gắn tại frame kiếm chạm đất (~65-75% clip).
    /// Spawn Water Slash đúng vị trí của waterSlashSpawnPoint.
    /// </summary>
    public void AnimationEvent_SpawnWaterSlash()
    {
        if (waterSlashPrefab == null) return;

        float dir = transform.localScale.x;
        
        // Dùng Transform đã gán trong Inspector làm mốc duy nhất
        Vector2 spawnPos = (waterSlashSpawnPoint != null) 
            ? (Vector2)waterSlashSpawnPoint.position 
            : (Vector2)transform.position; // Fallback an toàn nếu lỡ quên gán

        GameObject     obj  = Instantiate(waterSlashPrefab, spawnPos, Quaternion.identity);
        WaterSlashArea area = obj.GetComponent<WaterSlashArea>();

        if (area != null)
            area.Setup(dir, gameObject, targetLayer);
    }

    /// <summary>
    /// ★ ANIMATION EVENT – clip Skill2, gắn tại frame kết ấn xong (~80-85% clip).
    ///
    /// Cá mập được spawn và giữ yên (riseDelay = 0).
    /// Để cá lao đi ĐÚNG LÚC animation triệu hồi xong:
    ///   → Gán Animation Event tên "AnimEvent_StartCharge" trên clip Rise của Prefab Shark
    ///     tại FRAME CUỐI của clip Rise (khi cá mập đã hình thành hoàn chỉnh).
    ///   → SharkUltimate.AnimEvent_StartCharge() sẽ kích hoạt phase lao đi.
    /// </summary>
    public void AnimationEvent_SpawnShark()
    {
        if (sharkUltimatePrefab == null)
        {
            Debug.LogWarning("[KisameController] sharkUltimatePrefab chưa được gán!");
            return;
        }

        float   dir      = transform.localScale.x;
        // Spawn ngay cạnh Kisame, hơi ra phía trước
        Vector2 spawnPos = (Vector2)transform.position + new Vector2(dir * 1.5f, 0f);

        GameObject    obj    = Instantiate(sharkUltimatePrefab, spawnPos, Quaternion.identity);
        SharkUltimate effect = obj.GetComponent<SharkUltimate>();

        if (effect != null)
        {
            // riseDelay = 0 → cá mập KHÔNG tự lao sau timer
            // Thay vào đó: cắm AnimEvent_StartCharge trên clip Rise của prefab shark
            // tại frame cuối để kích hoạt đúng lúc animation xong
            effect.Setup(new Vector2(dir, 0f), gameObject, targetLayer, riseDelay: 0f);
        }
        else
            Debug.LogError("[KisameController] SharkUltimate component không tìm thấy trên Prefab!");
    }

    /// <summary>
    /// ★ ANIMATION EVENT – mỗi clip Combo tại frame lưỡi kiếm trúng địch.
    /// Áp sát thương: đòn thứ 4 là Heavy → KnockedDown.
    /// </summary>
    public override void AnimationEvent_DealDamage()
    {
        if (attackHitbox == null) return;

        Collider2D[] hits = Physics2D.OverlapCircleAll(attackHitbox.position, attackRadius, targetLayer);
        foreach (var hit in hits)
        {
            if (hit.gameObject == gameObject) continue;
            IDamageable damageable = hit.GetComponent<IDamageable>();
            if (damageable == null) continue;

            float dmg     = 10f + comboStep * 5f;
            bool  isHeavy = (comboStep == 4);
            damageable.TakeDamage(dmg, transform.position.x, isHeavy);
        }
    }
    #endregion

    // ────────────────────────────────────────────────────────────────────────────
    //  INPUT HELPERS (Unity Input System)
    // ────────────────────────────────────────────────────────────────────────────
    #region InputHelpers

    private bool GetKey(KeyCode keyCode)
    {
        if (Keyboard.current == null) return false;
        Key key = ToInputSystemKey(keyCode);
        return key != Key.None && Keyboard.current[key].isPressed;
    }

    private bool GetKeyDown(KeyCode keyCode)
    {
        if (Keyboard.current == null) return false;
        Key key = ToInputSystemKey(keyCode);
        return key != Key.None && Keyboard.current[key].wasPressedThisFrame;
    }

    private static Key ToInputSystemKey(KeyCode kc)
    {
        switch (kc)
        {
            case KeyCode.A:     return Key.A;
            case KeyCode.D:     return Key.D;
            case KeyCode.W:     return Key.W;
            case KeyCode.S:     return Key.S;
            case KeyCode.J:     return Key.J;
            case KeyCode.K:     return Key.K;
            case KeyCode.L:     return Key.L;
            case KeyCode.U:     return Key.U;
            case KeyCode.I:     return Key.I;
            case KeyCode.O:     return Key.O;
            case KeyCode.Space: return Key.Space;
            default:            return Key.None;
        }
    }
    #endregion
}

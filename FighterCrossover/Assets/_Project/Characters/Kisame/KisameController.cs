using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Controller nhân vật Kisame - kế thừa FighterBase, tuân theo cấu trúc SasukeController.
/// Skill 1 (U): Vung kiếm → phóng luồng sóng nước (WaterSlashProjectile).
/// Skill 2 (I): Kết ấn → triệu hồi Cá Mập Nước khổng lồ (SharkUltimate).
/// </summary>
public class KisameController : FighterBase
{
    // ────────────────────────────────────────────────────────────────────────────
    //  INSPECTOR FIELDS
    // ────────────────────────────────────────────────────────────────────────────
    [Header("─── KISAME SKILLS ───")]
    [Tooltip("Prefab luồng sóng nước (Skill 1 – phím U). Gắn WaterSlashProjectile.cs.")]
    public GameObject waterSlashPrefab;

    [Tooltip("Prefab cá mập nước khổng lồ (Skill 2 – phím I). Gắn SharkUltimate.cs.")]
    public GameObject sharkUltimatePrefab;

    [Tooltip("Điểm spawn luồng sóng nước (empty child ở đầu kiếm). Để trống sẽ dùng offset mặc định.")]
    public Transform waterSlashSpawnPoint;

    [Header("─── TIMING ───")]
    [Tooltip("Thời gian lock state sau khi phóng sóng nước (giây).")]
    public float rangedLockDuration = 1.2f;

    [Tooltip("Thời gian lock state sau khi triệu hồi cá mập (giây).")]
    public float ultimateLockDuration = 3.0f;

    // ────────────────────────────────────────────────────────────────────────────
    //  PRIVATE – KEYBINDINGS
    // ────────────────────────────────────────────────────────────────────────────
    private AnimeFighter.UI.KeybindingsData _keys;
    private bool _bindingsReady = false;

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
            // Giữ đỡ + nhảy/lướt → nhảy xuống platform
            if (GetKeyDown(_keys.jump) || GetKeyDown(_keys.dodge)) TryDropDown();
        }
        else
        {
            if (GetKeyDown(_keys.jump))  ExecuteJump();
            if (GetKeyDown(_keys.dodge)) TriggerDash();
        }

        if (GetKeyDown(_keys.attack))       TriggerAttack();
        if (GetKeyDown(_keys.rangedAttack)) TriggerRanged();
        if (GetKeyDown(_keys.specialMove))  TriggerUltimate();
    }

    protected override void HandleStateLogic()
    {
        // Reset combo nếu đã lâu không attack
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
        base.UpdateAnimations();
        // Truyền Speed cho Blend Tree chạy/đứng
        anim?.SetFloat("Speed", Mathf.Abs(moveInput.x));
    }
    #endregion

    // ────────────────────────────────────────────────────────────────────────────
    //  COMBAT TRIGGERS
    // ────────────────────────────────────────────────────────────────────────────
    #region CombatTriggers
    private void TriggerAttack()
    {
        if (!CanAct() || !isGrounded) return;
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
    //  COMBAT EXECUTION (Override FighterBase)
    // ────────────────────────────────────────────────────────────────────────────
    #region CombatExecution

    /// <summary>Combo thường 4 đòn (Attack1–Attack4).</summary>
    protected override void ExecuteAttack()
    {
        ChangeState(FighterState.Attacking);
        rb.linearVelocity = Vector2.zero;
        lastAttackTime     = Time.time;

        comboStep = (comboStep >= 4) ? 1 : comboStep + 1;
        anim?.SetTrigger("Attack" + comboStep);

        CancelInvoke(nameof(AnimationEvent_EndAttack));
        Invoke(nameof(AnimationEvent_EndAttack), 0.4f);
    }

    /// <summary>
    /// Skill 1 – Phóng luồng sóng nước.
    /// AnimationEvent_SpawnWaterSlash() sẽ Instantiate đạn tại frame vung kiếm.
    /// </summary>
    private void ExecuteRanged()
    {
        ChangeState(FighterState.Attacking);
        rb.linearVelocity = Vector2.zero;
        lastAttackTime     = Time.time;

        anim?.SetTrigger("SkillRanged");

        // Fallback: mở khóa state sau rangedLockDuration giây
        CancelInvoke(nameof(AnimationEvent_EndAttack));
        Invoke(nameof(AnimationEvent_EndAttack), rangedLockDuration);
    }

    /// <summary>
    /// Skill 2 – Triệu hồi Cá Mập Nước.
    /// AnimationEvent_SpawnShark() sẽ Instantiate cá mập tại frame kết ấn xong.
    /// </summary>
    private void ExecuteUltimate()
    {
        ChangeState(FighterState.Attacking);
        rb.linearVelocity = Vector2.zero;
        lastAttackTime     = Time.time;

        anim?.SetTrigger("SkillUltimate");

        CancelInvoke(nameof(AnimationEvent_EndAttack));
        Invoke(nameof(AnimationEvent_EndAttack), ultimateLockDuration);
    }

    protected override System.Collections.IEnumerator DashRoutine()
    {
        ChangeState(FighterState.Dashing);
        stats.stamina -= 20;
        anim?.SetTrigger("Dash");

        float dir      = transform.localScale.x;
        float dashDist = 5.0f;

        rb.gravityScale   = 0f;
        rb.linearVelocity = Vector2.zero;

        // Tính toán targetPos, tránh xuyên tường
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
    //  ANIMATION EVENTS  (gắn vào Animation Clip trong Unity Editor)
    // ────────────────────────────────────────────────────────────────────────────
    #region AnimationEvents

    /// <summary>
    /// Gọi từ Animation Event trên clip Skill1 (tại frame Kisame vung kiếm tiếp đất).
    /// Instantiate WaterSlashProjectile và truyền hướng + targetLayer.
    /// </summary>
    public void AnimationEvent_SpawnWaterSlash()
    {
        if (waterSlashPrefab == null)
        {
            Debug.LogWarning("[KisameController] waterSlashPrefab chưa được gán!");
            return;
        }

        float dir = transform.localScale.x; // 1 = phải, -1 = trái

        // Ưu tiên SpawnPoint được gán thủ công, fallback offset phía trước mặt
        Vector2 spawnPos = (waterSlashSpawnPoint != null)
            ? (Vector2)waterSlashSpawnPoint.position
            : (Vector2)transform.position + new Vector2(dir * 0.8f, 0.3f);

        GameObject obj  = Instantiate(waterSlashPrefab, spawnPos, Quaternion.identity);
        var         proj = obj.GetComponent<WaterSlashProjectile>();

        if (proj != null)
            proj.Setup(new Vector2(dir, 0f), gameObject, targetLayer);
        else
            Debug.LogError("[KisameController] WaterSlashProjectile.cs không tìm thấy trên Prefab!");
    }

    /// <summary>
    /// Gọi từ Animation Event trên clip Skill2 (tại frame Kisame kết ấn xong, nước bắt đầu trồi).
    /// Instantiate SharkUltimate ngay trước mặt Kisame.
    /// </summary>
    public void AnimationEvent_SpawnShark()
    {
        if (sharkUltimatePrefab == null)
        {
            Debug.LogWarning("[KisameController] sharkUltimatePrefab chưa được gán!");
            return;
        }

        float   dir      = transform.localScale.x;
        Vector2 spawnPos = (Vector2)transform.position + new Vector2(dir * 1.5f, 0f);

        GameObject obj    = Instantiate(sharkUltimatePrefab, spawnPos, Quaternion.identity);
        var        effect = obj.GetComponent<SharkUltimate>();

        if (effect != null)
            effect.Setup(new Vector2(dir, 0f), gameObject, targetLayer);
        else
            Debug.LogError("[KisameController] SharkUltimate.cs không tìm thấy trên Prefab!");
    }

    /// <summary>Gọi trong Animation Event combo thường – áp sát thương tại hitbox.</summary>
    public override void AnimationEvent_DealDamage()
    {
        if (attackHitbox == null) return;

        Collider2D[] hits = Physics2D.OverlapCircleAll(attackHitbox.position, attackRadius, targetLayer);
        foreach (var hit in hits)
        {
            if (hit.gameObject == gameObject) continue;

            var damageable = hit.GetComponent<IDamageable>();
            if (damageable == null) continue;

            float damage  = 10f + comboStep * 5f;          // Combo đòn cuối mạnh hơn
            bool  isHeavy = (comboStep == 4);               // Đòn thứ 4 → knockback
            damageable.TakeDamage(damage, transform.position.x, isHeavy);
        }
    }
    #endregion

    // ────────────────────────────────────────────────────────────────────────────
    //  INPUT HELPERS
    // ────────────────────────────────────────────────────────────────────────────
    #region InputHelpers
    private bool GetKey(KeyCode keyCode)
    {
        if (Keyboard.current == null) return false;
        var key = ToInputSystemKey(keyCode);
        return key != Key.None && Keyboard.current[key].isPressed;
    }

    private bool GetKeyDown(KeyCode keyCode)
    {
        if (Keyboard.current == null) return false;
        var key = ToInputSystemKey(keyCode);
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

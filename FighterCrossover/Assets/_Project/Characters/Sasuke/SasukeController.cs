using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class SasukeController : FighterBase
{
    [Header("--- SASUKE SETTINGS ---")]
    public GameObject fireballPrefab; // Kỹ năng tầm xa (U)
    public GameObject chidoriEffectPrefab; // Kỹ năng đặc biệt (I)
    public float chidoriDuration = 1.5f;

    [Header("--- DYNAMIC BINDINGS ---")]
    private AnimeFighter.UI.KeybindingsData keys;
    private bool initializedBindings = false;

    #region INITIALIZATION
    protected override void Awake()
    {
        base.Awake();
        LoadKeybindings();
    }

    private void LoadKeybindings()
    {
        string saveFilePath = System.IO.Path.Combine(Application.persistentDataPath, "settings.json");
        AnimeFighter.UI.GameSettingsData settingsData = null;

        if (System.IO.File.Exists(saveFilePath))
        {
            try
            {
                string json = System.IO.File.ReadAllText(saveFilePath);
                settingsData = JsonUtility.FromJson<AnimeFighter.UI.GameSettingsData>(json);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[SasukeController] Failed to parse settings.json: {ex.Message}");
            }
        }

        if (settingsData == null)
        {
            settingsData = new AnimeFighter.UI.GameSettingsData();
            settingsData.SetDefaultValues();
        }

        // Tự động nhận diện phím cho Player 1 hoặc Player 2
        keys = (playerNumber == 1) ? settingsData.player1Keys : settingsData.player2Keys;
        initializedBindings = true;

        Debug.Log($"[SasukeController] Bindings loaded for Player {playerNumber}.");
    }
    #endregion

    #region GAME LOOPS & LOGIC
    protected override void Update()
    {
        if (CurrentState == FighterState.Dead) return;

        // Các hàm check hệ thống gốc
        CheckGrounded();
        HandleStateLogic();
        HandleStaminaRegen();
        UpdateAnimations();

        // Xử lý Input riêng của Sasuke
        if (initializedBindings)
        {
            bool isDefenseHeld = GetKey(keys.defense);

            if (isDefenseHeld)
            {
                if (GetKeyDown(keys.jump) || GetKeyDown(keys.dodge))
                {
                    TryDropDown();
                }
            }
            else
            {
                if (GetKeyDown(keys.jump)) ExecuteJump();
                if (GetKeyDown(keys.dodge)) TriggerDash();
            }

            if (GetKeyDown(keys.attack)) TriggerAttack();
            if (GetKeyDown(keys.rangedAttack)) TriggerRanged();
            if (GetKeyDown(keys.specialMove)) TriggerUltimate();
        }
    }

    protected override void HandleStateLogic()
    {
        // Reset combo
        if (comboStep > 0 && Time.time - lastAttackTime > comboResetTime && CurrentState != FighterState.Attacking)
        {
            comboStep = 0;
        }

        float horizontal = 0f;
        if (initializedBindings)
        {
            if (GetKey(keys.moveLeft)) horizontal -= 1f;
            if (GetKey(keys.moveRight)) horizontal += 1f;
        }
        else
        {
            if (playerInput != null && playerInput.actions != null)
            {
                var moveAct = playerInput.actions.FindAction("Move");
                if (moveAct != null) horizontal = moveAct.ReadValue<Vector2>().x;
            }
        }

        moveInput = new Vector2(horizontal, 0f);

        if (CanAct())
        {
            bool isBlockingInput = initializedBindings ? GetKey(keys.defense) : false;

            if (isBlockingInput && isGrounded)
            {
                ChangeState(FighterState.Blocking);
            }
            else if (Mathf.Abs(moveInput.x) > 0.1f)
            {
                ChangeState(FighterState.Moving);
            }
            else
            {
                ChangeState(isGrounded ? FighterState.Idle : FighterState.Jumping);
            }
        }
    }

    // Ghi đè hàm gửi Animation để hỗ trợ Blend Tree di chuyển ngang
    protected override void UpdateAnimations()
    {
        base.UpdateAnimations();

        if (anim != null)
        {
            float currentSpeed = Mathf.Abs(moveInput.x);
            anim.SetFloat("Speed", currentSpeed);
        }
    }
    #endregion

    #region COMBAT EXECUTION
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

    protected override void ExecuteAttack()
    {
        ChangeState(FighterState.Attacking);
        rb.linearVelocity = Vector2.zero;
        lastAttackTime = Time.time;

        comboStep++;
        if (comboStep > 4) comboStep = 1;

        if (anim != null)
        {
            anim.SetTrigger("Attack" + comboStep);
        }
        CancelInvoke(nameof(AnimationEvent_EndAttack));
        Invoke(nameof(AnimationEvent_EndAttack), 0.4f);
    }

    private void ExecuteRanged()
    {
        ChangeState(FighterState.Attacking);
        rb.linearVelocity = Vector2.zero;
        lastAttackTime = Time.time;

        if (anim != null)
        {
            anim.SetTrigger("SkillRanged");
        }
        CancelInvoke(nameof(AnimationEvent_EndAttack));
        Invoke(nameof(AnimationEvent_EndAttack), 1.5f);
    }

    private void ExecuteUltimate()
    {
        ChangeState(FighterState.Attacking);
        rb.linearVelocity = Vector2.zero;
        lastAttackTime = Time.time;

        if (anim != null)
        {
            anim.SetTrigger("SkillUltimate");
        }
        CancelInvoke(nameof(AnimationEvent_EndAttack));
        Invoke(nameof(AnimationEvent_EndAttack), 1.2f);
    }

    // Lướt 
    protected override IEnumerator DashRoutine()
    {
        ChangeState(FighterState.Dashing);
        stats.stamina -= 20;

        if (anim != null) anim.SetTrigger("Dash");

        float dashDir = transform.localScale.x;
        rb.gravityScale = 0f;
        rb.linearVelocity = Vector2.zero;

        // Cơ chế lướt chống xuyên tường
        Vector2 startPos = rb.position;
        float dashDist = 5.0f;
        Vector2 targetPos = startPos + new Vector2(dashDir * dashDist, 0f);

        RaycastHit2D[] hits = Physics2D.RaycastAll(startPos, new Vector2(dashDir, 0f), dashDist, groundLayer);
        foreach (var h in hits)
        {
            if (h.collider != null && h.collider.gameObject != gameObject)
            {
                targetPos = h.point - new Vector2(dashDir * 0.4f, 0f);
                break;
            }
        }

        rb.position = targetPos;

        yield return new WaitForSeconds(dashDuration);

        rb.gravityScale = 3.5f;
        ChangeState(FighterState.Idle);
    }
    #endregion

    #region ANIMATION EVENTS
    public override void AnimationEvent_DealDamage()
    {
        if (attackHitbox == null) return;

        Collider2D[] hits = Physics2D.OverlapCircleAll(attackHitbox.position, attackRadius, targetLayer);
        foreach (var hit in hits)
        {
            if (hit.gameObject == gameObject) continue;

            IDamageable damageable = hit.GetComponent<IDamageable>();
            if (damageable != null)
            {
                float damage = 10f + (comboStep * 5f);
                bool isHeavy = (comboStep == 4);
                damageable.TakeDamage(damage, transform.position.x, isHeavy);
            }
        }
    }

    // Fireball
    public void AnimationEvent_SpawnFireball()
    {
        if (fireballPrefab == null) return;

        float dir = transform.localScale.x;
        Vector2 spawnPos = (attackHitbox != null) ? (Vector2)attackHitbox.position : (Vector2)transform.position + new Vector2(dir * 1.0f, 0.2f);

        GameObject projObj = Instantiate(fireballPrefab, spawnPos, Quaternion.identity);
        RangedProjectile proj = projObj.GetComponent<RangedProjectile>();
        if (proj != null)
        {
            proj.Setup(new Vector2(dir, 0f), gameObject, targetLayer);
        }
    }

    // Chidori
    public void AnimationEvent_SpawnChidori()
    {
        if (chidoriEffectPrefab == null) return;

        Vector2 spawnPos = transform.position;
        GameObject ultObj = Instantiate(chidoriEffectPrefab, spawnPos, Quaternion.identity);

        UltimateEffect effect = ultObj.GetComponent<UltimateEffect>();
        if (effect != null)
        {
            effect.Setup(gameObject, targetLayer, chidoriDuration);
        }
    }
    #endregion

    #region INPUT HELPERS
    // Bộ chuyển đổi phím chuẩn từ Input System
    private bool GetKey(KeyCode keyCode)
    {
        if (Keyboard.current == null) return false;
        Key key = GetInputSystemKey(keyCode);
        if (key == Key.None) return false;
        return Keyboard.current[key].isPressed;
    }

    private bool GetKeyDown(KeyCode keyCode)
    {
        if (Keyboard.current == null) return false;
        Key key = GetInputSystemKey(keyCode);
        if (key == Key.None) return false;
        return Keyboard.current[key].wasPressedThisFrame;
    }

    private Key GetInputSystemKey(KeyCode keyCode)
    {
        switch (keyCode)
        {
            case KeyCode.A: return Key.A;
            case KeyCode.D: return Key.D;
            case KeyCode.W: return Key.W;
            case KeyCode.S: return Key.S;
            case KeyCode.J: return Key.J;
            case KeyCode.K: return Key.K;
            case KeyCode.L: return Key.L;
            case KeyCode.U: return Key.U;
            case KeyCode.I: return Key.I;
            case KeyCode.O: return Key.O;
            case KeyCode.Space: return Key.Space;
            default: return Key.None;
        }
    }
    #endregion
}
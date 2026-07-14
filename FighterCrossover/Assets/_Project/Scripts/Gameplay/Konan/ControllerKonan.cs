using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class ControllerKonan : FighterBase
{
    [Header("--- KONAN SETTINGS ---")]
    public GameObject paperShurikenPrefab;    // Kỹ năng tầm xa (Ranged)
    public GameObject paperBlizzardPrefab;    // Kỹ năng đặc biệt (Special/Ultimate)
    public float blizzardDuration = 2.0f;

    [Tooltip("Vị trí spawn shuriken. Nếu để trống sẽ dùng offset mặc định.")]
    public Transform shurikenSpawnPoint;

    [Header("--- INSPECTOR ADJUSTMENTS ---")]
    [Tooltip("Tốc độ bay của shuriken giấy.")]
    public float shurikenSpeed = 8f;

    [Tooltip("Sát thương của shuriken.")]
    public float shurikenDamage = 25f;

    [Tooltip("Sát thương của đòn đánh cận chiến.")]
    public float attackDamage = 20f;

    [Tooltip("Hệ số tốc độ ra đòn combo (1 = mặc định).")]
    public float attackSpeedMultiplier = 1.0f;

    [Tooltip("Hệ số tốc độ ra chiêu tầm xa (1 = mặc định).")]
    public float rangedSkillSpeedMultiplier = 1.8f;

    [Header("--- ULTIMATE FLASH CUTSCENE ---")]
    [Tooltip("Ảnh hiển thị khi dùng Ultimate.")]
    public Sprite ultimateFlashSprite;
    public float ultimateFlashSizeFactor = 0.6f;
    public Vector2 ultimateFlashOffset = new Vector2(0f, 100f);

    [Header("--- DYNAMIC BINDINGS ---")]
    private AnimeFighter.UI.KeybindingsData keys;
    private bool initializedBindings = false;

    // Combo buffer: ghi nhớ input khi đang đánh
    private bool comboQueued = false;

    #region INITIALIZATION
    protected override void Awake()
    {
        base.Awake();
        LoadKeybindings();
        SetupPlayerInputBindings();
    }

    public override void InitializePlayer(int num)
    {
        base.InitializePlayer(num);
        LoadKeybindings();
        SetupPlayerInputBindings();
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
                Debug.LogWarning($"[ControllerKonan] Failed to parse settings.json: {ex.Message}");
            }
        }

        if (settingsData == null)
        {
            settingsData = new AnimeFighter.UI.GameSettingsData();
            settingsData.SetDefaultValues();
        }

        keys = (playerNumber == 1) ? settingsData.player1Keys : settingsData.player2Keys;
        supportKey = keys.support;
        initializedBindings = true;
    }

    private void SetupPlayerInputBindings()
    {
        if (playerInput == null || playerInput.actions == null) return;

        playerInput.actions.Disable();

        var playerMap = playerInput.actions.FindActionMap("Player");
        if (playerMap != null)
        {
            // 1. Setup Move Action
            var moveAction = playerMap.FindAction("Move");
            if (moveAction != null)
            {
                moveAction.RemoveAllBindingOverrides();
                for (int i = 0; i < moveAction.bindings.Count; i++)
                {
                    var binding = moveAction.bindings[i];
                    if (binding.isPartOfComposite)
                    {
                        if (binding.name == "left")
                            moveAction.ApplyBindingOverride(i, GetBindingPath(keys.moveLeft));
                        else if (binding.name == "right")
                            moveAction.ApplyBindingOverride(i, GetBindingPath(keys.moveRight));
                    }
                }
            }

            // 2. Setup Jump Action
            var jumpAction = playerMap.FindAction("Jump");
            if (jumpAction != null)
            {
                jumpAction.RemoveAllBindingOverrides();
                jumpAction.ApplyBindingOverride(GetBindingPath(keys.jump));
                jumpAction.performed += ctx => OnJump();
            }

            // 3. Setup Attack Action
            var attackAction = playerMap.FindAction("Attack");
            if (attackAction != null)
            {
                attackAction.RemoveAllBindingOverrides();
                attackAction.ApplyBindingOverride(GetBindingPath(keys.attack));
                attackAction.performed += ctx => OnAttack();
            }

            // 4. Setup Block Action
            var blockAction = playerMap.FindAction("Block");
            if (blockAction != null)
            {
                blockAction.RemoveAllBindingOverrides();
                blockAction.ApplyBindingOverride(GetBindingPath(keys.defense));
            }

            // 5. Setup Dash Action
            var dashAction = playerMap.FindAction("Dash");
            if (dashAction != null)
            {
                dashAction.RemoveAllBindingOverrides();
                dashAction.ApplyBindingOverride(GetBindingPath(keys.dodge));
                dashAction.performed += ctx => OnDash();
            }

            // 6. Setup Ranged Action
            var rangedAction = playerMap.FindAction("Ranged");
            if (rangedAction != null)
            {
                rangedAction.RemoveAllBindingOverrides();
                rangedAction.ApplyBindingOverride(GetBindingPath(keys.rangedAttack));
                rangedAction.performed += ctx => OnRanged();
            }

            // 7. Setup Special Action
            var specialAction = playerMap.FindAction("Special");
            if (specialAction != null)
            {
                specialAction.RemoveAllBindingOverrides();
                specialAction.ApplyBindingOverride(GetBindingPath(keys.specialMove));
                specialAction.performed += ctx => OnSpecial();
            }

            // 8. Setup Support Action
            var supportAction = playerMap.FindAction("Support");
            if (supportAction != null)
            {
                supportAction.RemoveAllBindingOverrides();
                supportAction.ApplyBindingOverride(GetBindingPath(keys.support));
                supportAction.performed += ctx => OnSupport();
            }
        }

        playerInput.actions.Enable();
    }

    private string GetBindingPath(KeyCode keyCode)
    {
        switch (keyCode)
        {
            case KeyCode.LeftArrow:   return "<Keyboard>/leftArrow";
            case KeyCode.RightArrow:  return "<Keyboard>/rightArrow";
            case KeyCode.UpArrow:     return "<Keyboard>/upArrow";
            case KeyCode.DownArrow:   return "<Keyboard>/downArrow";
            case KeyCode.Keypad0: return "<Keyboard>/numpad0";
            case KeyCode.Keypad1: return "<Keyboard>/numpad1";
            case KeyCode.Keypad2: return "<Keyboard>/numpad2";
            case KeyCode.Keypad3: return "<Keyboard>/numpad3";
            case KeyCode.Keypad4: return "<Keyboard>/numpad4";
            case KeyCode.Keypad5: return "<Keyboard>/numpad5";
            case KeyCode.Keypad6: return "<Keyboard>/numpad6";
            case KeyCode.Keypad7: return "<Keyboard>/numpad7";
            case KeyCode.Keypad8: return "<Keyboard>/numpad8";
            case KeyCode.Keypad9: return "<Keyboard>/numpad9";
            case KeyCode.Space:   return "<Keyboard>/space";
            case KeyCode.Return:  return "<Keyboard>/enter";
            case KeyCode.Escape:  return "<Keyboard>/escape";
            case KeyCode.Tab:     return "<Keyboard>/tab";
            case KeyCode.LeftShift:    return "<Keyboard>/leftShift";
            case KeyCode.RightShift:   return "<Keyboard>/rightShift";
            case KeyCode.LeftControl:  return "<Keyboard>/leftCtrl";
            case KeyCode.RightControl: return "<Keyboard>/rightCtrl";
            case KeyCode.LeftAlt:  return "<Keyboard>/leftAlt";
            case KeyCode.RightAlt: return "<Keyboard>/rightAlt";
            default:
                string name = keyCode.ToString();
                if (name.StartsWith("Alpha")) name = name.Substring(5);
                return $"<Keyboard>/{name.ToLower()}";
        }
    }
    #endregion

    #region COMBAT - BASIC ATTACK
    private void OnAttack()
    {
        if (!CanAct())
        {
            if (CurrentState == FighterState.Attacking && comboStep < 4)
            {
                comboQueued = true;
            }
            return;
        }
        ExecuteAttack();
    }

    protected override void ExecuteAttack()
    {
        comboQueued = false;
        ChangeState(FighterState.Attacking);
        rb.linearVelocity = Vector2.zero;
        lastAttackTime = Time.time;

        comboStep++;
        if (comboStep > 4) comboStep = 1;

        if (anim != null)
        {
            anim.speed = attackSpeedMultiplier;
            anim.SetTrigger("Attack" + comboStep);
        }
        else
        {
            AnimationEvent_DealDamage();
            Invoke(nameof(AnimationEvent_EndAttack), 0.4f / attackSpeedMultiplier);
        }
    }

    public override void AnimationEvent_EndAttack()
    {
        CancelInvoke(nameof(AnimationEvent_EndAttack));
        if (comboQueued && comboStep < 4)
        {
            comboQueued = false;
            ExecuteAttack();
        }
        else
        {
            comboQueued = false;
            comboStep = 0;
            if (anim != null) anim.speed = 1f;
            ChangeState(FighterState.Idle);
        }
    }
    #endregion

    #region COMBAT - SKILLS
    public void OnRanged()
    {
        if (!CanAct() || !isGrounded) return;
        ExecuteRanged();
    }

    public void OnSpecial()
    {
        if (!CanAct() || !isGrounded) return;
        if (!HasMana(50f))
        {
            Debug.Log("[MANA] Konan không đủ mana dùng Ultimate!");
            return;
        }
        ExecuteUltimate();
    }

    private void ExecuteRanged()
    {
        ChangeState(FighterState.Attacking);
        rb.linearVelocity = Vector2.zero;
        lastAttackTime = Time.time;

        if (anim != null)
        {
            anim.speed = rangedSkillSpeedMultiplier;
            anim.SetTrigger("SkillRanged");
        }
        else
        {
            AnimationEvent_SpawnProjectile();
        }

        CancelInvoke(nameof(AnimationEvent_EndAttack));
        Invoke(nameof(AnimationEvent_EndAttack), 3.0f / rangedSkillSpeedMultiplier);
    }

    private void ExecuteUltimate()
    {
        SpendMana(50f);
        ChangeState(FighterState.Attacking);
        rb.linearVelocity = Vector2.zero;
        lastAttackTime = Time.time;

        if (anim != null) anim.speed = 1f;

        StartCoroutine(ExecuteUltimateWithFlash());
    }

    private IEnumerator ExecuteUltimateWithFlash()
    {
        Sprite flashSprite = LoadUltimateAvatar();
        bool flashDone = false;

        if (flashSprite != null)
        {
            UltimateFlashEffect.Show(this, flashSprite, ultimateFlashSizeFactor, ultimateFlashOffset, new Color(0.2f, 0.6f, 1f), () =>
            {
                flashDone = true;
            });

            while (!flashDone)
            {
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                ChangeState(FighterState.Attacking);
                yield return null;
            }
        }

        rb.linearVelocity = Vector2.zero;
        if (anim != null)
        {
            anim.SetTrigger("SkillUltimate");
        }
        else
        {
            AnimationEvent_SpawnUltimate();
        }

        CancelInvoke(nameof(AnimationEvent_EndAttack));
        Invoke(nameof(AnimationEvent_EndAttack), 0.9f);
    }

    private Sprite LoadUltimateAvatar()
    {
        if (ultimateFlashSprite != null) return ultimateFlashSprite;
#if UNITY_EDITOR
        string avatarPath = "Assets/_Project/Characters/Konan/Konan/Ultimate.png";
        var assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(avatarPath);
        foreach (var a in assets)
        {
            if (a is Sprite s) return s;
        }
#endif
        return null;
    }
    #endregion

    #region DASH OVERRIDE
    protected override IEnumerator DashRoutine()
    {
        ChangeState(FighterState.Dashing);
        stats.stamina -= 20;
        lastStaminaUseTime = Time.time;

        if (anim != null) anim.SetTrigger("Dash");

        float dashDir = transform.localScale.x;
        rb.gravityScale = 0f;
        rb.linearVelocity = Vector2.zero;

        Vector2 startPos = rb.position;
        float dashDist = 5.0f;
        Vector2 targetPos = startPos + new Vector2(dashDir * dashDist, 0f);

        Vector2 rayStart = startPos + new Vector2(0f, 0.4f);
        RaycastHit2D[] hits = Physics2D.RaycastAll(rayStart, new Vector2(dashDir, 0f), dashDist, groundLayer);
        foreach (var h in hits)
        {
            if (h.collider != null && !h.collider.isTrigger && h.collider.gameObject != gameObject && !h.collider.transform.IsChildOf(transform))
            {
                targetPos = h.point - new Vector2(0f, 0.4f) - new Vector2(dashDir * 0.4f, 0f);
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
        bool didHit = false;
        foreach (var hit in hits)
        {
            if (hit.gameObject != gameObject)
            {
                IDamageable damageable = hit.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    damageable.TakeDamage(attackDamage, transform.position.x, isHeavyAttack: (comboStep == 4));
                    didHit = true;
                }
            }
        }

        if (didHit)
        {
            GainMana(10f);
        }
    }

    public void AnimationEvent_SpawnProjectile()
    {
        if (paperShurikenPrefab == null) return;

        float dir = transform.localScale.x;
        Vector2 spawnPos = (shurikenSpawnPoint != null)
            ? (Vector2)shurikenSpawnPoint.position
            : (Vector2)transform.position + new Vector2(dir * 1.0f, 0.5f);

        GameObject projObj = Instantiate(paperShurikenPrefab, spawnPos, Quaternion.identity);
        KonanPaperShuriken proj = projObj.GetComponent<KonanPaperShuriken>();
        if (proj != null)
        {
            proj.SetStats(shurikenSpeed, shurikenDamage);
            proj.Setup(new Vector2(dir, 0f), gameObject, targetLayer);
        }
    }

    public void AnimationEvent_SpawnUltimate()
    {
        if (paperBlizzardPrefab == null) return;

        Vector2 spawnPos = transform.position;
        GameObject ultObj = Instantiate(paperBlizzardPrefab, spawnPos, Quaternion.identity);

        KonanPaperBlizzard effect = ultObj.GetComponent<KonanPaperBlizzard>();
        if (effect != null)
        {
            effect.Setup(gameObject, targetLayer, blizzardDuration);
        }
    }
    protected override void CheckGrounded()
    {
        Vector2 origin = (Vector2)transform.position + new Vector2(0f, 0.1f);
        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, 1.0f, groundLayer);
        isGrounded = hit.collider != null;
    }
    #endregion
}

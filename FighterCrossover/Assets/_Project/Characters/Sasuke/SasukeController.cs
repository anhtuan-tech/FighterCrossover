using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class SasukeController : FighterBase
{
    [Header("--- SASUKE SETTINGS ---")]
    public GameObject fireballPrefab;       // Ky nang tam xa (Ranged)
    public GameObject chidoriEffectPrefab;  // Ky nang dac biet (Special/Ultimate)
    public float chidoriDuration = 1.5f;

    [Tooltip("Vi tri spawn cau lua. Neu de trong se dung offset mac dinh.")]
    public Transform fireballSpawnPoint;

    [Header("--- INSPECTOR ADJUSTMENTS ---")]
    [Tooltip("Toc do bay cua cau lua (fireball).")]
    public float fireballSpeed = 7f;

    [Tooltip("Sat thuong cua cau lua.")]
    public float fireballDamage = 30f;

    [Tooltip("He so toc do ra don combo (1 = mac dinh, 1.5 = nhanh gap 1.5).")]
    public float attackSpeedMultiplier = 1.0f;

    [Tooltip("He so toc do ra chieu tam xa (Phoenix Flower) (1 = mac dinh, 2.5 = nhanh gap 2.5).")]
    public float rangedSkillSpeedMultiplier = 2.5f;

    [Header("--- ULTIMATE FLASH CUTSCENE ---")]
    [Tooltip("Anh hien thi khi dung Ultimate (Neu de trong se tu dong load mac dinh)")]
    public Sprite ultimateFlashSprite;
    public float ultimateFlashSizeFactor = 0.6f;
    public Vector2 ultimateFlashOffset = new Vector2(0f, 100f);

    [Header("--- SOUND ---")]
    [Tooltip("Âm thanh phát khi dùng Ultimate.")]
    public AudioClip ultimateVoiceClip;
    [Range(0f, 1f)]
    public float ultimateVoiceVolume = 0.6f;
    private AudioSource audioSource;

    [Header("--- CHIDORI DASH ---")]
    [Tooltip("Toc do lao khi dam nhanh ban dau.")]
    public float chidoriDashSpeed = 18f;

    [Tooltip("Thoi gian duy tri phase dam nhanh (giay).")]
    public float chidoriDashDuration = 0.3f;

    [Tooltip("Toc do chay trong phase keo le (units/second).")]
    public float chidoriDragSpeed = 7f;

    [Tooltip("Thoi gian keo le sau khi dam (giay).")]
    public float chidoriDragDuration = 1.4f;

    [Header("--- DYNAMIC BINDINGS ---")]
    private AnimeFighter.UI.KeybindingsData keys;
    private bool initializedBindings = false;

    // Combo buffer: ghi nho input khi dang danh
    private bool comboQueued = false;

    #region INITIALIZATION
    protected override void Awake()
    {
        base.Awake();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
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
                Debug.LogWarning($"[SasukeController] Failed to parse settings.json: {ex.Message}");
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

        Debug.Log($"[SasukeController] Bindings loaded for Player {playerNumber}. Attack: {keys.attack}, Ranged: {keys.rangedAttack}, Special: {keys.specialMove}");
    }

    private void SetupPlayerInputBindings()
    {
        if (playerInput == null || playerInput.actions == null) return;

        playerInput.actions.Disable();

        var playerMap = playerInput.actions.FindActionMap("Player");
        if (playerMap != null)
        {
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

            var jumpAction = playerMap.FindAction("Jump");
            if (jumpAction != null)
            {
                jumpAction.RemoveAllBindingOverrides();
                jumpAction.ApplyBindingOverride(GetBindingPath(keys.jump));
                jumpAction.performed += ctx => OnJump();
            }

            var attackAction = playerMap.FindAction("Attack");
            if (attackAction != null)
            {
                attackAction.RemoveAllBindingOverrides();
                attackAction.ApplyBindingOverride(GetBindingPath(keys.attack));
                attackAction.performed += ctx => OnAttack();
            }

            var blockAction = playerMap.FindAction("Block");
            if (blockAction != null)
            {
                blockAction.RemoveAllBindingOverrides();
                blockAction.ApplyBindingOverride(GetBindingPath(keys.defense));
            }

            var dashAction = playerMap.FindAction("Dash");
            if (dashAction != null)
            {
                dashAction.RemoveAllBindingOverrides();
                dashAction.ApplyBindingOverride(GetBindingPath(keys.dodge));
                dashAction.performed += ctx => OnDash();
            }

            var rangedAction = playerMap.FindAction("Ranged");
            if (rangedAction != null)
            {
                rangedAction.RemoveAllBindingOverrides();
                rangedAction.ApplyBindingOverride(GetBindingPath(keys.rangedAttack));
                rangedAction.performed += ctx => OnRanged();
            }

            var specialAction = playerMap.FindAction("Special");
            if (specialAction != null)
            {
                specialAction.RemoveAllBindingOverrides();
                specialAction.ApplyBindingOverride(GetBindingPath(keys.specialMove));
                specialAction.performed += ctx => OnSpecial();
            }

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

    #region SKILLS INPUT
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
            Debug.Log("[MANA] Khong du mana dung Chidori!");
            return;
        }
        ExecuteUltimate();
    }
    #endregion

    #region COMBAT - ATTACK COMBO (voi combo buffer)
    public override void OnAttack()
    {
        if (CurrentState == FighterState.Dead) return;
        if (!isGrounded) return;

        // Dang danh -> buffer combo de thuc hien o hit tiep theo
        if (CurrentState == FighterState.Attacking)
        {
            if (comboStep < 4)
            {
                comboQueued = true;
            }
            return;
        }

        if (!CanAct()) return;
        ExecuteAttack();
    }

    protected override void ExecuteAttack()
    {
        PlayAttackSound();
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

    // Goi tu Animation Event khi ket thuc animation danh
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
    private void ExecuteRanged()
    {
        PlayRangedSound();
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
            AnimationEvent_SpawnFireball();
        }

        // Tự động kết thúc trạng thái Attacking sau thời gian chạy chiêu để tránh bị kẹt
        CancelInvoke(nameof(AnimationEvent_EndAttack));
        Invoke(nameof(AnimationEvent_EndAttack), 4.0f / rangedSkillSpeedMultiplier);
    }

    private void ExecuteUltimate()
    {
        SpendMana(50f); // Tieu 50% mana
        ChangeState(FighterState.Attacking);
        rb.linearVelocity = Vector2.zero;
        lastAttackTime = Time.time;

        if (ultimateVoiceClip != null && audioSource != null)
            audioSource.PlayOneShot(ultimateVoiceClip, ultimateVoiceVolume);

        if (anim != null) anim.speed = 1f;

        StartCoroutine(ExecuteUltimateWithFlash());
    }

    private IEnumerator ExecuteUltimateWithFlash()
    {
        Sprite flashSprite = LoadUltimateAvatar();
        bool flashDone = false;

        if (flashSprite != null)
        {
            UltimateFlashEffect.Show(this, flashSprite, ultimateFlashSizeFactor, ultimateFlashOffset, new Color(0.5f, 0.1f, 0.9f), () =>
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
            AnimationEvent_SpawnChidori();
        }

        // Tự động kết thúc trạng thái Attacking sau đúng 3.7 giây thi triển Chidori (sau khi flash đã kết thúc)
        CancelInvoke(nameof(AnimationEvent_EndAttack));
        Invoke(nameof(AnimationEvent_EndAttack), 3.7f);
    }

    private Sprite LoadUltimateAvatar()
    {
        if (ultimateFlashSprite != null) return ultimateFlashSprite;
#if UNITY_EDITOR
        string[] paths = new string[]
        {
            "Assets/_Project/Resources/Sasuke/avatar/avatar_sasuke.png",
            "Assets/_Project/Resources/Sasuke/avatar/avatar_sasuke.jpg"
        };
        foreach (string avatarPath in paths)
        {
            var assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(avatarPath);
            foreach (var a in assets)
            {
                if (a is Sprite s) return s;
            }
            Texture2D tex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(avatarPath);
            if (tex != null)
                return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        }
#else
        Sprite s = Resources.Load<Sprite>("Sasuke/avatar/avatar_sasuke");
        if (s != null) return s;
#endif
        return null;
    }
    #endregion

    #region DASH OVERRIDE
    protected override IEnumerator DashRoutine()
    {
        PlayDashSound();
        ChangeState(FighterState.Dashing);
        stats.stamina -= 20;
        lastStaminaUseTime = Time.time; // Ghi nhan thoi gian dung stamina

        if (anim != null) anim.SetTrigger("Dash");

        float dashDir = transform.localScale.x;
        rb.gravityScale = 0f;
        rb.linearVelocity = Vector2.zero;

        Vector2 startPos = rb.position;
        float dashDist = 5.0f;
        Vector2 targetPos = startPos + new Vector2(dashDir * dashDist, 0f);

        // Nâng điểm bắn raycast lên 0.4m để tránh đâm trực tiếp vào sàn dưới chân
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
            if (hit.gameObject == gameObject) continue;

            IDamageable damageable = hit.GetComponent<IDamageable>();
            if (damageable != null)
            {
                float damage = 12f + (comboStep * 6f); // 18, 24, 30, 36 theo combo
                bool isHeavy = (comboStep == 4);
                damageable.TakeDamage(damage, transform.position.x, isHeavy);
                didHit = true;
            }
        }

        // Hoi mana +10% khi danh trung dich
        if (didHit)
        {
            GainMana(10f);
            Debug.Log($"[MANA] Sasuke danh trung! +10% mana. Hien: {stats.currentMana / stats.maxMana * 100f:F0}%");
        }
    }

    // Gan vao Animation Event khi phong cau lua (Ranged)
    public void AnimationEvent_SpawnFireball()
    {
        if (fireballPrefab == null) return;

        float dir = transform.localScale.x;
        Vector2 spawnPos = (fireballSpawnPoint != null)
            ? (Vector2)fireballSpawnPoint.position
            : (Vector2)transform.position + new Vector2(dir * 1.0f, 0.5f);

        GameObject projObj = Instantiate(fireballPrefab, spawnPos, Quaternion.identity);
        SasukeFireball proj = projObj.GetComponent<SasukeFireball>();
        if (proj != null)
        {
            proj.SetStats(fireballSpeed, fireballDamage);
            proj.Setup(new Vector2(dir, 0f), gameObject, targetLayer);
        }
    }

    // Gan vao Animation Event khi kich hoat Chidori
    public void AnimationEvent_SpawnChidori()
    {
        if (chidoriEffectPrefab == null) return;

        Vector2 spawnPos = transform.position;
        GameObject ultObj = Instantiate(chidoriEffectPrefab, spawnPos, Quaternion.identity);

        SasukeChidoriEffect effect = ultObj.GetComponent<SasukeChidoriEffect>();
        if (effect != null)
        {
            effect.Setup(gameObject, targetLayer, chidoriDuration);
        }
    }

    /// <summary>
    /// Gan vao Animation Event tai frame bat dau dam tia set cua Chidori.
    /// Day Sasuke lao nhanh ve phia dang nhin, khac phuc pivot tinh Sprite 2D.
    /// </summary>
    public void AnimationEvent_ChidoriDash()
    {
        StartCoroutine(ChidoriDashRoutine());
    }

    private IEnumerator ChidoriDashRoutine()
    {
        PlayDashSound();
        float dashDir = transform.localScale.x;
        float originalGravity = rb.gravityScale;
        rb.gravityScale = 0f;

        // Phase 1: Dam nhanh (burst)
        rb.linearVelocity = new Vector2(dashDir * chidoriDashSpeed, 0f);
        yield return new WaitForSeconds(chidoriDashDuration);

        // Phase 2: Keo le (sustained drag)
        rb.linearVelocity = new Vector2(dashDir * chidoriDragSpeed, 0f);
        yield return new WaitForSeconds(chidoriDragDuration);

        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = originalGravity;
    }
    #endregion
}

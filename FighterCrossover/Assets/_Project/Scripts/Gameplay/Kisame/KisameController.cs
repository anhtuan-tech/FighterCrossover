using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Controller nhân vật Kisame - kế thừa FighterBase.
/// Sửa đổi sử dụng hệ thống Input Action qua SetupPlayerInputBindings() tương tự Sasuke.
/// Bỏ hoàn toàn trạng thái/hoạt ảnh Fall.
/// </summary>
public class KisameController : FighterBase
{
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

    [Header("─── COMBO SETTINGS ───")]
    [Tooltip("Độ dài trung bình 1 animation hit (giây). Sau thời gian này mới nhận combo tiếp theo.")]
    public float singleHitDuration = 0.35f;

    [Header("─── ULTIMATE FLASH CUTSCENE ───")]
    [Tooltip("Ảnh hiển thị khi dùng Ultimate (Nếu để trống sẽ tự động load mặc định)")]
    public Sprite ultimateFlashSprite;
    public float ultimateFlashSizeFactor = 0.6f;
    public Vector2 ultimateFlashOffset = new Vector2(0f, 100f);

    [Header("─── SOUND ───")]
    [Tooltip("Âm thanh phát khi dùng Ultimate.")]
    public AudioClip ultimateVoiceClip;
    [Range(0f, 1f)]
    public float ultimateVoiceVolume = 0.6f;
    private AudioSource audioSource;

    private AnimeFighter.UI.KeybindingsData keys;
    private bool initializedBindings = false;

    // Combo buffer: ghi nhớ input khi đang đánh
    private bool comboQueued = false;
    private float _attackStartTime = 0f;

    private int _airJumpCount = 0; // Số lần nhảy trong không khí

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
                Debug.LogWarning($"[KisameController] Failed to parse settings.json: {ex.Message}");
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

        Debug.Log($"[KisameController] Bindings loaded for Player {playerNumber}. Attack: {keys.attack}, Ranged: {keys.rangedAttack}, Special: {keys.specialMove}");
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
            case KeyCode.Keypad0:     return "<Keyboard>/numpad0";
            case KeyCode.Keypad1:     return "<Keyboard>/numpad1";
            case KeyCode.Keypad2:     return "<Keyboard>/numpad2";
            case KeyCode.Keypad3:     return "<Keyboard>/numpad3";
            case KeyCode.Keypad4:     return "<Keyboard>/numpad4";
            case KeyCode.Keypad5:     return "<Keyboard>/numpad5";
            case KeyCode.Keypad6:     return "<Keyboard>/numpad6";
            case KeyCode.Keypad7:     return "<Keyboard>/numpad7";
            case KeyCode.Keypad8:     return "<Keyboard>/numpad8";
            case KeyCode.Keypad9:     return "<Keyboard>/numpad9";
            case KeyCode.Space:       return "<Keyboard>/space";
            case KeyCode.Return:      return "<Keyboard>/enter";
            case KeyCode.Escape:      return "<Keyboard>/escape";
            case KeyCode.Tab:         return "<Keyboard>/tab";
            case KeyCode.LeftShift:   return "<Keyboard>/leftShift";
            case KeyCode.RightShift:  return "<Keyboard>/rightShift";
            case KeyCode.LeftControl: return "<Keyboard>/leftCtrl";
            case KeyCode.RightControl: return "<Keyboard>/rightCtrl";
            case KeyCode.LeftAlt:     return "<Keyboard>/leftAlt";
            case KeyCode.RightAlt:    return "<Keyboard>/rightAlt";
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
            Debug.Log("[MANA] Không đủ mana dùng Shark Ultimate!");
            return;
        }
        ExecuteUltimate();
    }
    #endregion

    #region COMBAT & COMBO BUFFER
    public override void OnAttack()
    {
        if (CurrentState == FighterState.Dead) return;
        if (!isGrounded) return;

        // Đang đánh -> buffer combo để thực hiện ở hit tiếp theo
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
        _attackStartTime = Time.time;

        comboStep++;
        if (comboStep > 4) comboStep = 1;

        if (anim != null)
        {
            anim.SetTrigger("Attack" + comboStep);
        }
        else
        {
            AnimationEvent_DealDamage();
            Invoke(nameof(AnimationEvent_EndAttack), singleHitDuration * 2f);
        }

        // Fallback unlock
        CancelInvoke(nameof(AnimationEvent_EndAttack));
        Invoke(nameof(AnimationEvent_EndAttack), singleHitDuration * 2f);
    }

    private void ExecuteRanged()
    {
        PlayRangedSound();
        ChangeState(FighterState.Attacking);
        rb.linearVelocity = Vector2.zero;
        lastAttackTime = Time.time;

        anim?.ResetTrigger("SkillRanged");
        anim?.SetTrigger("SkillRanged");

        CancelInvoke(nameof(AnimationEvent_EndAttack));
        Invoke(nameof(AnimationEvent_EndAttack), rangedLockDuration);
    }

    private void ExecuteUltimate()
    {
        SpendMana(50f);
        ChangeState(FighterState.Attacking);
        rb.linearVelocity = Vector2.zero;
        lastAttackTime = Time.time;

        if (ultimateVoiceClip != null && audioSource != null)
            audioSource.PlayOneShot(ultimateVoiceClip, ultimateVoiceVolume);

        StartCoroutine(ExecuteUltimateWithFlash());
    }

    private IEnumerator ExecuteUltimateWithFlash()
    {
        Sprite flashSprite = LoadUltimateAvatar();
        bool flashDone = false;

        if (flashSprite != null)
        {
            // Màu xanh dương cho Kisame (Hệ nước)
            UltimateFlashEffect.Show(this, flashSprite, ultimateFlashSizeFactor, ultimateFlashOffset, new Color(0.1f, 0.4f, 0.8f), () =>
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
            AnimationEvent_SpawnShark();
        }

        CancelInvoke(nameof(AnimationEvent_EndAttack));
        Invoke(nameof(AnimationEvent_EndAttack), ultimateLockDuration);
    }

    private Sprite LoadUltimateAvatar()
    {
        if (ultimateFlashSprite != null) return ultimateFlashSprite;
#if UNITY_EDITOR
        string avatarPath = "Assets/_Project/Resources/Kisame/avatar/2a88be23acb6604ff2bc84a5f5fd6423.jpg";
        var assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(avatarPath);
        foreach (var a in assets)
        {
            if (a is Sprite s) return s;
        }
        Texture2D tex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(avatarPath);
        if (tex != null)
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
#else
        Sprite s = Resources.Load<Sprite>("Kisame/avatar/2a88be23acb6604ff2bc84a5f5fd6423");
        if (s != null) return s;
#endif
        return null;
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
            ChangeState(FighterState.Idle);
        }
    }
    #endregion

    #region DASH OVERRIDE
    protected override IEnumerator DashRoutine()
    {
        PlayDashSound();
        ChangeState(FighterState.Dashing);
        stats.stamina -= 20;
        lastStaminaUseTime = Time.time;

        anim?.ResetTrigger("Dash");
        anim?.SetTrigger("Dash");

        float dir = transform.localScale.x;
        float dashDist = 5.0f;

        rb.gravityScale = 0f;
        rb.linearVelocity = Vector2.zero;

        Vector2 startPos = rb.position;
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

    #region DAMAGE OVERRIDES
    protected override void ApplyKnockback(float attackerPosX)
    {
        float knockbackDir = transform.position.x > attackerPosX ? 1f : -1f;
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(new Vector2(knockbackDir * knockbackForce.x, knockbackForce.y), ForceMode2D.Impulse);

        anim?.SetTrigger("Knockback");

        CancelInvoke(nameof(ResetStun));
        Invoke(nameof(ResetStun), 0.8f);
    }
    #endregion

    #region ANIMATION EVENT CALLBACKS
    public void AnimationEvent_SpawnWaterSlash()
    {
        if (waterSlashPrefab == null) return;

        float dir = transform.localScale.x;
        Vector2 spawnPos = (waterSlashSpawnPoint != null) 
            ? (Vector2)waterSlashSpawnPoint.position 
            : (Vector2)transform.position + new Vector2(dir * 1.5f, 0f);

        GameObject obj = Instantiate(waterSlashPrefab, spawnPos, Quaternion.identity);
        WaterSlashArea area = obj.GetComponent<WaterSlashArea>();

        if (area != null)
            area.Setup(dir, gameObject, targetLayer);
    }

    public void AnimationEvent_SpawnShark()
    {
        if (sharkUltimatePrefab == null) return;

        float dir = transform.localScale.x;
        Vector2 spawnPos = (Vector2)transform.position + new Vector2(dir * 1.5f, 0f);

        GameObject obj = Instantiate(sharkUltimatePrefab, spawnPos, Quaternion.identity);
        SharkUltimate effect = obj.GetComponent<SharkUltimate>();

        if (effect != null)
        {
            effect.Setup(new Vector2(dir, 0f), gameObject, targetLayer, riseDelay: 0f);
        }
    }

    public override void AnimationEvent_DealDamage()
    {
        if (attackHitbox == null) return;

        Collider2D[] hits = Physics2D.OverlapCircleAll(attackHitbox.position, attackRadius, targetLayer);
        bool didHit = false;
        foreach (var hit in hits)
        {
            if (hit.gameObject == gameObject) continue;
            IDamageable damageable = hit.GetComponent<IDamageable>();
            if (damageable == null) continue;

            float dmg = 10f + comboStep * 5f;
            bool isHeavy = (comboStep == 4);
            damageable.TakeDamage(dmg, transform.position.x, isHeavy);
            didHit = true;
        }

        if (didHit)
        {
            GainMana(10f); // Hồi 10% mana khi đánh trúng (giống Sasuke)
            Debug.Log($"[MANA] Kisame đánh trúng! +10% mana. Hiện: {stats.currentMana / stats.maxMana * 100f:F0}%");
        }
    }
    #endregion
}

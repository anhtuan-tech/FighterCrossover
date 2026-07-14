using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class SuperNhonController : FighterBase
{
    [Header("--- SUPERNHON SKILLS ---")]
    public SuperNhonRangedSkill rangedSkill;
    public SuperNhonUltimateSkill ultimateSkill;

    [Header("--- ULTIMATE FLASH CUTSCENE ---")]
    [Tooltip("Ảnh hiển thị khi dùng Ultimate (Nếu để trống sẽ tự động load mặc định)")]
    public Sprite ultimateFlashSprite;
    public float ultimateFlashSizeFactor = 0.6f;
    public Vector2 ultimateFlashOffset = new Vector2(0f, 100f);

    [Header("--- DYNAMIC BINDINGS ---")]
    private AnimeFighter.UI.KeybindingsData keys;
    private bool initializedBindings = false;

    // Combo buffer: ghi nhớ input khi đang đánh
    private bool comboQueued = false;

    protected override void Awake()
    {
        base.Awake();
        
        if (rangedSkill == null)
        {
            rangedSkill = GetComponent<SuperNhonRangedSkill>();
            if (rangedSkill == null)
            {
                rangedSkill = gameObject.AddComponent<SuperNhonRangedSkill>();
            }
        }

        if (ultimateSkill == null)
        {
            ultimateSkill = GetComponent<SuperNhonUltimateSkill>();
            if (ultimateSkill == null)
            {
                ultimateSkill = gameObject.AddComponent<SuperNhonUltimateSkill>();
            }
        }

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
                Debug.LogWarning($"[SuperNhonController] Failed to parse settings.json: {ex.Message}");
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
                        {
                            moveAction.ApplyBindingOverride(i, GetBindingPath(keys.moveLeft));
                        }
                        else if (binding.name == "right")
                        {
                            moveAction.ApplyBindingOverride(i, GetBindingPath(keys.moveRight));
                        }
                    }
                }
            }

            var jumpAction = playerMap.FindAction("Jump");
            if (jumpAction != null)
            {
                jumpAction.RemoveAllBindingOverrides();
                jumpAction.ApplyBindingOverride(GetBindingPath(keys.jump));
            }

            var attackAction = playerMap.FindAction("Attack");
            if (attackAction != null)
            {
                attackAction.RemoveAllBindingOverrides();
                attackAction.ApplyBindingOverride(GetBindingPath(keys.attack));
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
            case KeyCode.LeftArrow: return "<Keyboard>/leftArrow";
            case KeyCode.RightArrow: return "<Keyboard>/rightArrow";
            case KeyCode.UpArrow: return "<Keyboard>/upArrow";
            case KeyCode.DownArrow: return "<Keyboard>/downArrow";
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
            case KeyCode.Space: return "<Keyboard>/space";
            case KeyCode.Return: return "<Keyboard>/enter";
            case KeyCode.Escape: return "<Keyboard>/escape";
            case KeyCode.Tab: return "<Keyboard>/tab";
            case KeyCode.LeftShift: return "<Keyboard>/leftShift";
            case KeyCode.RightShift: return "<Keyboard>/rightShift";
            case KeyCode.LeftControl: return "<Keyboard>/leftCtrl";
            case KeyCode.RightControl: return "<Keyboard>/rightCtrl";
            case KeyCode.LeftAlt: return "<Keyboard>/leftAlt";
            case KeyCode.RightAlt: return "<Keyboard>/rightAlt";
            default:
                string name = keyCode.ToString();
                if (name.StartsWith("Alpha")) name = name.Substring(5);
                return $"<Keyboard>/{name.ToLower()}";
        }
    }

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
            Debug.Log("[MANA] Không đủ mana dùng Ultimate!");
            return;
        }
        ExecuteUltimate();
    }

    public override void OnAttack()
    {
        if (CurrentState == FighterState.Dead) return;
        if (!isGrounded) return;

        // Đang đánh → buffer combo để thực hiện ở hit tiếp theo
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
        comboQueued = false;
        ChangeState(FighterState.Attacking);
        rb.linearVelocity = Vector2.zero;
        lastAttackTime = Time.time;

        comboStep++;
        if (comboStep > 4) comboStep = 1;

        if (anim != null)
        {
            anim.SetTrigger("Attack" + comboStep);
        }
        else
        {
            AnimationEvent_DealDamage();
            Invoke(nameof(AnimationEvent_EndAttack), 0.4f);
        }
    }

    public override void AnimationEvent_EndAttack()
    {
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

    private void ExecuteRanged()
    {
        ChangeState(FighterState.Attacking);
        rb.linearVelocity = Vector2.zero;
        lastAttackTime = Time.time;

        if (rangedSkill != null)
        {
            rangedSkill.StartCast(this);
        }

        if (anim != null)
        {
            anim.SetTrigger("Ranged");
        }
        else
        {
            AnimationEvent_SpawnProjectile();
            Invoke(nameof(AnimationEvent_EndAttack), 0.5f);
        }
    }

    private void ExecuteUltimate()
    {
        SpendMana(50f);
        ChangeState(FighterState.Attacking);
        rb.linearVelocity = Vector2.zero;
        lastAttackTime = Time.time;

        StartCoroutine(ExecuteUltimateWithFlash());
    }

    private IEnumerator ExecuteUltimateWithFlash()
    {
        Sprite flashSprite = LoadUltimateAvatar();
        bool flashDone = false;

        if (flashSprite != null)
        {
            UltimateFlashEffect.Show(this, flashSprite, ultimateFlashSizeFactor, ultimateFlashOffset, new Color(0.2f, 0.9f, 0.4f), () =>
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
            anim.SetTrigger("Ultimate");
        }
        else
        {
            AnimationEvent_SpawnUltimate();
            Invoke(nameof(AnimationEvent_EndAttack), 1.5f);
        }
    }

    private Sprite LoadUltimateAvatar()
    {
        if (ultimateFlashSprite != null) return ultimateFlashSprite;
        Sprite s = Resources.Load<Sprite>("SuperNhon/avatar/avatar_supernhon");
        return s;
    }

    protected override IEnumerator DashRoutine()
    {
        ChangeState(FighterState.Dashing);
        stats.stamina -= 20;
        lastStaminaUseTime = Time.time;

        float dashDir = transform.localScale.x;
        rb.gravityScale = 0f;
        rb.linearVelocity = Vector2.zero;

        if (anim != null)
        {
            anim.SetTrigger("Dash");
        }

        Vector2 startPos = rb.position;
        float dashDist = 4.5f;
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
                float damage = 12f + (comboStep * 6f);
                bool isHeavy = (comboStep == 4);
                damageable.TakeDamage(damage, transform.position.x, isHeavy);
                didHit = true;
            }
        }

        if (didHit)
        {
            GainMana(10f);
        }
    }

    public void AnimationEvent_SpawnProjectile()
    {
        if (rangedSkill != null)
        {
            rangedSkill.SpawnProjectile(this, targetLayer);
        }
    }

    public void AnimationEvent_SpawnUltimate()
    {
        if (ultimateSkill != null)
        {
            ultimateSkill.SpawnUltimateCombo(this, targetLayer);
        }
    }

    public Sprite LoadSuperNhonSprite(string spriteName)
    {
        string filename = spriteName;
        int lastUnderscore = spriteName.LastIndexOf("_");
        if (lastUnderscore > 0)
        {
            string suffix = spriteName.Substring(lastUnderscore + 1);
            int frame;
            if (int.TryParse(suffix, out frame))
            {
                filename = spriteName.Substring(0, lastUnderscore);
            }
        }

        string charDir = "Assets/_Project/Characters/SuperNhon";
        string path = $"{charDir}/{filename}.png";
        
        #if UNITY_EDITOR
        if (!System.IO.File.Exists(path))
        {
            string[] files = System.IO.Directory.GetFiles(charDir, $"{filename}.png", System.IO.SearchOption.AllDirectories);
            if (files.Length > 0)
            {
                path = files[0].Replace("\\", "/");
            }
        }

        Object[] assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path);
        Sprite firstSprite = null;
        foreach (var a in assets)
        {
            if (a is Sprite s)
            {
                if (firstSprite == null) firstSprite = s;
                if (s.name == spriteName)
                {
                    return s;
                }
            }
        }
        if (firstSprite != null)
        {
            return firstSprite;
        }
        #endif
        return null;
    }
}

using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class GrayCharacterController : FighterBase
{
    [Header("--- GRAY SKILLS ---")]
    public GrayRangedSkill rangedSkill;
    public GrayUltimateSkill ultimateSkill;

    [Header("--- DYNAMIC BINDINGS ---")]
    private AnimeFighter.UI.KeybindingsData keys;
    private bool initializedBindings = false;

    protected override void Awake()
    {
        base.Awake();
        
        // Auto-acquire or add skill components if not assigned
        if (rangedSkill == null)
        {
            rangedSkill = GetComponent<GrayRangedSkill>();
            if (rangedSkill == null)
            {
                rangedSkill = gameObject.AddComponent<GrayRangedSkill>();
            }
        }

        if (ultimateSkill == null)
        {
            ultimateSkill = GetComponent<GrayUltimateSkill>();
            if (ultimateSkill == null)
            {
                ultimateSkill = gameObject.AddComponent<GrayUltimateSkill>();
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
                Debug.LogWarning($"[GrayCharacterController] Failed to parse settings.json: {ex.Message}");
            }
        }

        if (settingsData == null)
        {
            settingsData = new AnimeFighter.UI.GameSettingsData();
            settingsData.SetDefaultValues();
        }

        keys = (playerNumber == 1) ? settingsData.player1Keys : settingsData.player2Keys;
        initializedBindings = true;
        
        Debug.Log($"[GrayCharacterController] Bindings loaded for Player {playerNumber}. Left: {keys.moveLeft}, Right: {keys.moveRight}, Block: {keys.defense}, Attack: {keys.attack}, Jump: {keys.jump}, Dash: {keys.dodge}, Ranged: {keys.rangedAttack}, Ultimate: {keys.specialMove}");
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

            // 2. Setup Jump Action
            var jumpAction = playerMap.FindAction("Jump");
            if (jumpAction != null)
            {
                jumpAction.RemoveAllBindingOverrides();
                jumpAction.ApplyBindingOverride(GetBindingPath(keys.jump));
            }

            // 3. Setup Attack Action
            var attackAction = playerMap.FindAction("Attack");
            if (attackAction != null)
            {
                attackAction.RemoveAllBindingOverrides();
                attackAction.ApplyBindingOverride(GetBindingPath(keys.attack));
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
        ExecuteUltimate();
    }

    protected override void ExecuteAttack()
    {
        ChangeState(FighterState.Attacking);
        rb.linearVelocity = Vector2.zero; // Stop moving when attacking
        lastAttackTime = Time.time;

        comboStep++;
        if (comboStep > 4) comboStep = 1;

        if (anim != null)
        {
            anim.SetTrigger("Attack" + comboStep);
        }
        else
        {
            Debug.LogWarning($"[GrayCharacterController] Execute attack combo hit {comboStep}");
            
            // Execute fallback damage immediately since there's no animation event
            AnimationEvent_DealDamage();
            Invoke(nameof(AnimationEvent_EndAttack), 0.4f);
        }
    }

    private void ExecuteRanged()
    {
        ChangeState(FighterState.Attacking);
        rb.linearVelocity = Vector2.zero;
        lastAttackTime = Time.time;

        // Visual casting logic starting frame
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
            Debug.LogWarning("[GrayCharacterController] Execute ranged attack (Fallback)");
            AnimationEvent_SpawnProjectile();
            Invoke(nameof(AnimationEvent_EndAttack), 0.5f);
        }
    }

    private void ExecuteUltimate()
    {
        ChangeState(FighterState.Attacking);
        rb.linearVelocity = Vector2.zero;
        lastAttackTime = Time.time;

        if (anim != null)
        {
            anim.SetTrigger("Ultimate");
        }
        else
        {
            Debug.LogWarning("[GrayCharacterController] Execute ultimate skill (Fallback)");
            AnimationEvent_SpawnUltimate();
            Invoke(nameof(AnimationEvent_EndAttack), 1.5f);
        }
    }

    // --- OVERRIDE DASH ROUTINE (FLASH STEP) ---
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

        // Teleport short distance with obstacle detection
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

    // --- COMBAT ANIMATION EVENTS ---
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
                // Dynamic scaling damage based on combo step: 18, 24, 30, 36
                float damage = 12f + (comboStep * 6f);
                bool isHeavy = (comboStep == 4);
                damageable.TakeDamage(damage, transform.position.x, isHeavy);
            }
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


    public Sprite LoadGraySprite(string spriteName)
    {
        string filename = spriteName;
        if (spriteName.Contains("_"))
        {
            int index = spriteName.IndexOf("_");
            filename = spriteName.Substring(0, index);
        }

        string path = $"Assets/_Project/Characters/Gray/{filename}.png";
        
        #if UNITY_EDITOR
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

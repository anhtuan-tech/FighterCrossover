using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class IchigoController : FighterBase
{
    [Header("--- ICHIGO SETTINGS ---")]
    public GameObject rangedProjectilePrefab;
    public GameObject ultimateEffectPrefab;
    public float ultimateDuration = 1.0f;

    [Header("--- DYNAMIC BINDINGS ---")]
    private AnimeFighter.UI.KeybindingsData keys;
    private bool initializedBindings = false;

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
                Debug.LogWarning($"[IchigoController] Failed to parse settings.json: {ex.Message}");
            }
        }

        if (settingsData == null)
        {
            settingsData = new AnimeFighter.UI.GameSettingsData();
            settingsData.SetDefaultValues();
        }

        keys = (playerNumber == 1) ? settingsData.player1Keys : settingsData.player2Keys;
        initializedBindings = true;
        
        Debug.Log($"[IchigoController] Bindings loaded for Player {playerNumber}. Left: {keys.moveLeft}, Right: {keys.moveRight}, Block: {keys.defense}, Attack: {keys.attack}, Jump: {keys.jump}, Dash: {keys.dodge}, Ranged: {keys.rangedAttack}, Ultimate: {keys.specialMove}");
    }

    private void SetupPlayerInputBindings()
    {
        if (playerInput == null || playerInput.actions == null) return;

        // Disable all actions before changing bindings to avoid InvalidOperationException
        playerInput.actions.Disable();

        var playerMap = playerInput.actions.FindActionMap("Player");
        if (playerMap != null)
        {
            // 1. Setup Move Action (Composite)
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

        // Re-enable all actions
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
            // Fallback if no Animator
            Debug.LogWarning($"[IchigoController] Execute attack combo hit {comboStep}");
            Invoke(nameof(AnimationEvent_EndAttack), 0.4f);
        }
    }

    private void ExecuteRanged()
    {
        ChangeState(FighterState.Attacking);
        rb.linearVelocity = Vector2.zero;
        lastAttackTime = Time.time;

        if (anim != null)
        {
            anim.SetTrigger("Ranged");
        }
        else
        {
            Debug.LogWarning("[IchigoController] Execute ranged attack");
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
            Debug.LogWarning("[IchigoController] Execute ultimate skill");
            AnimationEvent_SpawnUltimate();
            Invoke(nameof(AnimationEvent_EndAttack), 1.0f);
        }
    }

    // --- OVERRIDE DASH ROUTINE (FLASH STEP) ---
    protected override IEnumerator DashRoutine()
    {
        ChangeState(FighterState.Dashing);
        stats.stamina -= 20;

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

        // Raycast to check for walls/obstacles, ignoring self-collision
        RaycastHit2D[] hits = Physics2D.RaycastAll(startPos, new Vector2(dashDir, 0f), dashDist, groundLayer);
        foreach (var h in hits)
        {
            if (h.collider != null && h.collider.gameObject != gameObject)
            {
                // Teleport to slightly before the hit point
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
                // Dynamic scaling damage based on combo step
                float damage = 12f + (comboStep * 6f); // 18, 24, 30, 36
                bool isHeavy = (comboStep == 4);
                damageable.TakeDamage(damage, transform.position.x, isHeavy);
            }
        }
    }

    public void AnimationEvent_SpawnProjectile()
    {
        float dir = transform.localScale.x;
        Vector2 spawnPos = (attackHitbox != null) ? (Vector2)attackHitbox.position : (Vector2)transform.position + new Vector2(dir * 1.0f, 0.2f);

        if (rangedProjectilePrefab != null)
        {
            GameObject projObj = Instantiate(rangedProjectilePrefab, spawnPos, Quaternion.identity);
            RangedProjectile proj = projObj.GetComponent<RangedProjectile>();
            if (proj != null)
            {
                proj.Setup(new Vector2(dir, 0f), gameObject, targetLayer);
            }
        }
        else
        {
            // Dynamic fallback projectile if prefab is unassigned
            Debug.LogWarning("[IchigoController] Ranged Projectile Prefab not assigned! Creating a dynamic projectile.");
            GameObject projObj = new GameObject("DynamicProjectile");
            projObj.transform.position = spawnPos;
            
            var col = projObj.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(0.8f, 0.4f);

            var rbProj = projObj.AddComponent<Rigidbody2D>();
            rbProj.bodyType = RigidbodyType2D.Kinematic;

            // Inner layer
            GameObject inner = new GameObject("Inner");
            inner.transform.SetParent(projObj.transform);
            inner.transform.localPosition = Vector3.zero;
            var srInner = inner.AddComponent<SpriteRenderer>();
            srInner.color = new Color(0f, 0.8f, 1f, 0.9f); // Cyan energy
            srInner.sprite = LoadIchigoSprite("image-removebg-preview_1");

            // Outer layer
            GameObject outer = new GameObject("Outer");
            outer.transform.SetParent(projObj.transform);
            outer.transform.localPosition = Vector3.zero;
            var srOuter = outer.AddComponent<SpriteRenderer>();
            srOuter.color = new Color(0f, 0.4f, 1f, 0.7f); // Blue energy
            srOuter.sprite = LoadIchigoSprite("image-removebg-preview_1");
            outer.transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);

            RangedProjectile proj = projObj.AddComponent<RangedProjectile>();
            proj.Setup(new Vector2(dir, 0f), gameObject, targetLayer);
        }
    }

    public void AnimationEvent_SpawnUltimate()
    {
        Vector2 spawnPos = transform.position;

        if (ultimateEffectPrefab != null)
        {
            GameObject ultObj = Instantiate(ultimateEffectPrefab, spawnPos, Quaternion.identity);
            UltimateEffect effect = ultObj.GetComponent<UltimateEffect>();
            if (effect != null)
            {
                effect.Setup(gameObject, targetLayer, ultimateDuration);
            }
        }
        else
        {
            // Dynamic fallback ultimate effect if prefab is unassigned
            Debug.LogWarning("[IchigoController] Ultimate Effect Prefab not assigned! Creating a dynamic effect.");
            GameObject ultObj = new GameObject("DynamicUltimate");
            ultObj.transform.position = spawnPos;

            var sr = ultObj.AddComponent<SpriteRenderer>();
            sr.sprite = LoadIchigoSprite("image-removebg-preview (7)_0");
            sr.color = new Color(0.9f, 0.1f, 0.1f, 0.8f); // Red energy blade aura

            UltimateEffect effect = ultObj.AddComponent<UltimateEffect>();
            effect.Setup(gameObject, targetLayer, ultimateDuration);
        }
    }

    private Sprite LoadIchigoSprite(string spriteName)
    {
        // Utility to load sprite dynamically in case prefab isn't fully configured
        string sheet = "bankai";
        if (spriteName.StartsWith("image-removebg-preview"))
        {
            if (spriteName.Contains("("))
            {
                int start = spriteName.IndexOf("(") + 1;
                int len = spriteName.IndexOf(")") - start;
                sheet = "image-removebg-preview (" + spriteName.Substring(start, len) + ")";
            }
            else
            {
                sheet = "image-removebg-preview";
            }
        }

        string path = $"Assets/_Project/Characters/Ichigo/ichigo/{sheet}.png";
        Object[] assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path);
        foreach (var a in assets)
        {
            if (a is Sprite s && s.name == spriteName)
            {
                return s;
            }
        }
        return null;
    }

    // Compatibility helpers replaced by dynamic PlayerInput bindings
}

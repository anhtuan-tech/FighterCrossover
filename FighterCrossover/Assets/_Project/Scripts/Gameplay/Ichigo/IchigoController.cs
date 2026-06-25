using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class IchigoController : FighterBase
{
    [Header("--- ICHIGO SETTINGS ---")]
    public int playerNumber = 1;
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

    protected override void Update()
    {
        if (CurrentState == FighterState.Dead) return;

        // Grounded check and FSM state logics
        CheckGrounded();
        HandleStateLogic();
        HandleStaminaRegen();
        UpdateAnimations();

        // Read direct keyboard inputs using dynamic bindings
        if (initializedBindings)
        {
            bool isDefenseHeld = GetKey(keys.defense);

            if (isDefenseHeld)
            {
                // Khi giữ defense: chỉ nhảy xuống platform, không bao giờ nhảy lên
                if (GetKeyDown(keys.jump) || GetKeyDown(keys.dodge))
                {
                    TryDropDown();
                }
            }
            else
            {
                if (GetKeyDown(keys.jump))
                {
                    ExecuteJump();
                }

                if (GetKeyDown(keys.dodge))
                {
                    TriggerDash();
                }
            }

            if (GetKeyDown(keys.attack))
            {
                TriggerAttack();
            }

            if (GetKeyDown(keys.rangedAttack))
            {
                TriggerRanged();
            }

            if (GetKeyDown(keys.specialMove))
            {
                TriggerUltimate();
            }
        }
    }

    protected override void HandleStateLogic()
    {
        // Reset combo if attack delay exceeds threshold
        if (comboStep > 0 && Time.time - lastAttackTime > comboResetTime && CurrentState != FighterState.Attacking)
        {
            comboStep = 0;
        }

        // Horizontal movement control
        float horizontal = 0f;
        if (initializedBindings)
        {
            if (GetKey(keys.moveLeft)) horizontal -= 1f;
            if (GetKey(keys.moveRight)) horizontal += 1f;
        }
        else
        {
            // Fallback to PlayerInput if bindings not yet initialized
            if (playerInput != null && playerInput.actions != null)
            {
                var moveAct = playerInput.actions.FindAction("Move");
                if (moveAct != null) horizontal = moveAct.ReadValue<Vector2>().x;
            }
        }
        moveInput = new Vector2(horizontal, 0f);

        if (CanAct())
        {
            bool isBlockingInput = false;
            if (initializedBindings)
            {
                isBlockingInput = GetKey(keys.defense);
            }
            else
            {
                if (playerInput != null && playerInput.actions != null)
                {
                    var blockAct = playerInput.actions.FindAction("Block");
                    if (blockAct != null) isBlockingInput = blockAct.IsPressed();
                }
            }

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

    // --- COMBAT EXECUTION ---
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

    // --- NEW INPUT SYSTEM COMPATIBILITY HELPERS ---
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
            case KeyCode.B: return Key.B;
            case KeyCode.C: return Key.C;
            case KeyCode.D: return Key.D;
            case KeyCode.E: return Key.E;
            case KeyCode.F: return Key.F;
            case KeyCode.G: return Key.G;
            case KeyCode.H: return Key.H;
            case KeyCode.I: return Key.I;
            case KeyCode.J: return Key.J;
            case KeyCode.K: return Key.K;
            case KeyCode.L: return Key.L;
            case KeyCode.M: return Key.M;
            case KeyCode.N: return Key.N;
            case KeyCode.O: return Key.O;
            case KeyCode.P: return Key.P;
            case KeyCode.Q: return Key.Q;
            case KeyCode.R: return Key.R;
            case KeyCode.S: return Key.S;
            case KeyCode.T: return Key.T;
            case KeyCode.U: return Key.U;
            case KeyCode.V: return Key.V;
            case KeyCode.W: return Key.W;
            case KeyCode.X: return Key.X;
            case KeyCode.Y: return Key.Y;
            case KeyCode.Z: return Key.Z;

            case KeyCode.Alpha0: return Key.Digit0;
            case KeyCode.Alpha1: return Key.Digit1;
            case KeyCode.Alpha2: return Key.Digit2;
            case KeyCode.Alpha3: return Key.Digit3;
            case KeyCode.Alpha4: return Key.Digit4;
            case KeyCode.Alpha5: return Key.Digit5;
            case KeyCode.Alpha6: return Key.Digit6;
            case KeyCode.Alpha7: return Key.Digit7;
            case KeyCode.Alpha8: return Key.Digit8;
            case KeyCode.Alpha9: return Key.Digit9;

            case KeyCode.LeftArrow: return Key.LeftArrow;
            case KeyCode.RightArrow: return Key.RightArrow;
            case KeyCode.UpArrow: return Key.UpArrow;
            case KeyCode.DownArrow: return Key.DownArrow;

            case KeyCode.Keypad0: return Key.Numpad0;
            case KeyCode.Keypad1: return Key.Numpad1;
            case KeyCode.Keypad2: return Key.Numpad2;
            case KeyCode.Keypad3: return Key.Numpad3;
            case KeyCode.Keypad4: return Key.Numpad4;
            case KeyCode.Keypad5: return Key.Numpad5;
            case KeyCode.Keypad6: return Key.Numpad6;
            case KeyCode.Keypad7: return Key.Numpad7;
            case KeyCode.Keypad8: return Key.Numpad8;
            case KeyCode.Keypad9: return Key.Numpad9;

            case KeyCode.Space: return Key.Space;
            case KeyCode.Return: return Key.Enter;
            case KeyCode.Escape: return Key.Escape;
            case KeyCode.Tab: return Key.Tab;
            case KeyCode.LeftShift: return Key.LeftShift;
            case KeyCode.RightShift: return Key.RightShift;
            case KeyCode.LeftControl: return Key.LeftCtrl;
            case KeyCode.RightControl: return Key.RightCtrl;
            case KeyCode.LeftAlt: return Key.LeftAlt;
            case KeyCode.RightAlt: return Key.RightAlt;

            default: return Key.None;
        }
    }
}

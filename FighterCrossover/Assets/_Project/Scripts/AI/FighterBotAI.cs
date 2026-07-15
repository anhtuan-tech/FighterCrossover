using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;

public class FighterBotAI : MonoBehaviour
{
    private FighterBase fighter;
    private FighterBase target;
    private int difficulty = 1; // 0 = Easy, 1 = Medium, 2 = Hard
    public int Difficulty => difficulty;

    // Easy mode state
    private int normalAttackCount = 0;
    private bool isEasyDelaying = false;
    private int easyRangedAttackCount = 0;
    private float lastEasyRangedCooldownTime = -99f;

    // Medium & Hard mode state
    private bool nextIsUltimate = true;

    // Jump & double jump logic
    private float lastJumpTime = 0f;
    private bool hasDoubleJumped = false;

    // Hard mode combo state
    private bool isExecutingHardCombo = false;

    // Action cooldown to prevent frame-spamming
    private float lastActionTime = 0f;
    private const float ACTION_COOLDOWN = 0.3f;

    public void Initialize(FighterBase ownerFighter)
    {
        fighter = ownerFighter;

        // Disable PlayerInput component and reference
        DisablePlayerInput();

        // Load difficulty settings
        LoadDifficulty();

        if (difficulty == 2)
        {
            fighter.stats.maxHp *= 2f;
            fighter.stats.currentHp = fighter.stats.maxHp;
            fighter.staminaRegenRate *= 2f;
        }

        Debug.Log($"[FighterBotAI] Initialized on {gameObject.name} with difficulty: {difficulty}");
    }

    private void DisablePlayerInput()
    {
        if (fighter == null) return;

        PlayerInput inputComponent = fighter.GetComponent<PlayerInput>();
        if (inputComponent != null)
        {
            inputComponent.enabled = false;
            if (inputComponent.actions != null)
            {
                inputComponent.actions.Disable();
            }
        }

        // Set the protected playerInput field in FighterBase to null via reflection
        FieldInfo playerInputField = typeof(FighterBase).GetField("playerInput", BindingFlags.Instance | BindingFlags.NonPublic);
        if (playerInputField != null)
        {
            playerInputField.SetValue(fighter, null);
        }
    }

    private void LoadDifficulty()
    {
        string saveFilePath = System.IO.Path.Combine(Application.persistentDataPath, "settings.json");
        if (System.IO.File.Exists(saveFilePath))
        {
            try
            {
                string json = System.IO.File.ReadAllText(saveFilePath);
                var settingsData = JsonUtility.FromJson<AnimeFighter.UI.GameSettingsData>(json);
                if (settingsData != null)
                {
                    difficulty = settingsData.botDifficulty;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[FighterBotAI] Failed to parse settings.json: {ex.Message}");
            }
        }
    }

    private void Update()
    {
        if (fighter == null || fighter.CurrentState == FighterState.Dead) return;

        // Automatically find target (Player 1) if not assigned
        if (target == null)
        {
            FindTargetPlayer();
            if (target == null) return;
        }

        if (target.CurrentState == FighterState.Dead)
        {
            SetMoveInput(Vector2.zero);
            return;
        }

        // 0. Double jump logic for all modes if target is higher
        HandleVerticalMovement();

        // 1. Apply AI logic based on difficulty
        if (difficulty == 0) // Easy
        {
            ExecuteEasyAI();
        }
        else if (difficulty == 1) // Medium
        {
            ExecuteMediumAI();
        }
        else // Hard
        {
            ExecuteHardAI();
        }
    }

    private void FindTargetPlayer()
    {
        FighterBase[] fighters = FindObjectsOfType<FighterBase>();
        foreach (var f in fighters)
        {
            if (f.playerNumber == 1)
            {
                target = f;
                break;
            }
        }
    }

    private void SetMoveInput(Vector2 val)
    {
        FieldInfo moveInputField = typeof(FighterBase).GetField("moveInput", BindingFlags.Instance | BindingFlags.NonPublic);
        if (moveInputField != null)
        {
            moveInputField.SetValue(fighter, val);
        }
    }

    private bool GetIsGrounded()
    {
        FieldInfo groundedField = typeof(FighterBase).GetField("isGrounded", BindingFlags.Instance | BindingFlags.NonPublic);
        if (groundedField != null)
        {
            return (bool)groundedField.GetValue(fighter);
        }
        return false;
    }

    private LayerMask GetGroundLayer()
    {
        FieldInfo groundLayerField = typeof(FighterBase).GetField("groundLayer", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (groundLayerField != null)
        {
            return (LayerMask)groundLayerField.GetValue(fighter);
        }
        return LayerMask.GetMask("Default");
    }

    private bool CanBotAct()
    {
        if (fighter == null) return false;
        MethodInfo method = typeof(FighterBase).GetMethod("CanAct", BindingFlags.Instance | BindingFlags.NonPublic);
        if (method != null)
        {
            return (bool)method.Invoke(fighter, null);
        }
        return false;
    }

    private void SetCurrentState(FighterState state)
    {
        PropertyInfo prop = typeof(FighterBase).GetProperty("CurrentState", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (prop != null)
        {
            prop.SetValue(fighter, state);
        }
    }

    private void HandleVerticalMovement()
    {
        float targetY = target.transform.position.y;
        float selfY = transform.position.y;
        bool isGrounded = GetIsGrounded();

        // If target is significantly higher, trigger jump/double jump
        if (targetY - selfY > 1.5f)
        {
            if (isGrounded && Time.time - lastJumpTime > 0.5f)
            {
                fighter.OnJump();
                lastJumpTime = Time.time;
                hasDoubleJumped = false;
            }
            else if (!isGrounded && !hasDoubleJumped && Time.time - lastJumpTime > 0.25f)
            {
                fighter.OnJump(); // Perform double jump
                hasDoubleJumped = true;
                lastJumpTime = Time.time;
            }
        }
    }

    private Vector2 FindBestPlatform(float searchRadius = 15f)
    {
        LayerMask groundLayer = GetGroundLayer();
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, searchRadius, groundLayer);

        Collider2D bestPlatform = null;
        float minHorizontalDist = float.MaxValue;

        float selfY = transform.position.y;
        float targetY = target.transform.position.y;

        foreach (var col in colliders)
        {
            if (col.gameObject == gameObject || col.transform.IsChildOf(transform)) continue;

            float colY = col.bounds.max.y;
            // The platform must be above the bot and below or close to the target
            if (colY > selfY + 0.5f && colY <= targetY + 1.0f)
            {
                float horizontalDist = Mathf.Abs(col.bounds.center.x - target.transform.position.x);
                if (horizontalDist < minHorizontalDist)
                {
                    minHorizontalDist = horizontalDist;
                    bestPlatform = col;
                }
            }
        }

        if (bestPlatform != null)
        {
            return bestPlatform.bounds.center;
        }
        return Vector2.zero;
    }

    private bool DetectIncomingProjectile()
    {
        Rigidbody2D[] rbs = FindObjectsOfType<Rigidbody2D>();
        float selfX = transform.position.x;
        float selfY = transform.position.y;

        foreach (var rb in rbs)
        {
            if (rb == null || rb.gameObject == gameObject || rb.transform.IsChildOf(transform)) continue;

            string nameLower = rb.gameObject.name.ToLower();
            if (nameLower.Contains("projectile") || nameLower.Contains("fireball") || 
                nameLower.Contains("shuriken") || nameLower.Contains("bullet") || 
                nameLower.Contains("slash") || nameLower.Contains("ball") ||
                nameLower.Contains("ult") || nameLower.Contains("skill"))
            {
                float rbX = rb.transform.position.x;
                float rbY = rb.transform.position.y;

                float distX = Mathf.Abs(rbX - selfX);
                float distY = Mathf.Abs(rbY - selfY);

                // Projectile is close horizontally and vertically
                if (distX < 4.5f && distY < 2.0f)
                {
                    // Check if it's moving towards us
                    float velocityX = rb.linearVelocity.x;
                    bool movingTowardsUs = (rbX < selfX && velocityX > 0.5f) || (rbX > selfX && velocityX < -0.5f);
                    if (movingTowardsUs)
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }

    private void ExecuteEasyAI()
    {
        if (isEasyDelaying)
        {
            SetMoveInput(Vector2.zero);
            return;
        }

        float directionX = target.transform.position.x - transform.position.x;
        float distance = Mathf.Abs(directionX);

        // 1. Ranged attack (u) - reduced range (4.0f to 7.0f) & 2s cooldown after 2 uses
        bool isRangedOnCooldown = Time.time - lastEasyRangedCooldownTime < 2.0f;
        if (!isRangedOnCooldown && fighter.HasMana(30f) && distance >= 4.0f && distance <= 7.0f && Time.time - lastActionTime > ACTION_COOLDOWN)
        {
            if (CanBotAct())
            {
                TriggerRangedAttack();
                lastActionTime = Time.time;
                easyRangedAttackCount++;

                if (easyRangedAttackCount >= 2)
                {
                    lastEasyRangedCooldownTime = Time.time;
                    easyRangedAttackCount = 0;
                }
                return;
            }
        }

        // 2. Normal attacks (j) - only when close (distance <= 1.0f)
        if (distance <= 1.0f)
        {
            SetMoveInput(Vector2.zero);
            if (CanBotAct() && Time.time - lastActionTime > ACTION_COOLDOWN)
            {
                fighter.OnAttack();
                normalAttackCount++;
                lastActionTime = Time.time;

                if (normalAttackCount >= 8)
                {
                    StartCoroutine(EasyDelayRoutine());
                }
            }
        }
        else
        {
            SetMoveInput(new Vector2(Mathf.Sign(directionX), 0f));
        }
    }

    private void ExecuteMediumAI()
    {
        float directionX = target.transform.position.x - transform.position.x;
        float distance = Mathf.Abs(directionX);

        // Dash if too far
        if (distance >= 5f && CanBotAct() && Time.time - lastActionTime > ACTION_COOLDOWN)
        {
            fighter.OnDash();
            lastActionTime = Time.time;
            return;
        }

        // Skill logic: alternate ultimate (i) and ranged (u) if mana >= 50%
        if (fighter.HasMana(50f) && distance <= 10f && Time.time - lastActionTime > ACTION_COOLDOWN)
        {
            if (CanBotAct())
            {
                if (nextIsUltimate)
                {
                    TriggerSpecialAttack();
                }
                else
                {
                    TriggerRangedAttack();
                }
                nextIsUltimate = !nextIsUltimate;
                lastActionTime = Time.time;
                return;
            }
        }

        // Normal attack and movement
        if (distance <= 1.0f)
        {
            SetMoveInput(Vector2.zero);
            if (CanBotAct() && Time.time - lastActionTime > ACTION_COOLDOWN)
            {
                fighter.OnAttack();
                lastActionTime = Time.time;
            }
        }
        else
        {
            SetMoveInput(new Vector2(Mathf.Sign(directionX), 0f));
        }
    }

    private void ExecuteHardAI()
    {
        // 0. Auto-block incoming projectiles (highest priority)
        if (DetectIncomingProjectile())
        {
            SetMoveInput(Vector2.zero);
            SetCurrentState(FighterState.Blocking);
            return;
        }

        float directionX = target.transform.position.x - transform.position.x;
        float distance = Mathf.Abs(directionX);
        float targetY = target.transform.position.y;
        float selfY = transform.position.y;

        // If target is too high, Hard mode will search for a platform to step on
        if (targetY - selfY > 1.5f)
        {
            Vector2 platformPos = FindBestPlatform();
            if (platformPos != Vector2.zero)
            {
                // Head towards the platform X position instead
                directionX = platformPos.x - transform.position.x;
                distance = Mathf.Abs(directionX);
            }
        }

        // 1. React when hit / attacked / stunned: walk towards player, jump for Y velocity, and block to approach safely
        if (fighter.CurrentState == FighterState.Stunned || (target.CurrentState == FighterState.Attacking && distance > 3f))
        {
            // Move towards player
            SetMoveInput(new Vector2(Mathf.Sign(directionX), 0f));
            
            // Jump if grounded to gain Y velocity
            if (GetIsGrounded() && Time.time - lastJumpTime > 0.4f)
            {
                fighter.OnJump();
                lastJumpTime = Time.time;
            }

            // Force block state
            SetCurrentState(FighterState.Blocking);
            return;
        }

        // 2. Dash distance trigger (4 to 6 units) -> Dash to target and start lock combo
        if (distance >= 4f && distance <= 6f && !isExecutingHardCombo && CanBotAct() && Time.time - lastActionTime > ACTION_COOLDOWN)
        {
            fighter.OnDash();
            lastActionTime = Time.time;
            StartCoroutine(HardComboRoutine());
            return;
        }

        if (isExecutingHardCombo)
        {
            if (distance > 1.0f)
            {
                SetMoveInput(new Vector2(Mathf.Sign(directionX), 0f));
            }
            else
            {
                SetMoveInput(Vector2.zero);
            }
            return;
        }

        // Default chase behavior
        if (distance >= 5f && CanBotAct() && Time.time - lastActionTime > ACTION_COOLDOWN)
        {
            fighter.OnDash();
            lastActionTime = Time.time;
            return;
        }

        // Skill alternate
        if (fighter.HasMana(50f) && distance <= 10f && Time.time - lastActionTime > ACTION_COOLDOWN)
        {
            if (CanBotAct())
            {
                if (nextIsUltimate)
                {
                    TriggerSpecialAttack();
                }
                else
                {
                    TriggerRangedAttack();
                }
                nextIsUltimate = !nextIsUltimate;
                lastActionTime = Time.time;
                return;
            }
        }

        // Normal attacks
        if (distance <= 1.0f)
        {
            SetMoveInput(Vector2.zero);
            if (CanBotAct() && Time.time - lastActionTime > ACTION_COOLDOWN)
            {
                fighter.OnAttack();
                lastActionTime = Time.time;
            }
        }
        else
        {
            SetMoveInput(new Vector2(Mathf.Sign(directionX), 0f));
        }
    }

    private IEnumerator HardComboRoutine()
    {
        isExecutingHardCombo = true;
        yield return new WaitForSeconds(0.15f); // Faster recovery after dash

        // Continuous combo: hit 6 times with very short delays to stun-lock the player completely
        for (int i = 0; i < 6; i++)
        {
            if (fighter.CurrentState == FighterState.Dead || target.CurrentState == FighterState.Dead) break;

            // Follow player slightly if they move away during combo
            float directionX = target.transform.position.x - transform.position.x;
            float distance = Mathf.Abs(directionX);
            if (distance > 1.0f)
            {
                SetMoveInput(new Vector2(Mathf.Sign(directionX), 0f));
            }
            else
            {
                SetMoveInput(Vector2.zero);
            }

            if (CanBotAct())
            {
                fighter.OnAttack();
            }
            yield return new WaitForSeconds(0.18f); // Very fast combo to keep player in stun state
        }

        // Non-stop skill execution
        if (CanBotAct())
        {
            if (fighter.HasMana(50f))
            {
                TriggerSpecialAttack();
            }
            else
            {
                TriggerRangedAttack();
            }
        }

        yield return new WaitForSeconds(0.3f);
        isExecutingHardCombo = false;
    }

    private IEnumerator EasyDelayRoutine()
    {
        isEasyDelaying = true;
        SetMoveInput(Vector2.zero);
        yield return new WaitForSeconds(2.0f);
        normalAttackCount = 0;
        isEasyDelaying = false;
    }

    private void TriggerRangedAttack()
    {
        MethodInfo method = fighter.GetType().GetMethod("OnRanged");
        if (method != null)
        {
            method.Invoke(fighter, null);
            Debug.Log("[FighterBotAI] Triggered Ranged Attack (u)");
        }
    }

    private void TriggerSpecialAttack()
    {
        MethodInfo method = fighter.GetType().GetMethod("OnSpecial");
        if (method != null)
        {
            method.Invoke(fighter, null);
            Debug.Log("[FighterBotAI] Triggered Special/Ultimate Attack (i)");
        }
    }
}

using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public enum FighterState
{
    Idle,
    Moving,
    Jumping,
    Dashing,
    Attacking,
    Blocking,
    Stunned, // Bị choáng/trúng đòn
    Dead
}

[System.Serializable]
public struct FighterStats
{
    public float maxHp;
    public float currentHp;
    public float maxMana;
    public float currentMana;
    public float maxStamina;
    public float stamina;

}

[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(Animator))]
[RequireComponent(typeof(PlayerInput))]
public class FighterBase : MonoBehaviour, IDamageable
{
    [Header("--- PLAYER CONFIG ---")]
    public int playerNumber = 1;

    [Header("--- STATS ---")]
    public FighterStats stats = new FighterStats
    {
        maxHp = 500f,
        currentHp = 500f,
        maxMana = 100f,
        currentMana = 50f,
        maxStamina = 100f,
        stamina = 100f
    };
    [Header("--- STAMINA REGEN ---")]
    public float staminaRegenRate = 20f; // Lượng stamina hồi mỗi giây
    public float staminaRegenDelay = 1f; // Thời gian đợi (giây) sau khi Dash mới bắt đầu hồi

    protected float lastStaminaUseTime; // Lưu lại mốc thời gian cuối cùng tiêu hao stamina

    [Header("--- MOVEMENT SETTINGS ---")]
    public float moveSpeed = 5f;
    public float jumpForce = 10f;
    public float dashForce = 15f;
    public float dashDuration = 0.10f;
    public int maxJumps = 2;
    public LayerMask groundLayer;

    [Header("--- COMBAT SETTINGS ---")]
    public float blockDamageReduction = 0.7f; // Giảm 70% sát thương khi đỡ
    public Vector2 knockbackForce = new Vector2(4f, 6f);
    public float comboResetTime = 0.8f;
    public Transform attackHitbox;
    public float attackRadius = 0.3f;
    public LayerMask targetLayer;

    [Header("--- COMMON COMBAT SFX ---")]
    public AudioClip attackSound;
    public AudioClip rangedSound;
    public AudioClip dashSound;

    [HideInInspector] public KeyCode supportKey = KeyCode.None;
    [HideInInspector] public string supportPrefabUrl = "";
    protected float lastSupportTime;
    private const float SUPPORT_COOLDOWN = 0f;

    // --- FSM STATE ---
    public FighterState CurrentState { get; protected set; } = FighterState.Idle;

    // --- VARIABLES ---
    protected Rigidbody2D rb;
    protected Animator anim;
    protected PlayerInput playerInput;
    protected BoxCollider2D playerCollider;
    protected Collider2D groundCollider;

    protected Vector2 moveInput;
    protected bool isGrounded;
    protected bool isDroppingDown = false;
    protected int currentJumps;
    protected int comboStep = 0;
    protected int hitReceivedCount = 0;
    protected float lastAttackTime;

    #region INITIALIZATION
    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        playerInput = GetComponent<PlayerInput>();
        playerCollider = GetComponent<BoxCollider2D>();

        rb.gravityScale = 3.5f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous; // Chống lọt hố khi dash nhanh

#if UNITY_EDITOR
        if (attackSound == null)
            attackSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/SFX/Sound_Attack.wav");
        if (rangedSound == null)
            rangedSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/SFX/Sound_NemXa.wav");
        if (dashSound == null)
            dashSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/SFX/dash.wav");
#endif

        // Cảnh báo nếu layer chưa được gán (thường xảy ra sau khi bấm Reset trong Inspector)
        if (groundLayer.value == 0)
            Debug.LogWarning($"[{gameObject.name}] groundLayer chưa được gán! Vui lòng set lại trong Inspector.", this);
        if (targetLayer.value == 0)
            Debug.LogWarning($"[{gameObject.name}] targetLayer chưa được gán! Vui lòng set lại trong Inspector.", this);
    }

    public virtual void InitializePlayer(int num)
    {
        playerNumber = num;
    }

    // Unity gọi hàm này khi bấm nút Reset trong Inspector
    // Tự động điền lại groundLayer và targetLayer về Default để tránh mất cấu hình
    protected virtual void Reset()
    {
        groundLayer = LayerMask.GetMask("Default");
        targetLayer = LayerMask.GetMask("Default");
    }
    #endregion

    #region GAME LOOPS
    protected virtual void Start()
    {
        // Load selected support prefab URL
        supportPrefabUrl = (playerNumber == 1) ? SelectionData.supportPrefabUrl1 : SelectionData.supportPrefabUrl2;
        
        // Auto-assign default keys if not overridden by child controllers
        if (supportKey == KeyCode.None)
        {
            supportKey = (playerNumber == 1) ? KeyCode.O : KeyCode.Keypad6;
        }
    }

    protected virtual void Update()
    {
        if (CurrentState == FighterState.Dead) return;

        if (SelectionData.CurrentGameMode == GameMode.Training)
        {
            stats.currentHp = stats.maxHp;
            stats.stamina = stats.maxStamina;
            stats.currentMana = stats.maxMana;
        }

        CheckGrounded();
        HandleStateLogic();
        HandleStaminaRegen();
        UpdateAnimations();
    }

    protected virtual void OnDestroy()
    {
        if (playerInput != null && playerInput.actions != null)
        {
            foreach (var action in playerInput.actions)
            {
                ClearActionCallbacks(action);
            }
        }
    }

    private void ClearActionCallbacks(UnityEngine.InputSystem.InputAction action)
    {
        try
        {
            var type = typeof(UnityEngine.InputSystem.InputAction);
            var bindingFlags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            
            string[] callbackFields = { "m_OnStarted", "m_OnPerformed", "m_OnCanceled" };
            foreach (var fieldName in callbackFields)
            {
                var field = type.GetField(fieldName, bindingFlags);
                if (field != null)
                {
                    var fieldType = field.FieldType;
                    var emptyValue = System.Activator.CreateInstance(fieldType);
                    field.SetValue(action, emptyValue);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[FighterBase] Failed to clear callbacks for action {action.name}: {e.Message}");
        }
    }

    protected virtual void FixedUpdate()
    {
        if (!IsGameplayActive())
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        if (CurrentState == FighterState.Moving || CurrentState == FighterState.Idle || CurrentState == FighterState.Jumping)
        {
            ApplyMovementPhysics();
        }
    }
    #endregion

    #region FSM & LOGIC
    private bool IsGameplayActive()
    {
        // Nếu không có MatchManager trong scene (cảnh test độc lập), mặc định cho phép di chuyển/đánh tự do
        if (MatchManager.Instance == null) return true;
        return MatchManager.IsMatchStarted && !MatchManager.IsMatchEnded;
    }

    protected virtual void HandleStateLogic()
    {
        // Kiểm tra xem trận đấu đã bắt đầu chưa hoặc đã kết thúc chưa
        if (!IsGameplayActive())
        {
            moveInput = Vector2.zero;
            if (CurrentState != FighterState.Dead)
            {
                ChangeState(isGrounded ? FighterState.Idle : FighterState.Jumping);
            }
            return;
        }

        // Reset combo nếu để quá lâu
        if (comboStep > 0 && Time.time - lastAttackTime > comboResetTime && CurrentState != FighterState.Attacking)
        {
            comboStep = 0;
        }

        if (playerInput != null && playerInput.actions != null)
        {
            var moveAct = playerInput.actions.FindAction("Move");
            if (moveAct != null) moveInput = moveAct.ReadValue<Vector2>();
        }

        // Chỉ cho phép đổi trạng thái nếu không bị khóa bởi đòn đánh/choáng/lướt
        if (CanAct())
        {
            bool isBlockingInput = false;
            if (playerInput != null && playerInput.actions != null)
            {
                var blockAct = playerInput.actions.FindAction("Block");
                if (blockAct != null) isBlockingInput = blockAct.IsPressed();
            }

            // Block chỉ kích hoạt khi đang trên mặt đất VÀ không đang rơi xuống
            // (tránh trường hợp giữ Block trong lúc nhảy → nhân vật chạm đất bị lock ngay)
            if (isBlockingInput && isGrounded && rb.linearVelocity.y >= -0.5f)
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

    protected bool CanAct()
    {
        if (!IsGameplayActive())
        {
            return false;
        }

        return CurrentState != FighterState.Attacking &&
            CurrentState != FighterState.Dashing &&
            CurrentState != FighterState.Stunned &&
            CurrentState != FighterState.Dead;
    }

    protected virtual void ChangeState(FighterState newState)
    {
        if (CurrentState == newState) return;
        CurrentState = newState;
    }
    protected virtual void HandleStaminaRegen()
    {
        // Kiểm tra nếu stamina chưa đầy và đã qua thời gian chờ (delay) kể từ lần lướt cuối
        if (stats.stamina < stats.maxStamina && Time.time - lastStaminaUseTime >= staminaRegenDelay)
        {
            stats.stamina += staminaRegenRate * Time.deltaTime;

            // Tránh việc hồi vượt mức tối đa
            if (stats.stamina > stats.maxStamina)
            {
                stats.stamina = stats.maxStamina;
            }
        }
    }

    // ===================== MANA SYSTEM =====================

    /// <summary>Hồi mana theo % thanh mana tối đa.</summary>
    public void GainMana(float percent)
    {
        float actualPercent = percent;
        FighterBotAI botAI = GetComponent<FighterBotAI>();
        if (botAI != null && botAI.Difficulty == 2)
        {
            actualPercent *= 2f;
        }
        stats.currentMana = Mathf.Min(stats.currentMana + stats.maxMana * actualPercent / 100f, stats.maxMana);
    }

    /// <summary>Tiêu mana theo % thanh mana tối đa. Trả false nếu không đủ mana.</summary>
    protected bool SpendMana(float percent)
    {
        if (SelectionData.CurrentGameMode == GameMode.Training)
        {
            return true;
        }
        float cost = stats.maxMana * percent / 100f;
        if (stats.currentMana < cost - 0.01f)
        {
            Debug.Log($"[MANA] Không đủ mana! Cần {percent}%, hiện có {stats.currentMana / stats.maxMana * 100f:F0}%");
            return false;
        }
        stats.currentMana = Mathf.Max(0f, stats.currentMana - cost);
        return true;
    }

    /// <summary>Kiểm tra có đủ mana không (không tiêu).</summary>
    public bool HasMana(float percent)
    {
        if (SelectionData.CurrentGameMode == GameMode.Training)
        {
            return true;
        }
        return stats.currentMana >= stats.maxMana * percent / 100f - 0.01f;
    }

    /// <summary>Gọi từ subclass khi skill tầm xa trúng: hồi 20% mana.</summary>
    public void GainManaOnRangedHit()
    {
        GainMana(20f);
        Debug.Log($"[MANA] Ranged hit! +20% mana. Hiện: {stats.currentMana / stats.maxMana * 100f:F0}%");
    }
    // ========================================================
    #endregion

    #region MOVEMENT & ACTIONS
    protected virtual void ApplyMovementPhysics()
    {
        if (CurrentState == FighterState.Blocking)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            return;
        }


        rb.linearVelocity = new Vector2(moveInput.x * moveSpeed, rb.linearVelocity.y);

        if (Mathf.Abs(moveInput.x) > 0.01f)
        {
            float direction = Mathf.Sign(moveInput.x);
            transform.localScale = new Vector3(direction, 1, 1);
        }
    }


    public virtual void OnJump()
    {
        if (!CanAct()) return;

        bool isPressingDown = false;
        if (playerInput != null && playerInput.actions != null)
        {
            var blockAct = playerInput.actions.FindAction("Block");
            if (blockAct != null) isPressingDown = blockAct.IsPressed();
        }

        if (isPressingDown)
        {
            TryDropDown(); // Thử nhảy xuống, nhưng dù thất bại cũng không nhảy lên
            return;
        }

        ExecuteJump();
    }

    protected virtual void ExecuteJump()
    {
        if (!CanAct()) return;

        if (isGrounded || currentJumps < maxJumps)
        {
            ChangeState(FighterState.Jumping);
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            currentJumps++;

            if (anim != null)
            {
                anim.SetTrigger(currentJumps > 1 ? "DoubleJump" : "Jump");
            }
        }
    }

    public virtual void OnDash()
    {
        if (!CanAct()) return;

        bool isPressingDown = false;
        if (playerInput != null && playerInput.actions != null)
        {
            var blockAct = playerInput.actions.FindAction("Block");
            if (blockAct != null) isPressingDown = blockAct.IsPressed();
        }

        if (isPressingDown && TryDropDown())
        {
            return;
        }

        TriggerDash();
    }

    protected virtual void TriggerDash()
    {
        if (!CanAct() || !isGrounded) return;
        if (SelectionData.CurrentGameMode != GameMode.Training && stats.stamina < 20) return;
        StartCoroutine(DashRoutine());
    }


    protected virtual IEnumerator DashRoutine()
    {
        ChangeState(FighterState.Dashing);
        if (SelectionData.CurrentGameMode != GameMode.Training)
        {
            stats.stamina -= 20;
            lastStaminaUseTime = Time.time; // Ghi nhận thời gian vừa xài stamina
        }

        float dashDir = transform.localScale.x;
        rb.gravityScale = 0f;
        rb.linearVelocity = new Vector2(dashDir * dashForce, 0f);

        yield return new WaitForSeconds(dashDuration);

        rb.gravityScale = 3.5f;
        rb.linearVelocity = Vector2.zero;
        ChangeState(FighterState.Idle);
    }
    #endregion

    #region COMBAT
    public virtual void OnAttack()
    {
        if (!CanAct() || !isGrounded) return;
        ExecuteAttack();
    }

    public void PlayAttackSound()
    {
        AudioSource myAudio = GetComponent<AudioSource>();
        if (myAudio != null && attackSound != null)
            myAudio.PlayOneShot(attackSound, 1.0f);
    }

    public void PlayRangedSound()
    {
        AudioSource myAudio = GetComponent<AudioSource>();
        if (myAudio != null && rangedSound != null)
            myAudio.PlayOneShot(rangedSound, 1.0f);
    }

    public void PlayDashSound()
    {
        AudioSource myAudio = GetComponent<AudioSource>();
        if (myAudio != null && dashSound != null)
            myAudio.PlayOneShot(dashSound, 1.0f);
    }

    protected virtual void ExecuteAttack()
    {
        ChangeState(FighterState.Attacking);
        rb.linearVelocity = Vector2.zero; // Dừng lại khi chém
        lastAttackTime = Time.time;

        comboStep++;
        if (comboStep > 4) comboStep = 1;

        // Kích hoạt Trigger Animation ở đây (Ví dụ: anim.SetTrigger("Attack" + comboStep))
        Debug.Log($"[COMBAT] Combo Hit {comboStep}");
    }


    // Hàm này sẽ được gán vào Animation Event tại khung hình vung vũ khí trúng mục tiêu
    public virtual void AnimationEvent_DealDamage()
    {
        if (attackHitbox == null) return;

        Collider2D[] hits = Physics2D.OverlapCircleAll(attackHitbox.position, attackRadius, targetLayer);
        bool didHit = false;
        foreach (var hit in hits)
        {
            IDamageable damageable = hit.GetComponent<IDamageable>();
            if (damageable != null)
            {
                // Đòn thứ 4 sẽ là Heavy Attack (gây knockback)
                bool isHeavy = (comboStep == 4);
                damageable.TakeDamage(25f, transform.position.x, isHeavy);
                didHit = true;
            }
        }

        // Hồi mana khi đánh trúng: +10% mana mỗi đòn
        if (didHit)
        {
            GainMana(10f);
            Debug.Log($"[MANA] Đánh trúng! +10% mana. Hiện: {stats.currentMana / stats.maxMana * 100f:F0}%");
        }
    }

    // Hàm này gán vào Animation Event ở cuối Frame đánh, để trả nhân vật về trạng thái rảnh
    public virtual void AnimationEvent_EndAttack()
    {
        ChangeState(FighterState.Idle);
    }

    // Giao diện nhận sát thương
    public virtual void TakeDamage(float damage, float attackerPosX, bool isHeavyAttack = false)
    {
        if (!IsGameplayActive()) return;
        if (CurrentState == FighterState.Dead) return;

        if (CurrentState == FighterState.Blocking)
        {
            // Block giảm 70% sát thương theo chuẩn thiết kế
            damage *= (1f - blockDamageReduction);
            Debug.Log($"[DEFENSE] Đỡ đòn thành công! Nhận {damage} sát thương.");
            if (anim != null) anim.SetTrigger("BlockHit");
        }
        else
        {
            ChangeState(FighterState.Stunned);
            hitReceivedCount++;


            if (hitReceivedCount % 4 == 0 || isHeavyAttack)
            {
                CancelInvoke(nameof(ResetStun));
                ApplyKnockback(attackerPosX);
            }
            else
            {
                if (anim != null) anim.SetTrigger("Hit");
                // Giả lập thời gian bị choáng (Hit Stun)
                CancelInvoke(nameof(ResetStun));
                Invoke(nameof(ResetStun), 0.4f);
            }
        }

        if (SelectionData.CurrentGameMode == GameMode.Training)
        {
            stats.currentHp = stats.maxHp;
        }
        else
        {
            stats.currentHp -= damage;
            if (stats.currentHp <= 0)
            {
                Die();
            }
        }
    }

    protected virtual void ApplyKnockback(float attackerPosX)
    {
        float knockbackDir = transform.position.x > attackerPosX ? 1f : -1f;
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(new Vector2(knockbackDir * knockbackForce.x, knockbackForce.y), ForceMode2D.Impulse);

        if (anim != null) anim.SetTrigger("Knockback");
        CancelInvoke(nameof(ResetStun));
        Invoke(nameof(ResetStun), 0.8f); // Stun lâu hơn khi bị văng
    }

    protected void ResetStun()
    {
        if (CurrentState == FighterState.Stunned)
            ChangeState(FighterState.Idle);
    }

    protected virtual void Die()
    {
        ChangeState(FighterState.Dead);
        rb.linearVelocity = Vector2.zero;
        rb.simulated = false; // Tắt va chạm vật lý
        if (anim != null) anim.SetTrigger("Die");
        Debug.Log("[SYSTEM] Player K.O");
    }
    #endregion

    #region UTILITIES
    protected virtual void CheckGrounded()
    {
        if (isDroppingDown)
        {
            isGrounded = false;
            groundCollider = null;
            return;
        }

        // Start raycast slightly above feet (0.1f) and check 0.25f down (0.15f below feet) to ignore self-collision
        Vector2 rayStart = (Vector2)transform.position + Vector2.up * 0.1f;
        RaycastHit2D[] hits = Physics2D.RaycastAll(rayStart, Vector2.down, 0.25f, groundLayer);

        isGrounded = false;
        groundCollider = null;

        foreach (var hit in hits)
        {
            if (hit.collider != null && hit.collider.gameObject != gameObject)
            {
                isGrounded = true;
                groundCollider = hit.collider;
                break;
            }
        }

        if (isGrounded && rb.linearVelocity.y <= 0.01f)
        {
            currentJumps = 0;
        }
    }

    protected bool TryDropDown()
    {
        if (isGrounded && groundCollider != null)
        {
            if (groundCollider.usedByEffector)
            {
                StartCoroutine(DropDownRoutine(groundCollider));
                return true;
            }
        }
        return false;
    }

    private IEnumerator DropDownRoutine(Collider2D platformCollider)
    {
        isDroppingDown = true;
        Physics2D.IgnoreCollision(playerCollider, platformCollider, true);
        yield return new WaitForSeconds(0.3f);
        if (platformCollider != null && playerCollider != null)
        {
            Physics2D.IgnoreCollision(playerCollider, platformCollider, false);
        }
        isDroppingDown = false;
    }

    protected virtual void UpdateAnimations()
    {
        if (anim == null) return;
        // Chuẩn hóa gửi parameter cho Animator
        anim.SetBool("IsMoving", CurrentState == FighterState.Moving);
        anim.SetBool("IsGrounded", isGrounded);
        anim.SetBool("IsBlocking", CurrentState == FighterState.Blocking);
        anim.SetFloat("VelocityY", rb.linearVelocity.y);
    }

    private void OnDrawGizmosSelected()
    {
        if (attackHitbox != null)
        {
            Gizmos.color = new Color(1, 0, 0, 0.5f);
            Gizmos.DrawSphere(attackHitbox.position, attackRadius);
        }
    }
    #endregion

    // --- SUPPORT SUMMON LOGIC ---
    public virtual void OnSupport()
    {
        TrySummonSupport();
    }

    private void TrySummonSupport()
    {
        if (!CanAct()) return;
        if (Time.time - lastSupportTime < SUPPORT_COOLDOWN) return;

        // Try to load the prefab from Resources
        if (string.IsNullOrEmpty(supportPrefabUrl))
        {
            // Default fallback to Lucy if not assigned
            supportPrefabUrl = "Support_Lucy/prefabs/Support_Lucy";
        }

        GameObject supportPrefab = Resources.Load<GameObject>(supportPrefabUrl);
        if (supportPrefab == null)
        {
            Debug.LogError($"[Support] Could not find support prefab at Resources/{supportPrefabUrl}!");
            return;
        }

        if (!SpendMana(100f)) return; // Tiêu 100% mana

        lastSupportTime = Time.time;

        // Instantiate the prefab
        GameObject supportGo = Instantiate(supportPrefab);
        supportGo.name = "SupportSummon_" + gameObject.name;

        float facingDir = transform.localScale.x;

        // Hỗ trợ SupportLucy
        SupportLucy lucy = supportGo.GetComponent<SupportLucy>();
        if (lucy != null)
        {
            lucy.Setup(this, playerNumber, targetLayer, facingDir);
            Debug.Log($"[Support] Summoned SupportLucy for Player {playerNumber}!");
            return;
        }

        // Hỗ trợ SupportNeji
        SupportNeji neji = supportGo.GetComponent<SupportNeji>();
        if (neji != null)
        {
            neji.Setup(this, playerNumber, targetLayer, facingDir);
            Debug.Log($"[Support] Summoned SupportNeji for Player {playerNumber}!");
            return;
        }

        // Hỗ trợ GiornoGiovanna
        GiornoGiovannaController giorno = supportGo.GetComponent<GiornoGiovannaController>();
        if (giorno != null)
        {
            giorno.Setup(this, playerNumber, targetLayer, facingDir);
            Debug.Log($"[Support] Summoned GiornoGiovanna for Player {playerNumber}!");
            return;
        }

        // Hỗ trợ Jinbei
        JinbeiController jinbei = supportGo.GetComponent<JinbeiController>();
        if (jinbei != null)
        {
            jinbei.Setup(this, playerNumber, targetLayer, facingDir);
            Debug.Log($"[Support] Summoned Jinbei for Player {playerNumber}!");
            return;
        }

        Debug.LogWarning($"[Support] Prefab '{supportPrefabUrl}' has no recognized Support component!");
    }
}

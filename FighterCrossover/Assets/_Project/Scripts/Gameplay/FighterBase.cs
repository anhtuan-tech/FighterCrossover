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
        public int mana;
        public float maxStamina;
        public float stamina;   
        
    }

    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(Animator))]
    [RequireComponent(typeof(PlayerInput))]
    public class FighterBase : MonoBehaviour, IDamageable
    {
        [Header("--- STATS ---")]
        public FighterStats stats = new FighterStats
        {
            maxHp = 500f,
            currentHp = 500f,
            mana = 250,
            maxStamina = 100f, 
            stamina = 100f
            
            
        };
        [Header("--- STAMINA REGEN ---")]
        public float staminaRegenRate = 20f; // Lượng stamina hồi mỗi giây
        public float staminaRegenDelay = 1f; // Thời gian đợi (giây) sau khi Dash mới bắt đầu hồi

        protected float lastStaminaUseTime; // Lưu lại mốc thời gian cuối cùng tiêu hao stamina

        [Header("--- MOVEMENT SETTINGS ---")]
        public float moveSpeed = 6.5f;
        public float jumpForce = 11f;
        public float dashForce = 25f;
        public float dashDuration = 0.10f;
        public int maxJumps = 2;
        public LayerMask groundLayer;

        [Header("--- COMBAT SETTINGS ---")]
        public float blockDamageReduction = 0.7f; // Giảm 70% sát thương khi đỡ
        public Vector2 knockbackForce = new Vector2(8f, 4f);
        public float comboResetTime = 0.8f;
        public Transform attackHitbox;
        public float attackRadius = 0.8f;
        public LayerMask targetLayer;

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

            // Cảnh báo nếu layer chưa được gán (thường xảy ra sau khi bấm Reset trong Inspector)
            if (groundLayer.value == 0)
                Debug.LogWarning($"[{gameObject.name}] groundLayer chưa được gán! Vui lòng set lại trong Inspector.", this);
            if (targetLayer.value == 0)
                Debug.LogWarning($"[{gameObject.name}] targetLayer chưa được gán! Vui lòng set lại trong Inspector.", this);
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
        protected virtual void Update()
        {
            if (CurrentState == FighterState.Dead) return;

            CheckGrounded();
            HandleStateLogic();
            HandleStaminaRegen();
            UpdateAnimations();
        }

        protected virtual void FixedUpdate()
        {
            if (CurrentState == FighterState.Moving || CurrentState == FighterState.Idle || CurrentState == FighterState.Jumping)
            {
                ApplyMovementPhysics();
            }
        }
        #endregion

        #region FSM & LOGIC
        protected virtual void HandleStateLogic()
        {
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

        protected bool CanAct()
        {
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
            if (!CanAct() || !isGrounded || stats.stamina < 20) return;
            StartCoroutine(DashRoutine());
        }


        protected virtual IEnumerator DashRoutine()
        {
            ChangeState(FighterState.Dashing);
            stats.stamina -= 20;
            lastStaminaUseTime = Time.time; // Ghi nhận thời gian vừa xài stamina

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
            foreach (var hit in hits)
            {
                IDamageable damageable = hit.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    // Đòn thứ 4 sẽ là Heavy Attack (gây knockback)
                    bool isHeavy = (comboStep == 4);
                    damageable.TakeDamage(25f, transform.position.x, isHeavy);
                }
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

            stats.currentHp -= damage;
            if (stats.currentHp <= 0)
            {
                Die();
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
            Physics2D.IgnoreCollision(playerCollider, platformCollider, true);
            yield return new WaitForSeconds(0.3f);
            if (platformCollider != null && playerCollider != null)
            {
                Physics2D.IgnoreCollision(playerCollider, platformCollider, false);
            }
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
    }

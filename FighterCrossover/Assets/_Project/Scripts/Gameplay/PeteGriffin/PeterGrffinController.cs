using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// FIX NOTE: Animator Controller của Peter Griffin có tất cả AnyState transitions
// đến Attack/Ranged/Ultimate/Dash bị rỗng conditions (m_Conditions: []).
// Do đó SetTrigger() không có tác dụng. Giải pháp: dùng Animator.Play() trực tiếp.

public class PeterGriffinController : FighterBase
{
    [Header("--- PETER GRIFFIN SKILLS ---")]
    public PeterGriffinRangedSkill rangedSkill;
    public PeterGriffinUltimateSkill ultimateSkill;

    [Header("--- ULTIMATE FLASH CUTSCENE ---")]
    [Tooltip("Ảnh hiển thị khi dùng Ultimate (Nếu để trống sẽ tự động load mặc định)")]
    public Sprite ultimateFlashSprite;
    [Tooltip("Kích thước ảnh theo tỉ lệ chiều cao màn hình (0.6 = 60% màn hình)")]
    public float ultimateFlashSizeFactor = 0.6f;
    [Tooltip("Vị trí ảnh so với tâm màn hình (pixel), ví dụ (0, 100) = lên trên 100px")]
    public Vector2 ultimateFlashOffset = new Vector2(0f, 100f);

    [Header("--- DYNAMIC BINDINGS ---")]
    private AnimeFighter.UI.KeybindingsData keys;
    private bool initializedBindings = false;

    protected override void Awake()
    {
        base.Awake();

        if (rangedSkill == null)
        {
            rangedSkill = GetComponent<PeterGriffinRangedSkill>();
            if (rangedSkill == null)
            {
                rangedSkill = gameObject.AddComponent<PeterGriffinRangedSkill>();
            }
        }

        if (ultimateSkill == null)
        {
            ultimateSkill = GetComponent<PeterGriffinUltimateSkill>();
            if (ultimateSkill == null)
            {
                ultimateSkill = gameObject.AddComponent<PeterGriffinUltimateSkill>();
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
                Debug.LogWarning($"[PeterGriffinController] Failed to parse settings.json: {ex.Message}");
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

        Debug.Log($"[PeterGriffinController] Bindings loaded for Player {playerNumber}. Left: {keys.moveLeft}, Right: {keys.moveRight}, Block: {keys.defense}, Attack: {keys.attack}, Jump: {keys.jump}, Dash: {keys.dodge}, Ranged: {keys.rangedAttack}, Ultimate: {keys.specialMove}, Support: {supportKey}");
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

    protected override void ExecuteAttack()
    {
        ChangeState(FighterState.Attacking);
        rb.linearVelocity = Vector2.zero;
        lastAttackTime = Time.time;

        comboStep++;
        if (comboStep > 4) comboStep = 1;

        if (anim != null)
        {
            anim.Play("Hit_" + comboStep, 0, 0f);
        }

        // Animation clips KHÔNG có Animation Events (m_Events: [])
        // → phải tự gọi DealDamage và EndAttack bằng coroutine
        StartCoroutine(AttackTimingRoutine(comboStep));
    }

    /// <summary>
    /// Tự trigger DealDamage ở giữa animation và EndAttack khi animation gần xong.
    /// Thay thế cho Animation Events vì các .anim clips không có events.
    /// </summary>
    private IEnumerator AttackTimingRoutine(int combo)
    {
        // Thời gian animation gốc / speed trong animator
        // Hit_1: 0.417s/0.5speed = 0.834s | Hit_2: 0.5s/0.5speed = 1s
        // Hit_3: 0.417s/0.5speed = 0.834s | Hit_4: 0.583s/0.75speed = 0.778s
        float dealDamageDelay = 0.15f; // Gây sát thương ngay khoảng giữa đòn
        float endAttackDelay = (combo == 4) ? 0.5f : 0.35f; // Đòn 4 chậm hơn

        yield return new WaitForSeconds(dealDamageDelay);

        // Gây sát thương
        if (CurrentState == FighterState.Attacking)
        {
            AnimationEvent_DealDamage();
        }

        yield return new WaitForSeconds(endAttackDelay - dealDamageDelay);

        // Kết thúc attack → trả về Idle
        if (CurrentState == FighterState.Attacking)
        {
            AnimationEvent_EndAttack();
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
            anim.Play("Ranged_Skill", 0, 0f);
        }

        // Animation clips không có events → dùng coroutine
        StartCoroutine(RangedTimingRoutine());
    }

    private IEnumerator RangedTimingRoutine()
    {
        // Ranged_Skill.anim: 0.833s / speed 0.5 ≈ 1.67s total
        // Spawn projectile ở khoảng 40% animation
        yield return new WaitForSeconds(0.3f);

        if (CurrentState == FighterState.Attacking)
        {
            AnimationEvent_SpawnProjectile();
        }

        // Kết thúc attack
        yield return new WaitForSeconds(0.4f);

        if (CurrentState == FighterState.Attacking)
        {
            AnimationEvent_EndAttack();
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
            anim.Play("Ultimate", 0, 0f);
        }

        // Ultimate.anim: 0.583s / speed 0.5 ≈ 1.17s total
        // Spawn ultimate object ở khoảng 30%
        yield return new WaitForSeconds(0.25f);

        if (CurrentState == FighterState.Attacking)
        {
            AnimationEvent_SpawnUltimate();
        }

        // Kết thúc attack
        yield return new WaitForSeconds(0.5f);

        if (CurrentState == FighterState.Attacking)
        {
            AnimationEvent_EndAttack();
        }
    }

    private Sprite LoadUltimateAvatar()
    {
        if (ultimateFlashSprite != null) return ultimateFlashSprite;

        string avatarPath = "Assets/_Project/Resources/PeterGriffin/avatar/ultil.png";
#if UNITY_EDITOR
        var assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(avatarPath);
        foreach (var a in assets)
        {
            if (a is Sprite s) return s;
        }
        Texture2D tex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(avatarPath);
        if (tex != null)
        {
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        }
#else
        Sprite s = Resources.Load<Sprite>("PeterGriffin/avatar/ultil");
        return s;
#endif
        return null;
    }

    protected override void ExecuteJump()
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
                // FIX: Dùng Play() thay SetTrigger() — bypass transitions rỗng
                string jumpState = currentJumps > 1 ? "Double_Jump" : "Jump";
                anim.Play(jumpState, 0, 0f);
            }
        }
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
            // FIX: Dùng Play() thay SetTrigger() — bypass AnyState transition rỗng
            anim.Play("Dash", 0, 0f);
        }

        Vector2 startPos = rb.position;
        float dashDist = 4.5f;
        Vector2 targetPos = startPos + new Vector2(dashDir * dashDist, 0f);

        RaycastHit2D[] hits = Physics2D.RaycastAll(startPos, new Vector2(dashDir, 0f), dashDist, groundLayer);
        foreach (var h in hits)
        {
            if (h.collider != null && h.collider.gameObject != gameObject)
            {
                // Bỏ qua nếu vật thể va chạm là Player khác để tránh bị khựng khi Dash xuyên qua nhau
                if (h.collider.GetComponent<FighterBase>() != null) continue;

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
            if (hit.gameObject == gameObject || hit.transform.IsChildOf(transform)) continue;

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
            Debug.Log($"[MANA] Peter Griffin đánh trúng! +10% mana. Hiện: {stats.currentMana / stats.maxMana * 100f:F0}%");
        }
    }

    public void AnimationEvent_SpawnProjectile()
    {
        if (rangedSkill != null)
        {
            rangedSkill.SpawnProjectile(this, targetLayer);
        }
    }

    // Thay đổi từ gọi Combo (Boa Hancock) sang gọi Spawn Vật thể (Peter Griffin)
    public void AnimationEvent_SpawnUltimate()
    {
        if (ultimateSkill != null)
        {
            // Gọi hàm Spawn vật thể bay khổng lồ thay vì Combo chuỗi đòn
            ultimateSkill.SpawnUltimateObject(this, targetLayer);
        }
    }

    public Sprite LoadPeterGriffinSprite(string spriteName)
    {
        string filename = spriteName;
        if (spriteName.Contains("_"))
        {
            int index = spriteName.IndexOf("_");
            filename = spriteName.Substring(0, index);
        }

        string path = $"Assets/_Project/Characters/PeterGriffin/{filename}.png";

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

    protected override void UpdateAnimations()
    {
        if (anim == null) return;

        // 1. Thay vì truyền IsMoving (Bool), ta truyền giá trị cho 'Speed' (Float)
        // Nếu đang ở trạng thái di chuyển (Moving), đặt Speed = 1f, ngược lại là 0f
        float speedValue = (CurrentState == FighterState.Moving) ? 1f : 0f;
        anim.SetFloat("Speed", speedValue);

        // 2. Sửa chữ 'I' hoa thành 'i' thường để khớp với 'isGrounded' trong Animator của bạn
        anim.SetBool("isGrounded", isGrounded);

        // 3. Sửa chữ 'I' hoa thành 'i' thường để khớp với 'isBlocking' trong Animator của bạn
        anim.SetBool("isBlocking", CurrentState == FighterState.Blocking);

        // 4. Sửa 'VelocityY' thành 'yVelocity' để khớp chính xác với Float parameter của bạn
        if (rb != null)
        {
            anim.SetFloat("yVelocity", rb.linearVelocity.y);
        }

        // FIX: Force chuyển về Movement state khi grounded nhưng animator stuck ở Jump/Double_Jump
        if (isGrounded && CurrentState != FighterState.Attacking && CurrentState != FighterState.Dashing
            && CurrentState != FighterState.Stunned && CurrentState != FighterState.Dead)
        {
            AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.IsName("Jump") || stateInfo.IsName("Double_Jump"))
            {
                // Nhân vật đã chạm đất nhưng animator vẫn ở Jump/Double_Jump → force về Movement
                anim.Play("Movement", 0, 0f);
            }
        }
    }

    public override void TakeDamage(float damage, float attackerPosX, bool isHeavyAttack = false)
    {
        if (CurrentState == FighterState.Dead) return;

        Debug.Log($"[DAMAGE_CHECK] Nhận dmg: {damage} | Nguồn X: {attackerPosX} | Vị trí hiện tại X: {transform.position.x}");

        // Bỏ qua sát thương do trùng khít tọa độ X với nguồn phát
        if (Mathf.Approximately(attackerPosX, transform.position.x))
        {
            return;
        }

        if (CurrentState == FighterState.Blocking)
        {
            damage *= (1f - blockDamageReduction);
            // Block state không có transition riêng, giữ nguyên BlockHit trigger
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
                if (anim != null)
                {
                    // FIX: Trigger trong Animator tên "Hitted", không phải "Hit"
                    // Dùng Play() để bypass transition rỗng
                    anim.Play("Hitted", 0, 0f);
                }

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

    protected override void CheckGrounded()
    {
        if (playerCollider == null)
        {
            playerCollider = GetComponent<BoxCollider2D>();
        }

        Vector2 rayStart;
        float rayLength;

        if (playerCollider != null)
        {
            // Bắt đầu từ 0.15f trên đáy của collider thực tế
            float bottomY = playerCollider.bounds.min.y;
            float centerX = playerCollider.bounds.center.x;
            rayStart = new Vector2(centerX, bottomY + 0.15f);
            rayLength = 0.35f; // Quét xuống dưới đáy collider thêm 0.20f để đảm bảo chạm đất
        }
        else
        {
            // Fallback nếu không có collider
            rayStart = (Vector2)transform.position + Vector2.up * 0.1f;
            rayLength = 0.25f;
        }

        // Thực hiện quét tia (Raycast)
        RaycastHit2D[] hits = Physics2D.RaycastAll(rayStart, Vector2.down, rayLength, groundLayer);

        isGrounded = false;
        groundCollider = null;

        foreach (var hit in hits)
        {
            if (hit.collider != null)
            {
                // BỎ QUA bản thân Peter và các bộ phận con
                if (hit.collider.gameObject == gameObject || hit.collider.transform.IsChildOf(transform))
                {
                    continue;
                }

                // BỎ QUA ĐỐI THỦ
                if (hit.collider.GetComponent<FighterBase>() != null)
                {
                    continue;
                }

                // Nếu vượt qua các bộ lọc trên, xác nhận đã chạm đất hợp lệ
                isGrounded = true;
                groundCollider = hit.collider;
                break;
            }
        }

        if (isGrounded)
        {
            // Chỉ reset số lần nhảy khi đã chạm đất và không đang đi lên (đồng bộ chuẩn FighterBase)
            if (rb != null && rb.linearVelocity.y <= 0.01f)
            {
                currentJumps = 0;
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
            }
        }

        // Vẽ tia màu xanh lá nếu chạm đất, màu đỏ nếu đang bay để dễ nhìn trong tab Scene
        Debug.DrawLine(rayStart, rayStart + Vector2.down * rayLength, isGrounded ? Color.green : Color.red);
    }
}
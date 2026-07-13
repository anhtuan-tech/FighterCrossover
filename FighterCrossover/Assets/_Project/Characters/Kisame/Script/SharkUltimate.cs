using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Thủy Độn – Đại Tạp Sáp Thao (Skill 2 / Ultimate của Kisame)
///
/// ┌─── Pha 1 (Hình thành) ──────────────────────────────────────────────────┐
/// │  Cá mập đứng HOÀN TOÀN yên. Rigidbody2D.linearVelocity = Vector2.zero. │
/// │  Animator phát hoạt ảnh "nước trồi lên" (Effect2 – các frame đầu).      │
/// │  Thời gian chờ: riseDelay (giây).                                        │
/// └─────────────────────────────────────────────────────────────────────────┘
/// ┌─── Pha 2 (Lao đi) ──────────────────────────────────────────────────────┐
/// │  Kích hoạt trigger "Charge" trên Animator (chuyển sang clip bơi/lao).   │
/// │  Gán linearVelocity để lao về trục X theo hướng Kisame đang nhìn.        │
/// │  Multi-hit: OnTriggerStay2D + cooldown per-target tránh spam damage.     │
/// │  Tự hủy khi ra khỏi màn hình (Camera viewport) hoặc hết timeout.        │
/// └─────────────────────────────────────────────────────────────────────────┘
///
/// Yêu cầu Component: Rigidbody2D (Kinematic), BoxCollider2D (IsTrigger=true), Animator.
/// Gọi Setup() ngay sau Instantiate từ KisameController.AnimationEvent_SpawnShark().
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(Animator))]
public class SharkUltimate : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────────
    //  INSPECTOR FIELDS
    // ─────────────────────────────────────────────────────────────────────────────
    [Header("─── SHARK CONFIG ───")]
    [Tooltip("Giây đứng yên ở Pha 1 (hoạt ảnh nước trồi lên). " +
             "Điều chỉnh khớp với số frame clip 'trồi' chia FPS. " +
             "Ví dụ: 10 frame / 12fps ≈ 0.83s → đặt 0.85f để dư 1 frame.")]
    [SerializeField] private float riseDelay = 0.85f;

    [Tooltip("Tốc độ lao đi ở Pha 2 (units/second).")]
    [SerializeField] private float chargeSpeed = 18f;

    [Tooltip("Timeout tự hủy kể từ khi BẮT ĐẦU lao (giây). " +
             "Đảm bảo cá mập không tồn tại mãi nếu không ra khỏi màn hình.")]
    [SerializeField] private float chargeTimeout = 3.0f;

    [Header("─── DAMAGE ───")]
    [Tooltip("Sát thương mỗi lần hit (Heavy Attack → gây knockback).")]
    [SerializeField] private float damage = 45f;

    [Tooltip("Cooldown giữa 2 lần hit cùng 1 target (giây). " +
             "Tránh spam damage khi target nằm trong vùng Collider lớn.")]
    [SerializeField] private float hitCooldown = 0.4f;

    [Header("─── OUT-OF-SCREEN ───")]
    [Tooltip("Padding thêm ngoài biên màn hình (world units) trước khi tự hủy. " +
             "Đặt đủ lớn để cá mập bay hẳn ra ngoài view.")]
    [SerializeField] private float screenDestroyPadding = 2f;

    // ─────────────────────────────────────────────────────────────────────────────
    //  PRIVATE FIELDS  (cache – không GetComponent trong Update)
    // ─────────────────────────────────────────────────────────────────────────────
    private Rigidbody2D  _rb;
    private Animator     _anim;
    private Camera       _cam;

    private Vector2      _direction;
    private GameObject   _owner;
    private LayerMask    _targetLayer;

    private bool         _isCharging  = false; // True từ khi bắt đầu Pha 2
    private bool         _isDestroyed = false; // Guard tránh double-Destroy

    // Per-target hit cooldown (Dictionary tái sử dụng – tránh GC allocation)
    private readonly Dictionary<Collider2D, float> _lastHitTime
        = new Dictionary<Collider2D, float>();

    // Animator parameter hash (tránh boxing string mỗi frame)
    private static readonly int HashCharge = Animator.StringToHash("Charge");

    // ─────────────────────────────────────────────────────────────────────────────
    //  PUBLIC API
    // ─────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Khởi tạo cá mập ngay sau khi Instantiate.
    /// </summary>
    /// <param name="direction">Hướng lao (đã normalize). Ví dụ: Vector2(1,0) hoặc Vector2(-1,0).</param>
    /// <param name="owner">GameObject của Kisame – bỏ qua va chạm với chính mình.</param>
    /// <param name="targetLayer">Layer của kẻ địch cần gây sát thương.</param>
    public void Setup(Vector2 direction, GameObject owner, LayerMask targetLayer)
    {
        // Cache components (GetComponent 1 lần duy nhất trong Setup)
        _rb          = GetComponent<Rigidbody2D>();
        _anim        = GetComponent<Animator>();
        _cam         = Camera.main;

        _direction   = direction.normalized;
        _owner       = owner;
        _targetLayer = targetLayer;

        // Lật sprite theo hướng lao (sprite mặc định hướng phải)
        if (_direction.x < 0f)
            transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x),
                                               transform.localScale.y, 1f);

        // Pha 1: đứng yên hoàn toàn
        _rb.bodyType       = RigidbodyType2D.Kinematic;
        _rb.linearVelocity = Vector2.zero;
        _rb.gravityScale   = 0f;

        StartCoroutine(SharkSequence());
    }

    // ─────────────────────────────────────────────────────────────────────────────
    //  2-PHASE SEQUENCE
    // ─────────────────────────────────────────────────────────────────────────────

    private IEnumerator SharkSequence()
    {
        // ── Pha 1: Hình thành – cá mập ĐỨNG YÊN, hoạt ảnh nước trồi ─────────────
        _rb.linearVelocity = Vector2.zero;

        yield return new WaitForSeconds(riseDelay);

        // ── Pha 2: Lao đi ─────────────────────────────────────────────────────────
        _isCharging = true;

        // Báo Animator chuyển sang state "bơi/lao" (cần có trigger "Charge" trong Controller)
        _anim?.SetTrigger(HashCharge);

        _rb.linearVelocity = _direction * chargeSpeed;

        // Timeout failsafe: tự hủy sau chargeTimeout giây kể từ lúc bắt đầu lao
        Destroy(gameObject, chargeTimeout);
    }

    // ─────────────────────────────────────────────────────────────────────────────
    //  UPDATE – KIỂM TRA OUT-OF-SCREEN  (chỉ khi đang lao)
    // ─────────────────────────────────────────────────────────────────────────────

    private void Update()
    {
        if (!_isCharging || _isDestroyed || _cam == null) return;

        // Chuyển vị trí sang Viewport coordinates (0-1 trong màn hình)
        Vector3 vp = _cam.WorldToViewportPoint(transform.position);

        // Hủy khi hoàn toàn ra ngoài biên (tính cả padding world)
        float padVP = screenDestroyPadding / Screen.width; // xấp xỉ padding viewport
        if (vp.x < -padVP || vp.x > 1f + padVP)
        {
            SafeDestroy();
        }
    }

    // ─────────────────────────────────────────────────────────────────────────────
    //  COLLISION – MULTI-HIT
    // ─────────────────────────────────────────────────────────────────────────────

    private void OnTriggerEnter2D(Collider2D other)
    {
        ProcessHit(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        // Tiếp tục gây sát thương theo cooldown khi target đứng trong vùng cá mập
        ProcessHit(other);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        // Dọn Dictionary khi target rời khỏi vùng – tránh rò rỉ bộ nhớ
        _lastHitTime.Remove(other);
    }

    private void ProcessHit(Collider2D other)
    {
        if (_isDestroyed) return;
        if (_owner  != null && other.gameObject == _owner) return;
        if (((1 << other.gameObject.layer) & _targetLayer.value) == 0) return;

        float now = Time.time;
        if (_lastHitTime.TryGetValue(other, out float last) && now - last < hitCooldown)
            return;

        _lastHitTime[other] = now;

        // Heavy Attack → isHeavyAttack = true → FighterBase.ApplyKnockback() được gọi
        other.GetComponent<IDamageable>()?.TakeDamage(damage, transform.position.x, isHeavyAttack: true);
    }

    // ─────────────────────────────────────────────────────────────────────────────
    //  ANIMATOR EVENT  (tùy chọn – gán trên clip Effect2 của Prefab cá mập)
    // ─────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// [Tùy chọn] Gán Animation Event tên "AnimEvent_StartCharge" trên clip Effect2
    /// tại FRAME cá mập thành hình xong. Cách thay thế cho riseDelay timer.
    /// Nếu dùng hàm này thì đặt riseDelay = 0 (hoặc rất nhỏ) trong Inspector.
    /// </summary>
    public void AnimEvent_StartCharge()
    {
        if (_isCharging) return; // Đã lao rồi, bỏ qua

        StopAllCoroutines();     // Dừng timer coroutine cũ nếu có

        _isCharging            = true;
        _anim?.SetTrigger(HashCharge);
        _rb.linearVelocity     = _direction * chargeSpeed;

        Destroy(gameObject, chargeTimeout);
    }

    // ─────────────────────────────────────────────────────────────────────────────
    //  HELPER
    // ─────────────────────────────────────────────────────────────────────────────

    private void SafeDestroy()
    {
        if (_isDestroyed) return;
        _isDestroyed = true;
        Destroy(gameObject);
    }
}

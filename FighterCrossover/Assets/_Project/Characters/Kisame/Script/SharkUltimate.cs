using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Thủy Độn – Đại Tạp Sáp Thao (Skill 2 / Ultimate của Kisame – phím I)
///
/// ┌─── Pha 1: Hình Thành ───────────────────────────────────────────────────────┐
/// │  Cá mập ĐỨNG YÊN (linearVelocity = zero, Kinematic).                      │
/// │  Animator phát clip "Rise" (nước trồi lên, các frame đầu của Effect2).     │
/// │  Chờ riseDelay giây HOẶC đến khi Animation Event AnimEvent_StartCharge().  │
/// └────────────────────────────────────────────────────────────────────────────┘
/// ┌─── Pha 2: Lao Đi ───────────────────────────────────────────────────────────┐
/// │  Gán linearVelocity = direction * chargeSpeed (chuẩn Unity 6).            │
/// │  Set Trigger "Charge" → Animator chuyển sang clip bơi/lao.                │
/// │  Multi-hit: OnTriggerStay2D + per-target cooldown tránh spam damage.       │
/// │  Tự hủy khi ra ngoài màn hình (Camera viewport) HOẶC hết chargeTimeout.   │
/// └────────────────────────────────────────────────────────────────────────────┘
///
/// Setup Prefab:
///   • Rigidbody2D   → Body Type = Kinematic, Gravity Scale = 0, Collision = Continuous
///   • BoxCollider2D → Is Trigger = true (bao phủ toàn thân cá mập)
///   • Animator      → 2 clip: Effect2_Rise (loop=false) + Effect2_Charge (loop=true)
///     └─ Animator Controller: State "Rise" --(Trigger "Charge")--> State "Charge"
///     └─ Animation Event tuỳ chọn trên clip Rise (frame thành hình): AnimEvent_StartCharge()
///
/// Gọi Setup() ngay sau Instantiate từ KisameController.AnimationEvent_SpawnShark().
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(Animator))]
public class SharkUltimate : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────────
    //  INSPECTOR FIELDS
    // ─────────────────────────────────────────────────────────────────────────────
    [Header("─── SHARK CONFIG ───")]
    [Tooltip("Giây đứng yên ở Pha 1 (hoạt ảnh nước trồi lên).\n" +
             "Ước tính: số frame clip 'Rise' ÷ FPS.\n" +
             "Ví dụ: 10 frame ÷ 12fps ≈ 0.83s → đặt 0.85f.\n" +
             "Đặt 0 nếu dùng AnimEvent_StartCharge() từ Animation Event.")]
    [SerializeField] private float riseDelay = 0.85f;

    [Tooltip("Tốc độ lao đi ở Pha 2 (units/second). Khuyến nghị 15–20.")]
    [SerializeField] private float chargeSpeed = 18f;

    [Tooltip("Timeout tự hủy kể từ khi BẮT ĐẦU lao (giây).\n" +
             "Đảm bảo cá mập không tồn tại mãi nếu không thoát màn hình.")]
    [SerializeField] private float chargeTimeout = 3.0f;

    [Header("─── DAMAGE ───")]
    [Tooltip("Sát thương mỗi lần hit. isHeavyAttack = true → gây KnockedDown cho target.")]
    [SerializeField] private float damage = 45f;

    [Tooltip("Cooldown giữa 2 lần hit cùng 1 target (giây).\n" +
             "Tránh spam damage khi target nằm lâu trong vùng Collider lớn.")]
    [SerializeField] private float hitCooldown = 0.4f;

    [Header("─── OUT-OF-SCREEN ───")]
    [Tooltip("Padding thêm ngoài biên màn hình (world units) trước khi tự hủy.\n" +
             "Đặt đủ lớn để cá mập bay hẳn ra ngoài view rồi mới destroy.")]
    [SerializeField] private float screenDestroyPadding = 2f;

    // ─────────────────────────────────────────────────────────────────────────────
    //  PRIVATE FIELDS  (cache – không GetComponent trong Update)
    // ─────────────────────────────────────────────────────────────────────────────
    private Rigidbody2D _rb;
    private Animator    _anim;
    private Camera      _cam;

    private Vector2     _direction;
    private GameObject  _owner;
    private LayerMask   _targetLayer;

    private bool        _isCharging  = false; // true từ khi bắt đầu Pha 2
    private bool        _isDestroyed = false; // guard tránh double-Destroy

    /// <summary>Per-target hit cooldown. Dictionary tái sử dụng để tránh GC allocation.</summary>
    private readonly Dictionary<Collider2D, float> _lastHitTime
        = new Dictionary<Collider2D, float>();

    // Animator hash (tránh boxing string mỗi frame)
    private static readonly int HashCharge = Animator.StringToHash("Charge");

    // ─────────────────────────────────────────────────────────────────────────────
    //  PUBLIC API
    // ─────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Khởi tạo cá mập ngay sau khi Instantiate.
    /// GetComponent được gọi tại đây, KHÔNG gọi lại trong Update/Collision.
    /// </summary>
    /// <param name="direction">Hướng lao (đã normalize). Ví dụ: Vector2(1,0) hoặc Vector2(-1,0).</param>
    /// <param name="owner">GameObject của Kisame – bỏ qua va chạm với chính mình.</param>
    /// <param name="targetLayer">Layer của kẻ địch cần gây sát thương.</param>
    /// <param name="riseDelay">
    /// Ghi đè thời gian chờ Pha 1 (giây). Mặc định -1 = dùng giá trị Inspector.
    /// Truyền 0f để tắt timer và điều khiển hoàn toàn bằng AnimEvent_StartCharge.
    /// </param>
    public void Setup(Vector2 direction, GameObject owner, LayerMask targetLayer, float riseDelay = -1f)
    {
        // Cache components (GetComponent 1 lần duy nhất)
        _rb          = GetComponent<Rigidbody2D>();
        _anim        = GetComponent<Animator>();
        _cam         = Camera.main;

        _direction   = direction.normalized;
        _owner       = owner;
        _targetLayer = targetLayer;

        // Ghi đè riseDelay nếu caller truyền giá trị hợp lệ (>= 0)
        if (riseDelay >= 0f)
            this.riseDelay = riseDelay;

        // Lật sprite theo hướng lao (sprite mặc định hướng PHẢI)
        if (_direction.x < 0f)
        {
            Vector3 s = transform.localScale;
            transform.localScale = new Vector3(-Mathf.Abs(s.x), s.y, 1f);
        }

        // Pha 1: đứng hoàn toàn yên
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

        // ── Pha 2: Lao đi (chỉ kích hoạt nếu chưa lao – AnimEvent có thể kích sớm hơn) ──
        if (!_isCharging)
        {
            StartCharge();
        }
    }

    /// <summary>Logic bắt đầu Pha 2 – tách ra để cả Coroutine và AnimEvent đều dùng được.</summary>
    private void StartCharge()
    {
        if (_isCharging) return;
        _isCharging = true;

        // Báo Animator chuyển sang state "Charge" (clip bơi/lao)
        _anim?.SetTrigger(HashCharge);

        // ► linearVelocity: chuẩn Unity 6 (thay rb.velocity đã deprecated)
        _rb.linearVelocity = _direction * chargeSpeed;

        // Timeout failsafe: tự hủy sau chargeTimeout giây kể từ khi bắt đầu lao
        Destroy(gameObject, chargeTimeout);
    }

    // ─────────────────────────────────────────────────────────────────────────────
    //  UPDATE – KIỂM TRA OUT-OF-SCREEN  (chỉ khi đang lao Pha 2)
    // ─────────────────────────────────────────────────────────────────────────────

    private void Update()
    {
        if (!_isCharging || _isDestroyed || _cam == null) return;

        // Chuyển vị trí sang Viewport coordinates (0-1 trong màn hình)
        Vector3 vp = _cam.WorldToViewportPoint(transform.position);

        // Tính padding viewport (world units → viewport units xấp xỉ)
        float padVP = screenDestroyPadding / Screen.width;

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
        // Tiếp tục gây sát thương khi target đứng lâu trong vùng cá mập
        ProcessHit(other);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        // Dọn Dictionary khi target rời khỏi vùng – tránh rò rỉ bộ nhớ
        _lastHitTime.Remove(other);
    }

    private void ProcessHit(Collider2D other)
    {
        if (_isDestroyed)                                              return;
        if (_owner != null && other.gameObject == _owner)             return;
        if (((1 << other.gameObject.layer) & _targetLayer.value) == 0) return;

        float now = Time.time;
        if (_lastHitTime.TryGetValue(other, out float last) && now - last < hitCooldown)
            return;

        // Gây sát thương
        // isHeavyAttack = true → FighterBase/KisameController.ApplyKnockback() → Trigger KnockedDown
        other.GetComponent<IDamageable>()?.TakeDamage(damage, transform.position.x, isHeavyAttack: true);

        // Biến mất ngay sau khi trúng mục tiêu
        SafeDestroy();
    }

    // ─────────────────────────────────────────────────────────────────────────────
    //  ANIMATION EVENT  (tuỳ chọn – gán trên clip Rise của Prefab cá mập)
    // ─────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// [Tuỳ chọn] Thay thế cho riseDelay timer – cho phép Animator kiểm soát chính xác hơn.
    ///
    /// Cách dùng:
    ///   1. Đặt riseDelay = 0 trong Inspector.
    ///   2. Gán Animation Event tên "AnimEvent_StartCharge" trên clip Effect2_Rise
    ///      tại frame cá mập thành hình xong (thường là frame cuối clip Rise).
    ///
    /// Nếu KHÔNG dùng Animation Event, để riseDelay > 0 và bỏ qua hàm này.
    /// </summary>
    public void AnimEvent_StartCharge()
    {
        StopAllCoroutines(); // Dừng timer coroutine SharkSequence nếu đang chạy
        StartCharge();
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

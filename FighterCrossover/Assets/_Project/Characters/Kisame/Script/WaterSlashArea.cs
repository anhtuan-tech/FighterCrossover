using UnityEngine;

/// <summary>
/// Thủy Độn – Trảm Sóng Nước (Skill 1 của Kisame – phím U)
///
/// ┌─── Cơ chế ─────────────────────────────────────────────────────────────────┐
/// │  Close-range AoE ĐỨNG YÊN. KHÔNG bay đi.                                  │
/// │  • Xuất hiện ngay tại vị trí attackHitbox phía trước Kisame.              │
/// │  • Phát hoạt ảnh Effect1 tại chỗ, gây sát thương 1 lần cho mọi kẻ địch   │
/// │    bước vào vùng (OnTriggerEnter2D).                                       │
/// │  • Tự hủy khi hoạt ảnh kết thúc (Animation Event "OnEffectEnd")           │
/// │    hoặc sau fallbackLifetime giây.                                         │
/// └────────────────────────────────────────────────────────────────────────────┘
///
/// Setup Prefab:
///   • Rigidbody2D  → Body Type = Kinematic, Gravity Scale = 0
///   • BoxCollider2D → Is Trigger = true (điều chỉnh kích thước theo sprite)
///   • Animator     → clip Effect1 (loop = false)
///     └─ Animation Event tại frame cuối: OnEffectEnd()
///
/// Gọi Setup() ngay sau Instantiate từ KisameController.AnimationEvent_SpawnWaterSlash().
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(Animator))]
public class WaterSlashArea : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────────
    //  INSPECTOR FIELDS
    // ─────────────────────────────────────────────────────────────────────────────
    [Header("─── DAMAGE CONFIG ───")]
    [Tooltip("Sát thương gây ra khi kẻ địch bước vào vùng hiệu ứng. " +
             "Kiểu thường (isHeavyAttack = false) → không gây Knockback.")]
    [SerializeField] private float damage = 25f;

    [Header("─── LIFETIME ───")]
    [Tooltip("Thời gian sống fallback (giây) – tự hủy dù Animation Event không được gọi.\n" +
             "Công thức ước tính: số frame clip Effect1 ÷ FPS.\n" +
             "Ví dụ: 9 frame ÷ 12fps ≈ 0.75s → đặt 0.8f để dư 1 frame.")]
    [SerializeField] private float fallbackLifetime = 0.8f;

    // ─────────────────────────────────────────────────────────────────────────────
    //  PRIVATE FIELDS  (cache – không GetComponent trong Update/Collision)
    // ─────────────────────────────────────────────────────────────────────────────
    private GameObject _owner;
    private LayerMask  _targetLayer;

    /// <summary>
    /// Chống hit nhiều lần:
    ///   • false = chưa ai bị trúng → Trigger mở.
    ///   • true  = đã có 1 target bị trúng → khoá lại (AoE 1 hit duy nhất).
    /// Nếu muốn AoE trúng NHIỀU địch cùng lúc, bỏ điều kiện _alreadyHit
    /// và thay bằng HashSet để chặn cùng một target bị hit 2 lần.
    /// </summary>
    private bool _alreadyHit = false;

    // ─────────────────────────────────────────────────────────────────────────────
    //  PUBLIC API
    // ─────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Khởi tạo vùng sóng nước. Gọi ngay sau Instantiate.
    /// Không dùng Awake/Start để tránh race condition với Instantiate.
    /// </summary>
    /// <param name="facingDir">Hướng nhân vật (1 = phải, -1 = trái) – dùng để lật sprite.</param>
    /// <param name="owner">GameObject của Kisame – bỏ qua va chạm với chính mình.</param>
    /// <param name="targetLayer">Layer của kẻ địch cần gây sát thương.</param>
    public void Setup(float facingDir, GameObject owner, LayerMask targetLayer)
    {
        _owner       = owner;
        _targetLayer = targetLayer;

        // Lật sprite theo hướng Kisame đang nhìn
        if (facingDir < 0f)
        {
            Vector3 s = transform.localScale;
            transform.localScale = new Vector3(-Mathf.Abs(s.x), s.y, 1f);
        }

        // Đứng HOÀN TOÀN yên tại chỗ
        var rb            = GetComponent<Rigidbody2D>(); // GetComponent CHỈ 1 lần trong Setup
        rb.bodyType       = RigidbodyType2D.Kinematic;
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale   = 0f;

        // Fallback tự hủy (đề phòng Animation Event không được gán clip)
        Destroy(gameObject, fallbackLifetime);
    }

    // ─────────────────────────────────────────────────────────────────────────────
    //  COLLISION – ONE-TIME HIT
    // ─────────────────────────────────────────────────────────────────────────────

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Kiểm tra: chưa hit ai, không phải owner, thuộc đúng layer địch
        if (_alreadyHit) return;
        if (_owner != null && other.gameObject == _owner) return;
        if (((1 << other.gameObject.layer) & _targetLayer.value) == 0) return;

        _alreadyHit = true; // Khoá – chỉ hit 1 target (thay bằng HashSet nếu muốn multi-hit)

        IDamageable damageable = other.GetComponent<IDamageable>();
        damageable?.TakeDamage(damage, transform.position.x, isHeavyAttack: false);

        // Không Destroy ngay để hiệu ứng visual tiếp tục phát.
        // OnEffectEnd() sẽ Destroy sau khi animation kết thúc.
    }

    // ─────────────────────────────────────────────────────────────────────────────
    //  ANIMATION EVENT  (gán trên clip Effect1 – frame CUỐI cùng)
    // ─────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Gán vào Animation Event ở FRAME CUỐI của clip Effect1 trên Prefab WaterSlash.
    /// Tên hàm dùng trong Animation Event string: "OnEffectEnd"
    /// </summary>
    public void OnEffectEnd()
    {
        Destroy(gameObject);
    }
}

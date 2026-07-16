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

    private void Awake()
    {
        var col = GetComponent<BoxCollider2D>();
        if (col != null)
        {
            col.isTrigger = true;
            Debug.Log($"[WaterSlash] Awake: BoxCollider2D.isTrigger set to true. Size={col.size}, Offset={col.offset}");
        }
        else
        {
            Debug.LogWarning("[WaterSlash] Awake: BoxCollider2D component not found!");
        }
    }

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
        
        Debug.Log($"[WaterSlash] Setup: Owner={(_owner != null ? _owner.name : "Null")}, TargetLayer={_targetLayer.value}");

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

        // Chạy kiểm tra va chạm ngay lập tức đề phòng đối thủ đã đứng sẵn ở vị trí đó
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        if (col != null)
        {
            Vector2 checkCenter = (Vector2)transform.position + col.offset;
            Vector2 checkSize = new Vector2(col.size.x * Mathf.Abs(transform.localScale.x), col.size.y * transform.localScale.y);
            Collider2D[] hits = Physics2D.OverlapBoxAll(checkCenter, checkSize, 0f, _targetLayer);
            Debug.Log($"[WaterSlash] OverlapBox check at {checkCenter} with size {checkSize}. Hits found: {hits.Length}");
            foreach (var hit in hits)
            {
                if (hit != null && hit.gameObject != _owner)
                {
                    Debug.Log($"[WaterSlash] OverlapBox hit: {hit.gameObject.name}");
                    TryApplyDamage(hit);
                }
            }
        }

        // Fallback tự hủy (đề phòng Animation Event không được gán clip)
        Destroy(gameObject, fallbackLifetime);
    }

    // ─────────────────────────────────────────────────────────────────────────────
    //  COLLISION – ONE-TIME HIT
    // ─────────────────────────────────────────────────────────────────────────────

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"[WaterSlash] OnTriggerEnter2D with {other.gameObject.name}");
        TryApplyDamage(other);
    }

    private void TryApplyDamage(Collider2D other)
    {
        if (other == null) return;
        
        bool isOwner = (_owner != null && other.gameObject == _owner);
        bool isLayerMatch = (((1 << other.gameObject.layer) & _targetLayer.value) != 0);
        
        Debug.Log($"[WaterSlash] TryApplyDamage with {other.gameObject.name}: alreadyHit={_alreadyHit}, isOwner={isOwner}, layerMatch={isLayerMatch} (layer={other.gameObject.layer}, mask={_targetLayer.value})");

        // Kiểm tra: chưa hit ai, không phải owner, thuộc đúng layer địch
        if (_alreadyHit) return;
        if (isOwner) return;
        if (!isLayerMatch) return;

        IDamageable damageable = other.GetComponent<IDamageable>();
        Debug.Log($"[WaterSlash] IDamageable found: {damageable != null}");
        
        if (damageable != null)
        {
            _alreadyHit = true; // Khóa sát thương CHỈ KHI thực sự gây sát thương lên mục tiêu hợp lệ!
            
            damageable.TakeDamage(damage, transform.position.x, isHeavyAttack: false);
            Debug.Log($"[WaterSlash] Applied {damage} damage to {other.gameObject.name}!");
            
            if (_owner != null)
            {
                FighterBase fb = _owner.GetComponent<FighterBase>();
                if (fb != null)
                {
                    fb.GainManaOnRangedHit();
                }
            }
        }
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

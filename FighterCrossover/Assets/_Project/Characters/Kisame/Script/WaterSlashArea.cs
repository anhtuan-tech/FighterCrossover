using UnityEngine;

/// <summary>
/// Thủy Độn – Trảm Sóng Nước (Skill 1 của Kisame)
/// 
/// Cơ chế: Close-range AoE đứng yên. KHÔNG bay đi.
///   - Xuất hiện ngay tại attackHitbox phía trước Kisame khi Animation Event gọi.
///   - Phát hoạt ảnh Effect1 tại chỗ, gây sát thương mọi kẻ địch dẫm phải (OnTriggerEnter2D).
///   - Tự hủy sau khi hoạt ảnh kết thúc (dùng Animator Event "OnEffectEnd" hoặc fallback Destroy timer).
/// 
/// Yêu cầu Component: Rigidbody2D (Kinematic, gravityScale=0), BoxCollider2D (IsTrigger=true), Animator.
/// Gọi Setup() ngay sau Instantiate từ KisameController.AnimationEvent_SpawnWaterSlash().
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(Animator))]
public class WaterSlashArea : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────────
    //  INSPECTOR FIELDS
    // ─────────────────────────────────────────────────────────────────────────────
    [Header("─── DAMAGE CONFIG ───")]
    [Tooltip("Sát thương gây ra khi kẻ địch bước vào vùng hiệu ứng.")]
    [SerializeField] private float damage = 25f;

    [Header("─── LIFETIME ───")]
    [Tooltip("Thời gian sống fallback (giây) – tự hủy dù Animator Event không được gọi. " +
             "Khớp với độ dài clip Effect1 chia FPS. Ví dụ: 6 frame / 12fps = 0.5s.")]
    [SerializeField] private float fallbackLifetime = 0.8f;

    // ─────────────────────────────────────────────────────────────────────────────
    //  PRIVATE FIELDS  (cache – không GetComponent trong Update/Collision)
    // ─────────────────────────────────────────────────────────────────────────────
    private GameObject  _owner;
    private LayerMask   _targetLayer;
    private bool        _alreadyHit = false; // Chống hit hai lần cùng một frame

    // ─────────────────────────────────────────────────────────────────────────────
    //  PUBLIC API
    // ─────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Khởi tạo vùng sóng nước. Gọi ngay sau Instantiate.
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
            transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x),
                                               transform.localScale.y, 1f);

        // Đứng HOÀN TOÀN yên tại chỗ – Rigidbody Kinematic, velocity = 0
        var rb             = GetComponent<Rigidbody2D>();
        rb.bodyType        = RigidbodyType2D.Kinematic;
        rb.linearVelocity  = Vector2.zero;
        rb.gravityScale    = 0f;

        // Fallback tự hủy (phòng khi Animation Event "OnEffectEnd" không được gán)
        Destroy(gameObject, fallbackLifetime);
    }

    // ─────────────────────────────────────────────────────────────────────────────
    //  COLLISION
    // ─────────────────────────────────────────────────────────────────────────────

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_alreadyHit) return;
        if (_owner != null && other.gameObject == _owner) return;
        if (((1 << other.gameObject.layer) & _targetLayer.value) == 0) return;

        _alreadyHit = true;

        var damageable = other.GetComponent<IDamageable>();
        damageable?.TakeDamage(damage, transform.position.x, isHeavyAttack: false);

        // Hủy ngay sau khi trúng đích (hiệu ứng visual vẫn phát thêm 1 tick)
        // Không Destroy ngay để tránh abort hiệu ứng; gọi qua Animator Event thay thế.
        // Nếu muốn hủy ngay: Destroy(gameObject);
    }

    // ─────────────────────────────────────────────────────────────────────────────
    //  ANIMATOR EVENT  (gán trên clip Effect1 – frame cuối cùng)
    // ─────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Gán vào Animation Event ở FRAME CUỐI của clip Effect1 trên Prefab WaterSlash.
    /// Gọi hàm này bằng tên "OnEffectEnd" trong Animation Event string.
    /// </summary>
    public void OnEffectEnd()
    {
        Destroy(gameObject);
    }
}

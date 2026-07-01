using UnityEngine;

/// <summary>
/// Luồng sóng nước – Skill 1 của Kisame.
/// Yêu cầu Component: Rigidbody2D (Kinematic), BoxCollider2D (IsTrigger = true), SpriteRenderer, Animator.
/// Gọi Setup() ngay sau khi Instantiate để khởi tạo hướng và owner.
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
public class WaterSlashProjectile : MonoBehaviour
{
    // ────────────────────────────────────────────────────────────────────────────
    //  INSPECTOR FIELDS
    // ────────────────────────────────────────────────────────────────────────────
    [Header("─── PROJECTILE CONFIG ───")]
    [Tooltip("Tốc độ bay (units/second).")]
    [SerializeField] private float speed = 14f;

    [Tooltip("Sát thương gây ra khi trúng mục tiêu.")]
    [SerializeField] private float damage = 20f;

    [Tooltip("Thời gian sống tối đa (giây) trước khi tự hủy dù không trúng ai.")]
    [SerializeField] private float lifetime = 2.5f;

    // ────────────────────────────────────────────────────────────────────────────
    //  PRIVATE FIELDS
    // ────────────────────────────────────────────────────────────────────────────
    private Rigidbody2D _rb;
    private Vector2     _direction;
    private GameObject  _owner;
    private LayerMask   _targetLayer;
    private bool        _hasHit = false; // Chống double-hit

    // ────────────────────────────────────────────────────────────────────────────
    //  PUBLIC API
    // ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Khởi tạo đạn ngay sau khi Instantiate.
    /// </summary>
    /// <param name="direction">Vector2 hướng bay (đã normalize).</param>
    /// <param name="owner">GameObject của caster – bỏ qua va chạm với object này.</param>
    /// <param name="targetLayer">Layer của mục tiêu cần gây sát thương.</param>
    public void Setup(Vector2 direction, GameObject owner, LayerMask targetLayer)
    {
        _rb          = GetComponent<Rigidbody2D>();
        _direction   = direction.normalized;
        _owner       = owner;
        _targetLayer = targetLayer;

        // Lật sprite theo hướng bay (đảm bảo SpriteRenderer mặc định quay phải)
        if (_direction.x < 0f)
            transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x),
                                                transform.localScale.y, 1f);

        // Kinematic Rigidbody2D di chuyển bằng velocity (không phụ thuộc gravity)
        _rb.linearVelocity = _direction * speed;

        // Tự hủy sau thời gian sống tối đa
        Destroy(gameObject, lifetime);
    }

    // ────────────────────────────────────────────────────────────────────────────
    //  COLLISION
    // ────────────────────────────────────────────────────────────────────────────
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_hasHit) return;                                              // Đã trúng rồi, bỏ qua
        if (_owner  != null && other.gameObject == _owner) return;        // Bỏ qua owner
        if (((1 << other.gameObject.layer) & _targetLayer.value) == 0)   // Không thuộc targetLayer
            return;

        _hasHit = true; // Đánh dấu đã xử lý, chống callback nhiều lần

        // Gây sát thương
        var damageable = other.GetComponent<IDamageable>();
        damageable?.TakeDamage(damage, transform.position.x);

        // Hủy ngay sau khi trúng đích
        Destroy(gameObject);
    }
}

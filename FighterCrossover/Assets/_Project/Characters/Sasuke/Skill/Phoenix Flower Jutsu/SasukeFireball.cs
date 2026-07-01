using UnityEngine;

/// <summary>
/// Điều khiển viên đạn tầm xa (Fireball - Hỏa Độn) của Sasuke.
/// Tên class: SasukeFireball (tránh conflict với RangedProjectile của Ichigo).
/// Yêu cầu: Rigidbody2D (Dynamic, GravityScale=0, Continuous), CircleCollider2D (IsTrigger=true).
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
public class SasukeFireball : MonoBehaviour
{
    // ─── Inspector Config ───────────────────────────────────────────────────────
    [Header("--- FIREBALL CONFIG ---")]
    [Tooltip("Tốc độ bay của viên đạn (units/second).")]
    [SerializeField] private float speed = 7f;

    [Tooltip("Sát thương gây ra khi chạm mục tiêu.")]
    [SerializeField] private float damage = 30f;

    [Tooltip("Thời gian tồn tại tối đa trước khi tự hủy (giây).")]
    [SerializeField] private float maxLifetime = 3f;

    // ─── Runtime Data ────────────────────────────────────────────────────────────
    private Rigidbody2D rb;
    private Vector2     travelDirection;
    private GameObject  ownerObject;
    private LayerMask   hitLayer;
    private bool        hasHit = false; // Guard flag chống xử lý double-hit

    // ─── Lifecycle ───────────────────────────────────────────────────────────────
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        rb.gravityScale           = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.freezeRotation         = true;
    }

    // ─── Public API ──────────────────────────────────────────────────────────────
    /// <summary>
    /// Khởi tạo fireball từ SasukeController. Gọi ngay sau Instantiate.
    /// </summary>
    /// <param name="direction">Hướng bay, vd: Vector2(1,0) hoặc (-1,0).</param>
    /// <param name="owner">GameObject của Sasuke — bị bỏ qua khi va chạm.</param>
    /// <param name="targetLayer">LayerMask của đối tượng bị sát thương.</param>
    public void Setup(Vector2 direction, GameObject owner, LayerMask targetLayer)
    {
        travelDirection = direction.normalized;
        ownerObject     = owner;
        hitLayer        = targetLayer;

        rb.linearVelocity = travelDirection * speed;

        // Lật sprite đúng hướng bay
        if (direction.x < 0f)
            transform.localScale = new Vector3(-1f, 1f, 1f);

        // Self-destruct khi bay ra ngoài màn hình
        Destroy(gameObject, maxLifetime);
    }

    // ─── Collision ───────────────────────────────────────────────────────────────
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHit) return;
        if (ownerObject != null && other.gameObject == ownerObject) return;
        if (((1 << other.gameObject.layer) & hitLayer.value) == 0) return;

        hasHit = true;
        rb.linearVelocity = Vector2.zero;

        IDamageable damageable = other.GetComponent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(damage, transform.position.x, isHeavyAttack: false);
        }

        Destroy(gameObject);
    }
}

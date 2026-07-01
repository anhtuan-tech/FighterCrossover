using UnityEngine;

/// <summary>
/// Hitbox kéo dài cho kỹ năng Ultimate Chidori của Sasuke.
/// Tên class: SasukeChidoriEffect (tránh conflict với UltimateEffect của Ichigo).
/// Gây sát thương liên tục (với cooldown per-target) cho mục tiêu trong vùng collider.
/// Tự hủy sau duration giây.
/// Yêu cầu: BoxCollider2D (IsTrigger=true).
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class SasukeChidoriEffect : MonoBehaviour
{
    // ─── Inspector Config ───────────────────────────────────────────────────────
    [Header("--- CHIDORI EFFECT CONFIG ---")]
    [Tooltip("Sát thương mỗi lần tick.")]
    [SerializeField] private float damage = 20f;

    [Tooltip("Thời gian tối thiểu giữa 2 lần gây sát thương cho cùng 1 mục tiêu (giây).")]
    [SerializeField] private float damageCooldown = 0.25f;

    // ─── Runtime Data ────────────────────────────────────────────────────────────
    private GameObject caster;
    private LayerMask  targetLayer;

    // Per-target cooldown — tránh spam damage
    private System.Collections.Generic.Dictionary<Collider2D, float> lastHitTime
        = new System.Collections.Generic.Dictionary<Collider2D, float>();

    // ─── Public API ──────────────────────────────────────────────────────────────
    /// <summary>
    /// Khởi tạo Chidori effect từ SasukeController.
    /// </summary>
    /// <param name="casterObj">GameObject của Sasuke — bị bỏ qua khi va chạm.</param>
    /// <param name="layer">LayerMask đối tượng bị sát thương.</param>
    /// <param name="duration">Thời gian tồn tại của effect (giây).</param>
    public void Setup(GameObject casterObj, LayerMask layer, float duration)
    {
        caster      = casterObj;
        targetLayer = layer;

        // Lật đúng hướng theo caster
        if (casterObj != null)
        {
            float facingDir = casterObj.transform.localScale.x;
            transform.localScale = new Vector3(facingDir, 1f, 1f);
        }

        Destroy(gameObject, duration);
    }

    // ─── Follow Caster ───────────────────────────────────────────────────────────
    // Effect bám theo Sasuke trong suốt thời gian tồn tại để tạo hiệu ứng "kéo lê tia sét"
    private void Update()
    {
        if (caster == null)
        {
            Destroy(gameObject);
            return;
        }

        // Bám theo vị trí caster + cập nhật hướng nếu Sasuke flip
        transform.position = caster.transform.position;
        float facingDir = caster.transform.localScale.x;
        transform.localScale = new Vector3(facingDir, 1f, 1f);
    }

    // ─── Collision ───────────────────────────────────────────────────────────────
    private void OnTriggerStay2D(Collider2D other)
    {
        if (caster != null && other.gameObject == caster) return;
        if (((1 << other.gameObject.layer) & targetLayer.value) == 0) return;

        float now = Time.time;
        if (lastHitTime.TryGetValue(other, out float lastTime) && now - lastTime < damageCooldown)
            return;

        lastHitTime[other] = now;

        IDamageable damageable = other.GetComponent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(damage, transform.position.x, isHeavyAttack: true);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        lastHitTime.Remove(other);
    }
}

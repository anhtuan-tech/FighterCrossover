using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Cá Mập Nước khổng lồ – Skill 2 (Ultimate) của Kisame.
///
/// Pha 1 (Trồi lên): Cá mập đứng yên, đóng băng trong N giây đầu
///                   để hoạt ảnh "nước trồi" phát hết trước khi lao đi.
/// Pha 2 (Lao đi):   Rigidbody2D được cấp velocity và cá mập lao về phía trước.
///                   Multi-hit với cooldown per-target, là Heavy Attack (gây knockback).
///
/// Yêu cầu Component: Rigidbody2D (Kinematic), BoxCollider2D (IsTrigger, bao phủ toàn thân), Animator.
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
public class SharkUltimate : MonoBehaviour
{
    // ────────────────────────────────────────────────────────────────────────────
    //  INSPECTOR FIELDS
    // ────────────────────────────────────────────────────────────────────────────
    [Header("─── SHARK CONFIG ───")]
    [Tooltip("Thời gian (giây) đứng yên ở Pha 1 (hoạt ảnh nước trồi lên). " +
             "Điều chỉnh theo số frame của animation 'trồi lên' ở FPS clip.")]
    [SerializeField] private float riseDelay = 0.8f;

    [Tooltip("Tốc độ lao đi ở Pha 2 (units/second).")]
    [SerializeField] private float chargeSpeed = 18f;

    [Tooltip("Thời gian sống tối đa của Pha 2 (giây) trước khi tự hủy.")]
    [SerializeField] private float chargeLifetime = 2.5f;

    [Header("─── DAMAGE ───")]
    [Tooltip("Sát thương mỗi lần hit (Heavy – gây knockback).")]
    [SerializeField] private float damage = 40f;

    [Tooltip("Khoảng thời gian tối thiểu giữa 2 lần hit cùng một mục tiêu (giây). " +
             "Cá mập to nên cần cooldown để tránh spam damage trong 1 tick.")]
    [SerializeField] private float hitCooldown = 0.4f;

    // ────────────────────────────────────────────────────────────────────────────
    //  PRIVATE FIELDS
    // ────────────────────────────────────────────────────────────────────────────
    private Rigidbody2D  _rb;
    private Animator     _anim;
    private Vector2      _direction;
    private GameObject   _owner;
    private LayerMask    _targetLayer;

    // Dictionary theo dõi thời điểm hit cuối cho từng collider (tránh GC – reuse giữa các frame)
    private readonly Dictionary<Collider2D, float> _lastHitTime = new Dictionary<Collider2D, float>();

    // Animator parameter hash (tối ưu – không boxing string mỗi frame)
    private static readonly int HashCharge = Animator.StringToHash("Charge");

    // ────────────────────────────────────────────────────────────────────────────
    //  PUBLIC API
    // ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Khởi tạo cá mập ngay sau khi Instantiate.
    /// </summary>
    public void Setup(Vector2 direction, GameObject owner, LayerMask targetLayer)
    {
        _rb          = GetComponent<Rigidbody2D>();
        _anim        = GetComponent<Animator>();
        _direction   = direction.normalized;
        _owner       = owner;
        _targetLayer = targetLayer;

        // Lật sprite theo hướng lao (sprite mặc định phải hướng phải)
        if (_direction.x < 0f)
            transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x),
                                                transform.localScale.y, 1f);

        // Bắt đầu luồng 2 pha
        StartCoroutine(SharkSequence());
    }

    // ────────────────────────────────────────────────────────────────────────────
    //  SEQUENCE (2 PHASES)
    // ────────────────────────────────────────────────────────────────────────────
    private IEnumerator SharkSequence()
    {
        // ── Pha 1: Đứng yên, chờ hoạt ảnh nước trồi lên ───────────────────────
        _rb.linearVelocity = Vector2.zero;

        yield return new WaitForSeconds(riseDelay);

        // ── Pha 2: Lao đi ──────────────────────────────────────────────────────
        // Báo cho Animator chuyển sang trạng thái bơi / lao
        _anim?.SetTrigger(HashCharge);

        _rb.linearVelocity = _direction * chargeSpeed;

        // Tự hủy sau khi đã lao hết quãng đường hoặc timeout
        Destroy(gameObject, chargeLifetime);
    }

    // ────────────────────────────────────────────────────────────────────────────
    //  COLLISION – MULTI-HIT
    // ────────────────────────────────────────────────────────────────────────────

    private void OnTriggerEnter2D(Collider2D other)
    {
        ProcessHit(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        // OnTriggerStay đảm bảo target nằm trong vùng cá mập vẫn bị hit theo cooldown
        ProcessHit(other);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        // Dọn dẹp entry khi target thoát ra – tránh rò rỉ bộ nhớ trong Dictionary
        _lastHitTime.Remove(other);
    }

    private void ProcessHit(Collider2D other)
    {
        if (_owner != null && other.gameObject == _owner) return;   // Bỏ qua owner
        if (((1 << other.gameObject.layer) & _targetLayer.value) == 0) return; // Không đúng layer

        // Kiểm tra cooldown per-target
        float now = Time.time;
        if (_lastHitTime.TryGetValue(other, out float last) && now - last < hitCooldown)
            return;

        _lastHitTime[other] = now;

        // Cá mập là Heavy Attack → gây knockback
        other.GetComponent<IDamageable>()?.TakeDamage(damage, transform.position.x, isHeavyAttack: true);
    }
}

using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
public class KonanPaperShuriken : MonoBehaviour
{
    [SerializeField] private float speed = 8f;
    [SerializeField] private float damage = 25f;
    [SerializeField] private float maxLifetime = 3f;

    private Rigidbody2D rb;
    private Vector2     travelDirection;
    private GameObject  ownerObject;
    private LayerMask   hitLayer;
    private bool        hasHit = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale           = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.freezeRotation         = true;
    }

    public void SetStats(float customSpeed, float customDamage)
    {
        this.speed = customSpeed;
        this.damage = customDamage;
    }

    public void Setup(Vector2 direction, GameObject owner, LayerMask targetLayer)
    {
        travelDirection = direction.normalized;
        ownerObject     = owner;
        hitLayer        = targetLayer;

        rb.linearVelocity = travelDirection * speed;

        if (direction.x < 0f)
            transform.localScale = new Vector3(-1f, 1f, 1f);

        Destroy(gameObject, maxLifetime);
    }

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
            
            // Hoi mana cho Konan khi trung
            var controller = ownerObject.GetComponent<FighterBase>();
            if (controller != null)
            {
                controller.GainManaOnRangedHit();
            }
        }

        Destroy(gameObject);
    }
}

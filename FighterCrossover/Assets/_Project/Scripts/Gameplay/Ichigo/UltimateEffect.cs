using UnityEngine;

public class UltimateEffect : MonoBehaviour
{
    [Header("--- ULTIMATE EFFECT SETTINGS ---")]
    public float spinSpeed = 540f;
    public float damagePerTick = 15f;
    public float tickInterval = 0.4f;
    public float radius = 3.0f;
    public float lifetime = 6.0f;
    public LayerMask targetLayer;

    private GameObject owner;
    private float nextTickTime;

    public void Setup(GameObject owner, LayerMask targetLayer, float duration)
    {
        this.owner = owner;
        this.targetLayer = targetLayer;
        this.lifetime = duration;
        
        // Ensure the effect matches the lifetime of the ultimate skill duration
        Destroy(gameObject, lifetime);
    }

    private void Start()
    {
        nextTickTime = Time.time;
    }

    private void Update()
    {
        // If owner is destroyed, clean up the effect
        if (owner == null)
        {
            Destroy(gameObject);
            return;
        }

        // Lock to owner's position
        transform.position = owner.transform.position;

        // Spin around Z-axis
        transform.Rotate(0f, 0f, spinSpeed * Time.deltaTime);

        // Apply periodic damage ticks
        if (Time.time >= nextTickTime)
        {
            nextTickTime = Time.time + tickInterval;
            DealDamage();
        }
    }

    private void DealDamage()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius, targetLayer);
        foreach (var hit in hits)
        {
            if (hit.gameObject == owner) continue;

            IDamageable damageable = hit.GetComponent<IDamageable>();
            if (damageable != null)
            {
                // Deal tick damage to targets
                damageable.TakeDamage(damagePerTick, transform.position.x, false);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.8f, 0.2f, 0.8f, 0.4f);
        Gizmos.DrawSphere(transform.position, radius);
    }
}

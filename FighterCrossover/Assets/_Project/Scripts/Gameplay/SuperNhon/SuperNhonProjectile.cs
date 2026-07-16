using UnityEngine;

public class SuperNhonProjectile : MonoBehaviour
{
    private Vector2 direction;
    private GameObject owner;
    private LayerMask targetLayer;
    private float speed;
    private float damage;

    public void Setup(Vector2 dir, GameObject owner, LayerMask targetLayer, float speed, float damage, float lifetime)
    {
        this.direction = dir.normalized;
        this.owner = owner;
        this.targetLayer = targetLayer;
        this.speed = speed;
        this.damage = damage;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        transform.position += (Vector3)(direction * speed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (((1 << collision.gameObject.layer) & targetLayer) != 0)
        {
            if (collision.gameObject == owner) return;

            IDamageable damageable = collision.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(damage, transform.position.x, false);

                FighterBase ownerFighter = owner != null ? owner.GetComponent<FighterBase>() : null;
                if (ownerFighter != null) ownerFighter.GainManaOnRangedHit();

                Destroy(gameObject);
            }
        }
    }
}

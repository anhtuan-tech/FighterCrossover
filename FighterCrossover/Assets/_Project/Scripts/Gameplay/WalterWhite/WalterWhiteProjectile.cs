using UnityEngine;

public class WalterWhiteProjectile : MonoBehaviour
{
    private Vector2 direction;
    private GameObject owner;
    private LayerMask targetLayer;
    private float speed = 12f;
    private float damage = 1f;

    private SpriteRenderer sr;
    private Rigidbody2D rb;
    private CircleCollider2D col;

    private void Awake()
    {
        sr = gameObject.AddComponent<SpriteRenderer>();
        rb = gameObject.AddComponent<Rigidbody2D>();
        col = gameObject.AddComponent<CircleCollider2D>();

        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        col.isTrigger = true;
        col.radius = 0.06f;

        transform.localScale = new Vector3(0.4f, 0.4f, 1f);

        // Generate a clean circular sprite programmatically
        sr.sprite = CreateCircleSprite();
        sr.color = Color.black; // Small black bullet
    }

    public void Setup(Vector2 dir, GameObject owner, LayerMask targetLayer, float speed, float damage, float lifetime)
    {
        this.direction = dir.normalized;
        this.owner = owner;
        this.targetLayer = targetLayer;
        this.speed = speed;
        this.damage = damage;

        rb.linearVelocity = direction * speed;
        Destroy(gameObject, lifetime);
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
                if (ownerFighter != null)
                {
                    // Shoot 10 bullets, so each bullet recovers 2% mana (totalling 20%)
                    ownerFighter.GainMana(2f);
                }

                Destroy(gameObject);
            }
        }
    }

    private Sprite CreateCircleSprite()
    {
        int size = 32;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        
        float center = size / 2f;
        float radius = size / 2f - 1f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float dy = y - center;
                if (dx * dx + dy * dy <= radius * radius)
                {
                    texture.SetPixel(x, y, Color.white);
                }
                else
                {
                    texture.SetPixel(x, y, Color.clear);
                }
            }
        }
        texture.Apply();

        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }
}

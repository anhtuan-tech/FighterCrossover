using UnityEngine;

public class PeterGriffinProjectile : MonoBehaviour
{
    private Vector2 direction;
    private GameObject owner;
    private LayerMask targetLayer;
    private float speed;
    private float damage;

    [Header("--- ANIMATION CONFIG ---")]
    public float frameRate = 12f;
    private Sprite[] animFrames;
    private int currentFrame = 0;
    private float animTimer = 0f;
    private SpriteRenderer sr;
    private Rigidbody2D rb;

    public void Setup(Vector2 dir, GameObject owner, LayerMask targetLayer, float speed, float damage, float lifetime)
    {
        this.direction = dir.normalized;
        this.owner = owner;
        this.targetLayer = targetLayer;
        this.speed = speed;
        this.damage = damage;

        transform.rotation = Quaternion.identity;
        transform.localScale = new Vector3(direction.x * Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);

        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.linearVelocity = direction * speed;
            rb.useFullKinematicContacts = false;
        }

        sr = GetComponent<SpriteRenderer>();
        var controller = owner != null ? owner.GetComponent<PeterGriffinController>() : null;
        if (controller != null)
        {
            animFrames = new Sprite[10];
            for (int i = 0; i < 10; i++)
            {
                animFrames[i] = controller.LoadPeterGriffinSprite($"ProjectileRanged_{i}");
            }
        }

        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        if (rb == null)
        {
            transform.position += (Vector3)(direction * speed * Time.deltaTime);
        }

        // Chạy hoạt ảnh projectile từ frame 0 đến 9
        if (sr != null && animFrames != null && animFrames.Length > 0)
        {
            if (currentFrame < animFrames.Length - 1)
            {
                animTimer += Time.deltaTime;
                if (animTimer >= 1f / frameRate)
                {
                    animTimer -= 1f / frameRate;
                    currentFrame++;
                    sr.sprite = animFrames[currentFrame];
                }
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        HandleCollision(collision.collider);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        HandleCollision(collision);
    }

    private void HandleCollision(Collider2D collision)
    {
        if (((1 << collision.gameObject.layer) & targetLayer) != 0)
        {
            if (collision.gameObject == owner) return;

            IDamageable damageable = collision.GetComponent<IDamageable>();
            if (damageable == null)
            {
                // Thử tìm ở parent phòng trường hợp collider nằm ở GameObject con
                damageable = collision.GetComponentInParent<IDamageable>();
            }

            if (damageable != null)
            {
                // Lấy tọa độ X của người bắn (owner), nếu owner null thì dùng của đạn
                float attackerX = (owner != null) ? owner.transform.position.x : transform.position.x;

                // Truyền attackerX thay vì transform.position.x để tránh lỗi trùng tọa độ X
                damageable.TakeDamage(damage, attackerX, false);

                FighterBase ownerFighter = owner != null ? owner.GetComponent<FighterBase>() : null;
                if (ownerFighter != null) ownerFighter.GainManaOnRangedHit();

                Destroy(gameObject);
                SpawnExplosion();
            }
        }
    }

    private void SpawnExplosion()
    {
        GameObject expObj = new GameObject("PeterProjectileExplosion");
        expObj.transform.position = transform.position;
        expObj.transform.localScale = Vector3.one * 1.5f;

        var srExplosion = expObj.AddComponent<SpriteRenderer>();
        if (sr != null)
        {
            srExplosion.sortingLayerID = sr.sortingLayerID;
            srExplosion.sortingOrder = sr.sortingOrder + 1;
        }
        srExplosion.color = new Color(0.5f, 0.75f, 1f, 1f);

        var ownerController = owner != null ? owner.GetComponent<PeterGriffinController>() : null;
        if (ownerController != null)
        {
            srExplosion.sprite = ownerController.LoadPeterGriffinSprite("ProjectileRanged_9");
        }
        else if (sr != null)
        {
            srExplosion.sprite = sr.sprite;
        }

        expObj.AddComponent<PeterExplosionEffect>();
    }
}

public class PeterExplosionEffect : MonoBehaviour
{
    public float duration = 0.25f;
    private float elapsed = 0f;
    private SpriteRenderer sr;
    private Vector3 startScale;
    private Vector3 targetScale;

    private void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        startScale = transform.localScale;
        targetScale = startScale * 1.8f;
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        float t = elapsed / duration;
        if (t >= 1f)
        {
            Destroy(gameObject);
            return;
        }

        transform.localScale = Vector3.Lerp(startScale, targetScale, t);
        if (sr != null)
        {
            Color c = sr.color;
            c.a = Mathf.Lerp(1f, 0f, t);
            sr.color = c;
        }
    }
}
using UnityEngine;

public class BoaHancockProjectile : MonoBehaviour
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

    [Header("--- TRAIL EFFECT ---")]
    public float trailInterval = 0.05f;
    private float trailTimer = 0f;

    public void Setup(Vector2 dir, GameObject owner, LayerMask targetLayer, float speed, float damage, float lifetime)
    {
        this.direction = dir.normalized;
        this.owner = owner;
        this.targetLayer = targetLayer;
        this.speed = speed;
        this.damage = damage;

        // Keep the heart sprite upright and mirror it horizontally based on direction
        transform.rotation = Quaternion.identity;
        transform.localScale = new Vector3(direction.x * Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);

        // Configure Rigidbody2D for perfect straight-line kinematic movement
        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.linearVelocity = direction * speed;
            rb.useFullKinematicContacts = false;
        }

        // Load animation frames dynamically from owner
        sr = GetComponent<SpriteRenderer>();
        var controller = owner != null ? owner.GetComponent<BoaHancockController>() : null;
        if (controller != null)
        {
            animFrames = new Sprite[10];
            for (int i = 0; i < 10; i++)
            {
                animFrames[i] = controller.LoadBoaHancockSprite($"ProjectileRanged_{i}");
            }
        }

        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        // If Rigidbody2D is missing, fall back to manual translation
        if (rb == null)
        {
            transform.position += (Vector3)(direction * speed * Time.deltaTime);
        }

        // Projectile heart animation: plays from frame 0 to 9 once (small to large), then holds at frame 9
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
        
        // Spawn trail smoke particles
        trailTimer += Time.deltaTime;
        if (trailTimer >= trailInterval)
        {
            trailTimer = 0f;
            SpawnTrailSmoke();
        }
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

                // Hồi 20% mana cho người bắn khi tầm xa trúng
                FighterBase ownerFighter = owner != null ? owner.GetComponent<FighterBase>() : null;
                if (ownerFighter != null) ownerFighter.GainManaOnRangedHit();

                SpawnExplosion();
                Destroy(gameObject);
            }
        }
    }

    private void SpawnTrailSmoke()
    {
        GameObject smoke = new GameObject("TrailSmoke");
        smoke.transform.position = transform.position;
        // Random slight offset perpendicular to movement to give depth
        Vector2 perp = new Vector2(-direction.y, direction.x);
        smoke.transform.position += (Vector3)(perp * Random.Range(-0.15f, 0.15f));

        var srSmoke = smoke.AddComponent<SpriteRenderer>();
        // Set sorting layer same as projectile
        if (sr != null)
        {
            srSmoke.sortingLayerID = sr.sortingLayerID;
            srSmoke.sortingOrder = sr.sortingOrder - 1; // Slightly behind projectile
        }

        // Randomly select one of the trail sprites (ProjectileRanged_1 to _4)
        int smokeIndex = Random.Range(1, 5);
        var ownerController = owner != null ? owner.GetComponent<BoaHancockController>() : null;
        if (ownerController != null)
        {
            srSmoke.sprite = ownerController.LoadBoaHancockSprite($"ProjectileRanged_{smokeIndex}");
        }

        // Semi-transparent pink heart trail color
        srSmoke.color = new Color(1f, 0.7f, 0.85f, 0.6f);

        // Keep trail sprites upright and scale/rotate them slightly
        smoke.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(-15f, 15f));
        smoke.transform.localScale = Vector3.one * Random.Range(0.6f, 1.1f);

        // Attach fader script to handle lifetime, fading, and shrinking
        var fader = smoke.AddComponent<SmokeTrailFader>();
        fader.fadeSpeed = 4f; // fades out in 0.25s
    }

    private void SpawnExplosion()
    {
        GameObject expObj = new GameObject("ProjectileExplosion");
        expObj.transform.position = transform.position;
        expObj.transform.localScale = Vector3.one * 1.5f;

        var srExplosion = expObj.AddComponent<SpriteRenderer>();
        if (sr != null)
        {
            srExplosion.sortingLayerID = sr.sortingLayerID;
            srExplosion.sortingOrder = sr.sortingOrder + 1;
        }
        srExplosion.color = new Color(1f, 0.5f, 0.75f, 1f); // Rich pink color

        // Use the largest heart sprite for the explosion pop
        var ownerController = owner != null ? owner.GetComponent<BoaHancockController>() : null;
        if (ownerController != null)
        {
            srExplosion.sprite = ownerController.LoadBoaHancockSprite("ProjectileRanged_9");
        }
        else if (sr != null)
        {
            srExplosion.sprite = sr.sprite;
        }

        // Add custom smooth heart pop and fade explosion script
        expObj.AddComponent<HeartExplosionEffect>();
    }
}

/// <summary>
/// Animates a heart pop explosion by scaling it up and fading it out smoothly
/// </summary>
public class HeartExplosionEffect : MonoBehaviour
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

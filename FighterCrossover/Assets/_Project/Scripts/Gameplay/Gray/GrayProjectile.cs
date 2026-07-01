using UnityEngine;

public class GrayProjectile : MonoBehaviour
{
    private Vector2 direction;
    private GameObject owner;
    private LayerMask targetLayer;
    private float speed;
    private float damage;
    private float spinSpeed = 360f;

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

        // Face movement direction
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        transform.position += (Vector3)(direction * speed * Time.deltaTime);
        
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

        var sr = smoke.AddComponent<SpriteRenderer>();
        // Set sorting layer same as projectile
        var projSr = GetComponent<SpriteRenderer>();
        if (projSr != null)
        {
            sr.sortingLayerID = projSr.sortingLayerID;
            sr.sortingOrder = projSr.sortingOrder - 1; // Slightly behind projectile
        }

        // Randomly select one of the smoke sprites (sprite-17-37_1 to _4)
        int smokeIndex = Random.Range(1, 5);
        var ownerController = owner != null ? owner.GetComponent<GrayCharacterController>() : null;
        if (ownerController != null)
        {
            sr.sprite = ownerController.LoadGraySprite($"sprite-17-37_{smokeIndex}");
        }

        // Semi-transparent icy blue smoke color
        sr.color = new Color(0.85f, 0.95f, 1f, 0.5f);

        // Random scale and rotation for variety
        smoke.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
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

        var sr = expObj.AddComponent<SpriteRenderer>();
        var projSr = GetComponent<SpriteRenderer>();
        if (projSr != null)
        {
            sr.sortingLayerID = projSr.sortingLayerID;
            sr.sortingOrder = projSr.sortingOrder + 1;
        }
        sr.color = new Color(0.8f, 0.95f, 1f, 0.9f); // light icy blue tint

        var animator = expObj.AddComponent<SpriteExplosionEffect>();
        
        if (owner != null)
        {
            var ownerController = owner.GetComponent<GrayCharacterController>();
            if (ownerController != null)
            {
                animator.frames = new Sprite[]
                {
                    ownerController.LoadGraySprite("sprite-17-37_1"),
                    ownerController.LoadGraySprite("sprite-17-37_2"),
                    ownerController.LoadGraySprite("sprite-17-37_3"),
                    ownerController.LoadGraySprite("sprite-17-37_4")
                };
            }
        }
        animator.frameRate = 12f;
    }
}

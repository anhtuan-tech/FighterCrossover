using UnityEngine;

public class PeterGriffinUltimateProjectile : MonoBehaviour
{
    private Vector2 direction;
    private GameObject owner;
    private LayerMask targetLayer;
    private float speed;
    private float damage;
    private bool isInitialized = false;

    [Header("--- ANIMATION CONFIG ---")]
    [Tooltip("Tốc độ chuyển khung hình của Ultimate Projectile")]
    public float frameRate = 10f;
    private Sprite[] animFrames;
    private int currentFrame = 0;
    private float animTimer = 0f;
    private SpriteRenderer sr;
    private Rigidbody2D rb;

    [Header("--- SFX & VFX EFFECTS ---")]
    [Tooltip("Có kích hoạt rung màn hình khi Ultimate va chạm không?")]
    public bool shakeScreenOnHit = true;

    /// <summary>
    /// Khởi tạo các thông số vật lý, hướng bay và nạp danh sách Sprite Animation từ thư mục nhân vật
    /// </summary>
    public void Setup(Vector2 dir, GameObject owner, LayerMask targetLayer, float speed, float damage, float lifetime)
    {
        this.direction = dir.normalized;
        this.owner = owner;
        this.targetLayer = targetLayer;
        this.speed = speed;
        this.damage = damage;

        // Xoay đầu projectile theo hướng di chuyển (Lật Sprite theo trục X)
        transform.rotation = Quaternion.identity;
        transform.localScale = new Vector3(
            direction.x * Mathf.Abs(transform.localScale.x),
            transform.localScale.y,
            transform.localScale.z
        );

        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.linearVelocity = direction * speed;
            rb.useFullKinematicContacts = false;
        }

        sr = GetComponent<SpriteRenderer>();

        // Tự động tìm nạp chuỗi Sprite hoạt ảnh Ultimate Projectile (Từ UltiProjectile_0 đến UltiProjectile_9)
        var controller = owner != null ? owner.GetComponent<PeterGriffinController>() : null;
        if (controller != null)
        {
            animFrames = new Sprite[10];
            for (int i = 0; i < 10; i++)
            {
                animFrames[i] = controller.LoadPeterGriffinSprite($"UltiProjectile_{i}");
            }
        }

        isInitialized = true;
        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        if (!isInitialized) return;

        // Fallback di chuyển tịnh tiến nếu Object không có Rigidbody2D
        if (rb == null)
        {
            transform.position += (Vector3)(direction * speed * Time.deltaTime);
        }

        // Kiểm soát hoạt ảnh lặp hoặc dừng ở frame cuối cùng (Thường là frame vụ nổ hoành tráng)
        if (sr != null && animFrames != null && animFrames.Length > 0)
        {
            animTimer += Time.deltaTime;
            if (animTimer >= 1f / frameRate)
            {
                animTimer -= 1f / frameRate;
                currentFrame = (currentFrame + 1) % animFrames.Length;

                if (animFrames[currentFrame] != null)
                {
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
                // Lấy tọa độ X của người bắn (owner) để gửi đi
                float attackerX = (owner != null) ? owner.transform.position.x : transform.position.x;

                // Truyền attackerX vào hàm gây sát thương tối thượng
                damageable.TakeDamage(damage, attackerX, true);

                FighterBase ownerFighter = owner != null ? owner.GetComponent<FighterBase>() : null;
                if (ownerFighter != null)
                {
                    ownerFighter.GainManaOnRangedHit();
                }

                if (shakeScreenOnHit)
                {
                    CameraPlayShake();
                }

                SpawnUltimateExplosion();
                Destroy(gameObject);
            }
        }
    }

    /// <summary>
    /// Sinh vụ nổ phạm vi rộng siêu to khổng lồ khi chạm mục tiêu
    /// </summary>
    private void SpawnUltimateExplosion()
    {
        GameObject expObj = new GameObject("PeterUltimateExplosion");
        expObj.transform.position = transform.position;
        expObj.transform.localScale = Vector3.one * 3.0f; // Scale cực lớn x3 lần bình thường

        var srExplosion = expObj.AddComponent<SpriteRenderer>();
        if (sr != null)
        {
            srExplosion.sortingLayerID = sr.sortingLayerID;
            srExplosion.sortingOrder = sr.sortingOrder + 2;
        }

        srExplosion.color = new Color(1f, 0.6f, 0.1f, 1f); // Màu cam lửa rực cháy

        var ownerController = owner != null ? owner.GetComponent<PeterGriffinController>() : null;
        if (ownerController != null)
        {
            srExplosion.sprite = ownerController.LoadPeterGriffinSprite("UltiProjectile_9");
        }
        else if (sr != null)
        {
            srExplosion.sprite = sr.sprite;
        }

        expObj.AddComponent<PeterExplosionEffect>();
    }

    private void CameraPlayShake()
    {
        Debug.Log("[CAMERA] Ultimate Projectile Hit! Shake screen triggered.");
    }
}
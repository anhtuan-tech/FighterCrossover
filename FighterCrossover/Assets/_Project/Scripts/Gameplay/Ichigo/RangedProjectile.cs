using UnityEngine;

public class RangedProjectile : MonoBehaviour
{
    [Header("--- PROJECTILE SETTINGS ---")]
    public float speed = 12f;
    public float damage = 35f;
    public float lifetime = 2.5f;
    public float spinSpeed = 480f;
    public LayerMask targetLayer;

    private Vector2 moveDirection = Vector2.right;
    private GameObject owner;
    private Transform innerLayer;
    private Transform outerLayer;

    public void Setup(Vector2 direction, GameObject owner, LayerMask targetLayer)
    {
        this.moveDirection = direction.normalized;
        this.owner = owner;
        this.targetLayer = targetLayer;

        // Face the movement direction
        float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void Start()
    {
        Destroy(gameObject, lifetime);

        // Find double-layered children to spin in opposite directions
        if (transform.childCount >= 2)
        {
            innerLayer = transform.GetChild(0);
            outerLayer = transform.GetChild(1);
        }
        else if (transform.childCount == 1)
        {
            innerLayer = transform.GetChild(0);
        }
    }

    private void Update()
    {
        // Move forward in world space
        transform.position += (Vector3)(moveDirection * speed * Time.deltaTime);

        // Spin the layers in opposite directions to look dynamic
        if (innerLayer != null)
        {
            innerLayer.Rotate(0f, 0f, spinSpeed * Time.deltaTime);
        }
        if (outerLayer != null)
        {
            outerLayer.Rotate(0f, 0f, -spinSpeed * Time.deltaTime);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Check if collision object is part of the target layer and not the owner
        if (((1 << collision.gameObject.layer) & targetLayer) != 0)
        {
            if (collision.gameObject == owner) return;

            IDamageable damageable = collision.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(damage, transform.position.x, false);
                Destroy(gameObject);
            }
        }
    }
}

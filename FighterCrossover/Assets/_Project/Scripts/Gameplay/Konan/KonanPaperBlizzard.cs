using UnityEngine;

[RequireComponent(typeof(CircleCollider2D))]
public class KonanPaperBlizzard : MonoBehaviour
{
    private GameObject ownerObject;
    private LayerMask hitLayer;
    private float duration;
    private float damagePerTick = 10f;
    private float tickInterval = 0.4f;
    private float nextDamageTime;

    private void Awake()
    {
        var col = GetComponent<CircleCollider2D>();
        col.isTrigger = true;
    }

    private Vector2 travelDirection;
    [SerializeField] private float speed = 5f;

    public void Setup(GameObject owner, LayerMask target, float durationSeconds)
    {
        ownerObject = owner;
        hitLayer = target;
        duration = durationSeconds;

        float dir = owner.transform.localScale.x;
        travelDirection = new Vector2(dir, 0f);

        if (dir < 0f)
        {
            transform.localScale = new Vector3(-1f, 1f, 1f);
        }

        Destroy(gameObject, duration);
    }

    private void Update()
    {
        transform.Translate(travelDirection * speed * Time.deltaTime, Space.World);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (ownerObject != null && other.gameObject == ownerObject) return;
        if (((1 << other.gameObject.layer) & hitLayer.value) == 0) return;
        if (Time.time < nextDamageTime) return;

        IDamageable damageable = other.GetComponent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(damagePerTick, transform.position.x, isHeavyAttack: true);
            nextDamageTime = Time.time + tickInterval;
        }
    }
}

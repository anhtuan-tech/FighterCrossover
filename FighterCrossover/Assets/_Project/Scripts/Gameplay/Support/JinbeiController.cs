using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controls the Support Summon behavior for Jinbei.
/// Jinbei xuất hiện trước mặt nhân vật, chơi animation tấn công (Jinbei_SupportAttack),
/// gây sát thương cho đối thủ trong vùng tấn công, sau đó fade out và biến mất (tương tự Neji).
/// </summary>
public class JinbeiController : MonoBehaviour
{
    [Header("--- Visual References ---")]
    public GameObject jinbeiVisual;

    [Header("--- Cài Đặt Jinbei ---")]
    [SerializeField] private float damage           = 40f;
    [SerializeField] private float attackDuration   = 2.5f;  // thời gian giữ animation tấn công (hoạt ảnh dài 2.25s + 0.25s đứng hình)
    [SerializeField] private float fadeDuration     = 0.3f;   // thời gian fade out
    [SerializeField] private float spawnOffsetX     = 1.5f;   // khoảng cách Jinbei xuất hiện trước mặt
    [SerializeField] private Vector2 hitboxSize     = new Vector2(2f, 2f);
    [SerializeField] private Vector2 hitboxOffset   = new Vector2(0f, 0.5f);

    private FighterBase owner;
    private LayerMask targetLayer;
    private float direction;

    private SpriteRenderer jinbeiSR;
    private Animator jinbeiAnim;

    private List<IDamageable> hitTargets = new List<IDamageable>();

    public void Setup(FighterBase owner, int playerNum, LayerMask targetLayer, float facingDirection)
    {
        this.owner       = owner;
        this.targetLayer = targetLayer;
        this.direction   = facingDirection;

        // Cache components
        if (jinbeiVisual != null)
        {
            jinbeiSR   = jinbeiVisual.GetComponent<SpriteRenderer>();
            jinbeiAnim = jinbeiVisual.GetComponent<Animator>();
        }

        StartCoroutine(SummonRoutine());
    }

    private IEnumerator SummonRoutine()
    {
        // 1. Đặt vị trí Jinbei xuất hiện trước mặt nhân vật
        Vector3 spawnPos = owner.transform.position + new Vector3(direction * spawnOffsetX, 0.1f, 0f);
        transform.position = spawnPos;

        // 2. Hiện Jinbei, quay đúng hướng, phát animation tấn công
        if (visualEntityActive())
        {
            jinbeiVisual.transform.localScale = new Vector3(direction, 1f, 1f);
            jinbeiVisual.SetActive(true);

            if (jinbeiAnim != null)
                jinbeiAnim.Play("Jinbei_SupportAttack");
        }

        // 3. Phát hiện và gây sát thương trong suốt thời gian animation
        float elapsed = 0f;
        while (elapsed < attackDuration)
        {
            elapsed += Time.deltaTime;

            // Tính vị trí hitbox (offset theo hướng)
            Vector3 hitboxCenter = transform.position
                + new Vector3(direction * hitboxOffset.x, hitboxOffset.y, 0f);

            Collider2D[] hits = Physics2D.OverlapBoxAll(
                hitboxCenter,
                hitboxSize,
                0f,
                targetLayer);

            foreach (var hit in hits)
            {
                if (hit.gameObject == owner.gameObject) continue;
                IDamageable damageable = hit.GetComponent<IDamageable>();
                if (damageable != null && !hitTargets.Contains(damageable))
                {
                    hitTargets.Add(damageable);
                    damageable.TakeDamage(damage, transform.position.x, true);
                    Debug.Log($"[SupportJinbei] Jinbei hit {hit.gameObject.name} dealing {damage} damage!");
                }
            }

            yield return null;
        }

        // 4. Fade out Jinbei
        float fadeElapsed = 0f;
        while (fadeElapsed < fadeDuration)
        {
            fadeElapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, fadeElapsed / fadeDuration);
            if (jinbeiSR != null)
                jinbeiSR.color = new Color(1f, 1f, 1f, alpha);
            yield return null;
        }

        // 5. Destroy
        Destroy(gameObject);
    }

    private bool visualEntityActive()
    {
        return jinbeiVisual != null;
    }

    private void OnDrawGizmosSelected()
    {
        if (owner != null)
        {
            Gizmos.color = Color.blue;
            Vector3 hitboxCenter = transform.position
                + new Vector3(direction * hitboxOffset.x, hitboxOffset.y, 0f);
            Gizmos.DrawWireCube(hitboxCenter, hitboxSize);
        }
    }
}

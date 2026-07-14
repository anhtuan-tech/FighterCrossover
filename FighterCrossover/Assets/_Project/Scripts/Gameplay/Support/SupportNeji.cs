using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controls the Support Summon behavior for Neji.
/// Neji xuất hiện trước mặt nhân vật, chơi animation tấn công (Neji_SupportAttack),
/// gây sát thương cho đối thủ trong vùng tấn công, sau đó fade out và biến mất.
/// </summary>
public class SupportNeji : MonoBehaviour
{
    [Header("--- Visual References ---")]
    public GameObject nejiVisual;

    [Header("--- Cài Đặt Neji ---")]
    [SerializeField] private float damage           = 40f;
    [SerializeField] private float attackDuration   = 0.55f;  // thời gian giữ animation tấn công
    [SerializeField] private float fadeDuration     = 0.3f;   // thời gian fade out
    [SerializeField] private float spawnOffsetX     = 1.5f;   // khoảng cách Neji xuất hiện trước mặt
    [SerializeField] private Vector2 hitboxSize     = new Vector2(2f, 2f);
    [SerializeField] private Vector2 hitboxOffset   = new Vector2(0f, 0.5f);

    private FighterBase owner;
    private LayerMask targetLayer;
    private float direction;

    private SpriteRenderer nejiSR;
    private Animator nejiAnim;

    private List<IDamageable> hitTargets = new List<IDamageable>();

    public void Setup(FighterBase owner, int playerNum, LayerMask targetLayer, float facingDirection)
    {
        this.owner       = owner;
        this.targetLayer = targetLayer;
        this.direction   = facingDirection;

        // Cache components
        if (nejiVisual != null)
        {
            nejiSR   = nejiVisual.GetComponent<SpriteRenderer>();
            nejiAnim = nejiVisual.GetComponent<Animator>();
        }

        StartCoroutine(SummonRoutine());
    }

    private IEnumerator SummonRoutine()
    {
        // 1. Đặt vị trí Neji xuất hiện trước mặt nhân vật
        Vector3 spawnPos = owner.transform.position + new Vector3(direction * spawnOffsetX, 0f, 0f);
        transform.position = spawnPos;

        // 2. Hiện Neji, quay đúng hướng, phát animation tấn công
        if (nejiVisual != null)
        {
            nejiVisual.transform.localScale = new Vector3(direction, 1f, 1f);
            nejiVisual.SetActive(true);

            if (nejiAnim != null)
                nejiAnim.Play("Neji_SupportAttack");
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
                    Debug.Log($"[SupportNeji] Neji hit {hit.gameObject.name} dealing {damage} damage!");
                }
            }

            yield return null;
        }

        // 4. Fade out Neji
        float fadeElapsed = 0f;
        while (fadeElapsed < fadeDuration)
        {
            fadeElapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, fadeElapsed / fadeDuration);
            if (nejiSR != null)
                nejiSR.color = new Color(1f, 1f, 1f, alpha);
            yield return null;
        }

        // 5. Destroy
        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        if (owner != null)
        {
            Gizmos.color = Color.cyan;
            Vector3 hitboxCenter = transform.position
                + new Vector3(direction * hitboxOffset.x, hitboxOffset.y, 0f);
            Gizmos.DrawWireCube(hitboxCenter, hitboxSize);
        }
    }
}

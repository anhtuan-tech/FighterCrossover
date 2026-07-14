using System.Collections;
using UnityEngine;

public class SuperNhonUltimateSkill : MonoBehaviour
{
    [Header("--- ULTIMATE CONFIG ---")]
    public float detectionRadius = 3.5f;
    public float searchOffset = 1.0f;
    public float tickInterval = 0.25f;

    [Header("--- VISUAL EFFECTS ---")]
    public GameObject timeStopPrefab;
    public GameObject typewriterPrefab;

    public void SpawnUltimateCombo(SuperNhonController owner, LayerMask targetLayer)
    {
        float dir = owner.transform.localScale.x;
        Vector3 detectPos = owner.transform.position + new Vector3(dir * searchOffset, 0.5f, 0f);

        Collider2D[] hits = Physics2D.OverlapCircleAll(detectPos, detectionRadius, targetLayer);
        IDamageable hitTarget = null;
        
        foreach (var hit in hits)
        {
            if (hit.gameObject == owner.gameObject) continue;

            IDamageable damageable = hit.GetComponent<IDamageable>();
            if (damageable != null)
            {
                hitTarget = damageable;
                break;
            }
        }

        if (hitTarget != null)
        {
            Debug.Log($"[Ultimate] SuperNhon TimeStop hit target {((MonoBehaviour)hitTarget).gameObject.name}!");
            owner.StartCoroutine(UltimateComboRoutine(owner, hitTarget, dir));
        }
        else
        {
            Debug.Log("[Ultimate] SuperNhon Ultimate missed.");
            owner.Invoke("AnimationEvent_EndAttack", 0.3f);
        }
    }

    private IEnumerator UltimateComboRoutine(SuperNhonController owner, IDamageable target, float dir)
    {
        MonoBehaviour targetMono = target as MonoBehaviour;
        if (targetMono == null) yield break;

        Transform targetTransform = targetMono.transform;

        Rigidbody2D ownerRb = owner.GetComponent<Rigidbody2D>();
        if (ownerRb != null) ownerRb.linearVelocity = Vector2.zero;

        // Load visual effects dynamically if not assigned
        if (timeStopPrefab == null)
        {
            timeStopPrefab = Resources.Load<GameObject>("SuperNhon/prefabs/Effect_TimeStop");
        }
        if (typewriterPrefab == null)
        {
            typewriterPrefab = Resources.Load<GameObject>("SuperNhon/prefabs/Effect_Typewriter");
        }

        // 1. Time Stop Phase
        GameObject timeStopVisual = null;
        if (timeStopPrefab != null)
        {
            timeStopVisual = Instantiate(timeStopPrefab, targetTransform.position, Quaternion.identity);
            Destroy(timeStopVisual, 2.0f);
        }

        // Freeze opponent Rigidbody2D velocity to simulate Time Stop
        Rigidbody2D targetRb = targetMono.GetComponent<Rigidbody2D>();
        Vector2 targetOrigVelocity = Vector2.zero;
        if (targetRb != null)
        {
            targetOrigVelocity = targetRb.linearVelocity;
            targetRb.linearVelocity = Vector2.zero;
            targetRb.bodyType = RigidbodyType2D.Kinematic; // Lock in place
        }

        int totalHits = 5;
        float normalHitDamage = 12f;
        float finalHitDamage = 45f;

        for (int i = 1; i <= totalHits; i++)
        {
            if (targetMono == null || owner == null) break;

            bool isLastHit = (i == totalHits);
            float damage = isLastHit ? finalHitDamage : normalHitDamage;

            // Spawn typewriter text blast on target
            if (typewriterPrefab != null)
            {
                GameObject tw = Instantiate(typewriterPrefab, targetTransform.position + new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(0.2f, 1.2f), 0f), Quaternion.identity);
                Destroy(tw, 0.8f);
            }

            // Deal damage
            target.TakeDamage(damage, owner.transform.position.x, isLastHit);

            yield return new WaitForSeconds(tickInterval);
        }

        // Release the target
        if (targetRb != null && targetMono != null)
        {
            targetRb.bodyType = RigidbodyType2D.Dynamic;
            targetRb.linearVelocity = targetOrigVelocity;
        }

        if (owner != null)
        {
            owner.AnimationEvent_EndAttack();
        }
    }
}

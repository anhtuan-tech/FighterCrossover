using System.Collections;
using UnityEngine;

public class GrayUltimateSkill : MonoBehaviour
{
    [Header("--- ULTIMATE CONFIG ---")]
    public float detectionRadius = 2.5f;
    public float searchOffset = 1.0f;
    public float tickInterval = 0.25f;
    public GameObject ultimateEffectPrefab;

    /// <summary>
    /// Spawns the Ultimate combo strike detection. If a target is hit, executes the combo sequence.
    /// </summary>
    public void SpawnUltimateCombo(GrayCharacterController owner, LayerMask targetLayer)
    {
        float dir = owner.transform.localScale.x;
        Vector3 detectPos = owner.attackHitbox != null 
            ? owner.attackHitbox.position 
            : owner.transform.position + new Vector3(dir * searchOffset, 0.2f, 0f);

        // Perform hit detection for the ultimate initiator hit
        Collider2D[] hits = Physics2D.OverlapCircleAll(detectPos, detectionRadius, targetLayer);
        IDamageable hitTarget = null;
        
        foreach (var hit in hits)
        {
            if (hit.gameObject == owner.gameObject) continue;

            IDamageable damageable = hit.GetComponent<IDamageable>();
            if (damageable != null)
            {
                hitTarget = damageable;
                break; // Target the first valid opponent hit
            }
        }

        if (hitTarget != null)
        {
            Debug.Log($"[Ultimate] Gray hit target {((MonoBehaviour)hitTarget).gameObject.name}! Initiating Ultimate Combo sequence.");
            owner.StartCoroutine(UltimateComboRoutine(owner, hitTarget, dir));
        }
        else
        {
            Debug.Log("[Ultimate] Gray Ultimate missed. Ending attack.");
            owner.Invoke("AnimationEvent_EndAttack", 0.3f);
        }
    }

    private IEnumerator UltimateComboRoutine(GrayCharacterController owner, IDamageable target, float dir)
    {
        MonoBehaviour targetMono = target as MonoBehaviour;
        if (targetMono == null) yield break;

        Transform targetTransform = targetMono.transform;

        // Freeze owner velocity during the combo execution to look cinematic
        Rigidbody2D ownerRb = owner.GetComponent<Rigidbody2D>();
        if (ownerRb != null) ownerRb.linearVelocity = Vector2.zero;

        // Perform 5 hit sequence
        int totalHits = 5;
        float normalHitDamage = 15f;
        float finalHitDamage = 40f;

        for (int i = 1; i <= totalHits; i++)
        {
            if (targetMono == null || owner == null) yield break;

            bool isLastHit = (i == totalHits);
            float damage = isLastHit ? finalHitDamage : normalHitDamage;

            // Deal damage. If it's the last hit, apply heavy flag for knockdown
            target.TakeDamage(damage, owner.transform.position.x, isLastHit);

            // Spawn dynamic ice-slash visual effect on the target
            SpawnHitEffectVisual(owner, targetTransform.position, i);

            // Wait before next hit in the combo chain
            if (!isLastHit)
            {
                yield return new WaitForSeconds(tickInterval);
            }
        }

        // Return owner to Idle state
        if (owner != null)
        {
            owner.AnimationEvent_EndAttack();
        }
    }

    private void SpawnHitEffectVisual(GrayCharacterController owner, Vector3 position, int hitIndex)
    {
        if (ultimateEffectPrefab != null)
        {
            GameObject fx = Instantiate(ultimateEffectPrefab, position, Quaternion.identity);
            Destroy(fx, 0.4f);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 0.5f, 1f, 0.3f);
        Gizmos.DrawSphere(transform.position, detectionRadius);
    }
}

using System.Collections;
using UnityEngine;

public class PainUltimateSkill : MonoBehaviour
{
    [Header("--- ULTIMATE CONFIG ---")]
    public float detectionRadius = 3.5f;
    public float searchOffset = 0.5f;
    public float tickInterval = 0.25f;
    public GameObject ultimateEffectPrefab;

    [Header("--- DYNAMIC ULTIMATE VISUAL ---")]
    public string dynamicEffectSpriteSheet = "Pain_UltiSkill_Effect";
    public float dynamicEffectScaleFactor = 1.2f;
    public float dynamicEffectFPS = 12f;

    public void SpawnUltimateCombo(PainController owner, LayerMask targetLayer)
    {
        float dir = owner.transform.localScale.x;
        Vector3 detectPos = owner.transform.position + new Vector3(dir * searchOffset, 0.5f, 0f);

        Collider2D[] hits = Physics2D.OverlapCircleAll(detectPos, detectionRadius, targetLayer);
        IDamageable hitTarget = null;
        
        foreach (var hit in hits)
        {
            if (hit.gameObject == owner.gameObject) continue;

            IDamageable damageable = hit.GetComponent<IDamageable>();
            if (damageable == null)
            {
                damageable = hit.GetComponentInParent<IDamageable>();
            }

            if (damageable != null)
            {
                hitTarget = damageable;
                break;
            }
        }

        if (hitTarget != null)
        {
            Debug.Log($"[Ultimate] Pain Shinra Tensei hit target {((MonoBehaviour)hitTarget).gameObject.name}!");
            owner.StartCoroutine(UltimateComboRoutine(owner, hitTarget, dir));
        }
        else
        {
            Debug.Log("[Ultimate] Pain Ultimate missed.");
            owner.Invoke("AnimationEvent_EndAttack", 0.3f);
        }
    }

    private IEnumerator UltimateComboRoutine(PainController owner, IDamageable target, float dir)
    {
        MonoBehaviour targetMono = target as MonoBehaviour;
        if (targetMono == null) yield break;

        Transform targetTransform = targetMono.transform;

        Rigidbody2D ownerRb = owner.GetComponent<Rigidbody2D>();
        if (ownerRb != null) ownerRb.linearVelocity = Vector2.zero;

        // Perform 4 rapid gravity shockwave hits and 1 final huge Shinra Tensei push
        int totalHits = 5;
        float normalHitDamage = 12f;
        float finalHitDamage = 45f;

        for (int i = 1; i <= totalHits; i++)
        {
            if (targetMono == null || owner == null) yield break;

            bool isLastHit = (i == totalHits);
            float damage = isLastHit ? finalHitDamage : normalHitDamage;

            // Apply damage. The last hit is a heavy attack that triggers the massive knockback
            target.TakeDamage(damage, owner.transform.position.x, isLastHit);

            // Spawn visual effect
            SpawnHitEffectVisual(owner, targetTransform.position, i);

            if (!isLastHit)
            {
                yield return new WaitForSeconds(tickInterval);
            }
        }

        if (owner != null)
        {
            owner.AnimationEvent_EndAttack();
        }
    }

    private void SpawnHitEffectVisual(PainController owner, Vector3 position, int hitIndex)
    {
        // Spawns Shinra Tensei wave centered on Pain
        GameObject wave = new GameObject("ShinraTenseiWave");
        wave.transform.position = owner.transform.position + new Vector3(0f, 0.5f, 0f);

        var sr = wave.AddComponent<SpriteRenderer>();
        var sprites = owner.LoadAllPainSprites(dynamicEffectSpriteSheet);
        
        if (sprites != null && sprites.Count > 0)
        {
            var animator = wave.AddComponent<SpriteAnimator>();
            animator.Setup(sr, sprites, dynamicEffectFPS, false, true); // Plays once and destroys itself
            
            float scale = hitIndex * dynamicEffectScaleFactor;
            wave.transform.localScale = new Vector3(scale, scale, 1f);
        }
        else
        {
            // Fallback if sprites are not loaded
            sr.color = new Color(0.5f, 0.5f, 0.6f, 0.4f);
            owner.StartCoroutine(ScaleEffectRoutine(wave, hitIndex * 1.5f));
        }
    }

    private IEnumerator ScaleEffectRoutine(GameObject obj, float maxScale)
    {
        float duration = 0.25f;
        float elapsed = 0f;
        Vector3 startScale = Vector3.zero;
        Vector3 endScale = new Vector3(maxScale, maxScale, 1f);

        SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();

        while (elapsed < duration)
        {
            if (obj == null) yield break;
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            obj.transform.localScale = Vector3.Lerp(startScale, endScale, t);
            if (sr != null)
            {
                sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, Mathf.Lerp(0.8f, 0f, t));
            }
            yield return null;
        }

        Destroy(obj);
    }
}

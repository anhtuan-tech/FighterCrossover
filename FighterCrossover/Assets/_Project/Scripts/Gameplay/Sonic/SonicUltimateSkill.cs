using System.Collections;
using UnityEngine;

public class SonicUltimateSkill : MonoBehaviour
{
    [Header("--- ULTIMATE CONFIG ---")]
    public float detectionRadius = 3.0f;
    public float searchOffset = 1.0f;
    public float tickInterval = 0.18f;

    [Header("--- DYNAMIC ULTIMATE VISUAL ---")]
    public string dynamicEffectSpriteSheet = "Sonic_UltiSkill_Effect";
    public Vector3 dynamicEffectScale = new Vector3(2.5f, 2.5f, 1f);
    public float dynamicEffectFPS = 15f;

    public void SpawnUltimateCombo(SonicController owner, LayerMask targetLayer)
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
            Debug.Log($"[Ultimate] Sonic Speed Blitz hit target {((MonoBehaviour)hitTarget).gameObject.name}!");
            owner.StartCoroutine(UltimateComboRoutine(owner, hitTarget, dir));
        }
        else
        {
            Debug.Log("[Ultimate] Sonic Ultimate missed.");
            owner.Invoke("AnimationEvent_EndAttack", 0.3f);
        }
    }

    private IEnumerator UltimateComboRoutine(SonicController owner, IDamageable target, float dir)
    {
        MonoBehaviour targetMono = target as MonoBehaviour;
        if (targetMono == null) yield break;

        Transform targetTransform = targetMono.transform;

        Rigidbody2D ownerRb = owner.GetComponent<Rigidbody2D>();
        if (ownerRb != null) ownerRb.linearVelocity = Vector2.zero;

        // Spawn dynamic visual effect on target
        SpawnUltimateVisualEffect(owner, targetTransform.position);

        int totalHits = 6;
        float normalHitDamage = 10f;
        float finalHitDamage = 35f;

        // Visual flash trail colors
        Color trailColor = new Color(0.1f, 0.6f, 1.0f, 0.6f);

        for (int i = 1; i <= totalHits; i++)
        {
            if (targetMono == null || owner == null) yield break;

            bool isLastHit = (i == totalHits);
            float damage = isLastHit ? finalHitDamage : normalHitDamage;

            // Teleport to the opposite side of the target for a blitz effect
            float offsetMultiplier = (i % 2 == 0) ? 1.2f : -1.2f;
            Vector3 blitzPos = targetTransform.position + new Vector3(offsetMultiplier, 0f, 0f);
            
            // Set owner position and localScale to face target
            owner.transform.position = blitzPos;
            owner.transform.localScale = new Vector3(Mathf.Sign(targetTransform.position.x - owner.transform.position.x), 1f, 1f);

            // Deal damage
            target.TakeDamage(damage, owner.transform.position.x, isLastHit);

            // Spawn double-image or trail effect
            SpawnBlitzTrail(owner, blitzPos, trailColor);

            yield return new WaitForSeconds(tickInterval);
        }

        if (owner != null)
        {
            owner.AnimationEvent_EndAttack();
        }
    }

    private void SpawnUltimateVisualEffect(SonicController owner, Vector3 position)
    {
        GameObject effectObj = new GameObject("SonicUltimateEffect");
        effectObj.transform.position = position + new Vector3(0f, 0.5f, 0f);

        var sr = effectObj.AddComponent<SpriteRenderer>();
        var sprites = owner.LoadAllSonicSprites(dynamicEffectSpriteSheet);
        if (sprites != null && sprites.Count > 0)
        {
            var animator = effectObj.AddComponent<SpriteAnimator>();
            animator.Setup(sr, sprites, dynamicEffectFPS, false, true);
            effectObj.transform.localScale = dynamicEffectScale;
        }
        else
        {
            Destroy(effectObj);
        }
    }

    private void SpawnBlitzTrail(SonicController owner, Vector3 position, Color color)
    {
        GameObject trail = new GameObject("SonicBlitzTrail");
        trail.transform.position = position;
        trail.transform.localScale = owner.transform.localScale;

        var sr = trail.AddComponent<SpriteRenderer>();
        var ownerSr = owner.GetComponent<SpriteRenderer>();
        if (ownerSr != null)
        {
            sr.sprite = ownerSr.sprite;
            sr.sortingLayerID = ownerSr.sortingLayerID;
            sr.sortingOrder = ownerSr.sortingOrder - 1;
        }
        else
        {
            sr.sprite = owner.LoadSonicSprite("Sonic_Run");
        }

        sr.color = color;

        // Smooth fade out
        owner.StartCoroutine(FadeOutRoutine(trail, 0.2f));
    }

    private IEnumerator FadeOutRoutine(GameObject obj, float duration)
    {
        float elapsed = 0f;
        SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
        if (sr == null) { Destroy(obj); yield break; }

        Color startColor = sr.color;
        Color endColor = new Color(startColor.r, startColor.g, startColor.b, 0f);

        while (elapsed < duration)
        {
            if (obj == null) yield break;
            elapsed += Time.deltaTime;
            sr.color = Color.Lerp(startColor, endColor, elapsed / duration);
            yield return null;
        }

        Destroy(obj);
    }
}

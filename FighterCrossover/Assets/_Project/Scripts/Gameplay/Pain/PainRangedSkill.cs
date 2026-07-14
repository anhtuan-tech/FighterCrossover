using System.Collections;
using UnityEngine;

public class PainRangedSkill : MonoBehaviour
{
    [Header("--- PROJECTILE CONFIG ---")]
    public GameObject projectilePrefab;
    public float projectileSpeed = 15f;
    public float projectileDamage = 30f;
    public float projectileLifetime = 2.0f;

    [Header("--- SPAWN OFFSET ---")]
    public float spawnHeightOffset = 0.8f;
    public float spawnForwardOffset = 1.0f;

    [Header("--- DYNAMIC PROJECTILE VISUAL ---")]
    public string dynamicProjectileSpriteSheet = "Pain_Skill2_Effect";
    public Vector3 dynamicProjectileScale = new Vector3(1.5f, 1.5f, 1f);
    public float dynamicProjectileFPS = 12f;

    public void StartCast(PainController owner)
    {
        // Add cast delay/visual if desired. In our case, the Animator trigger plays.
    }

    public void SpawnProjectile(PainController owner, LayerMask targetLayer)
    {
        float dir = owner.transform.localScale.x;
        Vector3 spawnPos = owner.transform.position + new Vector3(dir * spawnForwardOffset, spawnHeightOffset, 0f);

        if (projectilePrefab != null)
        {
            GameObject projObj = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
            PainProjectile proj = projObj.GetComponent<PainProjectile>();
            if (proj == null) proj = projObj.AddComponent<PainProjectile>();
            proj.Setup(new Vector2(dir, 0f), owner.gameObject, targetLayer, projectileSpeed, projectileDamage, projectileLifetime);
        }
        else
        {
            // Dynamic projectile using dynamicProjectileSpriteSheet
            GameObject projObj = new GameObject("DynamicPainProjectile");
            projObj.transform.position = spawnPos;

            var sr = projObj.AddComponent<SpriteRenderer>();
            var sprites = owner.LoadAllPainSprites(dynamicProjectileSpriteSheet);
            if (sprites != null && sprites.Count > 0)
            {
                var animator = projObj.AddComponent<SpriteAnimator>();
                animator.Setup(sr, sprites, dynamicProjectileFPS, true, false);
                projObj.transform.localScale = dynamicProjectileScale;
            }
            else
            {
                // Fallback to rod
                sr.sprite = owner.LoadPainSprite("Pain_Hit1");
                sr.color = new Color(0.1f, 0.1f, 0.1f, 1f);
                projObj.transform.localScale = new Vector3(0.8f, 0.15f, 1f);
            }

            var rb = projObj.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;

            var col = projObj.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(1.2f, 1.2f);

            PainProjectile proj = projObj.AddComponent<PainProjectile>();
            proj.Setup(new Vector2(dir, 0f), owner.gameObject, targetLayer, projectileSpeed, projectileDamage, projectileLifetime);
        }
    }
}

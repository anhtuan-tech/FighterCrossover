using System.Collections;
using UnityEngine;

public class SonicRangedSkill : MonoBehaviour
{
    [Header("--- PROJECTILE CONFIG ---")]
    public GameObject projectilePrefab;
    public float projectileSpeed = 16f;
    public float projectileDamage = 25f;
    public float projectileLifetime = 1.8f;

    [Header("--- SPAWN OFFSET ---")]
    public float spawnHeightOffset = 0.5f;
    public float spawnForwardOffset = 1.0f;

    [Header("--- DYNAMIC PROJECTILE VISUAL ---")]
    public string dynamicProjectileSpriteSheet = "Sonic_Skill1";
    public Vector3 dynamicProjectileScale = new Vector3(1.2f, 1.2f, 1f);
    public float dynamicProjectileFPS = 12f;

    public void StartCast(SonicController owner)
    {
    }

    public void SpawnProjectile(SonicController owner, LayerMask targetLayer)
    {
        float dir = owner.transform.localScale.x;
        Vector3 spawnPos = owner.transform.position + new Vector3(dir * spawnForwardOffset, spawnHeightOffset, 0f);

        if (projectilePrefab != null)
        {
            GameObject projObj = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
            SonicProjectile proj = projObj.GetComponent<SonicProjectile>();
            if (proj == null) proj = projObj.AddComponent<SonicProjectile>();
            proj.Setup(new Vector2(dir, 0f), owner.gameObject, targetLayer, projectileSpeed, projectileDamage, projectileLifetime);
        }
        else
        {
            // Dynamic projectile using dynamicProjectileSpriteSheet
            GameObject projObj = new GameObject("DynamicSonicProjectile");
            projObj.transform.position = spawnPos;

            var sr = projObj.AddComponent<SpriteRenderer>();
            var sprites = owner.LoadAllSonicSprites(dynamicProjectileSpriteSheet);
            if (sprites != null && sprites.Count > 0)
            {
                var animator = projObj.AddComponent<SpriteAnimator>();
                animator.Setup(sr, sprites, dynamicProjectileFPS, true, false);
                projObj.transform.localScale = dynamicProjectileScale;
            }
            else
            {
                // Fallback to static sprite
                sr.sprite = owner.LoadSonicSprite("Sonic_Skill1");
                if (sr.sprite == null) sr.sprite = owner.LoadSonicSprite("Sonic_Hit1");
                sr.color = new Color(0.2f, 0.7f, 1f, 0.9f);
                projObj.transform.localScale = new Vector3(1.2f, 1.2f, 1f);
            }

            var rb = projObj.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;

            var col = projObj.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(0.8f, 0.8f);

            SonicProjectile proj = projObj.AddComponent<SonicProjectile>();
            proj.Setup(new Vector2(dir, 0f), owner.gameObject, targetLayer, projectileSpeed, projectileDamage, projectileLifetime);
        }
    }
}

using System.Collections;
using UnityEngine;

public class IchigoRangedSkill : MonoBehaviour
{
    [Header("--- PROJECTILE CONFIG ---")]
    public GameObject projectilePrefab;
    public float projectileSpeed = 12f;
    public float projectileDamage = 35f;
    public float projectileLifetime = 2.5f;

    [Header("--- SPAWN OFFSET ---")]
    [Tooltip("Height relative to Ichigo's position to spawn the projectile")]
    public float spawnHeightOffset = 0.2f;
    [Tooltip("Forward offset relative to Ichigo's position to spawn the projectile")]
    public float spawnForwardOffset = 1.0f;

    public void StartCast(IchigoController owner)
    {
        // Add any starting cast visual or sound effects here if needed
    }

    public void SpawnProjectile(IchigoController owner, LayerMask targetLayer)
    {
        float dir = owner.transform.localScale.x;
        Vector3 spawnPos = owner.transform.position + new Vector3(dir * spawnForwardOffset, spawnHeightOffset, 0f);

        if (projectilePrefab != null)
        {
            GameObject projObj = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
            RangedProjectile proj = projObj.GetComponent<RangedProjectile>();
            if (proj == null) proj = projObj.AddComponent<RangedProjectile>();
            
            // Override stats with inspector configured parameters
            proj.speed = projectileSpeed;
            proj.damage = projectileDamage;
            proj.lifetime = projectileLifetime;

            proj.Setup(new Vector2(dir, 0f), owner.gameObject, targetLayer);
        }
        else
        {
            // Dynamic fallback projectile
            Debug.LogWarning("[IchigoRangedSkill] Ranged Projectile Prefab not assigned! Creating a dynamic projectile.");
            GameObject projObj = new GameObject("DynamicProjectile");
            projObj.transform.position = spawnPos;
            
            var col = projObj.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(0.8f, 0.4f);

            var rbProj = projObj.AddComponent<Rigidbody2D>();
            rbProj.bodyType = RigidbodyType2D.Kinematic;

            // Inner layer
            GameObject inner = new GameObject("Inner");
            inner.transform.SetParent(projObj.transform);
            inner.transform.localPosition = Vector3.zero;
            var srInner = inner.AddComponent<SpriteRenderer>();
            srInner.color = new Color(0f, 0.8f, 1f, 0.9f); // Cyan energy
            srInner.sprite = owner.LoadIchigoSprite("image-removebg-preview_1");

            // Outer layer
            GameObject outer = new GameObject("Outer");
            outer.transform.SetParent(projObj.transform);
            outer.transform.localPosition = Vector3.zero;
            var srOuter = outer.AddComponent<SpriteRenderer>();
            srOuter.color = new Color(0f, 0.4f, 1f, 0.7f); // Blue energy
            srOuter.sprite = owner.LoadIchigoSprite("image-removebg-preview_1");
            outer.transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);

            RangedProjectile proj = projObj.AddComponent<RangedProjectile>();
            
            proj.speed = projectileSpeed;
            proj.damage = projectileDamage;
            proj.lifetime = projectileLifetime;

            proj.Setup(new Vector2(dir, 0f), owner.gameObject, targetLayer);
        }
    }
}

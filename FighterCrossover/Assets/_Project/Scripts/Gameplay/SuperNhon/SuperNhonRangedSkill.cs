using System.Collections;
using UnityEngine;

public class SuperNhonRangedSkill : MonoBehaviour
{
    [Header("--- PROJECTILE CONFIG ---")]
    public GameObject projectilePrefab;
    public float projectileSpeed = 14f;
    public float projectileDamage = 32f;
    public float projectileLifetime = 2.2f;

    [Header("--- SPAWN OFFSET ---")]
    public float spawnHeightOffset = 0.8f;
    public float spawnForwardOffset = 1.0f;

    public void StartCast(SuperNhonController owner)
    {
    }

    public void SpawnProjectile(SuperNhonController owner, LayerMask targetLayer)
    {
        float dir = owner.transform.localScale.x;
        Vector3 spawnPos = owner.transform.position + new Vector3(dir * spawnForwardOffset, spawnHeightOffset, 0f);

        if (projectilePrefab != null)
        {
            GameObject projObj = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
            SuperNhonProjectile proj = projObj.GetComponent<SuperNhonProjectile>();
            if (proj == null) proj = projObj.AddComponent<SuperNhonProjectile>();
            proj.Setup(new Vector2(dir, 0f), owner.gameObject, targetLayer, projectileSpeed, projectileDamage, projectileLifetime);
        }
        else
        {
            // Dynamic fallback projectile: binary code block
            GameObject projObj = new GameObject("DynamicSuperNhonProjectile");
            projObj.transform.position = spawnPos;

            var sr = projObj.AddComponent<SpriteRenderer>();
            sr.sprite = owner.LoadSuperNhonSprite("SuperNhon_Skill1_Part2");
            if (sr.sprite == null)
            {
                sr.sprite = owner.LoadSuperNhonSprite("SuperNhon_Hit1");
            }
            sr.color = new Color(0.2f, 1f, 0.3f, 0.9f); // Matrix Green code block
            projObj.transform.localScale = new Vector3(1f, 0.8f, 1f);

            var rb = projObj.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;

            var col = projObj.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(0.8f, 0.6f);

            SuperNhonProjectile proj = projObj.AddComponent<SuperNhonProjectile>();
            proj.Setup(new Vector2(dir, 0f), owner.gameObject, targetLayer, projectileSpeed, projectileDamage, projectileLifetime);
        }
    }
}

using System.Collections;
using UnityEngine;

public class GrayRangedSkill : MonoBehaviour
{
    [Header("--- PROJECTILE CONFIG ---")]
    public GameObject projectilePrefab;
    public float projectileSpeed = 12f;
    public float projectileDamage = 35f;
    public float projectileLifetime = 2.5f;

    [Header("--- SPAWN OFFSET ---")]
    [Tooltip("Height relative to Gray's position to spawn the projectile (chest/hand level)")]
    public float spawnHeightOffset = 0.7f;
    [Tooltip("Forward offset relative to Gray's position to spawn the projectile")]
    public float spawnForwardOffset = 1.0f;

    [Header("--- VISUAL EFFECTS ---")]
    public GameObject magicCirclePrefab;
    public float magicCircleDuration = 0.5f;

    /// <summary>
    /// Starts the casting process, spawning the magic circle instantly (at the starting frame)
    /// </summary>
    public void StartCast(GrayCharacterController owner)
    {
        float dir = owner.transform.localScale.x;
        Vector3 spawnPos = owner.transform.position + new Vector3(dir * spawnForwardOffset, spawnHeightOffset, 0f);

        // Spawn Magic Circle
        if (magicCirclePrefab != null)
        {
            GameObject circleObj = Instantiate(magicCirclePrefab, spawnPos, Quaternion.identity);
            Destroy(circleObj, magicCircleDuration);
        }
        else
        {
            // Dynamic fallback Magic Circle using sprite-17-19_3
            GameObject circleObj = new GameObject("DynamicMagicCircle");
            circleObj.transform.position = spawnPos;
            circleObj.transform.localScale = new Vector3(dir * 1.2f, 1.2f, 1.2f);

            var sr = circleObj.AddComponent<SpriteRenderer>();
            sr.sprite = owner.LoadGraySprite("sprite-17-19_3");
            sr.color = new Color(0.2f, 0.6f, 1.0f, 0.85f); // Ice/Magic Blue

            // Add fader
            var fader = circleObj.AddComponent<MagicCircleFader>();
            fader.duration = magicCircleDuration;
        }

        // Fallback casting sprite animation if animator is not present
        Animator anim = owner.GetComponent<Animator>();
        if (anim == null)
        {
            owner.StartCoroutine(FallbackCastAnimationRoutine(owner));
        }
    }

    private IEnumerator FallbackCastAnimationRoutine(GrayCharacterController owner)
    {
        SpriteRenderer sr = owner.GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
        {
            Sprite orig = sr.sprite;
            Sprite f1 = owner.LoadGraySprite("sprite-17-19_0");
            Sprite f2 = owner.LoadGraySprite("sprite-17-19_1");
            Sprite f3 = owner.LoadGraySprite("sprite-17-19_2");

            if (f1 != null) { sr.sprite = f1; yield return new WaitForSeconds(0.1f); }
            if (f2 != null) { sr.sprite = f2; yield return new WaitForSeconds(0.1f); }
            if (f3 != null) { sr.sprite = f3; yield return new WaitForSeconds(0.1f); }

            sr.sprite = orig;
        }
    }

    /// <summary>
    /// Fires the projectile forward
    /// </summary>
    public void SpawnProjectile(GrayCharacterController owner, LayerMask targetLayer)
    {
        float dir = owner.transform.localScale.x;
        Vector3 spawnPos = owner.transform.position + new Vector3(dir * spawnForwardOffset, spawnHeightOffset, 0f);

        if (projectilePrefab != null)
        {
            GameObject projObj = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
            GrayProjectile proj = projObj.GetComponent<GrayProjectile>();
            if (proj == null) proj = projObj.AddComponent<GrayProjectile>();
            proj.Setup(new Vector2(dir, 0f), owner.gameObject, targetLayer, projectileSpeed, projectileDamage, projectileLifetime);
        }
        else
        {
            // Dynamic fallback projectile using sprite-17-37_6
            GameObject projObj = new GameObject("DynamicGrayProjectile");
            projObj.transform.position = spawnPos;

            var sr = projObj.AddComponent<SpriteRenderer>();
            sr.sprite = owner.LoadGraySprite("sprite-17-37_6");
            sr.color = new Color(0.5f, 0.8f, 1f, 1f); // Ice shard blue

            var col = projObj.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(0.8f, 0.3f);

            var rb = projObj.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;

            GrayProjectile proj = projObj.AddComponent<GrayProjectile>();
            proj.Setup(new Vector2(dir, 0f), owner.gameObject, targetLayer, projectileSpeed, projectileDamage, projectileLifetime);
        }
    }
}

using System.Collections;
using UnityEngine;

public class PeterGriffinRangedSkill : MonoBehaviour
{
    [Header("--- PROJECTILE CONFIG ---")]
    public GameObject projectilePrefab;
    public float projectileSpeed = 15f;
    public float projectileDamage = 35f;
    public float projectileLifetime = 5f;

    [Header("--- SPAWN OFFSET ---")]
    [Tooltip("Độ cao so với chân nhân vật để bắn projectile (ngang tầm ngực/tay)")]
    public float spawnHeightOffset = 0.7f;
    [Tooltip("Khoảng cách lệch về phía trước nhân vật")]
    public float spawnForwardOffset = 1.0f;

    [Header("--- VISUAL EFFECTS ---")]
    public GameObject magicCirclePrefab;
    public float magicCircleDuration = 0.5f;

    /// <summary>
    /// Bắt đầu quá trình cast skill, tạo hiệu ứng vòng tròn phép thuật ngay lập tức
    /// </summary>
    public void StartCast(PeterGriffinController owner)
    {
        float dir = owner.transform.localScale.x;
        Vector3 spawnPos = owner.transform.position + new Vector3(dir * spawnForwardOffset, spawnHeightOffset, 0f);

        if (magicCirclePrefab != null)
        {
            GameObject circleObj = Instantiate(magicCirclePrefab, spawnPos, Quaternion.identity);
            Destroy(circleObj, magicCircleDuration);
        }

        Animator anim = owner.GetComponent<Animator>();
        if (anim == null)
        {
            owner.StartCoroutine(FallbackCastAnimationRoutine(owner));
        }
    }

    private IEnumerator FallbackCastAnimationRoutine(PeterGriffinController owner)
    {
        SpriteRenderer sr = owner.GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
        {
            Sprite orig = sr.sprite;
            Sprite f1 = owner.LoadPeterGriffinSprite("Ranged_0");
            Sprite f2 = owner.LoadPeterGriffinSprite("Ranged_1");
            Sprite f3 = owner.LoadPeterGriffinSprite("Ranged_2");

            if (f1 != null) { sr.sprite = f1; yield return new WaitForSeconds(0.1f); }
            if (f2 != null) { sr.sprite = f2; yield return new WaitForSeconds(0.1f); }
            if (f3 != null) { sr.sprite = f3; yield return new WaitForSeconds(0.1f); }

            sr.sprite = orig;
        }
    }

    /// <summary>
    /// Bắn quả cầu projectile thẳng về phía trước
    /// </summary>
    public void SpawnProjectile(PeterGriffinController owner, LayerMask targetLayer)
    {
        float dir = owner.transform.localScale.x;
        Vector3 spawnPos = owner.transform.position + new Vector3(dir * spawnForwardOffset, spawnHeightOffset, 0f);

        if (projectilePrefab != null)
        {
            GameObject projObj = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
            PeterGriffinProjectile proj = projObj.GetComponent<PeterGriffinProjectile>();
            if (proj == null) proj = projObj.AddComponent<PeterGriffinProjectile>();
            proj.Setup(new Vector2(dir, 0f), owner.gameObject, targetLayer, projectileSpeed, projectileDamage, projectileLifetime);
        }
        else
        {
            // Thiết lập fallback động sử dụng sprite ProjectileRanged_0 nếu không gán prefab
            GameObject projObj = new GameObject("DynamicPeterGriffinProjectile");
            projObj.transform.position = spawnPos;

            var sr = projObj.AddComponent<SpriteRenderer>();
            sr.sprite = owner.LoadPeterGriffinSprite("ProjectileRanged_0");
            sr.color = new Color(0.2f, 0.8f, 1f, 1f); // Tông màu xanh dương

            var col = projObj.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(0.8f, 0.3f);

            var rb = projObj.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;

            PeterGriffinProjectile proj = projObj.AddComponent<PeterGriffinProjectile>();
            proj.Setup(new Vector2(dir, 0f), owner.gameObject, targetLayer, projectileSpeed, projectileDamage, projectileLifetime);
        }
    }
}
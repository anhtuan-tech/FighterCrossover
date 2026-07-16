using UnityEngine;

public class PeterGriffinUltimateSkill : MonoBehaviour
{
    [Header("--- ULTIMATE OBJECT CONFIG ---")]
    [Tooltip("Prefab của vật thể/đạn chiêu cuối khổng lồ")]
    public GameObject ultimateProjectilePrefab;
    public float projectileSpeed = 8f;
    public float projectileDamage = 100f;
    public float projectileLifetime = 4.0f;

    [Header("--- SPAWN OFFSET ---")]
    public float spawnHeightOffset = 0.5f;
    public float spawnForwardOffset = 1.2f;

    /// <summary>
    /// Triệu hồi/Tạo ra Object Ultimate Projectile bay về phía trước
    /// </summary>
    public void SpawnUltimateObject(PeterGriffinController owner, LayerMask targetLayer)
    {
        float dir = owner.transform.localScale.x;
        Vector3 spawnPos = owner.transform.position + new Vector3(dir * spawnForwardOffset, spawnHeightOffset, 0f);

        if (ultimateProjectilePrefab != null)
        {
            GameObject ultiObj = Instantiate(ultimateProjectilePrefab, spawnPos, Quaternion.identity);

            // Lấy hoặc tự thêm script điều khiển Ultimate Projectile
            PeterGriffinUltimateProjectile proj = ultiObj.GetComponent<PeterGriffinUltimateProjectile>();
            if (proj == null)
            {
                proj = ultiObj.AddComponent<PeterGriffinUltimateProjectile>();
            }

            // Thiết lập thông số bay, sát thương và layer mục tiêu
            proj.Setup(new Vector2(dir, 0f), owner.gameObject, targetLayer, projectileSpeed, projectileDamage, projectileLifetime);
            Debug.Log("[Ultimate] Peter Griffin đã tạo ra Object Ultimate thành công!");
        }
        else
        {
            // Thiết lập Fallback động nếu chưa gán Prefab trong Inspector
            Debug.LogWarning("[Ultimate] Chưa gán ultimateProjectilePrefab! Tự động tạo Object Fallback.");

            GameObject ultiObj = new GameObject("DynamicPeterUltimateProjectile");
            ExpressPos(ultiObj, spawnPos); // Đã sửa lỗi chữ Trung Quốc '表达Pos' thành 'ExpressPos'

            var sr = ultiObj.AddComponent<SpriteRenderer>();
            sr.sprite = owner.LoadPeterGriffinSprite("UltiProjectile_0");
            sr.color = new Color(1f, 0.5f, 0f, 1f); // Tông cam lửa rực rỡ

            var col = ultiObj.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(1.5f, 1.0f); // Kích thước collider lớn hơn đạn thường

            var rb = ultiObj.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;

            PeterGriffinUltimateProjectile proj = ultiObj.AddComponent<PeterGriffinUltimateProjectile>();
            proj.Setup(new Vector2(dir, 0f), owner.gameObject, targetLayer, projectileSpeed, projectileDamage, projectileLifetime);
        }

        // Kết thúc animation cast chiêu, trả trạng thái Peter về Idle
        owner.Invoke("AnimationEvent_EndAttack", 0.5f);
    }

    private void ExpressPos(GameObject obj, Vector3 pos)
    {
        obj.transform.position = pos;
        obj.transform.localScale = Vector3.one * 2.0f; // Scale gấp đôi đạn thường
    }
}
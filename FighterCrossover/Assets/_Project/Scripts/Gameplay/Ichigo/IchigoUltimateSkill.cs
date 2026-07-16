using System.Collections;
using UnityEngine;

public class IchigoUltimateSkill : MonoBehaviour
{
    [Header("--- ULTIMATE CONFIG ---")]
    public float ultimateDuration = 1.0f;
    public float ultimateRadius = 3.0f;
    public float ultimateDamagePerTick = 15f;
    public float ultimateTickInterval = 0.4f;
    public float ultimateSpinSpeed = 540f;
    public GameObject ultimateEffectPrefab;

    public void SpawnUltimate(IchigoController owner, LayerMask targetLayer)
    {
        Vector2 spawnPos = owner.transform.position;

        if (ultimateEffectPrefab != null)
        {
            GameObject ultObj = Instantiate(ultimateEffectPrefab, spawnPos, Quaternion.identity);
            UltimateEffect effect = ultObj.GetComponent<UltimateEffect>();
            if (effect == null) effect = ultObj.AddComponent<UltimateEffect>();
            
            // Set properties from this skill component
            effect.spinSpeed = ultimateSpinSpeed;
            effect.damagePerTick = ultimateDamagePerTick;
            effect.tickInterval = ultimateTickInterval;
            effect.radius = ultimateRadius;
            
            effect.Setup(owner.gameObject, targetLayer, ultimateDuration);
        }
        else
        {
            // Dynamic fallback ultimate effect if prefab is unassigned
            Debug.LogWarning("[IchigoUltimateSkill] Ultimate Effect Prefab not assigned! Creating a dynamic effect.");
            GameObject ultObj = new GameObject("DynamicUltimate");
            ultObj.transform.position = spawnPos;

            var sr = ultObj.AddComponent<SpriteRenderer>();
            sr.sprite = owner.LoadIchigoSprite("image-removebg-preview (7)_0");
            sr.color = new Color(0.9f, 0.1f, 0.1f, 0.8f); // Red energy blade aura

            UltimateEffect effect = ultObj.AddComponent<UltimateEffect>();
            
            // Set properties from this skill component
            effect.spinSpeed = ultimateSpinSpeed;
            effect.damagePerTick = ultimateDamagePerTick;
            effect.tickInterval = ultimateTickInterval;
            effect.radius = ultimateRadius;
            
            effect.Setup(owner.gameObject, targetLayer, ultimateDuration);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.9f, 0.1f, 0.1f, 0.3f);
        Gizmos.DrawSphere(transform.position, ultimateRadius);
    }
}

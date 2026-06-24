using UnityEngine;

public interface IDamageable
{
    void TakeDamage(float damage, float attackerPosX, bool isHeavyAttack = false);
}
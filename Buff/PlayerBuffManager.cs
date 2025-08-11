using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerBuffManager : MonoBehaviour
{

    [HideInInspector]
    public Player player;

    public enum BuffType
    {
        IncreaseMaxHealth,
        IncreaseAttackPower,
        IncreaseDefensePower,
        IncreaseAttackRange,
        Heal
    }

    public void ApplyBuff(BuffType buff)
    {
        switch (buff)
        {
            case BuffType.IncreaseMaxHealth:
                IncreaseMaxHealth();
                break;
            case BuffType.IncreaseAttackPower:
                IncreaseAttackPower();
                break;
            case BuffType.IncreaseDefensePower:
                IncreaseDefensePower();
                break;
            case BuffType.IncreaseAttackRange:
                IncreaseAttackRange();
                break;
            case BuffType.Heal:
                Heal();
                break;
        }
    }

    public void ApplyAllBuffs()
    {
        IncreaseMaxHealth();
        IncreaseAttackPower();
        IncreaseDefensePower();
        IncreaseAttackRange();
        Heal();
    }

    private void IncreaseMaxHealth()
    {
        int healthIncrease = 10;
        int newMaxHealth = player.maxHealth + healthIncrease;
        
        player.SetMaxHealth(newMaxHealth);
        
        player.SetHealth(newMaxHealth);
        
        Debug.Log($"Max health increased by {healthIncrease}, current max health: {newMaxHealth}");
    }

    private void IncreaseAttackPower()
    {
        int attackPowerIncrease = 5;
        player.attackPowerLight += attackPowerIncrease;
        player.attackPowerHeavy += 2 * attackPowerIncrease;
    }

    private void IncreaseDefensePower()
    {
        int defensePowerIncrease = 1;
        player.defensePower += defensePowerIncrease;
    }

    private void IncreaseAttackRange()
    {
        float attackRangeIncrease = 1f;
        player.attackRange += attackRangeIncrease;
    }

    private void Heal()
    {
        int healAmount = player.maxHealth / 2;
        player.Heal(healAmount);
    }
}
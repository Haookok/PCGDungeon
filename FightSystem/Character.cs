using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Character : MonoBehaviour
{
    public string characterName;
    public int health;
    public int magic;
    public int attackPowerLight;
    public int attackPowerHeavy;
    public int defensePower;
    public float attackRange;

    public virtual void TakeDamage(int damage)
    {
        int damageTaken = Mathf.Max(damage - defensePower, 0);
        health -= damageTaken;
        Debug.Log($"{characterName} takes {damageTaken} damage. Health is now {health}.");
        
        if (health <= 0)
        {
            Die();
        }
    }

    public virtual void Die()
    {
        gameObject.SetActive(false);
    }

    public virtual int AttackLight()
    {
        return attackPowerLight;
    }
    public virtual int AttackHeavy()
    {
        return attackPowerHeavy;
    }
}

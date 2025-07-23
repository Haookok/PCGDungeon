using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : Character
{
    
    public LayerMask enemyLayer;        // 怪物的层级（用于范围检测）
    void Start()
    {
        characterName = "Hero";
        health = 100;
        attackPowerLight = 10;
        attackPowerHeavy = 20;
        defensePower = 5;
        attackRange = 2f;
        enemyLayer = LayerMask.GetMask("enemyLayer"); // 假设怪物的层级名为 "Enemy"
    }

    private void Update()
    {
        //左键轻攻击

        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log("成功发起轻攻击");
            Attack(AttackLight());
        }

        //右键重攻击
        if (Input.GetMouseButtonDown(1))
            Attack(AttackHeavy());
    }

    public override void TakeDamage(int damage)
    {
        base.TakeDamage(damage);
    }
    
    public override void Die()
    {
        base.Die();
        Debug.Log($"{characterName} has fallen in battle.");
    }
    
    public override int AttackLight()
    {
        Debug.Log($"{characterName} performs a light attack.");
        return base.AttackLight();
    }
    
    public override int AttackHeavy()
    {
        Debug.Log($"{characterName} performs a heavy attack.");
        return base.AttackHeavy();
    }

    public void Attack(int attackDamage)
    {
        RaycastHit hit;
        Ray ray = new Ray(transform.position, transform.forward);
        Debug.DrawRay(ray.origin, ray.direction * attackRange, Color.red, attackRange);
        if (Physics.Raycast(ray, out hit, attackRange))
        {
            Debug.Log("射线击中物体：" + hit.collider.name);
            if (hit.collider.CompareTag("Enemy"))
            {
                Debug.Log("2");
                Monster monster = hit.collider.GetComponent<Monster>();
                if (monster != null)
                {
                    monster.TakeDamage(attackDamage);
                    Debug.Log($"{characterName} attacks {monster.characterName} for {attackDamage} damage.");
                }
            }

            /*Collider[] enemiesInRange = Physics.OverlapSphere(transform.position, attackRange, enemyLayer);
            foreach (Collider enemy in enemiesInRange)
            {
                if (enemy.CompareTag("Enemy"))
                {
                    Monster monster = enemy.GetComponent<Monster>();
                    Debug.Log("范围攻击触发");
                    if(monster != null)
                    {
                        monster.TakeDamage(attackDamage);
                        Debug.Log($"{characterName} attacks {monster.characterName} for {attackDamage} damage.");
                    }
                }
            }*/
        }
    }

    
    
    
}

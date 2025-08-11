using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class Player : Character
{
    
    public LayerMask enemyLayer;
    public Slider HPbar;
    public Slider MPbar;

    [Header("Attack Parameters")] 
    public float attackCooldown = 1.3f;
    private float lastAttackTime = 0f;
    private bool isAttacking = false;
    
    
    [Header("Base Stats")]
    public int baseMaxHealth = 100; 
    public int baseAttackPowerLight = 10;
    public int baseAttackPowerHeavy = 20;
    public int baseDefensePower = 5;
    public float baseAttackRange = 2f;
    public int maxHealth = 100; // 最大生命值
    public int maxMagic = 100; // 最大魔法值
    
    
    [Header("Buff System")]
    public PlayerBuffManager buffManager;
    public GameObject specialSkillPrefab; 
    
    [Header("Death System")]
    public DeathUIManager deathUIManager; // 死亡UI管理器引用
    public float deathDelay = 0f;
    private bool isDead = false; 
    public bool isInLava = false; 
    private float lavaDamageTimer = 1f;
    public int lavaDamageAmount = 40; 
    
    void Start()
    {
        characterName = "Hero";
        ApplyPermenentBonuses();
        health = maxHealth;
        magic = maxMagic; // 初始化魔法值
        attackPowerLight = 10;
        attackPowerHeavy = 20;
        defensePower = 5;
        attackRange = 2f;
        enemyLayer = LayerMask.GetMask("enemyLayer"); // 假设怪物的层级名为 "Enemy"

        if (buffManager == null)
            buffManager = GetComponent<PlayerBuffManager>();

        if (buffManager != null)
            buffManager.player = this;
        
        if (HPbar != null)
        {
            HPbar.maxValue = maxHealth;
            HPbar.value = health; // 初始化血条
        }
        
        if(MPbar != null)
        {
            MPbar.maxValue = maxMagic;
            MPbar.value = maxMagic; // 初始化魔法值
        }

        FindDeathUIManager();
    }

    private void ApplyPermenentBonuses()
    {
        if (PlayerProgressManager.Instance != null)
        {
            var permennetStats = PlayerProgressManager.Instance.GetPermanentStats();
            
            maxHealth = permennetStats.health;
            attackPowerLight = permennetStats.attackLight;
            attackPowerHeavy = permennetStats.attackHeavy;
            defensePower = permennetStats.defense;
            attackRange = permennetStats.range;
            Debug.Log("加成应用成功");
        }
        else
        {
            maxHealth = baseMaxHealth;
            attackPowerLight = baseAttackPowerLight;
            attackPowerHeavy = baseAttackPowerHeavy;
            defensePower = baseDefensePower;
            attackRange = baseAttackRange;
            Debug.Log("未找到 PlayerProgressManager，使用默认属性值");
        }
    }

    private void Update()
    {
        if (isDead)
            return;
        //左键轻攻击
        if (Input.GetMouseButtonDown(0))
        {
            if (CanAttack())
            {
                Debug.Log("成功发起轻攻击");
                Attack(AttackLight());
            }
            else
            {
                Debug.Log("攻击冷却中，无法发起轻攻击");
            }
        }

        //右键重攻击
        if (Input.GetMouseButtonDown(1))
        {
            if (CanAttack())
            {
                Debug.Log("成功发起重攻击");
                Attack(AttackHeavy());
            }
            else
            {
                Debug.Log("攻击冷却中，无法发起重攻击");
            }
        }
        
        if (Input.GetKeyDown(KeyCode.E) && magic == maxMagic)
        {
            ResetMPbar();
            StartCoroutine(ShowSpecialSkill());
        }
        else
        {
            Debug.Log($"魔法值不足,当前魔法值: {magic}/{maxMagic}");
        }
        
        if (isInLava)
        {
            lavaDamageTimer += Time.deltaTime;
            if (lavaDamageTimer >= 1f)
            {
                TakeDamage(lavaDamageAmount);
                Debug.Log("玩家站在岩浆地板上，受到伤害: " + lavaDamageAmount);
                lavaDamageTimer = 0f;
            }
        }
        else
        {
            lavaDamageTimer = 0.8f;
        }
    }
    
    private IEnumerator ShowSpecialSkill()
    {
        Transform weaponTransform = null;
        
        weaponTransform = FindChildRecursive(transform, "LongSword");
        
        if (weaponTransform == null)
        {
            Transform[] allChildren = GetComponentsInChildren<Transform>();
            foreach (Transform child in allChildren)
            {
                if (child.CompareTag("Weapon"))
                {
                    weaponTransform = child;
                    break;
                }
            }
        }

        if (weaponTransform != null)
        {
            if (specialSkillPrefab != null)
            {
                GameObject skillObject = Instantiate(specialSkillPrefab, weaponTransform.position, weaponTransform.rotation);
                skillObject.transform.SetParent(weaponTransform);
                skillObject.transform.localPosition = new Vector3(0, 0.32f, 0);
                Debug.Log("Special skill activated on weapon: " + weaponTransform.name);
                attackPowerHeavy *= 2; 
                attackRange *= 2;
                yield return new WaitForSeconds(5.0f);
                attackRange /= 2;
                attackPowerHeavy /= 2; 
                if (skillObject != null)
                {
                    Destroy(skillObject);
                }
            }
            else
            {
                Debug.LogWarning("技能预制体没设置");
            }
        }
        else
        {
            Debug.LogWarning("未找到武器子物体，尝试的查找方法都失败了");
        }
    }

    //递归查找子物体
    private Transform FindChildRecursive(Transform parent, string childName)
    {
        //检查直接子物体
        Transform directChild = parent.Find(childName);
        if (directChild != null)
            return directChild;
        
        //递归检查所有子物体
        foreach (Transform child in parent)
        {
            Transform found = FindChildRecursive(child, childName);
            if (found != null)
                return found;
        }
        
        return null;
    }

    private bool CanAttack()
    {
        return Time.time >= lastAttackTime + attackCooldown;
    }

    public void SetHealthBar(Slider healthBar)
    {
        HPbar = healthBar;
        if (HPbar != null)
        {
            HPbar.maxValue = maxHealth;
            HPbar.value = health;
        }
    }

    public void SetMagicBar(Slider mpBar)
    {
        MPbar = mpBar;
        if (MPbar != null)
        {
            MPbar.maxValue = maxMagic;
            MPbar.value = magic;
        }
    }

    public override void TakeDamage(int damage)
    {
        int damageTaken = Mathf.Max(damage - defensePower, 0);
        health -= damageTaken;
        health = Mathf.Max(health, 0);
        Debug.Log($"{characterName} takes {damageTaken} damage. Health is now {health}.");
        
        if(HPbar != null)
        {
            HPbar.value = health; // 更新血条
        }
        if (health <= 0)
        {
            Die();
        }
    }
    
    public override void Die()
    {
        if(isDead)
            return;
        isDead = true;
        isAttacking = false;
        Debug.Log($"{characterName} has died.");
        
        if(PlayerProgressManager.Instance != null)
        {
            PlayerProgressManager.Instance.OnPlayerDeath();
        }
        StartCoroutine(ShowDeathUIWithDelay());
        //gameObject.SetActive(false); // 玩家死亡时禁用游戏对象
    }

    private IEnumerator ShowDeathUIWithDelay()
    {
        if (deathUIManager == null)
        {
            FindDeathUIManager();
        }
        
        if (deathUIManager != null)
        {
            deathUIManager.ShowDeathUI();
            gameObject.SetActive(false);
        }
        else
        {
            Debug.LogError("DeathUIManager is not assigned.");
            gameObject.SetActive(false); // 如果没有死亡UI管理器，直接禁用玩家对象
        }
        yield return null;
    }
    
    private void FindDeathUIManager()
    {
        //尝试通过FindObjectOfType查找
        deathUIManager = FindObjectOfType<DeathUIManager>();
        
        if (deathUIManager != null)
        {
            Debug.Log("DeathUIManager found successfully!");
            //设置DeathUIManager中的player引用
            deathUIManager.SetPlayer(this);
        }
        else
        {
            Debug.LogWarning("DeathUIManager not found in scene. Searching by name...");
            
            //尝试通过GameObject名称查找
            GameObject deathUIObj = GameObject.Find("DeathUIManager");
            if (deathUIObj != null)
            {
                deathUIManager = deathUIObj.GetComponent<DeathUIManager>();
                if (deathUIManager != null)
                {
                    Debug.Log("DeathUIManager found by name!");
                    deathUIManager.SetPlayer(this);
                }
            }
            
            //尝试通过标签查找（如果你给DeathUIManager设置了特定标签）
            if (deathUIManager == null)
            {
                GameObject deathUIByTag = GameObject.FindWithTag("DeathUIManager"); // 需要给DeathUIManager设置这个标签
                if (deathUIByTag != null)
                {
                    deathUIManager = deathUIByTag.GetComponent<DeathUIManager>();
                    if (deathUIManager != null)
                    {
                        Debug.Log("DeathUIManager found by tag!");
                        deathUIManager.SetPlayer(this);
                    }
                }
            }
        }
    }
    
    public void ResetPlayer()
    {
        isDead = false;
        health = maxHealth;
        
        // 重置血条
        if (HPbar != null)
        {
            HPbar.value = health;
            HPbar.maxValue = maxHealth;
        }
        
        if (!gameObject.activeInHierarchy)
        {
            gameObject.SetActive(true);
        }
        
        Debug.Log($"{characterName} has been reset and revived!");
    }
    public bool IsDead()
    { 
        return isDead;
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
        lastAttackTime = Time.time;
        isAttacking = true;
        
        /*RaycastHit hit;
        Ray ray = new Ray(transform.position, transform.forward);
        Debug.DrawRay(ray.origin, ray.direction * attackRange, Color.red, attackRange);
        if (Physics.Raycast(ray, out hit, attackRange))
        {
            Debug.Log("射线击中物体：" + hit.collider.name);
            if (hit.collider.CompareTag("Enemy"))
            {
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
            }#1#
        }*/
        
        //这里重新修改攻击逻辑，攻击在玩家前方攻击范围内的所有敌人
        Collider[] enemiesInRange = Physics.OverlapSphere(transform.position, attackRange, enemyLayer);
        if (enemiesInRange != null && enemiesInRange.Length > 0)
        {
            RestoreMagic(10);
        }
        foreach (Collider enemy in enemiesInRange)
        {
            if (enemy.CompareTag("Enemy"))
            {
                //敌人相对于玩家的方向向量
                Vector3 directionToEnemy = enemy.transform.position - transform.position;
                directionToEnemy.y = 0; // 忽略高度差异，只考虑水平方向
            
                //敌人方向与玩家前方的夹角
                float angle = Vector3.Angle(transform.forward, directionToEnemy);
            
                //只攻击前方120度范围内的敌人
                float attackAngle = 120f;
                if (angle <= attackAngle / 2)
                {
                    Monster monster = enemy.GetComponent<Monster>();
                    if (monster != null)
                    {
                        
                        float distanceFactor = 1.0f;
                        float distance = directionToEnemy.magnitude;
                    
                        if (distance > attackRange * 0.5f)
                        {
                            distanceFactor = 1.0f - ((distance - attackRange * 0.5f) / (attackRange * 0.5f) * 0.3f);
                        }
                    
                        int finalDamage = Mathf.RoundToInt(attackDamage * distanceFactor);
                        monster.TakeDamage(finalDamage);
                        Debug.Log($"{characterName} attacks {monster.characterName} for {finalDamage} damage at angle {angle}°.");
                    }
                }
            }
        }
    }

    public void UpdateHealthBar()
    {
        if (HPbar != null)
        {
            HPbar.maxValue = maxHealth;
            HPbar.value = health;
        }
    }
    
    public void SetMaxHealth(int newMaxHealth)
    {
        maxHealth = newMaxHealth;
        UpdateHealthBar();
    }
    
    public void SetHealth(int newHealth)
    {
        health = newHealth;
        UpdateHealthBar();
    }
    
    public void Heal(int healAmount)
    {
        health = Mathf.Min(maxHealth, health + healAmount);
        UpdateHealthBar();
        Debug.Log($"Healed {healAmount} points. Current health: {health}/{maxHealth}");
    }
    
    public void RestoreMagic(int amount)
    {
        magic = Mathf.Min(maxMagic, magic + amount);
        if (MPbar != null)
        {
            MPbar.value = magic;
        }
        Debug.Log($"Restored {amount} magic points. Current magic: {magic}/{maxMagic}");
    }
    
    public void ResetMPbar()
    {
        magic = 0;
        if (MPbar != null)
        {
            MPbar.value = magic;
        }
    }    
}

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI; 
using ooparts.dungen;
using Unity.AI.Navigation;
using UnityEngine.XR;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class Monster : Character
{
    private BaseRoom currentRoom;
    private NavMeshSurface roomNavMeshSurface;

    [Header("UI Health Bar")]
    private GameObject healthBarUI; // UI血条实例
    private Slider healthBarSlider; // 血条Slider
    private Canvas uiCanvas; // UI Canvas
    public float healthBarOffsetY = 50f; // 血条在怪物头顶的偏移高度（屏幕像素）
    
    private int maxHealth = 150; // 最大血量
    private Camera playerCamera; // 玩家摄像机

    public Renderer monsterRenderer;
    public Material defaultMaterial;
    public Material damageMaterial;

    public Animator animator;
    private bool isHit = false;
    private float hitAnimationTime = 1f;

    public enum State
    {
        Patrol = 0,
        Chase = 1,
        Attack = 2,
        Idle = 3
    }

    public State currentState = State.Patrol;
    public Vector3[] patrolPoints;
    public float patrolSpeed = 1.5f;
    public float chaseSpeed = 2f;
    public float detectionRange = 5f;

    private NavMeshAgent navMeshAgent;
    private Transform player;
    private int currentPatrolIndex = 0;
    private int numberOfPatrolPoints = 4;

    private Vector3 lastPosition;

    private bool isAttacking = false;
    private float attackCooldown = 2.0f;
    private float timeSinceLastAttack = 0f;
    
    private bool navMeshInitialized = false;
    private Vector3 spawnPosition;

    private void Awake()
    {
        health = maxHealth;
        attackRange = 2.0f;
        attackPowerLight = 10;
        UpdateMonsterData();
    }

    void Start()
    {
        currentRoom = GetComponentInParent<BaseRoom>();
        if (currentRoom != null)
        {
            roomNavMeshSurface = currentRoom.navMeshSurface;
        }
        
        animator = GetComponent<Animator>();
        monsterRenderer = GetComponent<SkinnedMeshRenderer>();
        defaultMaterial = monsterRenderer.material;
        
        Vector3 spawnPosition = transform.position;
        
        navMeshAgent = GetComponent<NavMeshAgent>();
        navMeshAgent.enabled = false;
        navMeshInitialized = false;
        
        player = GameObject.FindGameObjectWithTag("Player").transform;
        
        
        // 获取主摄像机
        playerCamera = Camera.main;
        if (playerCamera == null)
        {
            playerCamera = FindObjectOfType<Camera>();
        }

        if (Vector3.Distance(transform.position, spawnPosition) > 0.1f)
        {
            //Debug.Log($"怪物 {name} 位置被NavMeshAgent改变，强制恢复到生成位置");
            navMeshAgent.Warp(spawnPosition);
        }

        

        Invoke("InitializeAfterDelay", 0.1f);
    }

    public void SetHealthBarUI(GameObject healthBarUIInstance)
    {
        healthBarUI = healthBarUIInstance;
        if (health == 0)
        {
            health = maxHealth;
            Debug.Log($"为Monster {name} 初始化血量，当前血量为: {health}，最大血量为: {maxHealth}");
        }
             
        if (healthBarUI != null)
        {
            healthBarSlider = healthBarUI.GetComponentInChildren<Slider>();
            if (healthBarSlider != null)
            {
                healthBarSlider.maxValue = maxHealth;
                healthBarSlider.value = health;
                Debug.Log($"为Monster {name} 设置了UI血条,设置了血量，当前血量为: {health}，最大血量为: {maxHealth}");
            }
        }
    }

    void Update()
    {
        // 更新血条位置
        UpdateHealthBarPosition();

        if (!navMeshInitialized && roomNavMeshSurface != null && roomNavMeshSurface.navMeshData != null)
        {
            navMeshAgent.enabled = true;
            navMeshInitialized = true;
            GeneratePatrolPoints();
            ChangeState(State.Patrol);
            //Debug.Log($"Monster {name} NavMesh检测完成，Agent已启用");
        }
        
        if(IsPlayerInRoom() && currentState != State.Attack)
            ChangeState(State.Chase);
        
        switch (currentState)
        {
            case State.Patrol:
                Patrol();
                break;
            case State.Chase:
                ChasePlayer();
                break;
            case State.Attack:
                //Debug.Log("现在是攻击状态！！！");
                AttackPlayer();
                break;
            case State.Idle:
                Idle();
                break;
        }
    }

    private void UpdateHealthBarPosition()
    {
        if (healthBarUI != null && playerCamera != null)
        {
            // 将怪物的世界坐标转换为屏幕坐标
            Vector3 worldPosition = transform.position + Vector3.up * 3f; // 在怪物头顶上方3米
            Vector3 screenPosition = playerCamera.WorldToScreenPoint(worldPosition);
            
            // 检查怪物是否在摄像机前面
            if (screenPosition.z > 0 && IsPlayerInRoom())
            {
                // 添加偏移量
                screenPosition.y += healthBarOffsetY;
                
                // 设置血条UI位置
                RectTransform rectTransform = healthBarUI.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    rectTransform.position = screenPosition;
                }
                
                // 显示血条
                if (!healthBarUI.activeInHierarchy && IsPlayerInRoom())
                {
                    healthBarUI.SetActive(true);
                }
            }
            else
            {
                // 怪物在摄像机后面，隐藏血条
                if (healthBarUI.activeInHierarchy)
                {
                    healthBarUI.SetActive(false);
                }
            }
        }
    }

    private void UpdateHealthBar()
    {
        if (healthBarSlider != null)
        {
            healthBarSlider.value = health;
        }
    }

    public void UpdateMonsterData()
    {
        int totalCount = 0;
        if(PlayerProgressManager.Instance != null)
        {
            totalCount = PlayerProgressManager.Instance.progressData.totalRunsStarted;
        }
        if (totalCount > 0)
        {
            int times = totalCount / 5; // 每5次运行增加一次
            health += 5 * times;
            maxHealth += 5 * times; // 更新最大血量fH
            attackPowerLight += 1 * times;
            detectionRange += 0.01f * times;
            //Debug.Log($"Monster {name} stats updated: Health = {health}, Attack Power = {attackPowerLight}, Detection Range = {detectionRange}");
        }
    }

    public void TakeDamage(int damage)
    {
        health -= damage;
        health = Mathf.Max(health, 0); // 确保血量不为负

        isHit = true;
        PlayHitAnimation();
        //StartCoroutine(FlashRed());

        // 更新血条
        UpdateHealthBar();

        if (health <= 0)
        {
            StartCoroutine(Die());
        }
        else
        {
            Debug.Log($"{characterName} takes {damage} damage. Health is now {health}.");
        }
    }

    private IEnumerator Die()
    {
        animator.SetTrigger("Die");
        
        // 隐藏血条
        if (healthBarUI != null)
        {
            healthBarUI.SetActive(false);
        }
        
        yield return new WaitForSeconds(1.5f);
        
        // 销毁血条UI
        if (healthBarUI != null)
        {
            Destroy(healthBarUI);
        }
        
        gameObject.SetActive(false);
    }

    private void InitializeAfterDelay()
    {
        GeneratePatrolPoints();
        ChangeState(State.Patrol);
    }

    private bool IsPlayerInRoom()
    {
        if (currentRoom == null || player == null || player.GetComponent<Player>().health <= 0)
            return false;
        
        Vector3 roomCenter = currentRoom.transform.position;
        Vector3 playerPosition = player.position;
        
        // 计算房间的边界范围
        float halfSizeX = currentRoom.Size.x * 0.5f;
        float halfSizeZ = currentRoom.Size.z * 0.5f;
        
        // 计算房间的边界坐标
        float minX = roomCenter.x - halfSizeX;
        float maxX = roomCenter.x + halfSizeX;
        float minZ = roomCenter.z - halfSizeZ;
        float maxZ = roomCenter.z + halfSizeZ;
        
        // 判断玩家是否在房间的矩形边界内
        return playerPosition.x >= minX && playerPosition.x <= maxX &&
               playerPosition.z >= minZ && playerPosition.z <= maxZ;
    }
    
    public void ChangeState(State newState)
    {
        currentState = newState;
        animator.SetInteger("State", (int)newState);
        
        if(newState == State.Patrol)
        {
            animator.SetBool("IsWalking", true);
            isAttacking = false;
        }
        else if(newState == State.Chase)
        {
            animator.SetBool("IsWalking", true);
            isAttacking = false;
        }
        else if(newState == State.Attack)
        {
            animator.SetBool("IsWalking", false);
            isAttacking = true;
        }
        else if(newState == State.Idle)
        {
            animator.SetBool("IsWalking", false);
            isAttacking = false;
        }
    }

    private void GeneratePatrolPoints()
    {
        patrolPoints = new Vector3[numberOfPatrolPoints];
        
        for (int i = 0; i < patrolPoints.Length; i++)
        {
            Vector3 randomPosition = transform.position + new Vector3(Random.Range(-10f, 10f), 0, Random.Range(-10f, 10f));
            //判断randomPosition是否在当前房间内
            if (!IsPatrolPointInRoom(randomPosition))
                continue;
            patrolPoints[i] = randomPosition;
        }
    }
    
    //判断巡逻点是否在当前房间内
    private bool IsPatrolPointInRoom(Vector3 point)
    {
        if (currentRoom == null)
            return false;

        Vector3 roomCenter = currentRoom.transform.position;
        float halfSizeX = currentRoom.Size.x * 0.5f;
        float halfSizeZ = currentRoom.Size.z * 0.5f;

        float minX = roomCenter.x - halfSizeX;
        float maxX = roomCenter.x + halfSizeX;
        float minZ = roomCenter.z - halfSizeZ;
        float maxZ = roomCenter.z + halfSizeZ;

        return point.x >= minX && point.x <= maxX && point.z >= minZ && point.z <= maxZ;
    }

    private void Patrol()
    {
        if (patrolPoints.Length == 0)
            return;

        if (Vector3.Distance(transform.position, patrolPoints[currentPatrolIndex]) < 1f || !IsPatrolPointInRoom(patrolPoints[currentPatrolIndex]))
        {
            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
        }

        if (navMeshAgent.enabled == false)
            return;
        navMeshAgent.speed = patrolSpeed;
        navMeshAgent.SetDestination(patrolPoints[currentPatrolIndex]);
    }

    private void ChasePlayer()
    {
        if (player == null)
            return;

        navMeshAgent.speed = chaseSpeed;
        navMeshAgent.SetDestination(player.position);

        if (Vector3.Distance(transform.position, player.position) <= attackRange)
        {
            timeSinceLastAttack = attackCooldown; // 初始化攻击冷却时间
            ChangeState(State.Attack);
            isAttacking = true;
            animator.SetBool("IsWalking", false);
        }
        else if (Vector3.Distance(transform.position, player.position) > detectionRange)
        {
            ChangeState(State.Patrol);
            animator.SetBool("IsWalking", true);
        }
    }

    private void AttackPlayer()
    {
        if (player == null)
            return;

        if (player.GetComponent<Player>() == null || player.GetComponent<Player>().health <= 0)
        {
            ChangeState(State.Patrol);
            navMeshAgent.isStopped = false;
            return;
        }

        navMeshAgent.isStopped = true;
    
        Vector3 direction = (player.position - transform.position).normalized;
        direction.y = 0;
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
    
        
    
        if (timeSinceLastAttack >= attackCooldown)
        {
            //Debug.Log($"Monster {name} 攻击玩家！");
            animator.SetTrigger("isAttacking");
        
            if (player.GetComponent<Player>() != null)
            {
                player.GetComponent<Player>().TakeDamage(attackPowerLight);
                Debug.Log($"对玩家造成 {attackPowerLight} 点伤害");
            }
        
            timeSinceLastAttack = 0f;
        }

        if (Vector3.Distance(transform.position, player.position) > attackRange)
        {
            ChangeState(State.Chase);
            animator.SetBool("IsWalking", true);
            navMeshAgent.isStopped = false;
            timeSinceLastAttack = 0f;
        } 
        
        timeSinceLastAttack += Time.deltaTime;
    }

    private void Idle()
    {
        Debug.Log("Monster is idle.");
    }

    private void PlayHitAnimation()
    {
        animator.SetTrigger("CombatDamage");
        if (health <= 0)
            StartCoroutine(Die());
        else
            StartCoroutine(TransitionToIdle());
    }

    /*private IEnumerator FlashRed()
    {
        Debug.Log("变化颜色");
        monsterRenderer.material = damageMaterial;
        yield return new WaitForSeconds(0.3f);
        monsterRenderer.material = defaultMaterial;
        isHit = false;
    }*/

    private IEnumerator TransitionToIdle()
    {
        yield return new WaitForSeconds(hitAnimationTime);
        isHit = false;
        animator.SetBool("isHit", isHit);
    }

    public void OnFootstep()
    {
        //Debug.Log("Monster footstep sound played.");
    }
}

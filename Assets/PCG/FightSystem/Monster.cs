using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI; 
using ooparts.dungen;
using Unity.AI.Navigation;
using UnityEngine.XR;

public class Monster : Character
{
    private Room currentRoom; 
    private NavMeshSurface roomNavMeshSurface; 
    
    public Renderer monsterRenderer; // 用于设置怪物的颜色
    public Material defaultMaterial; // 默认材质
    public Material damageMaterial; // 受伤时的材质

    public Animator animator;
    private bool isHit = false; // 用于防止重复触发受伤效果

    private float hitAnimationTime = 1f;

    //状态机
    public enum State
    {
        Patrol = 0,
        Chase = 1,
        Attack = 2,
        Idle = 3
    }

    public State currentState = State.Patrol;
    public Transform[] patrolPoints; // 巡逻点
    public float patrolSpeed = 1f; // 巡逻速度
    public float chaseSpeed = 1f; // 追逐速度
    public float detectionRange = 5f; // 追逐范围

    private NavMeshAgent navMeshAgent;
    private Transform player;
    private int currentPatrolIndex = 0;
    private int numberOfPatrolPoints = 4;
    
    // Start is called before the first frame update
    void Start()
    {
        
        currentRoom = GetComponentInParent<Room>();
        if (currentRoom != null)
        {
            roomNavMeshSurface = currentRoom.navMeshSurface;
            BakeNavMeshForRoom();
        }
        
        health = 100;
        attackRange = 1f;
        animator = GetComponent<Animator>();
        monsterRenderer = GetComponent<SkinnedMeshRenderer>();
        defaultMaterial = monsterRenderer.material;
        
        navMeshAgent = GetComponent<NavMeshAgent>();
        player = GameObject.FindGameObjectWithTag("Player").transform;
        
        GeneratePatrolPoints();
        
        ChangeState(State.Patrol);
    }

    void Update()
    {
        switch (currentState)
        {
            case State.Patrol:
                Patrol();
                break;
            case State.Chase:
                ChasePlayer();
                break;
            case State.Attack:
                AttackPlayer();
                break;
            case State.Idle:
                Idle();
                break;
        }
    }
    
    private void BakeNavMeshForRoom()
    {
        if (roomNavMeshSurface != null)
        {
            roomNavMeshSurface.BuildNavMesh();
            Debug.Log("NavMesh for the room has been baked.");
        }
        else
        {
            Debug.LogWarning("No NavMeshSurface found in the current room.");
        }
    }
    
    private void ChangeState(State newState)
    {
        currentState = newState;
        animator.SetInteger("State", (int)newState);
    }
    
    private void GeneratePatrolPoints()
    {
        // 生成巡逻点
        patrolPoints = new Transform[numberOfPatrolPoints];
        for (int i = 0; i < patrolPoints.Length; i++)
        {
            Vector3 randomPosition = transform.position + new Vector3(Random.Range(-5f, 5f), 0, Random.Range(-5f, 5f));
            GameObject point = new GameObject($"PatrolPoint_{i}");
            point.transform.position = randomPosition;
            patrolPoints[i] = point.transform;
        }
    }

    private void Patrol()
    {
        if(patrolPoints.Length == 0)
            return;
        
        if(Vector3.Distance(transform.position, patrolPoints[currentPatrolIndex].position) < 1f)
        {
            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
        }

        navMeshAgent.speed = patrolSpeed;
        navMeshAgent.SetDestination(patrolPoints[currentPatrolIndex].position);
    }

    private void ChasePlayer()
    {
        if (player == null)
            return;

        navMeshAgent.speed = chaseSpeed;
        navMeshAgent.SetDestination(player.position);
        
        if(Vector3.Distance(transform.position, player.position) <= attackRange)
        {
            ChangeState(State.Attack);
        }
        else if(Vector3.Distance(transform.position, player.position) > detectionRange)
        {
            ChangeState(State.Patrol);
        }
    }
    
    private void AttackPlayer()
    {
        if (player == null)
            return;

        navMeshAgent.isStopped = true; // 停止移动
        animator.SetTrigger("Attack");

        player.GetComponent<Player>().TakeDamage(attackPowerLight); 
        
        if(Vector3.Distance(transform.position, player.position) > attackRange)
        {
            ChangeState(State.Chase);
        }
    }

    private void Idle()
    {
        Debug.Log("Monster is idle.");
    }

    public void TakeDamage(int damage)
    {
        health -= damage;

        isHit = true;
        
        PlayHitAnimation();

        StartCoroutine(FlashRed());
        
        if(health <= 0)
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
        yield return new WaitForSeconds(1.5f);
        gameObject.SetActive(false);
    }

    private void PlayHitAnimation()
    {
        animator.SetTrigger("CombatDamage");
        if(health <= 0)
            StartCoroutine(Die());
        else
            StartCoroutine(TransitionToIdle());
    }

    private IEnumerator FlashRed()
    {
        Debug.Log("变化颜色");
        
        monsterRenderer.material = damageMaterial;
        
        yield return new WaitForSeconds(0.3f); // 持续时间可以根据需要调整

        monsterRenderer.material = defaultMaterial;

        isHit = false;
    }
    
    // 受击动画播放完后，自动返回 Idle 动画
    private IEnumerator TransitionToIdle()
    {
        // 等待 CombatDamage 动画播放结束（假设 CombatDamage 动画持续 0.5 秒）
        yield return new WaitForSeconds(hitAnimationTime);

        isHit = false;
        // 将 isHit 设置为 false，允许过渡回 Idle
        animator.SetBool("isHit", isHit);
    }
}

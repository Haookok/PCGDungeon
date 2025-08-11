using System.Collections;
using System.Collections.Generic;
using ooparts.dungen;
using UnityEngine;

public class ArrowTrap : TrapBase
{
    [Header("箭矢属性")]
    public GameObject arrowPrefab; // 箭矢预制体
    public float arrowSpeed = 0.1f; // 箭矢速度
    public float arrowCooldown = 1f; // 箭矢发射冷却时间
    public int arrowCount = 3; // 每次触发发射的箭矢数量

    private bool isFiring = false; // 是否正在发射箭矢

    protected override void Start()
    {
        base.Start();
        canBeTriggeredMultipleTimes = true;
        cooldownTime = arrowCooldown;
    }
    
    protected override void ExecuteTrapEffect(GameObject target)
    {
        if (isFiring || !isActivated) return; // 如果正在发射或陷阱未激活，则不执行

        isFiring = true;
        PlayTriggerSound(); // 播放触发音效

        StartCoroutine(ReleaseArrows());
    }
    
    private IEnumerator ReleaseArrows()
    {
        // 获取目标位置
        Vector3 targetPosition = transform.position + Vector3.up * 0.5f;
        
        // 计算两侧发射点位置（以玩家为中心）
        float sideDistance = 5f; // 两侧发射点与玩家的距离
        Vector3 playerRight = Vector3.right; // 玩家右侧方向
        
        Vector3 leftSide = targetPosition - playerRight * sideDistance;
        Vector3 rightSide = targetPosition + playerRight * sideDistance;

        leftSide.y = targetPosition.y;
        rightSide.y = targetPosition.y;
        
        for (int i = 0; i < arrowCount; i++)
        {
            //从左侧发射
            GameObject leftArrow = Instantiate(arrowPrefab, leftSide, Quaternion.identity);
            Vector3 leftDirection = (targetPosition - leftSide).normalized;
            leftArrow.transform.rotation = Quaternion.LookRotation(leftDirection);
            //在x上旋转90度
            leftArrow.transform.Rotate(90, 0, 0);
            Arrow leftArrowScript = leftArrow.GetComponent<Arrow>();
            if (leftArrowScript != null)
            {
                leftArrowScript.Initialize(leftDirection, arrowSpeed);
            }
            
            //从右侧发射
            GameObject rightArrow = Instantiate(arrowPrefab, rightSide, Quaternion.identity);
            Vector3 rightDirection = (targetPosition - rightSide).normalized;
            rightArrow.transform.rotation = Quaternion.LookRotation(rightDirection);
            rightArrow.transform.Rotate(90, 0, 0);
            Arrow rightArrowScript = rightArrow.GetComponent<Arrow>();
            if (rightArrowScript != null)
            {
                rightArrowScript.Initialize(rightDirection, arrowSpeed);
            }
            //yield return new WaitForSeconds(0.2f);
            targetPosition += Vector3.up * 0.5f + Vector3.forward * 0.25f; // 每次发射后稍微抬高目标位置
            leftSide += Vector3.up * 0.5f + Vector3.forward * 0.25f; // 更新左侧发射点位置
            rightSide += Vector3.up * 0.5f + Vector3.forward * 0.25f; // 更新右侧发射点位置
            Destroy(leftArrow, 3f);
            Destroy(rightArrow, 3f);
        }
        
        //发射完毕，重置发射状态
        isFiring = false;
        yield return null;
    }
}

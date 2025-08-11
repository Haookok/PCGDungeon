using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ooparts.dungen
{
    public class TeleporterTrap : TrapBase
    {
        [Header("传送门属性")]
        public float teleportDelay = 0.1f; 
        public GameObject teleportEffectPrefab; 
        public float teleportRadius = 1f; 
        public bool preventImmediateRetrigger = true; 
        public float retriggerCooldown = 2f; 
        
        [Header("传送位置设置")]
        public float teleportHeightOffset = 0.5f; // 增加高度偏移
        public float teleportDistanceOffset = 2f; // 添加距离偏移
        
        [Header("视觉反馈")]
        public GameObject activationEffect; 
        public Color teleporterColor = Color.cyan; 
        
        private NormalRoom parentRoom; 
        private bool isCurrentlyTeleporting = false; 
        private static TeleporterTrap lastUsedTeleporter; 
        private static float lastTeleportTime; 
        
        // 添加个人冷却时间跟踪
        private float personalCooldownEndTime = 0f;
        
        [Header("调试设置")]
        public bool showTeleportDebugLines = true;
        public float debugLineDuration = 5f;
        public Color debugLineColor = Color.magenta;
        
        protected override void Start()
        {
            base.Start();
            
            parentRoom = GetComponentInParent<NormalRoom>();
            if (parentRoom == null)
            {
                Debug.LogError($"传送门 {gameObject.name} 未找到父房间！");
            }
            
            if (triggerCollider is SphereCollider sphereCollider)
            {
                sphereCollider.radius = teleportRadius;
            }
            
            canBeTriggeredMultipleTimes = true;
            cooldownTime = retriggerCooldown;
        }
        
        protected override bool CanTrigger(Collider other)
        {
            if (!base.CanTrigger(other)) return false;
            
            if (isCurrentlyTeleporting) return false;
            
            // 检查个人冷却时间
            if (Time.time < personalCooldownEndTime)
            {
                Debug.Log($"传送门 {gameObject.name} 仍在个人冷却中");
                return false;
            }
            
            // 防止立即重复触发同一个传送门
            if (preventImmediateRetrigger && lastUsedTeleporter == this && 
                Time.time - lastTeleportTime < retriggerCooldown)
            {
                Debug.Log($"传送门 {gameObject.name} 被防重复触发机制阻止");
                return false;
            }
            
            return true;
        }
        
        protected override void ExecuteTrapEffect(GameObject target)
        {
            Player player = target.GetComponent<Player>();
            if (player != null && parentRoom != null)
            {
                StartCoroutine(TeleportPlayer(player));
            }
        }
        
        private IEnumerator TeleportPlayer(Player player)
        {
            isCurrentlyTeleporting = true;
            
            List<TeleporterTrap> teleporters = parentRoom.GetTeleportersInRoom();
            
            // 移除当前传送门
            teleporters.Remove(this);
            
            // 移除正在冷却的传送门
            teleporters.RemoveAll(t => Time.time < t.personalCooldownEndTime);
            
            if (teleporters.Count == 0)
            {
                Debug.LogWarning($"房间中没有其他可用传送门！");
                isCurrentlyTeleporting = false;
                yield break;
            }
            
            // 播放传送门激活音效
            PlayTriggerSound();
            Vector3 startPosition = player.transform.position;
            yield return new WaitForSeconds(teleportDelay);
            
            TeleporterTrap targetTeleporter = teleporters[Random.Range(0, teleporters.Count)];
            Vector3 teleportPosition = targetTeleporter.transform.position;
            
            Debug.Log($"玩家即将从传送门 {gameObject.name} 传送到{targetTeleporter.gameObject.name} 传送前的位置: {player.transform.position} 目标位置: {teleportPosition}");
            CharacterController characterController = player.GetComponent<CharacterController>();
            characterController.enabled = false;
            player.transform.position = teleportPosition;
            characterController.enabled = true; // 重新启用角色控制器
            if (showTeleportDebugLines)
            {
                Debug.DrawLine(startPosition, teleportPosition, debugLineColor, debugLineDuration);
                Debug.Log($"传送路径: {startPosition} -> {teleportPosition}");
            }
            
            lastUsedTeleporter = targetTeleporter;
            lastTeleportTime = Time.time;
            targetTeleporter.StartPersonalCooldown();
            StartPersonalCooldown();
            Debug.Log("传送后的位置: " + player.transform.position);
            
            isCurrentlyTeleporting = false;
        }
        
        private Vector3 GetTeleportPosition()
        {
            Vector3 basePosition = transform.position;
            Vector3 safePosition = basePosition + Vector3.up * teleportHeightOffset;
            return safePosition;
        }
        
        private void StartPersonalCooldown()
        {
            personalCooldownEndTime = Time.time + retriggerCooldown;
        }
        
        private void StartCooldown()
        {
            StartPersonalCooldown();
        }
       
        private void OnDrawGizmosSelected()
        {
            // 绘制触发范围
            Gizmos.color = teleporterColor;
            Gizmos.DrawWireSphere(transform.position, teleportRadius);
            
            // 绘制传送位置
            Vector3 teleportPos = GetTeleportPosition();
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(teleportPos, 0.5f);
            Gizmos.DrawLine(transform.position, teleportPos);
        }
    }
}
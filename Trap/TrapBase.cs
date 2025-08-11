using System;
using UnityEngine;
using System.Collections;

namespace ooparts.dungen
{
    public enum TrapType
    {
        Bomb,           // 炸弹
        Teleporter,     // 传送门
        ArrowTrap,      // 暗箭陷阱
        Springboard,    // 跳板
        FireTrap,      // 火焰
        FreezeCloud,    // 冰冻
        HealRegeneration, // 治疗
    }
    
    [System.Serializable]
    public abstract class TrapBase : MonoBehaviour
    {
        [Header("基础属性")]
        public TrapType trapType;
        public bool isActivated = true;
        public bool canBeTriggeredMultipleTimes = false;
        public float cooldownTime = 2f;
        public float detectionRadius = 0.5f;
        
        [Header("视觉效果")]
        public GameObject visualEffect;
        public AudioClip triggerSound;
        
        protected bool isOnCooldown = false;
        protected AudioSource audioSource;
        protected Collider triggerCollider;
        
        protected virtual void Start()
        {
            //获取相关音效
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();
                
            //设置触发区域
            SetupTriggerArea();
        }
        
        protected virtual void SetupTriggerArea()
        {
            //找碰撞体当触发器
            triggerCollider = GetComponent<Collider>();
            if (triggerCollider == null)
            {
                SphereCollider sphere = gameObject.AddComponent<SphereCollider>();
                sphere.radius = detectionRadius;
                sphere.isTrigger = true;
                triggerCollider = sphere;
            }
        }
        
        protected virtual void OnTriggerEnter(Collider other)
        {
            if (CanTrigger(other))
            {
                TriggerTrap(other.gameObject);
            }
        }
        
        protected virtual bool CanTrigger(Collider other)
        {
            if (isOnCooldown) return false;
            if (isActivated && !canBeTriggeredMultipleTimes) return false;
            
            // 检查是否是玩家
            return other.CompareTag("Player");
        }
        
        public virtual void TriggerTrap(GameObject target)
        {
            if (!CanTrigger(target.GetComponent<Collider>())) return;
            
            isActivated = true;
            ExecuteTrapEffect(target);
            //PlayTriggerSound();
            if (canBeTriggeredMultipleTimes)
            {
                StartCoroutine(CooldownCoroutine());
            }
            else
            {
                isActivated = false;
            }
        }
        
        protected abstract void ExecuteTrapEffect(GameObject target);
        
        protected virtual void PlayTriggerSound()
        {
            if (triggerSound != null && audioSource != null)
            {
                Debug.Log("释放音效");
                audioSource.PlayOneShot(triggerSound);
            }
        }
        
        protected virtual IEnumerator CooldownCoroutine()
        {
            isOnCooldown = true;
            yield return new WaitForSeconds(cooldownTime);
            isOnCooldown = false;
        }
        
        protected virtual void ShowVisualEffect()
        {
            if (visualEffect != null)
            {
                GameObject effect = Instantiate(visualEffect, transform.position, transform.rotation);
                Destroy(effect, 3f);
            }
        }
    }
}
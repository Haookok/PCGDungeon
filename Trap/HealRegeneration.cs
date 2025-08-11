using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ooparts.dungen
{
    public class HealRegeneration : TrapBase
    {
        [Header("治疗属性")]
        public int healAmount = 20;
        public GameObject healEffectPrefab; // 治疗效果预制体
    
        private GameObject healEffectInstance;
    
        protected override void ExecuteTrapEffect(GameObject target)
        {
            Player player = target.GetComponent<Player>();
            if (player != null)
            {
                // 播放治疗音效
                PlayTriggerSound();
                player.Heal(healAmount);
                // 启动治疗协程
                StartCoroutine(HealPlayer(player));
            }
        }
    
        private IEnumerator HealPlayer(Player player)
        {
            if (healEffectPrefab != null)
            {
                healEffectInstance = Instantiate(healEffectPrefab, player.transform.position, Quaternion.identity);
            }
            
            // 禁用模型和碰撞体，炸弹从视觉和物理世界中消失
            MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
            Collider collider = GetComponent<Collider>();

            if (meshRenderer != null)
            {
                meshRenderer.enabled = false;  // 禁用模型
            }

            if (collider != null)
            {
                collider.enabled = false;  // 禁用碰撞体
            }

            // 等待引爆时间
            yield return new WaitForSeconds(0.2f);
            
            if (healEffectInstance != null)
            {
                Destroy(healEffectInstance);
            }
            Destroy(gameObject);
            yield return null; 
        }
    }

}

using System.Collections;
using System.Collections.Generic;
using ooparts.dungen;
using UnityEngine;

namespace ooparts.dungen
{
    public class FireTrap : TrapBase
    {
        [Header("火焰属性")]
        public float burnDuration = 4f; // 火焰持续时间
        public int burnDamagePerSecond = 5; // 每秒伤害
        public GameObject fireEffectPrefab; // 火焰效果预制体
        
        
        [Header("燃烧效果")]
        public GameObject burnIndicator; // 火焰效果实例
        
        private GameObject fireEffectInstance;
        private GameObject burnEffect;
        protected override void ExecuteTrapEffect(GameObject target)
        {
            Player player = target.GetComponent<Player>();
            if (player != null)
            {
                // 播放触发音效
                PlayTriggerSound();
                // 启动燃烧协程
                StartCoroutine(BurnPlayer(player));
            }
        }

        private IEnumerator BurnPlayer(Player player)
        {
            if (fireEffectPrefab != null)
            {
                fireEffectInstance = Instantiate(fireEffectPrefab, player.transform.position, Quaternion.identity);
            }

            int count = 0;
            if (burnEffect == null)
            {
                burnEffect = Instantiate(burnIndicator, player.transform.position, Quaternion.identity);
                burnEffect.transform.SetParent(player.transform);
                burnEffect.transform.localPosition = Vector3.zero;
            }
            else
            {
                burnEffect.SetActive(true);
            }
            while (count < burnDuration)
            {
                player.TakeDamage(burnDamagePerSecond);
                count += 1;
                yield return new WaitForSeconds(1.0f);
            }
            
            // 移除燃烧状态指示器
            if (burnEffect != null)
            {
                burnEffect.SetActive(false);
            }

            if (fireEffectInstance != null)
            {
                Destroy(fireEffectInstance);
            }
        }
    }
}


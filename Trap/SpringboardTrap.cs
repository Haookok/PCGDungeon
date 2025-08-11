using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace ooparts.dungen
{
    public class SpringboardTrap : TrapBase
    {
        [Header("跳板属性")] public float launchForce = 800f;
        public Vector3 launchDirection = Vector3.up;
        public float launchHeight = 5f;

        protected override void Start()
        {
            base.Start();
            canBeTriggeredMultipleTimes = true;
            trapType = TrapType.Springboard;
            cooldownTime = 0.5f;
        }

        protected override void ExecuteTrapEffect(GameObject target)
        {
            if (target.TryGetComponent(out Player player))
            {
                if (!isOnCooldown)
                {
                    // 播放跳板音效
                    if (audioSource != null && triggerSound != null)
                    {
                        audioSource.PlayOneShot(triggerSound);
                    }

                    // 计算跳跃方向和力度
                    Vector3 force = launchDirection.normalized * launchForce;
                    force.y += Mathf.Sqrt(2 * Mathf.Abs(Physics.gravity.y) * launchHeight);

                    // 应用力到玩家身上
                    player.GetComponent<Rigidbody>().AddForce(force, ForceMode.Impulse);

                }
            }
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using ooparts.dungen;
using UnityEngine;

public class BombTrap : TrapBase
{
    [Header("炸弹属性")]
    public float explosionRadius = 1f;
    public int explosionDamage = 30;
    public GameObject explosionEffectPrefab;
    public float fuseTime = 0.5f;
    private GameObject explosionEffect;
    private bool isArmed = false;
    
    protected override void ExecuteTrapEffect(GameObject target)
    {
        if(explosionEffectPrefab != null)
        {
            explosionEffect = Instantiate(explosionEffectPrefab, transform.position, Quaternion.identity);
        }
        PlayTriggerSound();
        Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius);
        bool isPlayerGetHit = false;
        foreach (Collider hit in hits)
        {
            Player player = hit.GetComponent<Player>();
            if (player != null && !isPlayerGetHit)
            {
                player.TakeDamage(explosionDamage);
                isPlayerGetHit = true;
            }
        }

        StartCoroutine(DeleteBomb());
    }

    private IEnumerator DeleteBomb()
    {
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
        // 销毁实例化的爆炸效果
        if (explosionEffect != null)
        {
            Destroy(explosionEffect);
        }
        // 等待音效播放完成
        yield return new WaitForSeconds(0.5f);

        

        // 销毁炸弹对象本身
        Destroy(gameObject);
    }
}

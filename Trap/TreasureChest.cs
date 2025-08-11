using System;
using System.Collections;
using System.Collections.Generic;
using ooparts.dungen;
using UnityEngine;

public class TreasureChest : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        Player player = other.GetComponent<Player>();
        if (player != null)
        {
            PlayerBuffManager buffManager = player.GetComponent<PlayerBuffManager>();
            if (buffManager != null)
            {
                buffManager.ApplyAllBuffs();
                Debug.Log("宝箱已打开，所有增益效果已应用。");
                Destroy(gameObject);
            }
            else
            {
                Debug.Log("没找到buffManager");
            }
        }
    }
}

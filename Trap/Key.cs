using System.Collections;
using System.Collections.Generic;
using ooparts.dungen;
using UnityEngine;

public class Key : MonoBehaviour
{
    [Header("钥匙属性")]
    public string keyName = "Default Key"; //钥匙名称
    public AudioClip pickupSound; //声音
    public TrapRoom parentRoom; //钥匙所在的房间
    public bool isPickedUp = false; //是否被拾取

    private void Start()
    {
        //获取钥匙所在的房间
        parentRoom = GetComponentInParent<TrapRoom>();
        if (parentRoom == null)
        {
            Debug.LogError($"钥匙 {gameObject.name} 未找到父房间！");
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        Player player = other.GetComponent<Player>();
        if (player != null)
        {
            if (pickupSound != null)
            {
                AudioSource.PlayClipAtPoint(pickupSound, transform.position);
            }

            isPickedUp = true;
            
            OnDestroy();
        }
    }
    
    private void OnDestroy()
    {
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        Collider collider = GetComponent<Collider>();

        if (meshRenderer != null)
        {
            meshRenderer.enabled = false; 
        }

        if (collider != null)
        {
            collider.enabled = false;
        }
    }
}

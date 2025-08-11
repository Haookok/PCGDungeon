using System.Collections;
using UnityEngine;

public class LavaFloor : MonoBehaviour
{
    [Header("Lava Floor Settings")]
    private int damageAmount = 150;

    [Header("效果")]
    public AudioSource lavaAudio;


    private void Start()
    {
        if (lavaAudio != null)
        {
            lavaAudio.Play();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Player player = other.GetComponent<Player>();
            if (player != null)
            {
                player.isInLava = true;
                Debug.Log("Player entered lava.");
            }
        }
    }
    
    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Player player = other.GetComponent<Player>();
            if (player != null)
            {
                if (!player.isInLava)
                {
                    player.isInLava = true;
                }
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Player player = other.GetComponent<Player>();
            if (player != null)
            {
                player.isInLava = false;
                Debug.Log("Player exited lava.");
            }
        }
    }
}
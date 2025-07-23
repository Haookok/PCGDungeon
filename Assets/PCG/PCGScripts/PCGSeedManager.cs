using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PCGSeedManager : MonoBehaviour
{
    [Header("Seed Settings")] 
    public int masterSeed = 12345;

    public bool useTimeSeed = true; //使用时间戳作为种子
    
    private System.Random randomGenerator;
    
    // Start is called before the first frame update
    void Start()
    {
        if (useTimeSeed)
            masterSeed = System.DateTime.Now.Millisecond;

        randomGenerator = new System.Random(masterSeed);
    }

    // Update is called once per frame
    
    public System.Random GetRandomGenerator()
    {
        return randomGenerator;
    }
}

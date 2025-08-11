using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace ooparts.dungen
{
    [System.Serializable]
    public class TrapSpawnSettings
    {
        [Header("生成设置")] 
        public bool enableTraps = true;
        public float minDistanceBetweenTraps = 2f;
        public int maxTrapsPerRoom = 7;
        public float trapSpawnChance = 1.0f;
        
        [Header("陷阱类型")]
        public GameObject bombTrapPrefab;
        public GameObject SpringboardTrapPrefab;
        public GameObject HealRegenerationTrapPrefab;
        public GameObject FireTrapPrefab;
        public GameObject TeleporterTrapPrefab;
        public GameObject ArrowTrapPrefab;
        
        [Header("机关权重")]
        [Range(0, 1)] public float bombWeight = 0.3f;
        [Range(0, 1)] public float springboardWeight = 0f;
        [Range(0, 1)] public float healRegenerationWeight = 0.4f;
        [Range(0, 1)] public float fireTrapWeight = 0.3f;
        [Range(0, 1)] public float teleporterWeight = 1f;
        [Range(0, 1)] public float arrowTrapWeight = 0.3f;

        [Header("机关区域限制")] 
        public float wallMargin = 2f;
        public float monsterMargin = 1f;

        [Header("传送门设置")] 
        public int minTeleportersPerRoom = 4;
        public int maxTeleportersPerRoom = 7;
        public float minDistanceBetweenTeleporters = 7f;
        public bool guaranteeTeleporterPairs = false;
    }
}

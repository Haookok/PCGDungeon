using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace ooparts.dungen
{
    public class TrapRoom : BaseRoom
    {
        [Header("Trap Room Specific Settings")]
        public GameObject lavaFloorPrefab;
        public GameObject platformPrefab;
        public GameObject trapPlatformPrefab;
        public float platformHeight = 0.5f;
        public float trapPlatformProbability = 0.2f;
        
        [Header("Cellular Automata Settings")]
        public int cellularIterations = 5;
        public float initialPlatformDensity = 0.45f;
        public int birthLimit = 4;
        public int deathLimit = 3;
        
        [Header("Safety Settings")]
        public int minPlatformsRequired = 5;
        public float minConnectivityRatio = 0.7f;
        
        private GameObject _platformsObject;
        private GameObject _lavaObject;
        private bool[,] _platformMap;
        private List<Vector3> _platformPositions = new List<Vector3>();

        public GameObject KeyPrefab;
        private Key _key;
        public bool isCleared = false;
        
        [Header("FXs")]
        public GameObject LavaBubblesPrefab;
        public GameObject LavaParticlesPrefab;
        public GameObject LavaSteamPrefab;
        
        public override void Start()
        {
            base.Start();
            roomType = RoomType.Trap;
        }
        
        public override void Update()
        {
            CheckRoomClearStatus();
        }

        public override IEnumerator Generate()
        {
            yield return CreateLavaFloor();
            GeneratePlatformLayout();
            GenerateFX();
            yield return CreatePlatforms();
            FindUICanvas();
            SpawnKey();
        }

        public void GenerateFX()
        {
            if(LavaParticlesPrefab != null)
            {
                GameObject lavaParticles = Instantiate(LavaParticlesPrefab);
                lavaParticles.transform.parent = transform;
                lavaParticles.transform.localPosition = Vector3.zero;
            }
            if(LavaSteamPrefab != null)
            {
                GameObject lavaSteam = Instantiate(LavaSteamPrefab);
                lavaSteam.transform.parent = transform;
                lavaSteam.transform.localPosition = Vector3.zero;
            }
        }

        private IEnumerator CreateLavaFloor()
        {
            _lavaObject = new GameObject("Lava Floor");
            _lavaObject.transform.parent = transform;
            _lavaObject.transform.localPosition = Vector3.zero;
            
            for (int x = 0; x < Size.x; x++)
            {
                for (int z = 0; z < Size.z; z++)
                {
                    Vector3 position = RoomMapManager.TileSize * new Vector3(
                        x - Size.x * 0.5f + 0.5f, 
                        0f,
                        z - Size.z * 0.5f + 0.5f
                    );
                    
                    GameObject lavaFloor = Instantiate(lavaFloorPrefab);
                    lavaFloor.name = $"Lava ({x}, {z})";
                    lavaFloor.transform.parent = _lavaObject.transform;
                    lavaFloor.transform.localPosition = position;
                    lavaFloor.transform.localScale *= RoomMapManager.TileSize;
                    
                    if (_map != null)
                    {
                        IntVector2 coordinates = new IntVector2(Coordinates.x + x, Coordinates.z + z);
                        if (_map.GetTileType(coordinates) == TileType.Empty)
                        {
                            _map.SetTileType(coordinates, TileType.Room);
                        }
                    }
                }
            }
            yield return null;
        }

        private void GeneratePlatformLayout()
        {
            _platformMap = new bool[Size.x, Size.z];
            
            //随机生成初始平台布局
            for (int x = 0; x < Size.x; x++)
            {
                for (int z = 0; z < Size.z; z++)
                {
                    _platformMap[x, z] = Random.value < initialPlatformDensity;
                }
            }
            
            //使用细胞自动机规则迭代生成平台布局
            for (int i = 0; i < cellularIterations; i++)
            {
                _platformMap = ApplyCellularAutomataRules(_platformMap);
            }
            
            EnsureEdgePlatforms();
            EnsureConnectivity();
            
            //确保平台布局满足最小平台数量要求
            if (!ValidatePlatformLayout())
            {
                Debug.LogWarning($"Platform layout validation failed for room {Num}, regenerating...");
                GenerateFallbackLayout();
            }
        }

        private bool[,] ApplyCellularAutomataRules(bool[,] currentMap)
        {
            bool[,] newMap = new bool[Size.x, Size.z];
            
            for (int x = 0; x < Size.x; x++)
            {
                for (int z = 0; z < Size.z; z++)
                {
                    int neighborCount = CountPlatformNeighbors(currentMap, x, z);
                    
                    if (currentMap[x, z])
                    {
                        newMap[x, z] = neighborCount >= deathLimit;
                    }
                    else
                    {
                        newMap[x, z] = neighborCount > birthLimit;
                    }
                }
            }
            
            return newMap;
        }

        private int CountPlatformNeighbors(bool[,] map, int x, int z)
        {
            int count = 0;
            for (int i = -1; i <= 1; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    int neighborX = x + i;
                    int neighborZ = z + j;
                    
                    if (neighborX < 0 || neighborX >= Size.x || neighborZ < 0 || neighborZ >= Size.z)
                    {
                        count++;
                    }
                    else if (map[neighborX, neighborZ])
                    {
                        count++;
                    }
                }
            }
            return count;
        }

        private void EnsureEdgePlatforms()
        {
            int centerX = Size.x / 2;
            int centerZ = Size.z / 2;
            
            for (int x = centerX - 1; x <= centerX + 1; x++)
            {
                for (int z = centerZ - 1; z <= centerZ + 1; z++)
                {
                    if (x >= 0 && x < Size.x && z >= 0 && z < Size.z)
                    {
                        _platformMap[x, z] = true;
                    }
                }
            }
            
            for (int side = 0; side < 4; side++)
            {
                int edgeX = side < 2 ? (side == 0 ? 2 : Size.x - 3) : Random.Range(2, Size.x - 2);
                int edgeZ = side >= 2 ? (side == 2 ? 2 : Size.z - 3) : Random.Range(2, Size.z - 2);
                
                if (edgeX >= 0 && edgeX < Size.x && edgeZ >= 0 && edgeZ < Size.z)
                {
                    _platformMap[edgeX, edgeZ] = true;
                    
                    for (int i = -1; i <= 1; i++)
                    {
                        for (int j = -1; j <= 1; j++)
                        {
                            int adjX = edgeX + i;
                            int adjZ = edgeZ + j;
                            if (adjX >= 0 && adjX < Size.x && adjZ >= 0 && adjZ < Size.z)
                            {
                                if (Random.value < 0.6f)
                                {
                                    _platformMap[adjX, adjZ] = true;
                                }
                            }
                        }
                    }
                }
            }
        }

        private void EnsureConnectivity()
        {
            int centerX = Size.x / 2;
            int centerZ = Size.z / 2;
            
            for (int dir = 0; dir < 4; dir++)
            {
                int dx = dir == 0 ? 1 : (dir == 1 ? -1 : 0);
                int dz = dir == 2 ? 1 : (dir == 3 ? -1 : 0);
                
                for (int step = 1; step < Mathf.Max(Size.x, Size.z) / 3; step++)
                {
                    int x = centerX + dx * step;
                    int z = centerZ + dz * step;
                    
                    if (x >= 0 && x < Size.x && z >= 0 && z < Size.z)
                    {
                        if (Random.value < 0.7f)
                        {
                            _platformMap[x, z] = true;
                            
                            if (Random.value < 0.3f)
                            {
                                int branchDir = Random.Range(0, 2) == 0 ? -1 : 1;
                                int branchX = x + (dz != 0 ? branchDir : 0);
                                int branchZ = z + (dx != 0 ? branchDir : 0);
                                
                                if (branchX >= 0 && branchX < Size.x && branchZ >= 0 && branchZ < Size.z)
                                {
                                    _platformMap[branchX, branchZ] = true;
                                }
                            }
                        }
                    }
                }
            }
        }

        private bool ValidatePlatformLayout()
        {
            int platformCount = 0;
            for (int x = 0; x < Size.x; x++)
            {
                for (int z = 0; z < Size.z; z++)
                {
                    if (_platformMap[x, z])
                    {
                        platformCount++;
                    }
                }
            }
            
            return platformCount >= minPlatformsRequired;
        }

        private void GenerateFallbackLayout()
        {
            Debug.Log($"Generating fallback layout for room {Num}");
            
            //清除现有平台布局
            _platformMap = new bool[Size.x, Size.z];
            
            int centerX = Size.x / 2;
            int centerZ = Size.z / 2;
            
            for (int x = 1; x < Size.x - 1; x++)
            {
                _platformMap[x, centerZ] = true;
                if (Random.value < 0.3f && centerZ > 0)
                    _platformMap[x, centerZ - 1] = true;
                if (Random.value < 0.3f && centerZ < Size.z - 1)
                    _platformMap[x, centerZ + 1] = true;
            }
            
            for (int z = 1; z < Size.z - 1; z++)
            {
                _platformMap[centerX, z] = true;
                if (Random.value < 0.3f && centerX > 0)
                    _platformMap[centerX - 1, z] = true;
                if (Random.value < 0.3f && centerX < Size.x - 1)
                    _platformMap[centerX + 1, z] = true;
            }
            
            for (int i = 0; i < Size.x * Size.z * 0.2f; i++)
            {
                int x = Random.Range(1, Size.x - 1);
                int z = Random.Range(1, Size.z - 1);
                _platformMap[x, z] = true;
            }
        }

        private IEnumerator CreatePlatforms()
        {
            _platformsObject = new GameObject("Platforms");
            _platformsObject.transform.parent = transform;
            _platformsObject.transform.localPosition = Vector3.zero;
            
            _platformPositions.Clear();
            
            for (int x = 0; x < Size.x; x++)
            {
                for (int z = 0; z < Size.z; z++)
                {
                    if (_platformMap[x, z])
                    {
                        Vector3 position = RoomMapManager.TileSize * new Vector3(
                            x - Size.x * 0.5f + 0.5f,
                            platformHeight,
                            z - Size.z * 0.5f -0.5f
                        );
                        
                        bool isTrapPlatform = Random.value < trapPlatformProbability;
                        GameObject prefabToUse = isTrapPlatform ? trapPlatformPrefab : platformPrefab;
                        if (isTrapPlatform)
                            position += new Vector3(-0.5f, 0, 0.5f);
                        if (prefabToUse != null)
                        {
                            GameObject platform = Instantiate(prefabToUse);
                            platform.name = $"{(isTrapPlatform ? "Trap" : "Normal")}Platform ({x}, {z})";
                            platform.transform.parent = _platformsObject.transform;
                            platform.transform.localPosition = position;
                            platform.transform.localScale *= RoomMapManager.TileSize;
                            
                            _platformPositions.Add(position);
                        }
                    }
                }
            }
            
            Debug.Log($"Created {_platformPositions.Count} platforms in trap room {Num}");
            yield return null;
        }

        public override IEnumerator CreateMonsters()
        {
            Debug.Log("机关房不生成怪物");
            yield return null;
        }

        public override IEnumerator CreatePlayer()
        {
            Debug.Log("机关房不生成玩家");
            yield return null;
        }

        private void OnDrawGizmos()
        {
            if (_platformMap != null)
            {
                for (int x = 0; x < Size.x; x++)
                {
                    for (int z = 0; z < Size.z; z++)
                    {
                        Vector3 pos = transform.position + RoomMapManager.TileSize * new Vector3(
                            x - Size.x / 2f, 
                            platformHeight, 
                            z - Size.z / 2f
                        );
                        
                        Gizmos.color = _platformMap[x, z] ? Color.green : Color.red;
                        Gizmos.DrawWireCube(pos, Vector3.one * RoomMapManager.TileSize * 0.8f);
                    }
                }
            }
        }
        
        //随机选择一个平台位置
        public Vector3 GetRandomPlatformLocalPosition()
        {
            if (_platformPositions.Count == 0)
            {
                Debug.LogWarning($"No platforms available in trap room {Num}");
                return transform.localPosition;
            }
            
            return _platformPositions[Random.Range(0, _platformPositions.Count)];
        }
        
        //在随机平台位置上生成钥匙
        public void SpawnKey()
        {
            if (KeyPrefab == null)
            {
                Debug.LogError("KeyPrefab is not assigned in TrapRoom!");
                return;
            }
            
            GameObject key = Instantiate(KeyPrefab);
            key.transform.parent = transform;
            key.transform.localPosition = GetRandomPlatformLocalPosition() + new Vector3(-0.5f, 0, 0.5f);
            _key = key.GetComponent<Key>();
            Debug.Log($"Spawned key at {key.transform.localPosition} in trap room {Num}");
        }

        protected override void CheckRoomClearStatus()
        {
            if (hasGivenBuff) return;
            if (_key != null && _key.isPickedUp)
            {
                Debug.Log("钥匙已被拾取，机关房间清除");
                OnRoomCleared();
            }
        }
        
        protected override void OnRoomCleared()
        {
            base.OnRoomCleared();
            isCleared = true;
            Debug.Log($"Trap room {Num} cleared, giving player a buff!");
        }
        
    }
}
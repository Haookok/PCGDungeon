using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using ooparts.dungen;
using Cinemachine;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using Random = UnityEngine.Random;
using Slider = UnityEngine.UI.Slider;
using TMPro;

namespace ooparts.dungen
{
    public enum RoomType
    {
        Normal,
        Trap
    }

    public abstract class BaseRoom : MonoBehaviour
    {
        public Tile TilePrefab;
        public GameObject WallPrefab;
        public GameObject PillarPrefab;
        
        protected GameObject _tilesObject;
        protected GameObject _wallsObject;
        protected GameObject _pillarsObject;
        private Tile[,] _tiles;
        
        [Header("Basic Room Settings")]
        public Corridor CorridorPrefab;
        public IntVector2 Size;
        public IntVector2 Coordinates;
        public int Num;
        public RoomType roomType;

        [Header("Room Components")]
        public NavMeshSurface navMeshSurface;
        public RoomSetting Setting;

        [Header("Prefab References")]
        public GameObject PlayerPrefab;
        public GameObject MonsterPrefab;
        public int MonsterCount;
        public GameObject[] Monsters;

        [Header("UI References")]
        public Slider PlayerHealthBar;
        public GameObject MonsterHPbarPrefab;
        public PlayerBuffManager BuffManager;

        [Header("Room State")]
        public bool isRoomCleared;
        protected bool hasGivenBuff = false;

        [Header("UI Settings")]
        public GameObject buffNotificationPrefab;
        public float notificationDuration = 5f;

        [Header("Room Marking")]
        public bool showRoomNumberOnClear = true;
        public GameObject roomNumberPrefab;
        private GameObject roomNumberInstance;

        //protected内容
        protected Map _map;
        protected GameObject _monstersObject;
        protected Canvas uiCanvas;
        protected GameObject playerInRoom;
        protected Camera playerCamera;
        
        public bool isMonsterGenerated;
        
        //房间连接
        public Dictionary<BaseRoom, Corridor> RoomCorridor = new Dictionary<BaseRoom, Corridor>();

        #region 初始化
        public virtual void Init(Map map)
        {
            _map = map;
            isMonsterGenerated = false;
        }

        public virtual void Start()
        {
            isRoomCleared = false;
            InitializeNavMesh();
            InitializeCamera();
        }

        protected virtual void InitializeNavMesh()
        {
            if (navMeshSurface == null)
            {
                navMeshSurface = GetComponent<NavMeshSurface>();
                if (navMeshSurface == null)
                {
                    Debug.LogError("NavMeshSurface component is missing on the Room object.");
                }
            }
        }

        protected virtual void InitializeCamera()
        {
            playerCamera = Camera.main;
            if (playerCamera == null)
            {
                playerCamera = FindObjectOfType<Camera>();
            }
        }
        #endregion

        #region 抽象方法
        public abstract IEnumerator Generate();
        public abstract IEnumerator CreateMonsters();
        public abstract IEnumerator CreatePlayer();
        #endregion

        #region 虚函数
        public virtual void Update()
        {
            CheckRoomClearStatus();
        }
        
        protected virtual Vector3 CalculatePillarPosition(int x, int z, IntVector2 leftBottom, IntVector2 rightTop)
        {
            float xPos = x - Coordinates.x - Size.x * 0.5f + 0.5f;
            float zPos = z - Coordinates.z - Size.z * 0.5f + 0.5f;
            
            if (x == leftBottom.x)
                xPos += 0.3f;
            else if (x == rightTop.x)
                xPos -= 0.3f;
                
            if (z == leftBottom.z)
                zPos += 0.3f;
            else if (z == rightTop.z)
                zPos -= 0.3f;
                
            return new Vector3(xPos, 0f, zPos);
        }

        public virtual IEnumerator CreateWalls()
        {
            _wallsObject = new GameObject("Walls");
            _wallsObject.transform.parent = transform;
            _wallsObject.transform.localPosition = Vector3.zero;
            
            _pillarsObject = new GameObject("Pillars");
            _pillarsObject.transform.parent = transform;
            _pillarsObject.transform.localPosition = Vector3.zero;

            IntVector2 leftBottom = new IntVector2(Coordinates.x - 1, Coordinates.z - 1);
            IntVector2 rightTop = new IntVector2(Coordinates.x + Size.x, Coordinates.z + Size.z);
            
            for (int x = leftBottom.x; x <= rightTop.x; x++)
            {
                for (int z = leftBottom.z; z <= rightTop.z; z++)
                {
                    if ((x == leftBottom.x || x == rightTop.x) && (z == leftBottom.z || z == rightTop.z))
                    {
                        GameObject newPillar = Instantiate(PillarPrefab);
                        newPillar.name = "Pillar (" + x + ", " + z + ")";
                        newPillar.transform.parent = _pillarsObject.transform;
                        newPillar.transform.localPosition = RoomMapManager.TileSize * CalculatePillarPosition(x, z, leftBottom, rightTop);
                        newPillar.transform.localScale *= RoomMapManager.TileSize;
                        continue;
                    }
                    
                    //跳过不是墙的位置
                    if ((x != leftBottom.x && x != rightTop.x && z != leftBottom.z && z != rightTop.z) ||
                        (_map.GetTileType(new IntVector2(x, z)) != TileType.Wall))
                    {
                        continue;
                    }
                    
                    //创建墙体
                    Quaternion rotation = Quaternion.identity;
                    Vector3 position = Vector3.zero;
                    
                    if (x == leftBottom.x)
                    {
                        rotation = MapDirection.West.ToRotation();
                        position = RoomMapManager.TileSize * new Vector3(x - Coordinates.x - Size.x * 0.5f + 0.8f, 0f, z - Coordinates.z - Size.z * 0.5f + 0.5f);
                    }
                    else if (x == rightTop.x)
                    {
                        rotation = MapDirection.East.ToRotation();
                        position = RoomMapManager.TileSize * new Vector3(x - Coordinates.x - Size.x * 0.5f + 0.2f, 0f, z - Coordinates.z - Size.z * 0.5f + 0.5f);
                    }
                    else if (z == leftBottom.z)
                    {
                        rotation = MapDirection.South.ToRotation();
                        position = RoomMapManager.TileSize * new Vector3(x - Coordinates.x - Size.x * 0.5f + 0.5f, 0f, z - Coordinates.z - Size.z * 0.5f + 0.8f);
                    }
                    else if (z == rightTop.z)
                    {
                        rotation = MapDirection.North.ToRotation();
                        position = RoomMapManager.TileSize * new Vector3(x - Coordinates.x - Size.x * 0.5f + 0.5f, 0f, z - Coordinates.z - Size.z * 0.5f + 0.2f);
                    }

                    GameObject newWall = Instantiate(WallPrefab);
                    newWall.name = "Wall (" + x + ", " + z + ")";
                    newWall.transform.parent = _wallsObject.transform;
                    newWall.transform.localPosition = position;
                    newWall.transform.localRotation = rotation;
                    newWall.transform.localScale *= RoomMapManager.TileSize;
                }
            }
            yield return null;
        }

        protected virtual IEnumerator CreateTiles()
        {
            _tilesObject = new GameObject("Tiles");
            
            _tilesObject.transform.parent = transform;
            _tilesObject.transform.localPosition = Vector3.zero;

            _tiles = new Tile[Size.x, Size.z];
            for (int x = 0; x < Size.x; x++)
            {
                for (int z = 0; z < Size.z; z++)
                {
                    _tiles[x, z] = CreateTile(new IntVector2((Coordinates.x + x), Coordinates.z + z));
                }
            }
            yield return new WaitForEndOfFrame();
        }
        
        protected virtual Tile CreateTile(IntVector2 coordinates)
        {
            if (_map.GetTileType(coordinates) == TileType.Empty)
            {
                _map.SetTileType(coordinates, TileType.Room);
            }
            else
            {
                Debug.LogError("Tile Conflict!");
            }
            
            Tile newTile = Instantiate(TilePrefab);
            newTile.Coordinates = coordinates;
            newTile.name = "Tile " + coordinates.x + ", " + coordinates.z;
            newTile.transform.parent = _tilesObject.transform;
            newTile.transform.localPosition = RoomMapManager.TileSize * new Vector3(
                coordinates.x - Coordinates.x - Size.x * 0.5f + 0.5f, 
                0f, 
                coordinates.z - Coordinates.z - Size.z * 0.5f + 0.5f);
            newTile.transform.GetChild(0).GetComponent<Renderer>().material = Setting.floor;
            return newTile;
        }
        
        public virtual void BakeNavMesh()
        {
            if (navMeshSurface != null)
            {
                ConfigureNavMeshSurface();
                navMeshSurface.BuildNavMesh();
            }
        }

        public virtual void BakeNavMeshWithBounds()
        {
            if (navMeshSurface != null)
            {
                Vector3 roomSize = new Vector3(Size.x * RoomMapManager.TileSize, 
                    10f, 
                    Size.z * RoomMapManager.TileSize);
                
                GameObject boundingBox = new GameObject("NavMesh Bounds");
                boundingBox.transform.parent = transform;
                boundingBox.transform.localPosition = Vector3.zero;
                
                BoxCollider boundsCollider = boundingBox.AddComponent<BoxCollider>();
                boundsCollider.size = roomSize;
                boundsCollider.isTrigger = true;
                
                ConfigureNavMeshSurface();
                navMeshSurface.BuildNavMesh();
                
                DestroyImmediate(boundingBox);
            }
        }
        #endregion

        #region protected方法
        protected virtual void ConfigureNavMeshSurface()
        {
            if (navMeshSurface != null)
            {
                navMeshSurface.collectObjects = CollectObjects.Children;
                
                int defaultLayer = LayerMask.GetMask("Default");
                if (defaultLayer != -1)
                {
                    navMeshSurface.layerMask = defaultLayer;
                }
                navMeshSurface.useGeometry = NavMeshCollectGeometry.RenderMeshes;
            }
        }

        protected virtual void CheckRoomClearStatus()
        {
            if (hasGivenBuff) return;
            if (!isMonsterGenerated)
                return;
            if (Monsters == null || Monsters.Length == 0)
            {
                Debug.Log($"Monster长度为{Monsters.Length}，isMonsterGenerated为{isMonsterGenerated}，直接标记房间{Num}为清理完毕");
                isRoomCleared = true;
                return;
            }

            bool allMonstersDead = true;
            foreach (GameObject monster in Monsters)
            {
                if (monster != null && monster.activeInHierarchy)
                {
                    Monster monsterScript = monster.GetComponent<Monster>();
                    if (monsterScript != null && monsterScript.health > 0)
                    {
                        allMonstersDead = false;
                        break;
                    }
                }
            }
            
            if (allMonstersDead)
                OnRoomCleared();
        }

        protected virtual void OnRoomCleared()
        {
            isRoomCleared = true;
            hasGivenBuff = true;
            Debug.Log($"房间{Num}清理完毕，准备给予玩家Buff");

            if (showRoomNumberOnClear && roomNumberPrefab != null)
                DisplayRoomNumber();

            System.Array buffTypes = Enum.GetValues(typeof(PlayerBuffManager.BuffType));
            PlayerBuffManager.BuffType randomBuff = (PlayerBuffManager.BuffType)buffTypes.GetValue(Random.Range(0, buffTypes.Length));

            if (PlayerProgressManager.Instance != null)
            {
                PlayerProgressManager.Instance.OnRoomCleared(randomBuff);
            }
            
            GivePlayerBuff(randomBuff);
        }

        protected virtual void DisplayRoomNumber()
        {
            if (roomNumberInstance != null)
                Destroy(roomNumberInstance);
            
            roomNumberInstance = Instantiate(roomNumberPrefab, transform);
            roomNumberInstance.name = "RoomNumber_" + Num;
            
            roomNumberInstance.transform.localPosition = new Vector3(0, 1, 0);
            TextMeshPro textMesh = roomNumberInstance.GetComponentInChildren<TextMeshPro>();
            if (textMesh != null)
            {
                textMesh.text = $"{Num}";
                textMesh.fontSize = 16;
                textMesh.color = Color.red;
                textMesh.alignment = TextAlignmentOptions.Center;
                Debug.Log($"显示房间{Num}的房间号");
            }
            else
            {
                Debug.LogError("未找到TextMeshPro组件，请确保房间号预制件上有TextMeshPro组件");
            }
        }

        protected virtual void GivePlayerBuff(PlayerBuffManager.BuffType randomBuff)
        {
            if (playerInRoom == null)
                FindPlayer();
            if (playerInRoom == null)
            {
                Debug.Log("房间中未找到player");
                return;
            }

            PlayerBuffManager buffManager = playerInRoom.GetComponent<PlayerBuffManager>();
            if (buffManager != null)
            {
                buffManager.ApplyBuff(randomBuff);
                ShowBuffNotification(randomBuff);
            }
        }

        protected virtual void FindPlayer()
        {
            GameObject playerObject = GameObject.Find("Player");
            if (playerObject == null)
                playerObject = GameObject.FindWithTag("Player");
            if (playerObject != null)
            {
                playerInRoom = playerObject;
            }
            else
            {
                Debug.LogError("未找到玩家对象");
            }
        }

        protected virtual void FindUICanvas()
        {
            if (uiCanvas == null)
            {
                GameObject canvasObj = GameObject.Find("Canvas");
                if (canvasObj != null)
                {
                    uiCanvas = canvasObj.GetComponent<Canvas>();
                }
                
                if (uiCanvas == null)
                {
                    uiCanvas = FindObjectOfType<Canvas>();
                }
                
                if (uiCanvas == null)
                {
                    Canvas[] allCanvases = FindObjectsOfType<Canvas>();
                    foreach (Canvas canvas in allCanvases)
                    {
                        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                        {
                            uiCanvas = canvas;
                            break;
                        }
                    }
                }
                
                if (uiCanvas == null)
                {
                    Debug.LogError("未找到UI Canvas！请确保场景中有Canvas。");
                }
            }
        }

        protected virtual void CreateMonsterHealthBarUI(GameObject monster)
        {
            if (MonsterHPbarPrefab == null)
            {
                Debug.LogWarning("MonsterHPbarPrefab未设置！");
                return;
            }
            
            if (uiCanvas == null)
            {
                Debug.LogWarning("未找到UI Canvas，无法创建Monster血条！");
                return;
            }
            
            GameObject healthBarUI = Instantiate(MonsterHPbarPrefab);
            healthBarUI.name = $"{monster.name}_HealthBar";
            healthBarUI.transform.SetParent(uiCanvas.transform, false);
            healthBarUI.SetActive(false);
            
            RectTransform rectTransform = healthBarUI.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = new Vector2(0, 0);
            }
            
            Monster monsterScript = monster.GetComponent<Monster>();
            if (monsterScript != null)
            {
                monsterScript.SetHealthBarUI(healthBarUI);
            }
            else
            {
                Debug.LogWarning($"Monster {monster.name} 没有Monster脚本组件！");
                Destroy(healthBarUI);
            }
        }

        protected virtual Slider FindHealthBar()
        {
            if (PlayerHealthBar != null)
                return PlayerHealthBar;
            
            GameObject healthBarGameObject = GameObject.Find("HPbar");
            if (healthBarGameObject != null)
            {
                return healthBarGameObject.GetComponent<Slider>();
            }
            
            GameObject taggedHealthBar = GameObject.FindWithTag("HealthBar");
            if (taggedHealthBar != null)
            {
                return taggedHealthBar.GetComponent<Slider>();
            }

            return null;
        }
        
        protected virtual Slider FindMagicBar()
        {
            GameObject magicBarGameObject = GameObject.Find("MPbar");
            if (magicBarGameObject != null)
            {
                return magicBarGameObject.GetComponent<Slider>();
            }
            
            GameObject taggedMagicBar = GameObject.FindWithTag("MagicBar");
            if (taggedMagicBar != null)
            {
                return taggedMagicBar.GetComponent<Slider>();
            }

            return null;
        }

        protected virtual IEnumerator SetupPlayerCamera(GameObject player)
        {
            yield return null;
            
            GameObject cameraObj = GameObject.Find("PlayerFollowCamera");
            if (cameraObj != null)
            {
                CinemachineVirtualCamera virtualCamera = cameraObj.GetComponent<CinemachineVirtualCamera>();
                if (virtualCamera != null)
                {
                    Transform cameraRoot = player.transform.Find("PlayerCameraRoot");
                    if (cameraRoot != null)
                    {
                        virtualCamera.Follow = cameraRoot;
                    }
                    else
                    {
                        Debug.LogWarning("未找到玩家的 PlayerCameraRoot 子物体");
                    }
                }
            }
        }

        protected virtual void ShowBuffNotification(PlayerBuffManager.BuffType buffType)
        {
            Debug.Log("111");
            string message = GetBuffMessage(buffType);

            if (buffNotificationPrefab != null && uiCanvas != null)
            {
                Debug.Log("222");
                GameObject notification = Instantiate(buffNotificationPrefab, uiCanvas.transform);
                
                TextMeshProUGUI[] tmpTexts = notification.GetComponentsInChildren<TextMeshProUGUI>(true);
                if (tmpTexts.Length > 0)
                {
                    foreach (TextMeshProUGUI tmpText in tmpTexts)
                    {
                        tmpText.text = message;
                    }
                }

                StartCoroutine(DestroyNotificationAfterDelay(notification));
            }
        }

        protected virtual string GetBuffMessage(PlayerBuffManager.BuffType buffType)
        {
            switch (buffType)
            {
                case PlayerBuffManager.BuffType.IncreaseMaxHealth:
                    return "Room Cleared!\nBuff Gained: Max Health +10";
                case PlayerBuffManager.BuffType.IncreaseAttackPower:
                    return "Room Cleared!\nBuff Gained: Attack Power +5 (Light) /+10 (Heavy)";
                case PlayerBuffManager.BuffType.IncreaseDefensePower:
                    return "Room Cleared!\nBuff Gained: Defense +1";
                case PlayerBuffManager.BuffType.IncreaseAttackRange:
                    return "Room Cleared!\nBuff Gained: Attack Range +0.5";
                case PlayerBuffManager.BuffType.Heal:
                    return "Room Cleared!\nBuff Gained: Health Restored 50%";
                default:
                    return "Room Cleared!\nMystery Buff Gained";
            }
        }

        protected virtual IEnumerator DestroyNotificationAfterDelay(GameObject notification)
        {
            yield return new WaitForSeconds(notificationDuration);
            if (notification != null)
            {
                Destroy(notification);
            }
        }
        #endregion

        #region 走廊生成
        public virtual Corridor CreateCorridor(BaseRoom otherRoom)
        {
            if (RoomCorridor.ContainsKey(otherRoom))
            {
                return RoomCorridor[otherRoom];
            }

            Corridor newCorridor = Instantiate(CorridorPrefab);
            newCorridor.name = "Corridor (" + otherRoom.Num + ", " + Num + ")";
            newCorridor.transform.parent = transform.parent;
            newCorridor.Coordinates = new IntVector2(Coordinates.x + Size.x / 2, otherRoom.Coordinates.z + otherRoom.Size.z / 2);
            newCorridor.transform.localPosition = new Vector3(newCorridor.Coordinates.x - _map.MapSize.x / 2, 0, newCorridor.Coordinates.z - _map.MapSize.z / 2);
            newCorridor.Rooms[0] = otherRoom;
            newCorridor.Rooms[1] = this;
            newCorridor.Length = Vector3.Distance(otherRoom.transform.localPosition, transform.localPosition);
            newCorridor.Init(_map);
            otherRoom.RoomCorridor.Add(this, newCorridor);
            RoomCorridor.Add(otherRoom, newCorridor);

            return newCorridor;
        }
        #endregion
    }
}
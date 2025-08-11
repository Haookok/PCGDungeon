using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

namespace ooparts.dungen
{
    [System.Serializable]
    public struct MinMax
    {
        public int Min;
        public int Max;
    }

    public enum TileType
    {
        Empty,
        Room,
        Corridor,
        Wall,
        Pillar
    }

    [System.Serializable]
    public class RoomTypeSettings
    {
        [Header("Room Type Configuration")]
        public RoomType roomType;
        public BaseRoom roomPrefab;
        [Range(0f, 1f)]
        public float spawnProbability = 0.3f;//生成概率
        public bool allowAsFirstRoom = false; // 是否允许作为第一个房间（玩家出生点）
    }

    public class Map : MonoBehaviour
    {
        [Header("Room Generation Settings")]
        public BaseRoom DefaultRoomPrefab; // 默认房间预制件
        public RoomTypeSettings[] roomTypeSettings; // 各种房间类型设置
        
        [HideInInspector] public int RoomCount;
        public RoomSetting[] RoomSettings;
        [HideInInspector] public IntVector2 MapSize;
        [HideInInspector] public MinMax RoomSize;
        
        public float GenerationStepDelay;

        private List<BaseRoom> _rooms;
        private List<Corridor> _corridors;
        private TileType[,] _tilesTypes;

        private bool _hasPlayer = false;
        public GameObject treasureChestPrefab;
        private bool _isTreasureChestGenerate = false;
        private bool isAllRoomGenerated;
        
        
        public void SetTileType(IntVector2 coordinates, TileType tileType)
        {
            _tilesTypes[coordinates.x, coordinates.z] = tileType;
        }

        public TileType GetTileType(IntVector2 coordinates)
        {
            return _tilesTypes[coordinates.x, coordinates.z];
        }

        private void Update()
        {
            if (IsAllRoomsCleared() && !_isTreasureChestGenerate && isAllRoomGenerated)
            {
                _isTreasureChestGenerate = true;
                Debug.Log("生成宝箱");
                GenerateTreasureChest();
            }
        }
        
        
        
        private void GenerateTreasureChest()
        {
            Debug.Log("准备生成宝箱");
            int randomIndex = 2;
            BaseRoom roomChosed = _rooms[randomIndex];
            Debug.Log("找到房间：" + roomChosed.name);
            GameObject treasureChest = Instantiate(treasureChestPrefab);
            treasureChest.name = "Treasure Chest";
            treasureChest.transform.parent = roomChosed.transform;
            Debug.Log("宝箱已经生成在房间内");
            treasureChest.transform.localPosition = Vector3.zero;
            Debug.Log("宝箱位置：" + treasureChest.transform.position);
        }

        public bool IsAllRoomsCleared()
        {
            foreach(BaseRoom room in _rooms)
            {
                if (!room.isRoomCleared)
                    return false;
            }
            Debug.Log("所有房间已清理！");
            return true;
        }

        public IEnumerator Generate()
        {
            Stopwatch stopwatch = new Stopwatch();
            isAllRoomGenerated = false;
            stopwatch.Start();
            {
                _tilesTypes = new TileType[MapSize.x, MapSize.z];
                _rooms = new List<BaseRoom>();
                
                for (int i = 0; i < RoomCount; i++)
                {
                    BaseRoom roomInstance = CreateRoom(i);
                    if (roomInstance == null)
                    {
                        RoomCount = _rooms.Count;
                        break;
                    }
                    yield return roomInstance.Generate();

                    if (_hasPlayer)
                    {
                        if(roomInstance.roomType == RoomType.Normal)
                            yield return roomInstance.CreateMonsters();
                        roomInstance.isMonsterGenerated = true;
                    }
                    else
                    {
                        _hasPlayer = true;
                        yield return roomInstance.CreatePlayer();
                        roomInstance.isMonsterGenerated = true;
                    }
                    yield return null;
                }

                ForcePlayerToRoom1();
                Debug.Log("Every rooms are generated");

                //Delaunay三角形
                yield return BowyerWatson();

                //Prim最小生成树
                yield return PrimMST();
                Debug.Log("Every rooms are minimally connected");

                //走廊
                foreach (Corridor corridor in _corridors)
                {
                    StartCoroutine(corridor.Generate());
                    yield return null;
                }
                Debug.Log("Every corridors are generated");

                //墙壁
                yield return WallCheck();

                foreach (BaseRoom room in _rooms)
                {
                    yield return room.CreateWalls();
                }
                foreach (Corridor corridor in _corridors)
                {
                    yield return corridor.CreateWalls();
                }
                yield return BakeAllNavmeshes();
                isAllRoomGenerated = true;
            }

            stopwatch.Stop();
            print("Done in :" + stopwatch.ElapsedMilliseconds / 1000f + "s");
        }
        
        public void ForcePlayerToRoom1()
        {
            if (_rooms == null || _rooms.Count == 0)
            {
                Debug.LogError("没有找到房间！");
                return;
            }

            BaseRoom room1 = _rooms[0];
            GameObject player = GameObject.Find("Player");
            if (player == null)
                player = GameObject.FindWithTag("Player");
    
            if (player == null)
            {
                Debug.LogError("没有找到玩家！");
                return;
            }

            Vector3 room1Center = room1.transform.position;
            Vector3 targetPosition = new Vector3(room1Center.x, room1Center.y + 1f, room1Center.z);
            player.transform.position = targetPosition;
    
            Debug.Log($"玩家已强制移动到 {room1.name} 的中心位置: {targetPosition}");
        }

        private IEnumerator BakeAllNavmeshes()
        {
            foreach (BaseRoom room in _rooms)
            {
                room.BakeNavMeshWithBounds();
                yield return new WaitForEndOfFrame();
            }
        }

        private IEnumerator WallCheck()
        {
            for (int x = 0; x < MapSize.x; x++)
            {
                for (int z = 0; z < MapSize.z; z++)
                {
                    if (_tilesTypes[x, z] == TileType.Empty && IsWall(x, z))
                    {
                        _tilesTypes[x, z] = TileType.Wall;
                    }
                }
            }
            yield return null;
        }
        
        private bool IsWall(int x, int z)
        {
            for (int i = x - 1; i <= x + 1; i++)
            {
                if (i < 0 || i >= MapSize.x)
                {
                    continue;
                }
                for (int j = z - 1; j <= z + 1; j++)
                {
                    if (j < 0 || j >= MapSize.z || (i == x && j == z))
                    {
                        continue;
                    }
                    if (_tilesTypes[i, j] == TileType.Room || _tilesTypes[i, j] == TileType.Corridor)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private BaseRoom CreateRoom(int roomIndex)
        {
            BaseRoom newRoom = null;

            for (int i = 0; i < RoomCount * RoomCount; i++)
            {
                IntVector2 size = new IntVector2(Random.Range(RoomSize.Min, RoomSize.Max + 1), Random.Range(RoomSize.Min, RoomSize.Max + 1));
                IntVector2 coordinates = new IntVector2(Random.Range(1, MapSize.x - size.x), Random.Range(1, MapSize.z - size.z));
                if (!IsOverlapped(size, coordinates))
                {
                    RoomType roomType = DetermineRoomType(roomIndex);
                    BaseRoom roomPrefab = GetRoomPrefab(roomType);
                    
                    if (roomPrefab != null)
                    {
                        newRoom = Instantiate(roomPrefab);
                    }
                    else
                    {
                        Debug.LogWarning($"No prefab found for room type {roomType}, using default");
                        newRoom = Instantiate(DefaultRoomPrefab);
                    }
                    
                    _rooms.Add(newRoom);
                    newRoom.Num = _rooms.Count;
                    newRoom.name = $"Room {newRoom.Num} ({roomType}) ({coordinates.x}, {coordinates.z})";
                    newRoom.Size = size;
                    newRoom.Coordinates = coordinates;
                    newRoom.roomType = roomType;
                    newRoom.transform.parent = transform;
                    Vector3 position = CoordinatesToPosition(coordinates);
                    position.x += size.x * 0.5f - 0.5f;
                    position.z += size.z * 0.5f - 0.5f;
                    position *= RoomMapManager.TileSize;
                    newRoom.transform.localPosition = position;
                    newRoom.Init(this);
                    break;
                }
            }

            if (newRoom == null)
            {
                Debug.LogError("Too many rooms in map!! : " + _rooms.Count);
            }

            return newRoom;
        }

        private RoomType DetermineRoomType(int roomIndex)
        {
            //第一个房间必须是普通房间
            if (roomIndex == 0)
            {
                return RoomType.Normal;
            }
            
            //列出所有可能的房间类型
            List<RoomTypeSettings> possibleTypes = new List<RoomTypeSettings>();
            
            foreach (RoomTypeSettings setting in roomTypeSettings)
            {
                if (roomIndex == 0 && !setting.allowAsFirstRoom)
                    continue;
                    
                possibleTypes.Add(setting);
            }
            
            //如果没有可选类型，返回默认类型
            if (possibleTypes.Count == 0)
            {
                return RoomType.Normal;
            }
            
            float totalWeight = 0f;
            foreach (RoomTypeSettings setting in possibleTypes)
            {
                totalWeight += setting.spawnProbability;
            }
            
            float randomValue = Random.value * totalWeight;
            float currentWeight = 0f;
            
            foreach (RoomTypeSettings setting in possibleTypes)
            {
                currentWeight += setting.spawnProbability;
                if (randomValue <= currentWeight)
                {
                    return setting.roomType;
                }
            }
            
            return RoomType.Normal;
        }

        private BaseRoom GetRoomPrefab(RoomType roomType)
        {
            foreach (RoomTypeSettings setting in roomTypeSettings)
            {
                if (setting.roomType == roomType)
                {
                    return setting.roomPrefab;
                }
            }
            
            return DefaultRoomPrefab;
        }

        private bool IsOverlapped(IntVector2 size, IntVector2 coordinates)
        {
            foreach (BaseRoom room in _rooms)
            {
                if (Mathf.Abs(room.Coordinates.x - coordinates.x + (room.Size.x - size.x) * 0.5f) < (room.Size.x + size.x) * 0.7f &&
                    Mathf.Abs(room.Coordinates.z - coordinates.z + (room.Size.z - size.z) * 0.5f) < (room.Size.z + size.z) * 0.7f)
                {
                    return true;
                }
            }
            return false;
        }

        // Triangle and corridor management
        private Triangle LootTriangle
        {
            get
            {
                Vector3[] vertexs = new Vector3[]
                {
                    RoomMapManager.TileSize * new Vector3(MapSize.x * 2, 0, MapSize.z),
                    RoomMapManager.TileSize * new Vector3(-MapSize.x * 2, 0, MapSize.z),
                    RoomMapManager.TileSize * new Vector3(0, 0, -2 * MapSize.z)
                };

                BaseRoom[] tempRooms = new BaseRoom[3];
                for (int i = 0; i < 3; i++)
                {
                    tempRooms[i] = Instantiate(DefaultRoomPrefab);
                    tempRooms[i].transform.localPosition = vertexs[i];
                    tempRooms[i].name = "Loot Room " + i;
                    tempRooms[i].Init(this);
                }

                return new Triangle(tempRooms[0], tempRooms[1], tempRooms[2]);
            }
        }

        private IEnumerator BowyerWatson()
        {
            List<Triangle> triangulation = new List<Triangle>();

            Triangle loot = LootTriangle;
            triangulation.Add(loot);

            foreach (BaseRoom room in _rooms)
            {
                List<Triangle> badTriangles = new List<Triangle>();

                foreach (Triangle triangle in triangulation)
                {
                    if (triangle.IsContaining(room))
                    {
                        badTriangles.Add(triangle);
                    }
                }

                List<Corridor> polygon = new List<Corridor>();
                foreach (Triangle badTriangle in badTriangles)
                {
                    foreach (Corridor corridor in badTriangle.Corridors)
                    {
                        if (corridor.Triangles.Count == 1)
                        {
                            polygon.Add(corridor);
                            corridor.Triangles.Remove(badTriangle);
                            continue;
                        }

                        foreach (Triangle triangle in corridor.Triangles)
                        {
                            if (triangle == badTriangle)
                            {
                                continue;
                            }

                            if (badTriangles.Contains(triangle))
                            {
                                corridor.Rooms[0].RoomCorridor.Remove(corridor.Rooms[1]);
                                corridor.Rooms[1].RoomCorridor.Remove(corridor.Rooms[0]);
                                Destroy(corridor.gameObject);
                            }
                            else
                            {
                                polygon.Add(corridor);
                            }
                            break;
                        }
                    }
                }

                for (int index = badTriangles.Count - 1; index >= 0; --index)
                {
                    Triangle triangle = badTriangles[index];
                    badTriangles.RemoveAt(index);
                    triangulation.Remove(triangle);
                    foreach (Corridor corridor in triangle.Corridors)
                    {
                        corridor.Triangles.Remove(triangle);
                    }
                }

                foreach (Corridor corridor in polygon)
                {
                    Triangle newTriangle = new Triangle(corridor.Rooms[0], corridor.Rooms[1], room);
                    triangulation.Add(newTriangle);
                }
            }
            yield return null;

            for (int index = triangulation.Count - 1; index >= 0; index--)
            {
                if (triangulation[index].Rooms.Contains(loot.Rooms[0]) || triangulation[index].Rooms.Contains(loot.Rooms[1]) ||
                    triangulation[index].Rooms.Contains(loot.Rooms[2]))
                {
                    triangulation.RemoveAt(index);
                }
            }

            foreach (BaseRoom room in loot.Rooms)
            {
                List<Corridor> deleteList = new List<Corridor>();
                foreach (KeyValuePair<BaseRoom, Corridor> pair in room.RoomCorridor)
                {
                    deleteList.Add(pair.Value);
                }
                for (int index = deleteList.Count - 1; index >= 0; index--)
                {
                    Corridor corridor = deleteList[index];
                    corridor.Rooms[0].RoomCorridor.Remove(corridor.Rooms[1]);
                    corridor.Rooms[1].RoomCorridor.Remove(corridor.Rooms[0]);
                    Destroy(corridor.gameObject);
                }
                Destroy(room.gameObject);
            }
        }

        private IEnumerator PrimMST()
        {
            List<BaseRoom> connectedRooms = new List<BaseRoom>();
            _corridors = new List<Corridor>();

            connectedRooms.Add(_rooms[0]);

            while (connectedRooms.Count < _rooms.Count)
            {
                KeyValuePair<BaseRoom, Corridor> minLength = new KeyValuePair<BaseRoom, Corridor>();
                List<Corridor> deleteList = new List<Corridor>();

                foreach (BaseRoom room in connectedRooms)
                {
                    foreach (KeyValuePair<BaseRoom, Corridor> pair in room.RoomCorridor)
                    {
                        if (connectedRooms.Contains(pair.Key))
                        {
                            continue;
                        }
                        if (minLength.Value == null || minLength.Value.Length > pair.Value.Length)
                        {
                            minLength = pair;
                        }
                    }
                }

                foreach (KeyValuePair<BaseRoom, Corridor> pair in minLength.Key.RoomCorridor)
                {
                    if (connectedRooms.Contains(pair.Key) && (minLength.Value != pair.Value))
                    {
                        deleteList.Add(pair.Value);
                    }
                }

                for (int index = deleteList.Count - 1; index >= 0; index--)
                {
                    Corridor corridor = deleteList[index];
                    corridor.Rooms[0].RoomCorridor.Remove(corridor.Rooms[1]);
                    corridor.Rooms[1].RoomCorridor.Remove(corridor.Rooms[0]);
                    deleteList.RemoveAt(index);
                    Destroy(corridor.gameObject);
                }

                connectedRooms.Add(minLength.Key);
                _corridors.Add(minLength.Value);
            }
            yield return null;
        }

        public Vector3 CoordinatesToPosition(IntVector2 coordinates)
        {
            return new Vector3(coordinates.x - MapSize.x * 0.5f + 0.5f, 0f, coordinates.z - MapSize.z * 0.5f + 0.5f);
        }
        
        public Vector3 CoordinatesToPosition(IntVector2 coordinates, MapDirection direction)
        {
            if (direction == MapDirection.West)
            {
                return new Vector3(coordinates.x - MapSize.x * 0.5f + 0.8f, 0f, coordinates.z - MapSize.z * 0.5f + 0.5f);
            }
            if(direction == MapDirection.East)
            {
                return new Vector3(coordinates.x - MapSize.x * 0.5f + 0.2f, 0f, coordinates.z - MapSize.z * 0.5f + 0.5f);
            }
            if (direction == MapDirection.South)
            {
                return new Vector3(coordinates.x - MapSize.x * 0.5f + 0.5f, 0f, coordinates.z - MapSize.z * 0.5f + 0.8f);
            }
            if (direction == MapDirection.North)
            {
                return new Vector3(coordinates.x - MapSize.x * 0.5f + 0.5f, 0f, coordinates.z - MapSize.z * 0.5f + 0.2f);
            }
            return new Vector3(coordinates.x - MapSize.x * 0.5f + 0.5f, 0f, coordinates.z - MapSize.z * 0.5f + 0.5f);
        }
    }
}
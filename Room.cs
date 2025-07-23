using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using ooparts.dungen;
using Cinemachine;
using Unity.AI.Navigation;

namespace ooparts.dungen
{
	public class Room : MonoBehaviour
	{
		public Corridor CorridorPrefab;
		public IntVector2 Size;
		public IntVector2 Coordinates;
		public int Num;

		private GameObject _tilesObject;
		private GameObject _wallsObject;
		private GameObject _monstersObject;
		private GameObject _pillarsObject;
		public Tile TilePrefab;
		
		private Tile[,] _tiles;
		public NavMeshSurface navMeshSurface;
		
		public GameObject WallPrefab;
		public RoomSetting Setting;

		public Dictionary<Room, Corridor> RoomCorridor = new Dictionary<Room, Corridor>();

		private Map _map;

		public GameObject PlayerPrefab;

		public GameObject MonsterPrefab;
		public int MonsterCount;
		private GameObject[] Monsters;
		
		[Header("Pillar Settings")]
		public GameObject PillarPrefab;

		public void Init(Map map)
		{
			_map = map;
		}

		public void Start()
		{
			if(navMeshSurface == null)
			{
				navMeshSurface = GetComponent<NavMeshSurface>();
				if (navMeshSurface == null)
				{
					Debug.LogError("NavMeshSurface component is missing on the Room object.");
				}
			}
			
		}

		public IEnumerator Generate()
		{
			// Create parent object
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
			yield return null;
			
			Debug.Log("1, bake navmesh");
			BakeNavMesh();
		}
		
		public void BakeNavMesh()
		{
			Debug.Log("2, start baking navmesh");
			if (navMeshSurface != null)
			{
				navMeshSurface.BuildNavMesh();  // 烘焙当前房间的 NavMesh
				Debug.Log("NavMesh has been baked for room.");
			}
		}
		private Tile CreateTile(IntVector2 coordinates)
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
			newTile.transform.localPosition = RoomMapManager.TileSize * new Vector3(coordinates.x - Coordinates.x - Size.x * 0.5f + 0.5f, 0f, coordinates.z - Coordinates.z - Size.z * 0.5f + 0.5f);
			newTile.transform.GetChild(0).GetComponent<Renderer>().material = Setting.floor;
			return newTile;
		}

		public Corridor CreateCorridor(Room otherRoom)
		{
			// Don't create if already connected
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

		public IEnumerator CreateWalls()
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
					// If it's center or corner or not wall
					/*if((x == leftBottom.x || x == rightTop.x) && (z == leftBottom.z || z == rightTop.z))
					{

						GameObject newPillar = Instantiate(PillarPrefab);
						newPillar.name = "Pillar (" + x + ", " + z + ")";
						newPillar.transform.parent = _pillarsObject.transform;
						
						newPillar.transform.localPosition = RoomMapManager.TileSize * new Vector3(x - Coordinates.x - Size.x * 0.5f + 0.2f, 0f, z - Coordinates.z - Size.z * 0.5f + 0.2f);
						newPillar.transform.localScale *= RoomMapManager.TileSize;
						continue;
					}*/
					if((x == leftBottom.x || x == rightTop.x) && (z == leftBottom.z || z == rightTop.z))
					{
						GameObject newPillar = Instantiate(PillarPrefab);
						newPillar.name = "Pillar (" + x + ", " + z + ")";
						newPillar.transform.parent = _pillarsObject.transform;
						newPillar.transform.localPosition = RoomMapManager.TileSize * CalculatePillarPosition(x , z, leftBottom, rightTop);
						newPillar.transform.localScale *= RoomMapManager.TileSize;
						continue;
					}
					if ((x != leftBottom.x && x != rightTop.x && z != leftBottom.z && z != rightTop.z) ||
						//((x == leftBottom.x || x == rightTop.x) && (z == leftBottom.z || z == rightTop.z)) ||
						(_map.GetTileType(new IntVector2(x, z)) != TileType.Wall))
					{
						continue;
					}
					
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
					else
					{
						Debug.LogError("Wall is not on appropriate location!!");
					}

					GameObject newWall = Instantiate(WallPrefab);
					newWall.name = "Wall (" + x + ", " + z + ")";
					newWall.transform.parent = _wallsObject.transform;
					newWall.transform.localPosition = position;
					newWall.transform.localRotation = rotation;
					newWall.transform.localScale *= RoomMapManager.TileSize;
					//newWall.transform.GetChild(0).GetComponent<Renderer>().material = Setting.wall;
				}
			}
			yield return null;
		}
		
		private Vector3 CalculatePillarPosition(int x, int z, IntVector2 leftBottom, IntVector2 rightTop)
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
			return RoomMapManager.TileSize * new Vector3(xPos, 0f, zPos);
		}

		public IEnumerator CreateMonsters()
		{
			_monstersObject = new GameObject("Monsters");
			_monstersObject.transform.parent = transform;
			_monstersObject.transform.localPosition = Vector3.zero;

			Monsters = new GameObject[MonsterCount];

			// 获取可用空间的边界（保留一些边距，避免生成在墙边）
			int marginFromWall = 1;
			float minX = marginFromWall;
			float maxX = Size.x - marginFromWall;
			float minZ = marginFromWall;
			float maxZ = Size.z - marginFromWall;

			for (int i = 0; i < MonsterCount; i++)
			{
				GameObject newMonster = Instantiate(MonsterPrefab);
				newMonster.name = "Monster " + (i + 1);
				newMonster.transform.parent = _monstersObject.transform;

				// 生成随机位置
				float randomX = Random.Range(minX, maxX);
				float randomZ = Random.Range(minZ, maxZ);
				
				Vector3 newPosition = new Vector3(randomX, 0f, randomZ);

				while (IsPositionOccupied(newPosition))
				{
					randomX = Random.Range(minX, maxX);
					randomZ = Random.Range(minZ, maxZ);
					newPosition = new Vector3(randomX, 0f, randomZ);
				}

				// 将本地坐标系的位置转换为与瓦片系统一致的位置
				Vector3 randomPosition = RoomMapManager.TileSize * new Vector3(
					randomX - Size.x * 0.5f + 0.5f, 
					0f, 
					randomZ - Size.z * 0.5f + 0.5f
				);

				newMonster.transform.localPosition = randomPosition;
        
				// 随机旋转方向
				newMonster.transform.localRotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
        
				Monsters[i] = newMonster;
			}
			yield return null;
		}
		
		//检测怪物重叠
		bool IsPositionOccupied(Vector3 position)
		{
			foreach(GameObject monster in Monsters)
			{
				if (monster != null && Vector3.Distance(monster.transform.localPosition, position) < 1f)
				{
					return true; // 有重叠
				}
			}

			return false;
		}
		
		public IEnumerator CreatePlayer()
		{
			GameObject player = Instantiate((PlayerPrefab));
			player.name = "Player";
			player.transform.parent = transform.parent;
			player.transform.localPosition = transform.localPosition + 5 * Vector3.up;
			
			StartCoroutine(SetupPlayerCamera(player));
			
			yield return null;
		}

		private IEnumerator SetupPlayerCamera(GameObject player)
		{
			// 等待一帧，确保玩家完全生成
			yield return null;
            
			// 查找场景中的 PlayerFollowCamera
			GameObject cameraObj = GameObject.Find("PlayerFollowCamera");
			if (cameraObj != null)
			{
				CinemachineVirtualCamera virtualCamera = cameraObj.GetComponent<CinemachineVirtualCamera>();
				if (virtualCamera != null)
				{
					// 查找玩家的 PlayerCameraRoot 子物体
					Transform cameraRoot = player.transform.Find("PlayerCameraRoot");
					if (cameraRoot != null)
					{
						// 设置摄像机跟随目标
						virtualCamera.Follow = cameraRoot;
						Debug.Log("成功设置摄像机跟随目标: " + cameraRoot.name);
					}
					else
					{
						Debug.LogWarning("未找到玩家的 PlayerCameraRoot 子物体");
					}
				}
				else
				{
					Debug.LogWarning("PlayerFollowCamera 上没有 CinemachineVirtualCamera 组件");
				}
			}
			else
			{
				Debug.LogWarning("场景中未找到 PlayerFollowCamera 物体");
			}
		}
	}
}
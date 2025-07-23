using UnityEngine;
using System.Collections;
using ooparts.dungen;

namespace ooparts.dungen
{
	public class RoomMapManager : MonoBehaviour
	{
		public Map mapPrefap;
		private Map mapInstance;

		public int MapSizeX;
		public int MapSizeZ;
		public int MaxRooms;
		public int MinRoomSize;
		public int MaxRoomSize;

		public float TileSizeFactor = 1;
		public static float TileSize;
		

		void Start()
		{
			TileSize = TileSizeFactor;
			BeginGame();
		}

		void Update()
		{
			if (Input.GetKeyDown(KeyCode.J))
			{
				RestartGame();
			}
		}

		private void BeginGame()
		{
			mapInstance = Instantiate(mapPrefap);
			mapInstance.RoomCount = MaxRooms;
			mapInstance.MapSize = new IntVector2(MapSizeX, MapSizeZ);
			mapInstance.RoomSize.Min = MinRoomSize;
			mapInstance.RoomSize.Max = MaxRoomSize;
			TileSize = TileSizeFactor;

			StartCoroutine(mapInstance.Generate());
		}

		private void RestartGame()
		{
			StopAllCoroutines();
			Destroy(mapInstance.gameObject);
			BeginGame();
		}
	}
}
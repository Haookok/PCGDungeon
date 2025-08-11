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

        // 加载界面引用
        public GameObject loadingScreen;
        [SerializeField] 
        private int loadingScreenSortOrder = 100;
        
        void Start()
        {
            if(PlayerProgressManager.Instance != null)
            {
                PlayerProgressManager.Instance.OnStartNewGame();
            }
            if (loadingScreen != null)
            {
                // 设置加载界面的排序顺序为最高
                Canvas loadingCanvas = loadingScreen.GetComponent<Canvas>();
                if (loadingCanvas != null)
                {
                    loadingCanvas.sortingOrder = loadingScreenSortOrder;
                }
                
                loadingScreen.SetActive(true);
            }
            TileSize = TileSizeFactor;
            BeginGame();
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.J))
            {
                //RestartGame();
            }
        }

        private void BeginGame()
        {
            // 显示加载界面
            if (loadingScreen != null)
            {
                loadingScreen.SetActive(true);
            }

            mapInstance = Instantiate(mapPrefap);
            mapInstance.RoomCount = MaxRooms;
            mapInstance.MapSize = new IntVector2(MapSizeX, MapSizeZ);
            mapInstance.RoomSize.Min = MinRoomSize;
            mapInstance.RoomSize.Max = MaxRoomSize;
            TileSize = TileSizeFactor;

            // 启动生成地图的协程，并在完成后隐藏加载界面
            StartCoroutine(GenerateMapWithLoading());
        }

        private IEnumerator GenerateMapWithLoading()
        {
            // 等待地图生成完成
            yield return StartCoroutine(mapInstance.Generate());
            
            // 地图生成完成后，隐藏加载界面
            if (loadingScreen != null)
            {
                loadingScreen.SetActive(false);
            }
        }

        public void RestartGame()
        {
            // 显示加载界面
            if (loadingScreen != null)
            {
                loadingScreen.SetActive(true);
            }

            StopAllCoroutines();
            CleanupMonsterHealthBars();
            Destroy(mapInstance.gameObject);
            BeginGame();
        }

        private void CleanupMonsterHealthBars()
        {
            // 查找所有GameObject（包括未激活的）
            GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
            int cleanedCount = 0;

            foreach (GameObject obj in allObjects)
            {
                // 跳过预制件（预制件的scene.name为null）
                if (obj.scene.name == null) continue;

                // 检查标签
                if (obj.CompareTag("MonsterHealthBar"))
                {
                    Debug.Log($"Destroying monster health bar: {obj.name}, Active: {obj.activeInHierarchy}");
                    Destroy(obj);
                    cleanedCount++;
                }
            }

            Debug.Log($"Cleaned up {cleanedCount} monster health bars (including inactive ones)");
        }
    }
}
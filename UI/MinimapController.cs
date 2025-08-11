using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MinimapController : MonoBehaviour
{
    public Camera minimapCamera;   // 小地图摄像机
    public RawImage minimapImage;  // 小地图显示的UI RawImage
    public RenderTexture minimapTexture; // 小地图的RenderTexture
    public RectTransform minimapPlayerDot; // 小地图上表示玩家的红点（UI组件）
    public GameObject player; // 玩家Transform

    private float minimapScale = 1f; // 小地图与世界坐标的缩放比例，调整这个值可以改变显示的范围
    [Header("动态查找设置")]
    public string playerTag = "Player"; // 玩家标签
    
    private bool playerFound = false; // 标记是否已找到玩家
    void Start()
    {
        //确保小地图摄像机的输出纹理是RenderTexture
        if (minimapCamera != null && minimapImage != null && minimapTexture != null)
        {
            minimapCamera.targetTexture = minimapTexture;
            minimapImage.texture = minimapTexture;
        }
        if(player == null)
        {
            player = GameObject.Find("Player");
            if (player != null)
            {
                playerFound = true;
            }
        }
        if(player == null)
        {
            player = GameObject.FindGameObjectWithTag(playerTag);
            if (player != null)
            {
                playerFound = true;
            }
        }
        if(player == null)
        {
            Debug.LogError("MinimapController: 未找到玩家对象，请确保场景中有一个标签为 'Player' 的游戏对象，或者名称为 'Player' 的游戏对象。");
        }
    }

    void Update()
    {
        if(player == null)
        {
            player = GameObject.Find("Player");
            if (player != null)
            {
                playerFound = true;
            }
        }
        if(player == null)
        {
            player = GameObject.FindGameObjectWithTag(playerTag);
            if (player != null)
            {
                playerFound = true;
            }
        }

        if (player == null)
            return;
        //Debug.Log("111");
        //更新小地图上的玩家位置
        if (minimapPlayerDot != null && player != null)
        {
            //将玩家的位置转换为小地图上的位置
            Vector3 playerPosition = player.transform.position;

            //计算小地图上玩家的位置
            Vector2 minimapPos = new Vector2(playerPosition.x * minimapScale - 1, playerPosition.z * minimapScale + 1);

            //将玩家的位置设置为小红点的坐标
            minimapPlayerDot.localPosition = minimapPos;
            //Debug.Log($"playerPosition: {playerPosition}, minimapPos: {minimapPos}, minimapPlayerDot.localPosition: {minimapPlayerDot.localPosition}");
        }
    }
}
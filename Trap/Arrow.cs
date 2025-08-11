using UnityEngine;

public class Arrow : MonoBehaviour
{
    private Vector3 direction;
    private float speed;
    
    public void Initialize(Vector3 dir, float spd)
    {
        direction = dir;
        speed = spd;
    }
    
    void Update()
    {
        // 让箭矢沿指定方向移动
        transform.position += direction * speed * Time.deltaTime;
    }
    
    private void SetUpTriggerArea()
    {
        // 设置箭矢的碰撞体为触发器
        Collider collider = GetComponent<Collider>();
        if (collider != null)
        {
            collider.isTrigger = true;
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        // 检测到碰撞时，处理箭矢的逻辑
        Player player = other.GetComponent<Player>();
        if (player != null)
        {
            player.TakeDamage(10); // 假设箭矢造成10点伤害
            Destroy(gameObject); // 销毁箭矢
        }
    }
    
}
using UnityEngine;

/// <summary>
/// ✅ Spawner ساده برای Dementor
/// این را به یک Empty GameObject در Scene اضافه کنید
/// </summary>
public class DementorSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private GameObject dementorPrefab;
    [SerializeField] private float spawnInterval = 30f; // هر 30 ثانیه یک Dementor
    [SerializeField] private int maxDementors = 2; // حداکثر 2 Dementor همزمان
    [SerializeField] private float spawnRadius = 20f; // فاصله از بازیکن
    
    [Header("Spawn Zones")]
    [SerializeField] private Transform[] spawnPoints; // نقاط spawn دستی (اختیاری)
    
    private float lastSpawnTime;
    private Transform player;
    
    void Start()
    {
        // پیدا کردن بازیکن
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
        
        // ✅ اگر prefab تنظیم نشده، از Resources بارگذاری کن
        if (dementorPrefab == null)
        {
            dementorPrefab = Resources.Load<GameObject>("Prefabs/Enemy_dementor");
            
            if (dementorPrefab == null)
            {
                Debug.LogError("❌ Dementor prefab not found at Resources/Prefabs/Enemy_dementor");
                enabled = false;
                return;
            }
        }
        
        Debug.Log("✅ DementorSpawner ready");
    }
    
    void Update()
    {
        // چک شرایط spawn
        if (Time.time - lastSpawnTime >= spawnInterval)
        {
            int currentDementors = GameObject.FindGameObjectsWithTag("Dementor").Length;
            
            if (currentDementors < maxDementors)
            {
                SpawnDementor();
                lastSpawnTime = Time.time;
            }
        }
    }
    
    void SpawnDementor()
    {
        Vector3 spawnPosition = GetSpawnPosition();
        
        GameObject dementor = Instantiate(dementorPrefab, spawnPosition, Quaternion.identity);
        
        // ✅ تنظیم scale (اگر لازم باشد)
        dementor.transform.localScale = new Vector3(1.5f, 1.5f, 1f);
        
        Debug.Log($"👻 Dementor spawned at {spawnPosition}");
    }
    
    Vector3 GetSpawnPosition()
    {
        // ✅ اگر spawn points دستی تعریف شده، از آنها استفاده کن
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            int randomIndex = Random.Range(0, spawnPoints.Length);
            return spawnPoints[randomIndex].position;
        }
        
        // ✅ وگرنه، spawn تصادفی دور بازیکن
        if (player != null)
        {
            Vector2 randomDir = Random.insideUnitCircle.normalized;
            Vector3 offset = new Vector3(randomDir.x, randomDir.y, 0) * spawnRadius;
            return player.position + offset;
        }
        
        // ✅ fallback: spawn در موقعیت این GameObject
        return transform.position;
    }
    
    // ✅ برای تست - فشار دادن D در بازی یک Dementor spawn می‌کند
    void OnGUI()
    {
        if (GUI.Button(new Rect(10, 10, 150, 30), "Spawn Dementor (D)"))
        {
            SpawnDementor();
        }
        
        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.D)
        {
            SpawnDementor();
        }
    }
    
    // ✅ نمایش محدوده spawn در Scene
    void OnDrawGizmosSelected()
    {
        if (player != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(player.position, spawnRadius);
        }
        
        // نمایش spawn points
        if (spawnPoints != null)
        {
            Gizmos.color = Color.cyan;
            foreach (Transform point in spawnPoints)
            {
                if (point != null)
                {
                    Gizmos.DrawWireSphere(point.position, 2f);
                }
            }
        }
    }
}
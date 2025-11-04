using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// مدیریت تولید دشمنان به صورت رندوم و مداوم
/// ✅ نسخه بهبود یافته - با بررسی کامل Zone و MapManager
/// ✅ دشمنان به صورت نامحدود تولید می‌شوند
/// ✅ هر ناحیه حداکثر تعداد مشخصی دشمن دارد
/// 📁 LOCATION: Assets/Scripts/Managers/EnemySpawnerManager.cs
/// </summary>
public class EnemySpawnerManager : MonoBehaviour
{
    public static EnemySpawnerManager Instance { get; private set; }
    
    [Header("Spawn Settings")]
    [SerializeField] private bool autoSpawn = true;
    [SerializeField] private float spawnCheckInterval = 5f;
    [SerializeField] private float respawnDelay = 15f;
    
    [Header("Zone Limits")]
    [SerializeField] private int maxEnemiesPerZone = 3;
    [SerializeField] private int globalMaxEnemies = 20;
    
    [Header("Enemy Types")]
    [SerializeField] private string[] enemyHouses = { "slytherin", "ravenclaw", "hufflepuff", "deatheater" };
    [SerializeField] private int[] enemyWeights = { 3, 2, 2, 1 };
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;
    
    private MapManager mapManager;
    private Dictionary<string, List<GameObject>> zoneEnemies = new Dictionary<string, List<GameObject>>();
    private int totalEnemiesSpawned = 0;
    private float lastZoneLogTime = 0f; // ⭐ NEW: برای جلوگیری از spam log
    
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    
    // ⭐ CHANGED: از Start معمولی به Coroutine تبدیل شد
    void Start()
    {
        StartCoroutine(InitializeSpawner());
    }
    
    // ⭐ NEW: منتظر می‌ماند تا MapManager آماده شود
    System.Collections.IEnumerator InitializeSpawner()
    {
        // Wait for MapManager to be ready
        mapManager = FindFirstObjectByType<MapManager>();
        
        int maxAttempts = 50;
        int attempts = 0;
        
        while ((mapManager == null || mapManager.mapData == null) && attempts < maxAttempts)
        {
            yield return new WaitForSeconds(0.1f);
            mapManager = FindFirstObjectByType<MapManager>();
            attempts++;
        }
        
        if (mapManager == null)
        {
            Debug.LogWarning("⚠️ MapManager not found! Enemy spawning disabled.");
            enabled = false;
            yield break;
        }
        
        if (mapManager.mapData == null)
        {
            Debug.LogWarning("⚠️ MapManager.mapData is null! Enemy spawning disabled.");
            enabled = false;
            yield break;
        }
        
        Debug.Log($"✅ EnemySpawnerManager initialized with {mapManager.mapData.zones.Length} zones");
        
        if (autoSpawn)
        {
            InvokeRepeating(nameof(SpawnCycle), 2f, spawnCheckInterval);
        }
    }
    
    void Update()
    {
        if (showDebugInfo && Input.GetKeyDown(KeyCode.F2))
        {
            ShowDebugInfo();
        }
        
        // پاک‌سازی دشمنان null شده (مرده)
        CleanupDestroyedEnemies();
    }
    
    // ===== Spawn Logic =====
    
    void SpawnCycle()
    {
        if (!autoSpawn) return;
        
        CleanupDestroyedEnemies();
        
        int totalEnemies = GetTotalEnemyCount();
        
        if (totalEnemies >= globalMaxEnemies)
        {
            return;
        }
        
        List<string> availableZones = GetZonesNeedingEnemies();
        
        if (availableZones.Count == 0)
        {
            return;
        }
        
        string selectedZone = availableZones[Random.Range(0, availableZones.Count)];
        string enemyType = GetRandomEnemyType();
        
        SpawnEnemyInZone(selectedZone, enemyType);
    }
    
    List<string> GetZonesNeedingEnemies()
    {
        List<string> zones = new List<string>();
        
        string[] allZones = GetAvailableZones();
        
        foreach (string zone in allZones)
        {
            int currentCount = GetEnemyCountInZone(zone);
            
            if (currentCount < maxEnemiesPerZone)
            {
                zones.Add(zone);
            }
        }
        
        return zones;
    }
    
    // ⭐ IMPROVED: بررسی دقیق‌تر و مدیریت بهتر خطا
    string[] GetAvailableZones()
    {
        if (mapManager == null)
        {
            Debug.LogWarning("⚠️ MapManager is null!");
            return GetFallbackZones();
        }
        
        if (mapManager.mapData == null)
        {
            Debug.LogWarning("⚠️ MapManager.mapData is null! Waiting for map to load...");
            return GetFallbackZones();
        }
        
        if (mapManager.mapData.zones == null || mapManager.mapData.zones.Length == 0)
        {
            Debug.LogWarning("⚠️ No zones array in mapData!");
            return GetFallbackZones();
        }
        
        // Get zone IDs from actual map data
        List<string> zoneIds = new List<string>();
        foreach (var zone in mapManager.mapData.zones)
        {
            if (zone != null && !string.IsNullOrEmpty(zone.id))
            {
                zoneIds.Add(zone.id);
            }
        }
        
        if (zoneIds.Count == 0)
        {
            Debug.LogWarning("⚠️ No valid zones found in MapManager, using defaults");
            return GetFallbackZones();
        }
        
        // Log only once every 10 seconds to avoid spam
        if (Time.time - lastZoneLogTime > 10f)
        {
            Debug.Log($"📋 Active zones: {string.Join(", ", zoneIds)}");
            lastZoneLogTime = Time.time;
        }
        
        return zoneIds.ToArray();
    }
    
    // ⭐ NEW: Zone های پیش‌فرض برای زمان fallback
    string[] GetFallbackZones()
    {
        return new string[] { 
            "library", 
            "corridor", 
            "courtyard", 
            "tower_stairs", 
            "astronomy_tower"
        };
    }
    
    // ⭐ IMPROVED: بررسی وجود Zone قبل از spawn
    void SpawnEnemyInZone(string zoneId, string enemyHouse)
    {
        if (mapManager == null)
        {
            Debug.LogError("❌ MapManager is null, cannot spawn enemy!");
            return;
        }
        
        // Verify zone exists
        Zone zone = mapManager.GetZoneById(zoneId);
        if (zone == null)
        {
            Debug.LogError($"❌ Zone '{zoneId}' not found in MapManager!");
            return;
        }
        
        // Spawn دشمن
        try
        {
            mapManager.SpawnEnemyAtZone(zoneId, enemyHouse);
            
            // صبر یک فریم و سپس پیدا کردن دشمن جدید
            StartCoroutine(RegisterNewEnemy(zoneId, enemyHouse));
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"❌ Failed to spawn enemy in {zoneId}: {ex.Message}");
        }
    }
    
    System.Collections.IEnumerator RegisterNewEnemy(string zoneId, string enemyHouse)
    {
        yield return new WaitForEndOfFrame();
        
        // پیدا کردن تمام دشمنان
        GameObject[] allEnemies = GameObject.FindGameObjectsWithTag("Enemy");
        
        if (allEnemies == null || allEnemies.Length == 0)
        {
            Debug.LogWarning("⚠️ No enemies found with 'Enemy' tag!");
            yield break;
        }
        
        foreach (GameObject enemy in allEnemies)
        {
            if (enemy == null) continue;
            
            // بررسی اینکه این دشمن قبلاً ثبت نشده باشد
            bool alreadyRegistered = false;
            foreach (var kvp in zoneEnemies)
            {
                if (kvp.Value != null && kvp.Value.Contains(enemy))
                {
                    alreadyRegistered = true;
                    break;
                }
            }
            
            if (!alreadyRegistered)
            {
                // این دشمن جدید است
                if (!zoneEnemies.ContainsKey(zoneId))
                {
                    zoneEnemies[zoneId] = new List<GameObject>();
                }
                
                zoneEnemies[zoneId].Add(enemy);
                totalEnemiesSpawned++;
                
                Debug.Log($"👹 Spawned {enemyHouse} in {zoneId} | Zone: {GetEnemyCountInZone(zoneId)}/{maxEnemiesPerZone} | Total: {GetTotalEnemyCount()}/{globalMaxEnemies}");
                yield break;
            }
        }
        
        Debug.LogWarning($"⚠️ Could not find newly spawned enemy in {zoneId}");
    }
    
    // ===== Enemy Type Selection =====
    
    string GetRandomEnemyType()
    {
        if (enemyWeights == null || enemyWeights.Length == 0)
        {
            return enemyHouses[Random.Range(0, enemyHouses.Length)];
        }
        
        int totalWeight = enemyWeights.Sum();
        int randomValue = Random.Range(0, totalWeight);
        int currentWeight = 0;
        
        for (int i = 0; i < enemyHouses.Length; i++)
        {
            currentWeight += enemyWeights[i];
            
            if (randomValue < currentWeight)
            {
                return enemyHouses[i];
            }
        }
        
        return enemyHouses[0];
    }
    
    // ===== Utility =====
    
    int GetEnemyCountInZone(string zoneId)
    {
        if (!zoneEnemies.ContainsKey(zoneId))
        {
            return 0;
        }
        
        zoneEnemies[zoneId].RemoveAll(e => e == null);
        
        return zoneEnemies[zoneId].Count;
    }
    
    int GetTotalEnemyCount()
    {
        int count = 0;
        
        foreach (var kvp in zoneEnemies)
        {
            count += GetEnemyCountInZone(kvp.Key);
        }
        
        return count;
    }
    
    void CleanupDestroyedEnemies()
    {
        foreach (var kvp in zoneEnemies.ToList())
        {
            if (kvp.Value != null)
            {
                int beforeCount = kvp.Value.Count;
                kvp.Value.RemoveAll(e => e == null);
                int afterCount = kvp.Value.Count;
                
                // اگر دشمنی مرده، بعد از مدتی respawn کن
                if (beforeCount > afterCount)
                {
                    int died = beforeCount - afterCount;
                    for (int i = 0; i < died; i++)
                    {
                        Invoke(nameof(SpawnCycle), respawnDelay);
                    }
                }
                
                if (kvp.Value.Count == 0)
                {
                    zoneEnemies.Remove(kvp.Key);
                }
            }
        }
    }
    
    // ⭐ IMPROVED: اضافه شدن لیست zone ها
    void ShowDebugInfo()
    {
        Debug.Log("=== Enemy Spawner Debug Info ===");
        Debug.Log($"Total Enemies: {GetTotalEnemyCount()}/{globalMaxEnemies}");
        Debug.Log($"Total Spawned: {totalEnemiesSpawned}");
        Debug.Log($"Available Zones: {string.Join(", ", GetAvailableZones())}");
        
        foreach (var kvp in zoneEnemies)
        {
            Debug.Log($"  - {kvp.Key}: {GetEnemyCountInZone(kvp.Key)}/{maxEnemiesPerZone}");
        }
    }
    
    // ===== Public API =====
    
    public void StartSpawning()
    {
        autoSpawn = true;
        
        if (!IsInvoking(nameof(SpawnCycle)))
        {
            InvokeRepeating(nameof(SpawnCycle), 0f, spawnCheckInterval);
        }
    }
    
    public void StopSpawning()
    {
        autoSpawn = false;
        CancelInvoke(nameof(SpawnCycle));
    }
    
    
    public void ClearAllEnemies()
    {
        foreach (var kvp in zoneEnemies)
        {
            foreach (var enemy in kvp.Value)
            {
                if (enemy != null)
                {
                    Destroy(enemy);
                }
            }
        }
        
        zoneEnemies.Clear();
        Debug.Log("🧹 All enemies cleared");
    }
    
    public void SpawnEnemyNow(string zoneId = null, string enemyType = null)
    {
        if (string.IsNullOrEmpty(zoneId))
        {
            List<string> zones = GetZonesNeedingEnemies();
            if (zones.Count > 0)
            {
                zoneId = zones[Random.Range(0, zones.Count)];
            }
            else
            {
                Debug.LogWarning("⚠️ All zones are full!");
                return;
            }
        }
        
        if (string.IsNullOrEmpty(enemyType))
        {
            enemyType = GetRandomEnemyType();
        }
        
        SpawnEnemyInZone(zoneId, enemyType);
    }
}
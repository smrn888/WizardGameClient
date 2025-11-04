using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Linq;

// ===== کلاس‌های مدل داده =====
[System.Serializable]
public class MapData
{
    public string version;
    public string name;
    public int tileSize;
    public int width;
    public int height;
    public Zone[] zones;
    public SpawnData spawns;
    public MinimapConfig minimap;
}

[System.Serializable]
public class Zone
{
    public string id;
    public string name;
    public ZoneBounds bounds;
    public string texture;
    public string description;
    public Exit[] exits;
}

[System.Serializable]
public class ZoneBounds
{
    public int x;
    public int y;
    public int width;
    public int height;
}

[System.Serializable]
public class Exit
{
    public string side;
    public string connects_to;
    public int? door_at;
}

[System.Serializable]
public class SpawnData
{
    public SpawnPoint player;
    public EnemySpawn[] enemies;
}

[System.Serializable]
public class SpawnPoint
{
    public int x;
    public int y;
}

[System.Serializable]
public class EnemySpawn
{
    public int x;
    public int y;
    public string zone;
    public string name;
    public string house;
}

[System.Serializable]
public class MinimapConfig
{
    public bool enabled;
}

// ===== مدیر نقشه =====
public class MapManager : MonoBehaviour
{
    [Header("Map Settings")]
    public TextAsset mapJsonFile;
    public float tileWorldSize = 1f;
    
    [Header("Zone Textures")]
    public Sprite[] zoneSprites;
    
    [Header("Spawn Settings")]
    public GameObject playerPrefab;
    public bool spawnEnemiesOnStart = true;
    
    // ⭐ PUBLIC: برای دسترسی EnemySpawnerManager
    public MapData mapData { get; private set; }
    
    private Dictionary<string, Sprite> zoneSpriteDict;
    private Dictionary<string, GameObject> zoneObjects;
    private Zone currentZone;
    private Transform enemiesParent;
    private bool isMapReady = false;

    void Awake()
    {
        Debug.Log("🗺️ MapManager Awake - Starting initialization...");
        
        // ✅ CRITICAL: لود کردن نقشه در Awake
        LoadMap();
        CreateZoneDictionary();
        CreateEnemiesParent();
        
        // ✅ بررسی موفقیت لود
        if (mapData != null && mapData.zones != null && mapData.zones.Length > 0)
        {
            Debug.Log($"✅ MapManager ready with {mapData.zones.Length} zones");
            isMapReady = true;
        }
        else
        {
            Debug.LogError("❌ MapManager failed to initialize! mapData is null or empty");
        }
    }

    void Start()
    {
        if (!isMapReady)
        {
            Debug.LogError("❌ Cannot build map: MapManager not ready!");
            return;
        }
        
        BuildMap();
        
        if (spawnEnemiesOnStart)
        {
            SpawnAllEnemies();
        }
    }

    void CreateEnemiesParent()
    {
        GameObject parent = new GameObject("Enemies");
        parent.transform.parent = transform;
        enemiesParent = parent.transform;
    }

    void LoadMap()
    {
        Debug.Log("📥 LoadMap called...");
        
        if (mapJsonFile == null)
        {
            Debug.LogError("❌ Map JSON file not assigned in Inspector!");
            return;
        }

        try
        {
            Debug.Log($"📄 JSON file size: {mapJsonFile.text.Length} chars");
            
            mapData = JsonConvert.DeserializeObject<MapData>(mapJsonFile.text);
            
            if (mapData == null)
            {
                Debug.LogError("❌ Failed to deserialize map data!");
                return;
            }
            
            Debug.Log($"✅ Map loaded: {mapData.name}");
            Debug.Log($"📊 Total zones: {mapData.zones?.Length ?? 0}");
            
            // ✅ بررسی zones
            if (mapData.zones == null || mapData.zones.Length == 0)
            {
                Debug.LogError("❌ No zones in map data!");
                return;
            }
            
            // ✅ لیست کردن Zone ها
            foreach (var zone in mapData.zones)
            {
                if (zone != null)
                {
                    Debug.Log($"  Zone: {zone.id} - {zone.name}");
                }
            }
            
            if (mapData.spawns != null && mapData.spawns.enemies != null)
            {
                Debug.Log($"👹 Total enemies to spawn: {mapData.spawns.enemies.Length}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Failed to parse JSON: {e.Message}");
            Debug.LogError($"Stack trace: {e.StackTrace}");
            mapData = null;
        }
    }

    void CreateZoneDictionary()
    {
        zoneSpriteDict = new Dictionary<string, Sprite>();
        
        if (zoneSprites == null || zoneSprites.Length == 0)
        {
            Debug.LogWarning("⚠️ No zone sprites assigned!");
            return;
        }
        
        foreach (var sprite in zoneSprites)
        {
            if (sprite != null)
            {
                zoneSpriteDict[sprite.name] = sprite;
                Debug.Log($"📦 Loaded sprite: {sprite.name}");
            }
        }
    }

    void BuildMap()
    {
        if (mapData == null)
        {
            Debug.LogError("❌ Cannot build map: mapData is null!");
            return;
        }
        
        if (mapData.zones == null || mapData.zones.Length == 0)
        {
            Debug.LogError("❌ Cannot build map: no zones defined!");
            return;
        }

        zoneObjects = new Dictionary<string, GameObject>();

        foreach (var zone in mapData.zones)
        {
            if (zone == null)
            {
                Debug.LogWarning("⚠️ Null zone found, skipping...");
                continue;
            }
            
            GameObject zoneObj = new GameObject($"Zone_{zone.id}");
            zoneObj.transform.parent = transform;

            SpriteRenderer sr = zoneObj.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 0;
            
            string textureKey = zone.texture.Replace(".png", "");
            if (zoneSpriteDict.ContainsKey(textureKey))
            {
                sr.sprite = zoneSpriteDict[textureKey];
            }
            else
            {
                Debug.LogWarning($"⚠️ Texture not found for zone: {zone.id} (looking for: {textureKey})");
            }

            float posX = (zone.bounds.x + zone.bounds.width / 2f) * tileWorldSize;
            float posY = -(zone.bounds.y + zone.bounds.height / 2f) * tileWorldSize;
            zoneObj.transform.position = new Vector3(posX, posY, 1);

            if (sr.sprite != null)
            {
                float scaleX = zone.bounds.width * tileWorldSize / sr.sprite.bounds.size.x;
                float scaleY = zone.bounds.height * tileWorldSize / sr.sprite.bounds.size.y;
                zoneObj.transform.localScale = new Vector3(scaleX, scaleY, 1);
            }

            CreateZoneWalls(zoneObj, zone);
            zoneObjects[zone.id] = zoneObj;
        }
        
        Debug.Log($"✅ Map building complete! Created {zoneObjects.Count} zones");
    }

    void CreateZoneWalls(GameObject parent, Zone zone)
    {
        GameObject wallsParent = new GameObject("Walls");
        wallsParent.transform.parent = parent.transform;
        wallsParent.transform.localPosition = Vector3.zero;

        float left = -zone.bounds.width / 2f * tileWorldSize;
        float right = zone.bounds.width / 2f * tileWorldSize;
        float top = zone.bounds.height / 2f * tileWorldSize;
        float bottom = -zone.bounds.height / 2f * tileWorldSize;
        float wallThickness = 0.5f;

        CreateWall(wallsParent.transform, "Wall_North", 
            new Vector3(0, top, 0), 
            new Vector2(zone.bounds.width * tileWorldSize, wallThickness));

        CreateWall(wallsParent.transform, "Wall_South", 
            new Vector3(0, bottom, 0), 
            new Vector2(zone.bounds.width * tileWorldSize, wallThickness));

        CreateWall(wallsParent.transform, "Wall_West", 
            new Vector3(left, 0, 0), 
            new Vector2(wallThickness, zone.bounds.height * tileWorldSize));

        CreateWall(wallsParent.transform, "Wall_East", 
            new Vector3(right, 0, 0), 
            new Vector2(wallThickness, zone.bounds.height * tileWorldSize));

        if (zone.exits != null)
        {
            CreateDoors(parent.transform, zone);
        }
    }

    void CreateWall(Transform parent, string name, Vector3 position, Vector2 size)
    {
        GameObject wall = new GameObject(name);
        wall.transform.parent = parent;
        wall.transform.localPosition = position;
        wall.layer = LayerMask.NameToLayer("Wall");

        BoxCollider2D collider = wall.AddComponent<BoxCollider2D>();
        collider.size = size;
        collider.isTrigger = false;
    }

    void CreateDoors(Transform parent, Zone zone)
    {
        GameObject doorsParent = new GameObject("Doors");
        doorsParent.transform.parent = parent;
        doorsParent.transform.localPosition = Vector3.zero;

        foreach (var exit in zone.exits)
        {
            GameObject door = new GameObject($"Door_{exit.side}_{exit.connects_to}");
            door.transform.parent = doorsParent.transform;
            door.layer = LayerMask.NameToLayer("Door");

            Vector3 doorPos = CalculateDoorPosition(zone, exit);
            door.transform.localPosition = doorPos;

            BoxCollider2D collider = door.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(2f, 2f);
            collider.isTrigger = true;

            SpriteRenderer sr = door.AddComponent<SpriteRenderer>();
            sr.color = new Color(1f, 0.84f, 0f, 0.5f);
            sr.sortingOrder = 5;
        }
    }

    Vector3 CalculateDoorPosition(Zone zone, Exit exit)
    {
        float left = -zone.bounds.width / 2f * tileWorldSize;
        float right = zone.bounds.width / 2f * tileWorldSize;
        float top = zone.bounds.height / 2f * tileWorldSize;
        float bottom = -zone.bounds.height / 2f * tileWorldSize;

        switch (exit.side.ToLower())
        {
            case "north": return new Vector3(0, top, 0);
            case "south": return new Vector3(0, bottom, 0);
            case "east": return new Vector3(right, 0, 0);
            case "west": return new Vector3(left, 0, 0);
            default: return Vector3.zero;
        }
    }

    void SpawnAllEnemies()
    {
        if (mapData == null || mapData.spawns == null || mapData.spawns.enemies == null)
        {
            Debug.LogWarning("⚠️ No enemies to spawn!");
            return;
        }

        int spawnedCount = 0;
        
        foreach (var enemyData in mapData.spawns.enemies)
        {
            if (SpawnEnemy(enemyData))
            {
                spawnedCount++;
            }
        }
        
        Debug.Log($"✅ Spawned {spawnedCount} enemies!");
    }

    bool SpawnEnemy(EnemySpawn enemyData)
    {
        if (enemyData == null)
        {
            Debug.LogWarning("⚠️ Null enemy data, skipping...");
            return false;
        }
        
        string prefabPath = $"Prefabs/Enemy_{enemyData.house.ToLower()}";
        GameObject prefab = Resources.Load<GameObject>(prefabPath);
        
        if (prefab == null)
        {
            Debug.LogError($"❌ Enemy prefab not found: {prefabPath}");
            return false;
        }

        Vector3 worldPos = TileToWorldPosition(enemyData.x, enemyData.y);
        worldPos.z = 0;
        
        GameObject enemy = Instantiate(prefab, worldPos, Quaternion.identity, enemiesParent);
        enemy.name = $"{enemyData.name}_{enemyData.house}";
        
        SpriteRenderer sr = enemy.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.sortingOrder = 5;
        }
        
        Debug.Log($"👹 Spawned {enemy.name} at {worldPos}");
        return true;
    }

    Vector3 TileToWorldPosition(int tileX, int tileY)
    {
        float worldX = tileX * tileWorldSize;
        float worldY = -tileY * tileWorldSize;
        return new Vector3(worldX, worldY, 0);
    }

    public void SpawnEnemyAtZone(string zoneName, string house)
    {
        if (!isMapReady)
        {
            Debug.LogError("❌ Cannot spawn enemy: Map not ready!");
            return;
        }
        
        if (string.IsNullOrEmpty(zoneName))
        {
            Debug.LogError("❌ Zone name is null or empty!");
            return;
        }
        
        if (string.IsNullOrEmpty(house))
        {
            Debug.LogError("❌ House name is null or empty!");
            return;
        }
        
        Zone zone = GetZoneById(zoneName);
        if (zone == null)
        {
            Debug.LogError($"❌ Zone data not found: {zoneName}");
            return;
        }

        int centerX = zone.bounds.x + zone.bounds.width / 2;
        int centerY = zone.bounds.y + zone.bounds.height / 2;
        
        int randomOffsetX = Random.Range(-2, 3);
        int randomOffsetY = Random.Range(-2, 3);

        EnemySpawn data = new EnemySpawn
        {
            x = centerX + randomOffsetX,
            y = centerY + randomOffsetY,
            zone = zoneName,
            name = "Spawned Enemy",
            house = house
        };

        SpawnEnemy(data);
    }

    public Vector3 GetPlayerSpawnPosition()
    {
        if (mapData == null || mapData.spawns == null || mapData.spawns.player == null)
        {
            Debug.LogWarning("⚠️ No player spawn point defined, using (0,0,0)");
            return Vector3.zero;
        }

        Vector3 spawnPos = TileToWorldPosition(
            mapData.spawns.player.x, 
            mapData.spawns.player.y
        );
        
        Debug.Log($"🎮 Player spawn position: {spawnPos}");
        return spawnPos;
    }

    public Zone GetZoneAtPosition(Vector3 worldPos)
    {
        if (!isMapReady)
        {
            return null;
        }
        
        int tileX = Mathf.RoundToInt(worldPos.x / tileWorldSize);
        int tileY = Mathf.RoundToInt(-worldPos.y / tileWorldSize);

        foreach (var zone in mapData.zones)
        {
            if (zone == null) continue;
            
            if (tileX >= zone.bounds.x && tileX < zone.bounds.x + zone.bounds.width &&
                tileY >= zone.bounds.y && tileY < zone.bounds.y + zone.bounds.height)
            {
                return zone;
            }
        }

        return null;
    }

    public Zone GetZoneById(string zoneId)
    {
        if (!isMapReady)
        {
            Debug.LogWarning("⚠️ Map data not loaded!");
            return null;
        }
        
        if (string.IsNullOrEmpty(zoneId))
        {
            Debug.LogWarning("⚠️ Zone ID is null or empty!");
            return null;
        }

        foreach (var zone in mapData.zones)
        {
            if (zone != null && zone.id == zoneId)
            {
                return zone;
            }
        }

        Debug.LogWarning($"⚠️ Zone not found: {zoneId}");
        return null;
    }
    
    public string GetCurrentZoneName(Vector3 worldPos)
    {
        Zone zone = GetZoneAtPosition(worldPos);
        return zone != null ? zone.name : "Unknown";
    }
    
    public bool IsMapLoaded()
    {
        return isMapReady;
    }
}
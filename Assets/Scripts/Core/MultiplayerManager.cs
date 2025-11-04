using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// مدیریت بازیکنان آنلاین - SpawnØ UpdateØ Remove
/// این کد برای MultiplayerManager.cs است!
/// </summary>
public class MultiplayerManager : MonoBehaviour
{
    public static MultiplayerManager Instance { get; private set; }
    
    [Header("Prefabs")]
    [SerializeField] private GameObject remotePlayerPrefab;
    
    [Header("Settings")]
    [SerializeField] private float syncInterval = 0.3f;
    [SerializeField] private float playerTimeout = 10f;
    
    [Header("🔧 Development Settings")]
    [SerializeField] private bool developmentMode = false; // ✅ باید FALSE باشه!
    [SerializeField] private bool disablePositionSync = false; // ✅ باید FALSE باشه!
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;
    
    private Dictionary<string, RemotePlayerController> remotePlayers = new Dictionary<string, RemotePlayerController>();
    
    private NetworkManager networkManager;
    private float lastSyncTime;
    
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    
    void Start()
    {
        networkManager = NetworkManager.Instance;
        
        if (networkManager == null)
        {
            Debug.LogError("❌ NetworkManager not found!");
            enabled = false;
            return;
        }
        
        Debug.Log("✅ MultiplayerManager initialized");
        
        // ✅ اگر position sync غیرفعال است، روشن کنید
        if (disablePositionSync)
        {
            Debug.LogError("❌ WARNING: disablePositionSync = TRUE! Enabling it now...");
            disablePositionSync = false;
        }
        
        if (developmentMode)
        {
            Debug.LogWarning("🔧 [MultiplayerManager] Running in DEVELOPMENT MODE - DISABLE THIS!");
            developmentMode = false;
        }
        
        // ✅ شروع polling برای active players
        InvokeRepeating(nameof(FetchActivePlayers), 2f, 5f);
    }
    
    void Update()
    {
        // نمایش تعداد بازیکنان با F1
        if (showDebugInfo && Input.GetKeyDown(KeyCode.F1))
        {
            Debug.Log($"👥 Active Players: {remotePlayers.Count}");
            foreach (var kvp in remotePlayers)
            {
                var remote = kvp.Value;
                Debug.Log($"  - {remote.username} (ID: {kvp.Key}) at ({remote.gameObject.transform.position.x:F2}, {remote.gameObject.transform.position.y:F2})");
            }
        }
    }
    
    /// <summary>
    /// درخواست لیست بازیکنان آنلاین از سرور
    /// </summary>
    void FetchActivePlayers()
    {
        if (disablePositionSync) return;
        if (networkManager == null || !networkManager.isAuthenticated) return;
        
        Debug.Log("📡 Fetching active players from server...");
        
        networkManager.apiClient.Get("/api/game/player/active", (success, response) =>
        {
            if (success)
            {
                try
                {
                    Debug.Log($"📥 Server response: {response}");
                    
                    ActivePlayersResponse data = JsonUtility.FromJson<ActivePlayersResponse>(response);
                    
                    if (data?.players != null)
                    {
                        Debug.Log($"✅ Fetched {data.players.Length} players from server");
                        UpdateRemotePlayers(data.players);
                    }
                    else
                    {
                        Debug.LogWarning("⚠️ Players list is null");
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"❌ Failed to parse active players: {ex.Message}\n{ex.StackTrace}");
                }
            }
            else
            {
                Debug.LogWarning($"⚠️ Failed to fetch active players: {response}");
            }
        }, networkManager.sessionToken);
    }
    
    /// <summary>
    /// آپدیت یا spawn بازیکنان remote
    /// </summary>
    void UpdateRemotePlayers(PlayerPositionData[] players)
    {
        HashSet<string> activePlayerIds = new HashSet<string>();
        
        foreach (var playerData in players)
        {
            // خودمان رو نادیده بگیریم
            if (playerData.playerId == networkManager.playerId)
            {
                Debug.Log($"ℹ️ Ignoring self: {playerData.username}");
                continue;
            }
            
            activePlayerIds.Add(playerData.playerId);
            
            if (!remotePlayers.ContainsKey(playerData.playerId))
            {
                Debug.Log($"🆕 New player detected: {playerData.username}");
                SpawnRemotePlayer(playerData);
            }
            else
            {
                UpdateRemotePlayer(playerData);
            }
        }
        
        // حذف بازیکنانی که قطع شده‌اند
        RemoveInactivePlayers(activePlayerIds);
    }
    
    /// <summary>
    /// Spawn یک بازیکن remote جدید
    /// </summary>
    void SpawnRemotePlayer(PlayerPositionData data)
    {
        if (remotePlayerPrefab == null)
        {
            Debug.LogError("❌ Remote player prefab not assigned in Inspector!");
            return;
        }
        
        Vector3 spawnPos = new Vector3(data.position.x, data.position.y, 0);
        GameObject remoteObj = Instantiate(remotePlayerPrefab, spawnPos, Quaternion.identity);
        remoteObj.name = $"RemotePlayer_{data.username}";
        
        RemotePlayerController remoteController = remoteObj.GetComponent<RemotePlayerController>();
        if (remoteController == null)
        {
            remoteController = remoteObj.AddComponent<RemotePlayerController>();
        }
        
        remoteController.Initialize(
            data.playerId,
            data.username,
            data.house,
            data.position,
            data.health,
            data.maxHealth
        );
        
        remotePlayers.Add(data.playerId, remoteController);
        
        Debug.Log($"🌍 Spawned remote player: {data.username} ({data.house}) at {spawnPos}");
    }
    
    /// <summary>
    /// آپدیت موقعیت بازیکن موجود
    /// </summary>
    void UpdateRemotePlayer(PlayerPositionData data)
    {
        if (remotePlayers.TryGetValue(data.playerId, out RemotePlayerController remote))
        {
            remote.UpdatePosition(data.position);
            remote.UpdateHealth(data.health, data.maxHealth);
            remote.lastUpdateTime = Time.time;
        }
    }
    
    /// <summary>
    /// حذف بازیکنانی که دیگر آنلاین نیستند
    /// </summary>
    void RemoveInactivePlayers(HashSet<string> activeIds)
    {
        List<string> toRemove = new List<string>();
        
        foreach (var kvp in remotePlayers)
        {
            if (!activeIds.Contains(kvp.Key))
            {
                if (Time.time - kvp.Value.lastUpdateTime > playerTimeout)
                {
                    toRemove.Add(kvp.Key);
                }
            }
        }
        
        foreach (string playerId in toRemove)
        {
            RemoveRemotePlayer(playerId);
        }
    }
    
    /// <summary>
    /// حذف یک بازیکن remote
    /// </summary>
    public void RemoveRemotePlayer(string playerId)
    {
        if (remotePlayers.ContainsKey(playerId))
        {
            var remote = remotePlayers[playerId];
            Debug.Log($"👋 Removing remote player: {remote.username}");
            
            Destroy(remote.gameObject);
            remotePlayers.Remove(playerId);
        }
    }
    
    // ===== Socket.IO Event Handlers =====
    
    public void HandleActivePlayersList(ActivePlayersResponse response)
    {
        Debug.Log($"📥 HandleActivePlayersList called with {response.players.Length} players");
        UpdateRemotePlayers(response.players);
    }

    public void HandlePlayerJoined(PlayerPositionData playerPositionData)
    {
        if (networkManager.playerId != playerPositionData.playerId)
        {
            Debug.Log($"🚪 Player Joined: {playerPositionData.username}");
            UpdateRemotePlayerPosition(playerPositionData);
        }
    }

    public void HandlePlayerMoved(PlayerPositionData playerPositionData)
    {
        if (networkManager.playerId != playerPositionData.playerId)
        {
            UpdateRemotePlayerPosition(playerPositionData);
        }
    }

    public void HandlePlayerLeft(string playerId)
    {
        RemoveRemotePlayer(playerId); 
        Debug.Log($"👋 Player Left: {playerId}");
    }

    private void UpdateRemotePlayerPosition(PlayerPositionData data)
    {
        if (remotePlayers.TryGetValue(data.playerId, out RemotePlayerController remote))
        {
            remote.SetTargetPosition(data.position.ToVector3());
            remote.UpdateHealth(data.health, data.maxHealth); 
            remote.lastUpdateTime = Time.time;
        }
        else
        {
            SpawnRemotePlayer(data);
        }
    }

    public RemotePlayerController GetRemotePlayer(string playerId)
    {
        remotePlayers.TryGetValue(playerId, out RemotePlayerController remote);
        return remote;
    }
    
    public List<RemotePlayerController> GetAllRemotePlayers()
    {
        return new List<RemotePlayerController>(remotePlayers.Values);
    }
    
    void OnDestroy()
    {
        foreach (var remote in remotePlayers.Values)
        {
            if (remote != null && remote.gameObject != null)
            {
                Destroy(remote.gameObject);
            }
        }
        remotePlayers.Clear();
    }
}

// ===== Data Classes =====

[System.Serializable]
public class ActivePlayersResponse
{
    public PlayerPositionData[] players;
}

[System.Serializable]
public class PlayerPositionData
{
    public string playerId;
    public string username;
    public string house;
    public Vector2Serializable position;
    public string zoneId;
    public float health;
    public float maxHealth;
}

[System.Serializable]
public class Vector2Serializable
{
    public float x;
    public float y;
    
    public Vector2 ToVector2()
    {
        return new Vector2(x, y);
    }
    
    public Vector3 ToVector3()
    {
        return new Vector3(x, y, 0); 
    }
}
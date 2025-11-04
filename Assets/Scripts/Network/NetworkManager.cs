using UnityEngine;
using System;
using System.Collections;
using System.Threading.Tasks; 
using System.Collections.Generic;
using SocketIOClient;
using Newtonsoft.Json;
using SocketIOClient.Newtonsoft.Json; 

/// <summary>
/// مدیریت اتصال به سرور و سینک داده‌ها از طریق HTTP (APIClient) و Real-Time (Socket.IO)
/// ✅ FIXED: اضافه شدن تمام متدها و Eventها + Method Signatures
/// ✅ FIXED: جلوگیری از هنگ کردن با timeout و cooldown
/// ✅ FIXED: DisconnectSocketIO اضافه شد
/// </summary>
public class NetworkManager : MonoBehaviour
{
    // ================== Singleton ==================
    public static NetworkManager Instance { get; private set; }

    [Header("Server Settings")]
    [SerializeField] private string serverURL = "http://127.0.0.1:3000";
    [SerializeField] private float pingInterval = 5f;
    [SerializeField] private float syncInterval = 2f;

    [Header("Connection Status")]
    public bool isConnected = false;
    public bool isAuthenticated = false;
    public float currentPing = 0f;

    [Header("Player Session")]
    public string sessionToken;
    public string playerId;
    public PlayerData localPlayerData;
    private PlayerController playerControllerCache; // ✅ متغیر جدید

    // Socket.IO Client
    private SocketIO socket; 
    
    // HTTP Client
    public APIClient apiClient { get; private set; }
    private SaveManager saveManager;
    private float lastPingTime;
    private float lastSyncTime;
    
    // ✅ جلوگیری از هنگ
    private float lastSaveTime = 0f;
    private const float SAVE_COOLDOWN = 3f;
    private bool isSaving = false;
    
    // ================== Events ==================
    public event Action OnConnected;
    public event Action OnDisconnected;
    public event Action<PlayerData> OnPlayerDataUpdated;
    public event Action<string> OnError;
    
    // ✅ روویدادهای Combat (برای CombatNetworkSync)
    public event Action<SpellCastData> OnSpellCasted;
    public event Action<DamageReceivedData> OnDamageReceived;
    public event Action<PlayerDeathData> OnPlayerDied;

    // ✅ Propertyهای کمکی
    public string username => localPlayerData?.username;
    public string house => localPlayerData?.house;
    public int xp => localPlayerData?.xp ?? 0;
    public int galleons => localPlayerData?.galleons ?? 0;
    
    // ================== Lifecycle ==================

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        apiClient = GetComponent<APIClient>();
        if (apiClient == null)
        {
            apiClient = gameObject.AddComponent<APIClient>();
        }
        apiClient.SetServerURL(serverURL);

        saveManager = SaveManager.Instance;
        if (saveManager == null)
        {
            Debug.LogWarning("⚠️ SaveManager not found in scene, creating one...");
            GameObject saveManagerObj = new GameObject("SaveManager");
            saveManager = saveManagerObj.AddComponent<SaveManager>();
        }
    }

 // Replace the incorrect Start() method (lines 98-118) with this:

    void Start()
    {
        // Initialize timing variables
        lastPingTime = Time.time;
        lastSyncTime = Time.time;
        
        Debug.Log("✅ NetworkManager initialized");
        
        // ❌ AUTO-LOGIN DISABLED
        // Users must login manually through the UI
        // Session will be restored AFTER manual login
    }
    
    void Update()
    {
        if (isAuthenticated)
        {
            // ✅ اضافه کردن چک برای playerControllerCache
            if (playerControllerCache == null)
            {
                playerControllerCache = FindFirstObjectByType<PlayerController>();
            }

            if (isConnected && playerControllerCache != null && Time.time - lastSyncTime > syncInterval) // ✅ استفاده از Cache
            {
                SendPositionUpdate();
                lastSyncTime = Time.time;
            }

            if (isConnected && Time.time - lastPingTime > pingInterval)
            {
                SendPing();
                lastPingTime = Time.time;
            }
        }
    }

    // ================== Socket.IO Management ==================

    public void ConnectSocketIO()
    {
        Debug.Log("📌 [ConnectSocketIO] Starting Socket.IO connection...");
        
        if (!isAuthenticated)
        {
            Debug.LogError("❌ [ConnectSocketIO] Cannot connect Socket.IO: Not authenticated.");
            return;
        }

        if (socket != null && socket.Connected)
        {
            Debug.Log("⚠️ [ConnectSocketIO] Socket.IO already connected.");
            return;
        }

        try
        {
            Debug.Log($"📌 [ConnectSocketIO] Creating Socket.IO connection to: {serverURL}");
            
            var uri = new Uri(serverURL);
            Debug.Log($"📌 [ConnectSocketIO] URI created: {uri}");
            
            socket = new SocketIO(uri, new SocketIOOptions
            {
                Query = new Dictionary<string, string>
                {
                    {"token", sessionToken},
                    {"playerId", playerId}
                },
                EIO = SocketIOClient.EngineIO.V4,
                Reconnection = true,
                ReconnectionAttempts = 5,
                // Transports = SocketIOClient.Transport.WebSocket
            });
            
            Debug.Log("📌 [ConnectSocketIO] SocketIO object created");
            
            // socket.JsonSerializer = new NewtonsoftJsonSerializer();
            
            Debug.Log("📌 [ConnectSocketIO] Setting up event handlers...");
            SetupSocketIOEventHandlers();
            
            Debug.Log("📌 [ConnectSocketIO] Calling socket.ConnectAsync()...");
            socket.ConnectAsync();
            Debug.Log("📌 [ConnectSocketIO] ConnectAsync() called - waiting for connection...");
        }
        catch (Exception ex)
        {
            Debug.LogError($"❌ [ConnectSocketIO] Exception: {ex.Message}\n{ex.StackTrace}");
        }
    }

    // ✅ FIXED: DisconnectSocketIO اضافه شد
    public void DisconnectSocketIO()
    {
        Debug.Log("📌 [DisconnectSocketIO] Disconnecting Socket.IO...");
        
        if (socket != null)
        {
            try
            {
                socket.DisconnectAsync();
                socket.Dispose();
                socket = null;
                isConnected = false;
                Debug.Log("✅ [DisconnectSocketIO] Socket.IO disconnected");
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ [DisconnectSocketIO] Error: {ex.Message}");
            }
        }
    }
    
// فقط بخش‌های مربوط به Socket.IO event handlers رو جایگزین کنید

    private void SetupSocketIOEventHandlers()
    {
        Debug.Log("🔌 [SetupSocketIOEventHandlers] Setting up handlers...");
        
        socket.OnConnected += (sender, e) => 
        {
            Debug.Log("✅ [Socket.IO] CONNECTED!");
            isConnected = true;
            Debug.Log("🔌 [Socket.IO] Now emitting player:join event...");
            
            if (localPlayerData != null)
            {
                try
                {
                    var joinData = new {
                        playerId = playerId,
                        username = localPlayerData.username,
                        house = localPlayerData.house,
                        position = new { x = 0f, y = 0f }
                    };
                    
                    Debug.Log($"🔌 [Socket.IO] Emitting player:join with username: {localPlayerData.username}, house: {localPlayerData.house}");
                    socket.EmitAsync("player:join", joinData);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"❌ [Socket.IO] Error emitting player:join: {ex.Message}");
                }
            }
            
            OnConnected?.Invoke();
        };

        socket.OnDisconnected += (sender, e) => 
        {
            Debug.LogWarning("❌ [Socket.IO] DISCONNECTED!");
            isConnected = false;
            OnDisconnected?.Invoke();
        };

        socket.OnError += (sender, e) =>
        {
            Debug.LogError($"❌ [Socket.IO] ERROR: {e}");
            OnError?.Invoke(e);
        };

        socket.OnReconnectAttempt += (sender, e) =>
        {
            Debug.LogWarning($"⚠️ [Socket.IO] Reconnect attempt: {e}");
        };

        socket.OnReconnected += (sender, e) =>
        {
            Debug.Log($"✅ [Socket.IO] Reconnected after: {e}");
            isConnected = true;
        };

        // ===== CRITICAL EVENTS FOR MULTIPLAYER =====

        socket.On("players:list", response => 
        {
            UnityMainThreadDispatcher.Instance().Enqueue(() => 
            {
                try {
                    string json = response.GetValue().ToString();
                    Debug.Log($"👥 [players:list] Raw JSON: {json}");
                    
                    var wrapper = JsonConvert.DeserializeObject<ActivePlayersResponse>(json);
                    
                    if (wrapper != null && wrapper.players != null)
                    {
                        Debug.Log($"✅ [players:list] Parsed {wrapper.players.Length} players");
                        
                        // ✅ به MultiplayerManager ارسال کنید
                        if (MultiplayerManager.Instance != null)
                        {
                            MultiplayerManager.Instance.HandleActivePlayersList(wrapper);
                        }
                        else
                        {
                            Debug.LogWarning("⚠️ MultiplayerManager not found!");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"⚠️ [players:list] Wrapper or players is null");
                    }
                } 
                catch (Exception ex) {
                    Debug.LogError($"❌ [SocketIO] Error deserializing players:list: {ex.Message}\nJSON: {response.GetValue()}");
                }
            });
        });

        socket.On("player:joined", response => 
        {
            UnityMainThreadDispatcher.Instance().Enqueue(() => 
            {
                try {
                    string json = response.GetValue().ToString();
                    Debug.Log($"🚪 [player:joined] Received: {json}");
                    
                    PlayerPositionData data = JsonConvert.DeserializeObject<PlayerPositionData>(json);
                    
                    if (MultiplayerManager.Instance != null)
                    {
                        MultiplayerManager.Instance.HandlePlayerJoined(data);
                    }
                } 
                catch (Exception ex) {
                    Debug.LogError($"❌ [SocketIO] Error deserializing player:joined: {ex.Message}");
                }
            });
        });

        socket.On("player:moved", response => 
        {
            UnityMainThreadDispatcher.Instance().Enqueue(() => 
            {
                try {
                    string json = response.GetValue().ToString();
                    Debug.Log($"🚶 [player:moved] Received: {json}");
                    
                    PlayerPositionData data = JsonConvert.DeserializeObject<PlayerPositionData>(json);
                    
                    if (MultiplayerManager.Instance != null)
                    {
                        MultiplayerManager.Instance.HandlePlayerMoved(data);
                    }
                } 
                catch (Exception ex) {
                    Debug.LogError($"❌ [SocketIO] Error deserializing player:moved: {ex.Message}");
                }
            });
        });

        socket.On("player:left", response => 
        {
            UnityMainThreadDispatcher.Instance().Enqueue(() => 
            {
                try {
                    string playerId = response.GetValue().ToString();
                    Debug.Log($"👋 [player:left] Player left: {playerId}");
                    
                    if (MultiplayerManager.Instance != null)
                    {
                        MultiplayerManager.Instance.HandlePlayerLeft(playerId);
                    }
                }
                catch (Exception ex) {
                    Debug.LogError($"❌ Error handling player:left: {ex.Message}");
                }
            });
        });
            
        socket.On("spell:casted", response => 
        {
            UnityMainThreadDispatcher.Instance().Enqueue(() => 
            {
                try {
                    string json = response.GetValue().ToString();
                    SpellCastData data = JsonConvert.DeserializeObject<SpellCastData>(json);
                    OnSpellCasted?.Invoke(data);
                } catch (Exception ex) {
                    Debug.LogError($"[SocketIO] Error deserializing spell:casted: {ex.Message}");
                }
            });
        });

        socket.On("damage:received", response => 
        {
            UnityMainThreadDispatcher.Instance().Enqueue(() => 
            {
                try {
                    string json = response.GetValue().ToString();
                    DamageReceivedData data = JsonConvert.DeserializeObject<DamageReceivedData>(json);
                    OnDamageReceived?.Invoke(data);
                } catch (Exception ex) {
                    Debug.LogError($"[SocketIO] Error deserializing damage:received: {ex.Message}");
                }
            });
        });

        socket.On("player:died", response => 
        {
            UnityMainThreadDispatcher.Instance().Enqueue(() => 
            {
                try {
                    string json = response.GetValue().ToString();
                    PlayerDeathData data = JsonConvert.DeserializeObject<PlayerDeathData>(json);
                    OnPlayerDied?.Invoke(data);
                } catch (Exception ex) {
                    Debug.LogError($"[SocketIO] Error deserializing player:died: {ex.Message}");
                }
            });
        });
        
        Debug.Log("🔌 [SetupSocketIOEventHandlers] Handlers setup complete");
    }

    // ================== Real-Time Send Methods ==================

    public void SendPositionUpdate()
    {
        if (socket == null || !socket.Connected || localPlayerData == null) return;

        // ✅ استفاده از متغیر ذخیره‌شده
        if (playerControllerCache == null) return;

        var positionData = new {
            playerId = playerId,
            position = new { 
                x = playerControllerCache.transform.position.x,
                y = playerControllerCache.transform.position.y
            }
        };

        socket.EmitAsync("player:move", positionData);
    }

    public void SendSpellCast(string spellName, Vector3 position, Vector2 direction, Color color, int damage, float speed)
    {
        if (socket == null || !socket.Connected) return;

        var spellData = new {
            casterId = playerId,
            casterName = username,
            spellName = spellName,
            position = new { x = position.x, y = position.y },
            direction = new { x = direction.x, y = direction.y },
            color = new { r = color.r, g = color.g, b = color.b, a = color.a },
            damage = damage,
            speed = speed
        };

        // ✅ FIXED: Emit object directly
        socket.EmitAsync("spell:cast", spellData);
        Debug.Log($"🤔 Sent spell: {spellName}");
    }

    public void SendDamageDealt(string targetPlayerId, float damage, string spellName)
    {
        if (socket == null || !socket.Connected) return;

        var damageData = new {
            attackerId = playerId,
            targetId = targetPlayerId,
            damage = damage,
            source = spellName
        };

        // ✅ FIXED: Emit object directly
        socket.EmitAsync("damage:dealt", damageData);
        Debug.Log($"⚔️ Sent damage: {damage} to {targetPlayerId}");
    }

    public void SendPing()
    {
        if (socket == null || !socket.Connected) return;
        socket.EmitAsync("ping"); 
    }

    // ================== HTTP API Methods ==================

    public void ConnectToServer(Action callback = null)
    {
        if (isAuthenticated)
        {
            LoadPlayerData(() => 
            {
                ConnectSocketIO();
                callback?.Invoke();
            });
        }
        else
        {
            callback?.Invoke();
        }
    }

    public void Login(string username, string password, Action<bool, string> callback)
    {
        Debug.Log($"📝 Login attempt: {username}");
        
        var request = new NetworkLoginRequest 
        { 
            username = username, 
            password = password 
        };
        
        apiClient.Post("/api/auth/login", request, (success, response) =>
        {
            if (success)
            {
                try
                {
                    ExtendedAuthResponse authResponse = JsonUtility.FromJson<ExtendedAuthResponse>(response);
                    
                    sessionToken = authResponse.token;
                    playerId = authResponse.playerId;
                    isAuthenticated = true;
                    
                    localPlayerData = new PlayerData
                    {
                        playerId = authResponse.playerId,
                        username = authResponse.username,
                        house = authResponse.house ?? "Gryffindor",
                        xp = 0,
                        xpLevel = 1,
                        galleons = 100,
                        currentHealth = 100,
                        maxHealth = 100,
                        horcruxes = 0
                    };
                    
                    saveManager.SaveSession(sessionToken, playerId);
                    Debug.Log($"✅ Login successful! Player: {localPlayerData.username}, House: {localPlayerData.house}");
                    
                    LoadPlayerData(() => 
                    {
                        Debug.Log("📌 Login: Player data loaded, now connecting Socket.IO...");
                        ConnectSocketIO();
                        callback?.Invoke(true, "Login Successful");
                    });
                }
                catch (Exception ex)
                {
                    Debug.LogError($"❌ Auth parse error: {ex.Message}");
                    callback?.Invoke(false, "Invalid server response");
                }
            }
            else
            {
                Debug.LogError($"❌ Login failed: {response}");
                callback?.Invoke(false, response);
            }
        });
    }

    public void Register(string username, string email, string password, Action<bool, string> callback)
    {
        Debug.Log($"📝 Register attempt: {username}");
        
        var request = new NetworkRegisterRequest 
        { 
            username = username, 
            email = email, 
            password = password 
        };
        
        apiClient.Post("/api/auth/register", request, (success, response) =>
        {
            if (success)
            {
                Login(username, password, callback); 
            }
            else
            {
                callback?.Invoke(false, response);
            }
        });
    }

    public void Logout()
    {
        DisconnectSocketIO();
        isAuthenticated = false;
        sessionToken = null;
        playerId = null;
        localPlayerData = null;
        
        // ✅ حذف کامل session از SaveManager
        if (saveManager != null)
        {
            saveManager.ClearSession();
        }
        
        Debug.Log("🚪 Logged out and session cleared.");
    }
    
    public void LoadPlayerData(Action callback)
    {
        if (!isAuthenticated || string.IsNullOrEmpty(playerId)) 
        {
            callback?.Invoke();
            return;
        }

        apiClient.Get($"/api/game/player/{playerId}", (success, response) =>
        {
            if (success)
            {
                try
                {
                    PlayerData serverData = JsonUtility.FromJson<PlayerData>(response);
                    
                    if (serverData != null)
                    {
                        localPlayerData = serverData;
                        OnPlayerDataUpdated?.Invoke(localPlayerData);
                        Debug.Log($"✅ Full player data loaded from server for {localPlayerData.username}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"⚠️ Could not parse full player data: {ex.Message}");
                }
            }
            else
            {
                Debug.LogWarning($"⚠️ Player data endpoint not available (OK if using basic login data)");
            }
            
            callback?.Invoke();
            
        }, sessionToken);
    }
    
    public void SavePlayerData()
    {
        SavePlayerData((Action<bool>)null);
    }
    
    public void SavePlayerData(System.Action<bool> onComplete = null)
    {
        if (isSaving)
        {
            Debug.LogWarning("⚠️ Already saving, skipping...");
            onComplete?.Invoke(false);
            return;
        }
        
        if (Time.time - lastSaveTime < SAVE_COOLDOWN)
        {
            float remaining = SAVE_COOLDOWN - (Time.time - lastSaveTime);
            Debug.LogWarning($"⏳ Save cooldown: {remaining:F1}s remaining");
            onComplete?.Invoke(false);
            return;
        }
        
        if (!isAuthenticated || localPlayerData == null)
        {
            Debug.LogError("❌ Cannot save: Not authenticated or no player data!");
            onComplete?.Invoke(false);
            return;
        }
        
        isSaving = true;
        lastSaveTime = Time.time;
        
        Debug.Log("💾 Saving Player Data:");
        Debug.Log($"  - XP: {localPlayerData.xp}");
        Debug.Log($"  - Level: {localPlayerData.xpLevel}");
        Debug.Log($"  - Galleons: {localPlayerData.galleons}");
        Debug.Log($"  - HP: {localPlayerData.currentHealth}/{localPlayerData.maxHealth}");
        
        StartCoroutine(SavePlayerDataCoroutine(onComplete));
    }

    IEnumerator SavePlayerDataCoroutine(Action<bool> callback)
    {
        bool requestCompleted = false;
        bool requestSuccess = false;

        string endpoint = $"/api/game/player/{playerId}/save";
        
        apiClient.Post(endpoint, new { }, (success, response) =>
        {
            requestCompleted = true;
            requestSuccess = success;

            if (success)
            {
                Debug.Log("💾 Player Data Saved.");
            }
            else
            {
                Debug.LogError($"❌ Save failed: {response}");
            }
        }, sessionToken);

        float timeout = 5f;
        float elapsed = 0f;

        while (!requestCompleted && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (!requestCompleted)
        {
            Debug.LogWarning("⚠️ Save request timed out after 5s - game continues");
        }

        isSaving = false;
        callback?.Invoke(requestSuccess);
    }

    public void AddXP(int amount, Action<bool> callback = null)
    {
        if (localPlayerData == null)
        {
            callback?.Invoke(false);
            return;
        }
        
        localPlayerData.AddXP(amount);
        Debug.Log($"➕ Added {amount} XP. New XP: {localPlayerData.xp}");
        
        SavePlayerData((success) => 
        {
            OnPlayerDataUpdated?.Invoke(localPlayerData);
            callback?.Invoke(success);
        });
    }

    public void AddGalleons(int amount, Action<bool> callback = null)
    {
        if (localPlayerData == null)
        {
            callback?.Invoke(false);
            return;
        }
        
        localPlayerData.galleons += amount;
        Debug.Log($"💰 Added {amount} Galleons. New Balance: {localPlayerData.galleons}");
        
        SavePlayerData((success) => 
        {
            OnPlayerDataUpdated?.Invoke(localPlayerData);
            callback?.Invoke(success);
        });
    }

    public void ReportDamage(int damageTaken, float newHealth, float maxHealth)
    {
        if (socket == null || !socket.Connected || localPlayerData == null) return;
        
        // 1. به‌روزرسانی داده‌ی محلی
        localPlayerData.currentHealth = newHealth; 
        
        // 2. ارسال اطلاعات به سرور
        var damageReportData = new
        {
            playerId = playerId,
            damageTaken = damageTaken,
            newHealth = newHealth,
            maxHealth = maxHealth
        };

        socket.EmitAsync("player:reportDamage", damageReportData);
        
        // 3. اطلاع‌رسانی به UIهای محلی
        OnPlayerDataUpdated?.Invoke(localPlayerData);
        Debug.Log($"📤 Reported damage: {damageTaken} to server. New HP: {newHealth}");
    }

    public void TakeDamage(int damage, string source, Action<bool> callback = null)
    {
        if (localPlayerData == null)
        {
            callback?.Invoke(false);
            return;
        }
        
        // 1. محاسبه‌ی مقدار سلامتی جدید
        float newHealth = Mathf.Max(0, localPlayerData.currentHealth - damage);
        
        // 2. گزارش آسیب به سرور برای سینک شدن
        ReportDamage(damage, newHealth, localPlayerData.maxHealth);
        
        Debug.Log($"💔 Took {damage} Damage from {source}. HP: {newHealth}");
        
        callback?.Invoke(true);
    }
    
    // ================== Cleanup ==================

    void OnApplicationQuit()
    {
        DisconnectSocketIO(); 
        if (isAuthenticated && localPlayerData != null)
        {
            SavePlayerData();
        }
    }

    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            DisconnectSocketIO();
            if (isAuthenticated && localPlayerData != null)
            {
                SavePlayerData();
            }
        }
        else if (isAuthenticated)
        {
            ConnectSocketIO();
        }
    }
}
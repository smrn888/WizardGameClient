using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// مدیر اصلی بازی - مدیریت Game State، Spawn، و گیم‌پلی
/// </summary>
public class GameManager : MonoBehaviour
{
    [Header("Game State")]
    [SerializeField] private GameState currentState = GameState.Playing;
    [SerializeField] private bool isPaused = false;
    
    [Header("Player")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Transform playerSpawnPoint;
    
    [Header("Spawn Settings")]
    [SerializeField] private bool autoSpawnEnemies = true;
    [SerializeField] private float enemySpawnInterval = 30f;
    [SerializeField] private int maxEnemiesPerZone = 3;
    
    [Header("References")]
    [SerializeField] private UIManager uiManager;
    [SerializeField] private MapManager mapManager;
    
    // Singleton
    public static GameManager Instance { get; private set; }
    
    // References
    private NetworkManager networkManager;
    private PlayerController localPlayer;
//    private DamageSystem damageSystem;
    
    // State
    private float lastEnemySpawnTime;
    
    // Events
    public event System.Action OnGameStarted;
    public event System.Action OnGamePaused;
    public event System.Action OnGameResumed;
    public event System.Action OnGameOver;
    
    void Awake()
    {
        // Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        // Get references
        networkManager = NetworkManager.Instance;
        
        if (uiManager == null)
            uiManager = FindObjectOfType<UIManager>();
        
        if (mapManager == null)
            mapManager = FindObjectOfType<MapManager>();
        
    //     damageSystem = FindObjectOfType<DamageSystem>();
    //     if (damageSystem == null)
    //     {
    //         GameObject dmgSys = new GameObject("DamageSystem");
    //         damageSystem = dmgSys.AddComponent<DamageSystem>();
    //     }
    }

    void Start()
    {
        // ✅ فقط تو Game Scene منتظر بمون
        string currentScene = SceneManager.GetActiveScene().name;
        
        if (currentScene == "Game" || currentScene == "GameScene") // اسم دقیق Scene خودتون
        {
            StartCoroutine(WaitForLoginAndStart());
        }
        else
        {
            Debug.Log($"📍 GameManager in {currentScene} - Skipping game initialization");
        }
    }

    IEnumerator WaitForLoginAndStart()
    {
        Debug.Log("⏳ Waiting for authentication...");
        
        yield return new WaitUntil(() => NetworkManager.Instance != null);
        yield return new WaitUntil(() => NetworkManager.Instance.isAuthenticated);
        yield return new WaitUntil(() => NetworkManager.Instance.localPlayerData != null);

        Debug.Log("✅ Player authenticated AND data loaded!");
        
        // ✅ Find MapManager if not assigned
        if (mapManager == null)
        {
            Debug.Log("🔍 Searching for MapManager in scene...");
            mapManager = FindObjectOfType<MapManager>();
        }
        
        // ✅ Check if MapManager exists IN GAME SCENE
        if (mapManager == null)
        {
            Debug.LogError("❌ MapManager not found in Game Scene! Please add MapManager GameObject.");
            yield break;
        }
        
        // ✅ Wait for MapManager to load
        int maxWaitFrames = 100;
        int frameCount = 0;
        
        while (frameCount < maxWaitFrames)
        {
            if (mapManager.IsMapLoaded())
            {
                Debug.Log("✅ MapManager loaded successfully");
                break;
            }
            
            frameCount++;
            yield return null;
        }
        
        if (!mapManager.IsMapLoaded())
        {
            Debug.LogError("❌ MapManager failed to load! Check if map JSON is assigned in Inspector.");
            yield break;
        }
        
        // ✅ Continue with shop and player spawn
        yield return new WaitForSeconds(0.5f);
        Debug.Log("🛒 Shop is ready! Press B to open.");
        
        if (localPlayer == null)
        {
            SpawnPlayer();
        }
    }
 

    void Update()
    {
        // Check pause input
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }

        // 🛒 باز کردن Shop با کلید B
        if (Input.GetKeyDown(KeyCode.B))
        {
            Debug.Log("🔵 B key pressed!");
            ToggleShop();
        }
    }
    
    void ToggleShop()
    {
        Debug.Log("🔍 Looking for ShopUI...");
        
        // جستجوی دقیق
        ShopUI shopUI = FindObjectOfType<ShopUI>(true); // true = شامل غیرفعال‌ها
        
        if (shopUI != null)
        {
            Debug.Log("✅ ShopUI found! Opening...");
            shopUI.Show();
        }
        else
        {
            Debug.LogError("❌ ShopUI not found!");
            
            // جستجوی عمیق‌تر
            GameObject canvas = GameObject.Find("ShopCanvas");
            if (canvas != null)
            {
                Debug.Log("ShopCanvas found!");
                
                // فعال کردن Canvas اگه غیرفعاله
                if (!canvas.activeSelf)
                {
                    canvas.SetActive(true);
                    Debug.Log("ShopCanvas activated!");
                }
                
                ShopUI ui = canvas.GetComponentInChildren<ShopUI>(true);
                if (ui != null)
                {
                    Debug.Log("✅ Found ShopUI in children!");
                    ui.Show();
                }
                else
                {
                    Debug.LogError("❌ ShopUI component not found!");
                }
            }
            else
            {
                Debug.LogError("❌ ShopCanvas not found in scene!");
            }
        }
    }
    // ===== Game Flow =====

    void StartGame()
    {
        Debug.Log("🎮 Game Started");

        currentState = GameState.Playing;
        Time.timeScale = 1f;

        // // Check authentication
        // if (networkManager == null || !networkManager.isAuthenticated)
        // {
        //     Debug.LogError("❌ Not authenticated! Returning to main menu...");
        //     ReturnToMainMenu();
        //     return;
        // }

        // Spawn player
        SpawnPlayer();

        // Subscribe to player events
        if (localPlayer != null)
        {
            // TODO: Subscribe to player death, level up, etc
        }

        // Start enemy spawning
        lastEnemySpawnTime = Time.time;

        OnGameStarted?.Invoke();
    }
    


    public void PauseGame()
    {
        if (isPaused) return;
        
        isPaused = true;
        currentState = GameState.Paused;
        Time.timeScale = 0f;
        
        Debug.Log("⏸️ Game Paused");
        OnGamePaused?.Invoke();
    }
    
    public void ResumeGame()
    {
        if (!isPaused) return;
        
        isPaused = false;
        currentState = GameState.Playing;
        Time.timeScale = 1f;
        
        Debug.Log("▶️ Game Resumed");
        OnGameResumed?.Invoke();
    }
    
    public void TogglePause()
    {
        if (isPaused)
            ResumeGame();
        else
            PauseGame();
    }
    
    public void GameOver()
    {
        Debug.Log("💀 Game Over");
        
        currentState = GameState.GameOver;
        Time.timeScale = 0f;
        
        OnGameOver?.Invoke();
        
        // Show game over UI
        if (uiManager != null)
        {
            uiManager.ShowDeathScreen(0);
        }
        
        // Save final state
        if (networkManager != null)
        {
            networkManager.SavePlayerData();
        }
        
        // Return to menu after delay
        StartCoroutine(ReturnToMenuAfterDelay(5f));
    }
    
    IEnumerator ReturnToMenuAfterDelay(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        ReturnToMainMenu();
    }
    
    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }
    
    // ===== Player Management =====
    
    void SpawnPlayer()
    {
        // Find existing player
        GameObject existingPlayer = GameObject.FindGameObjectWithTag("Player");
        if (existingPlayer != null)
        {
            localPlayer = existingPlayer.GetComponent<PlayerController>();
            Debug.Log("✅ Found existing player");
            return;
        }
        
        // Spawn new player
        Vector3 spawnPos = Vector3.zero;
        
        if (mapManager != null)
        {
            spawnPos = mapManager.GetPlayerSpawnPosition();
        }
        else if (playerSpawnPoint != null)
        {
            spawnPos = playerSpawnPoint.position;
        }
        
        if (playerPrefab != null)
        {
            GameObject playerObj = Instantiate(playerPrefab, spawnPos, Quaternion.identity);
            localPlayer = playerObj.GetComponent<PlayerController>();
            
            Debug.Log($"✅ Player spawned at {spawnPos}");
        }
        else
        {
            Debug.LogError("❌ Player prefab not assigned!");
        }
    }
    
    public void RespawnPlayer()
    {
        if (localPlayer == null) return;
        
        Vector3 spawnPos = mapManager != null 
            ? mapManager.GetPlayerSpawnPosition() 
            : Vector3.zero;
        
        localPlayer.transform.position = spawnPos;
        
        // Reset health
        PlayerData data = networkManager.localPlayerData;
        if (data != null)
        {
            data.currentHealth = data.maxHealth;
        }
        
        Debug.Log("✅ Player respawned");
    }
    
    public void OnPlayerDeath()
    {
        Debug.Log("💀 Player died");
        
        if (networkManager == null || networkManager.localPlayerData == null)
        {
            GameOver();
            return;
        }
        
        PlayerData data = networkManager.localPlayerData;
        
        if (data.horcruxes > 0)
        {
            // Still has lives
            if (uiManager != null)
            {
                uiManager.ShowDeathScreen(data.horcruxes);
            }
            
            // Respawn after delay
            StartCoroutine(RespawnAfterDelay(3f));
        }
        else
        {
            // Game over - no lives left
            GameOver();
        }
    }
    
    IEnumerator RespawnAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        RespawnPlayer();
        
        if (uiManager != null)
        {
            uiManager.HideDeathScreen();
        }
    }
    
    // ===== Enemy Spawning =====
    
    void SpawnRandomEnemy()
    {
        if (mapManager == null) return;
        
        // Get random zone
        string[] zones = { "library", "corridor", "courtyard", "tower_stairs", "astronomy_tower" };
        string randomZone = zones[Random.Range(0, zones.Length)];
        
        // Get random house
        string[] houses = { "slytherin", "ravenclaw", "hufflepuff", "deatheater" };
        string randomHouse = houses[Random.Range(0, houses.Length)];
        
        // Spawn
        mapManager.SpawnEnemyAtZone(randomZone, randomHouse);
        
        Debug.Log($"👹 Spawned {randomHouse} enemy in {randomZone}");
    }
    
    public void OnEnemyKilled(string enemyHouse, int enemyXPLevel)
    {
        if (networkManager == null || networkManager.localPlayerData == null) return;
        
        PlayerData playerData = networkManager.localPlayerData;
        string playerHouse = playerData.house;
        
        // Calculate XP reward
        int xpReward = CalculateXPReward(enemyHouse, playerHouse, enemyXPLevel);
        
        // Award XP
        networkManager.AddXP(xpReward);
        
        // Show notification
        if (uiManager != null)
        {
            uiManager.ShowNotification($"Enemy defeated! +{xpReward} XP");
        }
        
        Debug.Log($"⚔️ Enemy killed: {enemyHouse} | XP: +{xpReward}");
    }
    
    int CalculateXPReward(string enemyHouse, string playerHouse, int enemyLevel)
    {
        int baseXP = 20;
        
        // Penalty for killing same house
        if (enemyHouse.ToLower() == playerHouse.ToLower())
        {
            baseXP = -30; // Lose XP
        }
        
        // Bonus for enemy level
        baseXP += enemyLevel * 2;
        
        return baseXP;
    }
    
    // ===== XP and Progression =====
    
    public void OnLevelUp(int newLevel)
    {
        Debug.Log($"🎉 Level Up! New level: {newLevel}");
        
        // Show UI
        if (uiManager != null)
        {
            int galleonsReward = 10;
            uiManager.ShowLevelUp(newLevel, galleonsReward);
        }
        
        // Play sound/effect
        // TODO: Add level up sound
    }
    
    // ===== Combat Events =====
    
    public void OnSpellCast(string spellName, GameObject caster)
    {
        Debug.Log($"✨ {caster.name} cast {spellName}");
        
        // Track spell usage for statistics
        // TODO: Implement spell tracking
    }
    
    public void OnDamageTaken(GameObject target, float damage, GameObject attacker)
    {
        Debug.Log($"💥 {target.name} took {damage:F1} damage from {attacker?.name ?? "unknown"}");
        
        // Show damage number
        if (uiManager != null)
        {
            uiManager.ShowDamage(target.transform.position, (int)damage, false);
        }
    }
    
    // ===== Zone Management =====
    
    public void OnPlayerEnteredZone(string zoneId)
    {
        Debug.Log($"🚪 Player entered: {zoneId}");
        
        // Update UI
        // TODO: Zone-specific logic
    }
    
    // ===== Utility =====
    
    public GameState GetCurrentState()
    {
        return currentState;
    }
    
    public bool IsGameActive()
    {
        return currentState == GameState.Playing;
    }
    
    public PlayerController GetLocalPlayer()
    {
        return localPlayer;
    }
    
    // public DamageSystem GetDamageSystem()
    // {
    //     return damageSystem;
    // }
    
    // ===== Cleanup =====
    
    void OnApplicationQuit()
    {
        // Save before quit
        if (networkManager != null && networkManager.isAuthenticated)
        {
            networkManager.SavePlayerData();
        }
    }
}

// ===== Enums =====

public enum GameState
{
    MainMenu,
    Loading,
    Playing,
    Paused,
    GameOver,
    Cutscene
}
using UnityEngine;
using System.Collections.Generic;


public class PlayerController : MonoBehaviour
{
    
    [Header("Movement")]
    public float moveSpeed = 4f;
    public float radius = 0.5f;

    [Header("Combat")]
    public int maxHealth = 100;
    public int lives = 3;

    [Header("Spells")]
    public GameObject spellPrefab;
    public Transform wandTip;

    [Header("Animation")]
    public Animator animator;
    public SpriteRenderer spriteRenderer;

    [Header("Network")]
    [SerializeField] private float positionSyncInterval = 0.5f;
    private float lastPositionSync;
    
    // وضعیت بازیکن
    private int currentHealth;
    private string currentZoneId = "great_hall";
    private Vector2 moveDirection;
    private Vector2 lastMoveDirection = Vector2.down;
    private bool isCasting = false;
    private string facing = "down";
    private CombatNetworkSync combatSync;

    // کول‌داون طلسم‌ها
    private Dictionary<KeyCode, float> lastCastTime = new Dictionary<KeyCode, float>();
    private Dictionary<KeyCode, float> spellCooldowns = new Dictionary<KeyCode, float>
    {
        { KeyCode.Q, 0.5f },
        { KeyCode.W, 0.8f },
        { KeyCode.E, 1.2f },
        { KeyCode.R, 5.0f },
        { KeyCode.T, 8.0f }
    };

public static PlayerController Instance { get; private set; } // ✅ اضافه کنید
    // مراجع
    private Rigidbody2D rb;
    private MapManager mapManager;
    private CameraFollow cameraFollow;

void Awake() // ✅ اضافه شد
{
    if (Instance == null)
    {
        Instance = this;
    }
    else
    {
        // در صورت وجود Instance، این آبجکت را نابود کن
        Destroy(gameObject);
        return;
    }
}


void Start()
{
    rb = GetComponent<Rigidbody2D>();
    
    // âœ… FIXED: Null check Ø¨Ø±Ø§ÛŒ MapManager
    mapManager = FindFirstObjectByType<MapManager>();
    if (mapManager == null)
    {
        Debug.LogError("âŒ MapManager not found! Player cannot spawn.");
        // Ø§Ø³ØªÙØ§Ø¯Ù‡ Ø§Ø² Ù…ÙˆÙ‚Ø¹ÛŒØª Ù¾ÛŒØ´â€ŒÙØ±Ø¶
        transform.position = Vector3.zero;
    }
    else
    {
        // ØªÙ†Ø¸ÛŒÙ… Ù…ÙˆÙ‚Ø¹ÛŒØª Ø§ÙˆÙ„ÛŒÙ‡
        transform.position = mapManager.GetPlayerSpawnPosition();
        Debug.Log($"âœ… Player spawned at: {transform.position}");
    }
    
    // âœ… FIXED: Null check Ø¨Ø±Ø§ÛŒ CameraFollow
    Camera mainCam = Camera.main;
    if (mainCam != null)
    {
        cameraFollow = mainCam.GetComponent<CameraFollow>();
        if (cameraFollow == null)
        {
            Debug.LogWarning("âš ï¸ CameraFollow not found on Main Camera");
        }
    }
    else
    {
        Debug.LogError("âŒ Main Camera not found!");
    }

    currentHealth = maxHealth;

    // ØªÙ†Ø¸ÛŒÙ… Tag Ùˆ Layer
    gameObject.tag = "Player";
    gameObject.layer = LayerMask.NameToLayer("Player");

    // ØªÙ†Ø¸ÛŒÙ… SpriteRenderer
    if (spriteRenderer != null)
    {
        spriteRenderer.enabled = true;
        spriteRenderer.sortingOrder = 10;
    }
    else
    {
        Debug.LogWarning("âš ï¸ SpriteRenderer not assigned!");
    }

    // ØªÙ†Ø¸ÛŒÙ… Ø§ÙˆÙ„ÛŒÙ‡ Ù¾Ø§Ø±Ø§Ù…ØªØ±Ù‡Ø§ÛŒ Ø§Ù†ÛŒÙ…ÛŒØ´Ù†
    if (animator != null)
    {
        animator.SetFloat("moveX", 0);
        animator.SetFloat("moveY", -1);
        animator.SetFloat("lastMoveX", 0);
        animator.SetFloat("lastMoveY", -1);
    }
    else
    {
        Debug.LogWarning("âš ï¸ Animator not assigned!");
    }

    // âœ… FIXED: Null check Ø¨Ø±Ø§ÛŒ CombatNetworkSync
    combatSync = CombatNetworkSync.Instance;
    if (combatSync == null)
    {
        Debug.LogWarning("âš ï¸ CombatNetworkSync not found!");
    }
}

    void Update()
    {
        if (!isCasting)
        {
            HandleMovement();
        }

        HandleSpellCasting();
        UpdateAnimation();

        // Debug: کلید P برای اطلاعات
        if (Input.GetKeyDown(KeyCode.P))
        {
            Debug.Log($"Player Position: {transform.position}");
            Debug.Log($"Camera Position: {Camera.main.transform.position}");
            Debug.Log($"Distance: {Vector3.Distance(transform.position, Camera.main.transform.position)}");
            
            if (spriteRenderer != null)
            {
                Debug.Log($"Sprite: {(spriteRenderer.sprite != null ? spriteRenderer.sprite.name : "NULL")}");
                Debug.Log($"Enabled: {spriteRenderer.enabled}");
                Debug.Log($"Sorting Layer: {spriteRenderer.sortingLayerName}");
                Debug.Log($"Order: {spriteRenderer.sortingOrder}");
            }
        }
        
        // ✅ سینک موقعیت
        if (Time.time - lastPositionSync > positionSyncInterval)
        {
            lastPositionSync = Time.time;
            SendPositionToServer();
        }
    }

    // ✅ FIXED: Position sync با endpoint درست
    void SendPositionToServer()
    {
        NetworkManager nm = NetworkManager.Instance;
        if (nm == null || !nm.isAuthenticated || !nm.isConnected)
        {
            return;
        }
        
        // ✅ استفاده از Socket.IO
        nm.SendPositionUpdate();
    }

    void FixedUpdate()
    {
        if (!isCasting && moveDirection != Vector2.zero)
        {
            Vector2 newPos = rb.position + moveDirection * moveSpeed * Time.fixedDeltaTime;
            rb.MovePosition(newPos);

            // اطمینان از Z = 0
            if (transform.position.z != 0)
            {
                Vector3 pos = transform.position;
                pos.z = 0;
                transform.position = pos;
            }
        }
    }

    void HandleMovement()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        moveDirection = new Vector2(horizontal, vertical).normalized;

        if (moveDirection.magnitude > 0.1f)
        {
            lastMoveDirection = moveDirection;

            if (Mathf.Abs(horizontal) > Mathf.Abs(vertical))
            {
                facing = horizontal > 0 ? "right" : "left";
            }
            else
            {
                facing = vertical > 0 ? "up" : "down";
            }
        }
    }

    void UpdateAnimation()
    {
        if (animator == null) return;

        bool isMoving = moveDirection.magnitude > 0.1f;

        animator.SetBool("isMoving", isMoving);
        animator.SetBool("isCasting", isCasting);

        if (isMoving)
        {
            animator.SetFloat("moveX", moveDirection.x);
            animator.SetFloat("moveY", moveDirection.y);
            animator.SetFloat("lastMoveX", moveDirection.x);
            animator.SetFloat("lastMoveY", moveDirection.y);
        }
        else
        {
            animator.SetFloat("moveX", 0);
            animator.SetFloat("moveY", 0);
            animator.SetFloat("lastMoveX", lastMoveDirection.x);
            animator.SetFloat("lastMoveY", lastMoveDirection.y);
        }

        if (isCasting)
        {
            animator.SetFloat("castX", lastMoveDirection.x);
            animator.SetFloat("castY", lastMoveDirection.y);
        }
    }

    // ✅ FIXED: LoadPlayerData - حذف خطاهای localPlayerData
    public void LoadPlayerData(PlayerData serverData)
    {
        if (serverData == null)
        {
            Debug.LogError("❌ LoadPlayerData failed: serverData is null");
            return;
        }

        // ===== FIX: Quests =====
        if (serverData.quests == null)
        {
            serverData.quests = new QuestsData
            {
                active = new List<string>(),
                completed = new List<string>()
            };
        }
        else
        {
            if (serverData.quests.active == null)
                serverData.quests.active = new List<string>();
            if (serverData.quests.completed == null)
                serverData.quests.completed = new List<string>();
        }

        // ===== FIX: Inventory =====
        if (serverData.inventory == null)
            serverData.inventory = new List<InventoryItem>();

        // ===== FIX: Unlocked Spells =====
        if (serverData.unlockedSpells == null)
            serverData.unlockedSpells = new List<string> { "Lumos", "Stupefy" };

        // ===== FIX: Stats =====
        if (serverData.stats == null)
            serverData.stats = new PlayerStats();

        // ===== FIX: Equipment =====
        if (serverData.equipment == null)
            serverData.equipment = new EquipmentData();

        // ===== FIX: Character Appearance =====
        if (serverData.characterAppearance == null)
            serverData.characterAppearance = new CharacterAppearance();

        // ===== FIX: Sorting Hat Data =====
        if (serverData.sortingHatData == null)
            serverData.sortingHatData = new SortingHatData();

        Debug.Log($"✅ Player data loaded: {serverData.username}, Quests: {serverData.quests.active.Count} active, {serverData.quests.completed.Count} completed");
    }

    void HandleSpellCasting()
    {
        if (Input.GetKeyDown(KeyCode.Q)) CastSpell(KeyCode.Q, "Lumos", Color.white, 10, 5f);
        if (Input.GetKeyDown(KeyCode.W)) CastSpell(KeyCode.W, "Stupefy", Color.red, 20, 5.6f);
        if (Input.GetKeyDown(KeyCode.E)) CastSpell(KeyCode.E, "Expelliarmus", new Color(1f, 0.4f, 0f), 25, 6.3f);
        if (Input.GetKeyDown(KeyCode.R)) CastSpell(KeyCode.R, "Avada Kedavra", Color.green, 100, 4.2f);
        if (Input.GetKeyDown(KeyCode.P)) // یا هر کلید دیگری
        {
            CastPatronus();
        }  
          }

    void CastPatronus()
    {
        GameObject patronusPrefab = Resources.Load<GameObject>("Prefabs/patronus");
        
        if (patronusPrefab == null)
        {
            Debug.LogError("❌ Patronus prefab not found! مسیر باید باشد: Assets/Resources/Prefabs/patronus.prefab");
            return;
        }

        if (lastMoveDirection == Vector2.zero)
            lastMoveDirection = Vector2.right; // پیش‌فرض

        Vector3 spawnPos = transform.position + (Vector3)(lastMoveDirection.normalized * 2f);

        GameObject patronus = Instantiate(patronusPrefab, spawnPos, Quaternion.identity);

        Debug.Log($"✨ Expecto Patronum! ({patronus.name})");

        // اگر خواستی بهش رفرنس کستر بده
        PatronusController ctrl = patronus.GetComponent<PatronusController>();
        if (ctrl != null)
        {
            // می‌تونی هر داده‌ای لازم داری منتقل کنی، مثلاً رنگ یا damage
        }
    }



    void CastSpell(KeyCode key, string spellName, Color color, int damage, float speed)
    {
        if (lastCastTime.ContainsKey(key))
        {
            float timeSince = Time.time - lastCastTime[key];
            if (timeSince < spellCooldowns[key])
            {
                Debug.Log($"⏳ {spellName} cooldown: {(spellCooldowns[key] - timeSince):F1}s");
                return;
            }
        }

        lastCastTime[key] = Time.time;

        isCasting = true;
        Invoke("EndCasting", 0.4f);

        if (spellPrefab != null)
        {
            Vector3 spawnPos = wandTip != null ? wandTip.position : transform.position;
            Vector2 direction = GetSpellDirection();

            // ✅ ساخت طلسم local
            GameObject spell = Instantiate(spellPrefab, spawnPos, Quaternion.identity);
            SpellController spellCtrl = spell.GetComponent<SpellController>();

            if (spellCtrl != null)
            {
                spellCtrl.Initialize(direction, speed, damage, color, spellName, "player");
            }
            
            // ✅ 🌐 Broadcast به سرور برای بازیکنان دیگه
            if (combatSync != null)
            {
                combatSync.BroadcastSpell(spawnPos, direction, spellName, color, damage, speed);
            }
        }

        Debug.Log($"✨ Cast: {spellName}");
    }

    void EndCasting()
    {
        isCasting = false;
    }

    Vector2 GetSpellDirection()
    {
        if (lastMoveDirection.magnitude > 0.1f)
        {
            return lastMoveDirection.normalized;
        }

        switch (facing)
        {
            case "right": return Vector2.right;
            case "left": return Vector2.left;
            case "up": return Vector2.up;
            case "down": return Vector2.down;
            default: return Vector2.down;
        }
    }

    public void TransitionToZone(string targetZoneId, string exitSide)
    {
        Zone targetZone = mapManager.GetZoneById(targetZoneId);
        if (targetZone == null)
        {
            Debug.LogError($"❌ Zone not found: {targetZoneId}");
            return;
        }

        currentZoneId = targetZoneId;

        Vector3 spawnPos = CalculateSpawnPosition(targetZone, exitSide);
        transform.position = spawnPos;

        UpdateFacingOnTransition(exitSide);

        if (cameraFollow != null)
        {
            cameraFollow.SnapToTarget();
        }

        Debug.Log($"✅ Entered: {targetZone.name}");
    }

    void UpdateFacingOnTransition(string entranceSide)
    {
        switch (entranceSide)
        {
            case "north":
                lastMoveDirection = Vector2.down;
                facing = "down";
                break;
            case "south":
                lastMoveDirection = Vector2.up;
                facing = "up";
                break;
            case "west":
                lastMoveDirection = Vector2.right;
                facing = "right";
                break;
            case "east":
                lastMoveDirection = Vector2.left;
                facing = "left";
                break;
        }

        if (animator != null)
        {
            animator.SetFloat("lastMoveX", lastMoveDirection.x);
            animator.SetFloat("lastMoveY", lastMoveDirection.y);
        }
    }

    Vector3 CalculateSpawnPosition(Zone zone, string entranceSide)
    {
        float ts = mapManager.tileWorldSize;
        ZoneBounds b = zone.bounds;
        float offset = 1.5f * ts;

        float centerX = (b.x + b.width / 2f) * ts;
        float centerY = -(b.y + b.height / 2f) * ts;

        Vector3 spawnPos;

        switch (entranceSide)
        {
            case "north":
                spawnPos = new Vector3(centerX, -(b.y + 1.5f) * ts + offset, 0);
                break;
            case "south":
                spawnPos = new Vector3(centerX, -(b.y + b.height - 1.5f) * ts - offset, 0);
                break;
            case "west":
                spawnPos = new Vector3((b.x + 1.5f) * ts + offset, centerY, 0);
                break;
            case "east":
                spawnPos = new Vector3((b.x + b.width - 1.5f) * ts - offset, centerY, 0);
                break;
            default:
                spawnPos = new Vector3(centerX, centerY, 0);
                break;
        }

        spawnPos.z = 0;
        return spawnPos;
    }


// PlayerController.cs -> متد TakeDamage(int damage)

public void TakeDamage(int damage)
{
    currentHealth -= damage;
    currentHealth = Mathf.Max(0, currentHealth);

    Debug.Log($"💔 Player took {damage} damage. Health: {currentHealth}/{maxHealth}");
    
    // --- ✅ ADDED: Network Sync for Damage ---
    NetworkManager nm = NetworkManager.Instance;
    if (nm != null && nm.isAuthenticated)
    {
        // این متد باید در NetworkManager پیاده‌سازی شود
        nm.ReportDamage(damage, currentHealth, maxHealth); 
    }
    // ----------------------------------------

    if (animator != null)
    {
        animator.SetTrigger("hit");
    }

    if (currentHealth <= 0)
    {
        Die();
    }
}

    void Die()
    {
        lives--;

        if (lives > 0)
        {
            currentHealth = maxHealth;
            transform.position = mapManager.GetPlayerSpawnPosition();

            if (animator != null)
            {
                animator.SetTrigger("respawn");
            }

            Debug.Log($"💀 Player died! Lives remaining: {lives}");
        }
        else
        {
            Debug.Log("💀 GAME OVER");
            if (animator != null)
            {
                animator.SetBool("isDead", true);
            }
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Enemy"))
        {
            TakeDamage(10);
        }
    }

    // Helper Methods
    public int GetCurrentHealth() => currentHealth;
    public int GetMaxHealth() => maxHealth;
    public int GetLives() => lives;
    public string GetCurrentZoneId() => currentZoneId;
    public Vector2 GetFacingDirection() => lastMoveDirection;
}
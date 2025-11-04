using UnityEngine;
using System;
using System.Collections;

/// <summary>
/// مدیریت سیستم XP و محاسبات پیشرفت
/// هر 20 XP = 1 قسمت، هر 5 قسمت = 1 لول
/// ✅ FIXED: جلوگیری از هنگ کردن بعد از کشتن دشمن
/// </summary>
public class XPManager : MonoBehaviour
{
    [Header("XP Settings")]
    [SerializeField] private int xpPerSegment = 20;
    [SerializeField] private int segmentsPerLevel = 5;
    [SerializeField] private bool enableXPMultiplier = true;
    
    [Header("XP Sources")]
    [SerializeField] private int enemyKillXP = 20;
    [SerializeField] private int questCompleteXP = 50;
    [SerializeField] private int secretFoundXP = 30;
    [SerializeField] private int teammateKillPenalty = -30;
    
    [Header("Multipliers")]
    [SerializeField] private float weekendMultiplier = 1.5f;
    [SerializeField] private float eventMultiplier = 2.0f;
    [SerializeField] private bool isWeekendBonus = false;
    [SerializeField] private bool isEventActive = false;
    
    // ✅ NEW: جلوگیری از هنگ
    private bool isSaving = false;
    private float lastSaveTime = 0f;
    private const float SAVE_COOLDOWN = 3f;
    
    // Events
    public event Action<int> OnXPGained;
    public event Action<int> OnXPLost;
    public event Action<int, int> OnXPChanged;
    public event Action<int> OnSegmentComplete;
    public event Action<int> OnLevelUp;
    
    // References
    private NetworkManager networkManager;
    private UIManager uiManager;
    private LevelUpManager levelUpManager;
    
    // Singleton
    public static XPManager Instance { get; private set; }
    
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
        uiManager = UIManager.Instance;
        levelUpManager = LevelUpManager.Instance;
        
        CheckWeekendBonus();
        
        Debug.Log("✅ XPManager initialized");
    }
    
    // ===== XP Award Methods =====
    
    /// <summary>
    /// ✅ FIXED: اضافه کردن XP بدون هنگ کردن
    /// </summary>
    public void AddXP(int baseAmount, string source = "")
    {
        if (networkManager == null || networkManager.localPlayerData == null)
        {
            Debug.LogWarning("⚠️ Cannot add XP - NetworkManager or PlayerData is null");
            return;
        }
        
        PlayerData data = networkManager.localPlayerData;
        int oldXP = data.xp;
        int oldLevel = data.xpLevel;
        int oldProgress = data.xpProgress;
        
        // Apply multipliers
        float multiplier = GetCurrentMultiplier();
        int finalAmount = Mathf.RoundToInt(baseAmount * multiplier);
        
        // ✅ Add XP directly
        data.AddXP(finalAmount);
        
        // ✅ Force sync back to NetworkManager
        networkManager.localPlayerData.xp = data.xp;
        networkManager.localPlayerData.xpLevel = data.xpLevel;
        networkManager.localPlayerData.xpProgress = data.xpProgress;
        
        int newXP = data.xp;
        int newLevel = data.xpLevel;
        int newProgress = data.xpProgress;
        
        // Log
        Debug.Log($"➕ XP Gained: +{finalAmount} (base: {baseAmount}, mult: {multiplier:F1}x) from {source}");
        Debug.Log($"📊 Total XP: {newXP} | Level: {newLevel} ({newProgress}/{segmentsPerLevel})");
        
        // Fire events
        OnXPGained?.Invoke(finalAmount);
        OnXPChanged?.Invoke(oldXP, newXP);
        
        // Check segment completion
        if (newProgress > oldProgress || newLevel > oldLevel)
        {
            OnSegmentComplete?.Invoke(newProgress);
        }
        
        // Check level up
        if (newLevel > oldLevel)
        {
            HandleLevelUp(oldLevel, newLevel);
        }
        
        // Show UI feedback
        ShowXPGainedUI(finalAmount, source);
        
        // ✅ FIX: ذخیره غیرهمزمان با timeout
        SaveXPAsync();
    }
    
    /// <summary>
    /// ✅ NEW: ذخیره غیرهمزمان با cooldown و timeout
    /// </summary>
    void SaveXPAsync()
    {
        // بررسی cooldown
        if (Time.time - lastSaveTime < SAVE_COOLDOWN)
        {
            float remaining = SAVE_COOLDOWN - (Time.time - lastSaveTime);
            Debug.Log($"⏳ XP save on cooldown ({remaining:F1}s remaining)");
            return;
        }

        // جلوگیری از ذخیره همزمان
        if (isSaving)
        {
            Debug.Log("⏳ XP save already in progress, skipping...");
            return;
        }

        isSaving = true;
        lastSaveTime = Time.time;

        Debug.Log("💾 Saving XP to server...");
        StartCoroutine(SaveWithTimeout());
    }

    /// <summary>
    /// ✅ NEW: Coroutine با timeout برای جلوگیری از هنگ
    /// </summary>
    IEnumerator SaveWithTimeout()
    {
        bool saveCompleted = false;
        bool saveSuccess = false;

        // فراخوانی ذخیره
        networkManager.SavePlayerData((success) =>
        {
            saveCompleted = true;
            saveSuccess = success;
        });

        // منتظر بمان حداکثر 5 ثانیه
        float timeout = 5f;
        float elapsed = 0f;

        while (!saveCompleted && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        // نتیجه
        if (saveCompleted && saveSuccess)
        {
            Debug.Log("✅ XP saved successfully!");
        }
        else if (saveCompleted)
        {
            Debug.LogWarning("⚠️ XP save failed, but game continues");
        }
        else
        {
            Debug.LogWarning("⚠️ XP save timed out after 5s - game continues");
        }

        // ✅ در هر صورت، بازی ادامه پیدا کند
        isSaving = false;
    }
    
    /// <summary>
    /// کم کردن XP (جریمه)
    /// </summary>
    public void RemoveXP(int amount, string reason = "")
    {
        if (networkManager == null || networkManager.localPlayerData == null) return;
        
        PlayerData data = networkManager.localPlayerData;
        int oldXP = data.xp;
        
        data.RemoveXP(amount);
        
        int newXP = data.xp;
        
        Debug.Log($"➖ XP Lost: -{amount} | Reason: {reason}");
        Debug.Log($"📊 Total XP: {newXP}");
        
        OnXPLost?.Invoke(amount);
        OnXPChanged?.Invoke(oldXP, newXP);
        
        // Show UI feedback
        if (uiManager != null)
        {
            uiManager.ShowNotification($"Lost {amount} XP! ({reason})");
        }
        
        SaveXPAsync();
    }
    
    // ===== Specific XP Sources =====
    
    /// <summary>
    /// XP از کشتن دشمن
    /// </summary>
    public void AwardEnemyKillXP(string enemyHouse, int enemyLevel)
    {
        if (networkManager == null || networkManager.localPlayerData == null) return;
        
        string playerHouse = networkManager.localPlayerData.house;
        
        // Check if killing teammate (same house)
        if (enemyHouse.ToLower() == playerHouse.ToLower())
        {
            RemoveXP(Mathf.Abs(teammateKillPenalty), "Teammate Kill");
            return;
        }
        
        // Calculate XP based on enemy level
        int baseXP = enemyKillXP;
        int levelBonus = enemyLevel * 2;
        int totalXP = baseXP + levelBonus;
        
        AddXP(totalXP, $"Enemy Kill ({enemyHouse})");
    }
    
    /// <summary>
    /// XP از تکمیل ماموریت
    /// </summary>
    public void AwardQuestXP(string questId, int questDifficulty)
    {
        int baseXP = questCompleteXP;
        int difficultyBonus = questDifficulty * 10;
        int totalXP = baseXP + difficultyBonus;
        
        AddXP(totalXP, $"Quest Complete: {questId}");
    }
    
    /// <summary>
    /// XP از کشف راز
    /// </summary>
    public void AwardSecretXP(string secretId)
    {
        AddXP(secretFoundXP, $"Secret Found: {secretId}");
    }
    
    /// <summary>
    /// XP از بازی‌های جانبی (کوییدیچ، شطرنج، و...)
    /// </summary>
    public void AwardMinigameXP(string minigameName, bool won)
    {
        int baseXP = won ? 30 : 10;
        AddXP(baseXP, $"{minigameName} {(won ? "Won" : "Participated")}");
    }
    
    /// <summary>
    /// XP از ساخت معجون
    /// </summary>
    public void AwardPotionCraftXP(string potionName, int potionRarity)
    {
        int baseXP = 15;
        int rarityBonus = potionRarity * 5;
        AddXP(baseXP + rarityBonus, $"Potion Crafted: {potionName}");
    }
    
    // ===== Level Up Handling =====
    
    void HandleLevelUp(int oldLevel, int newLevel)
    {
        Debug.Log($"🎉 LEVEL UP! {oldLevel} → {newLevel}");
        
        OnLevelUp?.Invoke(newLevel);
        
        // Delegate to LevelUpManager for rewards and effects
        if (levelUpManager != null)
        {
            levelUpManager.HandleLevelUp(oldLevel, newLevel);
        }
        
        // Show UI
        if (uiManager != null)
        {
            uiManager.ShowLevelUp(newLevel, 10); // 10 galleons reward
        }
    }
    
    // ===== Multipliers =====
    
    float GetCurrentMultiplier()
    {
        float multiplier = 1f;
        
        if (!enableXPMultiplier) return multiplier;
        
        if (isWeekendBonus)
        {
            multiplier *= weekendMultiplier;
        }
        
        if (isEventActive)
        {
            multiplier *= eventMultiplier;
        }
        
        return multiplier;
    }
    
    void CheckWeekendBonus()
    {
        DayOfWeek today = DateTime.Now.DayOfWeek;
        isWeekendBonus = (today == DayOfWeek.Saturday || today == DayOfWeek.Sunday);
        
        if (isWeekendBonus)
        {
            Debug.Log($"🎊 Weekend XP Bonus Active! ({weekendMultiplier}x)");
        }
    }
    
    public void SetEventMultiplier(bool active, float multiplier = 2f)
    {
        isEventActive = active;
        eventMultiplier = multiplier;
        
        if (active)
        {
            Debug.Log($"🎉 Event XP Multiplier Active! ({multiplier}x)");
            
            if (uiManager != null)
            {
                uiManager.ShowNotification($"Special Event: {multiplier}x XP!");
            }
        }
    }
    
    // ===== UI Feedback =====
    
    void ShowXPGainedUI(int amount, string source)
    {
        if (uiManager == null) return;
        
        // Show floating text
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            uiManager.ShowXPGain(player.transform.position, amount);
        }
        
        // Show notification for large amounts
        if (amount >= 50)
        {
            uiManager.ShowNotification($"+{amount} XP from {source}");
        }
    }
    
    // ===== Calculations =====
    
    /// <summary>
    /// محاسبه XP مورد نیاز برای رسیدن به لول بعدی
    /// </summary>
    public int GetXPForNextLevel(int currentLevel)
    {
        int currentSegments = currentLevel * segmentsPerLevel;
        int nextLevelSegments = (currentLevel + 1) * segmentsPerLevel;
        int segmentsNeeded = nextLevelSegments - currentSegments;
        
        return segmentsNeeded * xpPerSegment;
    }
    
    /// <summary>
    /// محاسبه کل XP مورد نیاز برای رسیدن به یک لول خاص
    /// </summary>
    public int GetTotalXPForLevel(int level)
    {
        int totalSegments = level * segmentsPerLevel;
        return totalSegments * xpPerSegment;
    }
    
    /// <summary>
    /// محاسبه درصد پیشرفت در لول فعلی
    /// </summary>
    public float GetLevelProgress()
    {
        if (networkManager == null || networkManager.localPlayerData == null) return 0f;
        
        PlayerData data = networkManager.localPlayerData;
        return (float)data.xpProgress / segmentsPerLevel;
    }
    
    /// <summary>
    /// محاسبه XP باقی‌مانده تا لول بعدی
    /// </summary>
    public int GetXPToNextLevel()
    {
        if (networkManager == null || networkManager.localPlayerData == null) return 0;
        
        PlayerData data = networkManager.localPlayerData;
        int currentLevelXP = GetTotalXPForLevel(data.xpLevel);
        int nextLevelXP = GetTotalXPForLevel(data.xpLevel + 1);
        int remaining = nextLevelXP - data.xp;
        
        return Mathf.Max(0, remaining);
    }
    
    // ===== Public Getters =====
    
    public int GetCurrentXP()
    {
        return networkManager?.localPlayerData?.xp ?? 0;
    }
    
    public int GetCurrentLevel()
    {
        return networkManager?.localPlayerData?.xpLevel ?? 0;
    }
    
    public int GetCurrentProgress()
    {
        return networkManager?.localPlayerData?.xpProgress ?? 0;
    }
    
    public bool IsWeekendBonusActive()
    {
        return isWeekendBonus;
    }
    
    public bool IsEventActive()
    {
        return isEventActive;
    }
}
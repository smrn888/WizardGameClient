using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// مدیریت لول‌آپ و پاداش‌ها
/// شامل آنلاک طلسم‌ها، افزایش آمار، و پاداش‌های ویژه
/// </summary>
public class LevelUpManager : MonoBehaviour
{
    [Header("Rewards")]
    [SerializeField] private int galleonsPerLevel = 10;
    [SerializeField] private int horcruxEveryNLevels = 10;
    [SerializeField] private float healthIncreasePerHorcrux = 50f;
    
    [Header("Stat Increases")]
    [SerializeField] private float healthIncreasePerLevel = 5f;
    [SerializeField] private float damageIncreasePerLevel = 0.02f; // 2% per level
    [SerializeField] private float defenseIncreasePerLevel = 0.02f; // 2% per level
    
    [Header("Visual Effects")]
    [SerializeField] private GameObject levelUpEffectPrefab;
    [SerializeField] private GameObject horcruxEffectPrefab;
    [SerializeField] private AudioClip levelUpSound;
    [SerializeField] private AudioClip horcruxUnlockSound;
    [SerializeField] private AudioClip spellUnlockSound;
    
    [Header("Spell Unlocks")]
    [SerializeField] private SpellUnlockData[] spellUnlocks;
    
    // Events
    public event System.Action<int> OnLevelUp;
    public event System.Action<int> OnHorcruxGained;
    public event System.Action<string> OnSpellUnlocked;
    
    // References
    private NetworkManager networkManager;
    private UIManager uiManager;
    private AudioSource audioSource;
    
    // Singleton
    public static LevelUpManager Instance { get; private set; }
    
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        audioSource = gameObject.AddComponent<AudioSource>();
    }
    
    void Start()
    {
        networkManager = NetworkManager.Instance;
        uiManager = UIManager.Instance;
        
        InitializeSpellUnlocks();
        
        Debug.Log("✅ LevelUpManager initialized");
    }
    
    void InitializeSpellUnlocks()
    {
        if (spellUnlocks == null || spellUnlocks.Length == 0)
        {
            spellUnlocks = new SpellUnlockData[]
            {
                new SpellUnlockData { level = 0, spellName = "Lumos", description = "Basic light spell" },
                new SpellUnlockData { level = 0, spellName = "Stupefy", description = "Stunning spell" },
                new SpellUnlockData { level = 3, spellName = "Expelliarmus", description = "Disarming spell" },
                new SpellUnlockData { level = 5, spellName = "Protego", description = "Shield charm" },
                new SpellUnlockData { level = 10, spellName = "Expecto Patronum", description = "Patronus charm" },
                new SpellUnlockData { level = 15, spellName = "Avada Kedavra", description = "Killing curse" },
                new SpellUnlockData { level = 20, spellName = "Imperio", description = "Imperius curse" }
            };
        }
    }
    
    // ===== Main Level Up Handler =====
    
    public void HandleLevelUp(int oldLevel, int newLevel)
    {
        Debug.Log($"🎉 Processing level up: {oldLevel} → {newLevel}");
        
        if (networkManager == null || networkManager.localPlayerData == null) return;
        
        PlayerData data = networkManager.localPlayerData;
        
        AwardGalleons(newLevel);
        
        if (newLevel % horcruxEveryNLevels == 0)
        {
            AwardHorcrux(newLevel);
        }
        
        IncreasePlayerStats(newLevel);
        UnlockSpells(newLevel);
        PlayLevelUpEffects(newLevel);
        
        OnLevelUp?.Invoke(newLevel);
        
        networkManager.SavePlayerData();
        
        Debug.Log($"✅ Level up complete! Player is now level {newLevel}");
    }
    
    // ===== Rewards =====
    
    void AwardGalleons(int level)
    {
        int amount = galleonsPerLevel;
        
        if (level % 5 == 0)
        {
            amount += 10;
        }
        if (level % 10 == 0)
        {
            amount += 25;
        }
        
        networkManager.AddGalleons(amount);
        
        Debug.Log($"💰 Awarded {amount} Galleons for reaching level {level}");
    }
    
    void AwardHorcrux(int level)
    {
        if (networkManager.localPlayerData == null) return;
        
        PlayerData data = networkManager.localPlayerData;
        
        data.horcruxes++;
        data.maxHealth += healthIncreasePerHorcrux;
        data.currentHealth = data.maxHealth;
        
        Debug.Log($"💎 Awarded Horcrux! Total: {data.horcruxes}");
        
        if (horcruxEffectPrefab != null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                GameObject effect = Instantiate(horcruxEffectPrefab, player.transform.position, Quaternion.identity);
                Destroy(effect, 3f);
            }
        }
        
        if (audioSource != null && horcruxUnlockSound != null)
        {
            audioSource.PlayOneShot(horcruxUnlockSound);
        }
        
        if (uiManager != null)
        {
            uiManager.ShowNotification($"🔮 NEW HORCRUX UNLOCKED!\n+{healthIncreasePerHorcrux} Max HP");
        }
        
        OnHorcruxGained?.Invoke(data.horcruxes);
    }
    
    // ===== Stat Increases =====
    
    void IncreasePlayerStats(int level)
    {
        if (networkManager.localPlayerData == null) return;
        
        PlayerData data = networkManager.localPlayerData;
        
        data.maxHealth += healthIncreasePerLevel;
        
        float healAmount = data.maxHealth * 0.3f;
        data.currentHealth = Mathf.Min(data.currentHealth + healAmount, data.maxHealth);
        
        Debug.Log($"📊 Stats increased: Max HP +{healthIncreasePerLevel}");
    }
    
    // ===== Spell Unlocking =====
    
    void UnlockSpells(int level)
    {
        if (networkManager.localPlayerData == null) return;
        
        PlayerData data = networkManager.localPlayerData;
        
        foreach (var unlock in spellUnlocks)
        {
            if (unlock.level == level)
            {
                if (!data.unlockedSpells.Contains(unlock.spellName))
                {
                    data.unlockedSpells.Add(unlock.spellName);
                    
                    Debug.Log($"✨ Spell unlocked: {unlock.spellName}");
                    
                    if (uiManager != null)
                    {
                        uiManager.ShowNotification($"✨ New Spell Unlocked!\n{unlock.spellName}\n{unlock.description}");
                    }
                    
                    if (audioSource != null && spellUnlockSound != null)
                    {
                        audioSource.PlayOneShot(spellUnlockSound);
                    }
                    
                    OnSpellUnlocked?.Invoke(unlock.spellName);
                }
            }
        }
    }
    
    // ===== Effects =====
    
    void PlayLevelUpEffects(int level)
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;
        
        if (levelUpEffectPrefab != null)
        {
            GameObject effect = Instantiate(levelUpEffectPrefab, player.transform.position, Quaternion.identity);
            effect.transform.SetParent(player.transform);
            Destroy(effect, 2f);
        }
        
        if (audioSource != null && levelUpSound != null)
        {
            audioSource.PlayOneShot(levelUpSound);
        }
        
        // FIXED: Camera shake بدون LeanTween
        StartCoroutine(CameraShake());
    }
    
    // FIXED: Camera shake با Coroutine
    IEnumerator CameraShake()
    {
        Camera mainCam = Camera.main;
        if (mainCam == null) yield break;
        
        Vector3 originalPos = mainCam.transform.position;
        float duration = 0.3f;
        float magnitude = 0.1f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;
            
            mainCam.transform.position = originalPos + new Vector3(x, y, 0);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        mainCam.transform.position = originalPos;
    }
    
    // ===== Level Benefits Calculation =====
    
    public float GetDamageBonusForLevel(int level)
    {
        return 1f + (level * damageIncreasePerLevel);
    }
    
    public float GetDefenseBonusForLevel(int level)
    {
        return 1f + (level * defenseIncreasePerLevel);
    }
    
    public float GetMaxHealthForLevel(int level, int horcruxCount)
    {
        float baseHealth = 100f;
        float levelBonus = level * healthIncreasePerLevel;
        float horcruxBonus = horcruxCount * healthIncreasePerHorcrux;
        
        return baseHealth + levelBonus + horcruxBonus;
    }
    
    // ===== Spell Check =====
    
    public bool IsSpellUnlocked(string spellName)
    {
        if (networkManager == null || networkManager.localPlayerData == null) return false;
        
        return networkManager.localPlayerData.unlockedSpells.Contains(spellName);
    }
    
    public List<string> GetUnlockedSpells()
    {
        if (networkManager == null || networkManager.localPlayerData == null)
            return new List<string>();
        
        return networkManager.localPlayerData.unlockedSpells;
    }
    
    public int GetSpellUnlockLevel(string spellName)
    {
        foreach (var unlock in spellUnlocks)
        {
            if (unlock.spellName == spellName)
            {
                return unlock.level;
            }
        }
        
        return -1;
    }
    
    public SpellUnlockData GetNextSpellUnlock(int currentLevel)
    {
        SpellUnlockData nextUnlock = null;
        int minLevel = int.MaxValue;
        
        foreach (var unlock in spellUnlocks)
        {
            if (unlock.level > currentLevel && unlock.level < minLevel)
            {
                minLevel = unlock.level;
                nextUnlock = unlock;
            }
        }
        
        return nextUnlock;
    }
    
    // ===== Milestone Rewards =====
    
    public void CheckMilestoneRewards(int level)
    {
        if (level == 50)
        {
            if (uiManager != null)
            {
                uiManager.ShowNotification("🎁 MILESTONE REWARD!\nReceived: Elder Wand Fragment");
            }
        }
        
        if (level == 100)
        {
            if (uiManager != null)
            {
                uiManager.ShowNotification("👑 MASTER WIZARD!\nYou have reached the pinnacle of magical power!");
            }
        }
    }
    
    // ===== Public Getters =====
    
    public int GetGalleonsPerLevel()
    {
        return galleonsPerLevel;
    }
    
    public int GetHorcruxInterval()
    {
        return horcruxEveryNLevels;
    }
    
    public float GetHealthIncreasePerLevel()
    {
        return healthIncreasePerLevel;
    }
}

// ===== Data Classes =====

[System.Serializable]
public class SpellUnlockData
{
    public int level;
    public string spellName;
    public string description;
    public Sprite icon;
}
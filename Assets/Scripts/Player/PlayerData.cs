using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// مدل داده اصلی هر بازیکن - برای ذخیره در دیتابیس و سینک با سرور
/// ✅ FIXED: Inventory structure
/// ✅ FIXED: Removed duplicate PlayerPositionUpdatePayload
/// </summary>
[Serializable]
public class PlayerData
{
    // ===== اطلاعات پایه =====
    public string playerId;
    public string username;
    public string email;
    public string house;
    
    // ===== پیشرفت بازی =====
    public int xp;
    public int xpLevel;
    public int xpProgress;
    public int galleons;
    public int horcruxes;
    public float currentHealth;
    public float maxHealth;
    
    // ===== موقعیت و وضعیت =====
    public string currentZoneId;
    public PlayerPosition position;
    
    // ===== اینونتوری و تجهیزات =====
    public List<InventoryItem> inventory;
    public EquipmentData equipment;
    
    // ===== طلسم‌های آنلاک شده =====
    public List<string> unlockedSpells;
    
    // ===== آمار و دستاوردها =====
    public PlayerStats stats;
    public List<string> achievements;
    public List<string> friends;
    
    // ===== Quest structure =====
    public QuestsData quests;
    
    // ===== تنظیمات Sorting Hat =====
    public SortingHatData sortingHatData;
    
    // ===== Character Appearance =====
    public CharacterAppearance characterAppearance;
    
    // ===== Dates =====
    public string createdAt;
    public string lastLogin;
    
    // ===== Constructor =====
    public PlayerData()
    {
        playerId = Guid.NewGuid().ToString();
        createdAt = System.DateTime.UtcNow.ToString("o");
        lastLogin = System.DateTime.UtcNow.ToString("o");
        
        xp = 0;
        xpLevel = 1;
        xpProgress = 0;
        galleons = 100;
        horcruxes = 1;
        maxHealth = 100f;
        currentHealth = 100f;
        
        currentZoneId = "great_hall";
        position = new PlayerPosition { x = 75, y = 35, z = 0 };
        
        inventory = new List<InventoryItem>();
        equipment = new EquipmentData();
        stats = new PlayerStats();
        characterAppearance = new CharacterAppearance();
        
        unlockedSpells = new List<string> { "Lumos", "Stupefy" };
        achievements = new List<string>();
        friends = new List<string>();
        
        quests = new QuestsData
        {
            active = new List<string>(),
            completed = new List<string>()
        };
        
        sortingHatData = new SortingHatData();
    }
    
    public void AddXP(int amount)
    {
        xp += amount;
        
        int totalSegments = xp / 20;
        int newLevel = totalSegments / 5;
        int newProgress = totalSegments % 5;
        
        if (newLevel > xpLevel)
        {
            OnLevelUp(newLevel);
        }
        
        xpLevel = newLevel;
        xpProgress = newProgress;
    }
    
    public void RemoveXP(int amount)
    {
        xp = Math.Max(0, xp - amount);
        RecalculateLevel();
    }
    
    private void RecalculateLevel()
    {
        int totalSegments = xp / 20;
        xpLevel = totalSegments / 5;
        xpProgress = totalSegments % 5;
    }
    
    private void OnLevelUp(int newLevel)
    {
        galleons += 10;
        
        if (newLevel % 10 == 0)
        {
            horcruxes++;
            maxHealth += 50f;
            currentHealth = maxHealth;
        }
        
        UnlockSpellsForLevel(newLevel);
    }
    
    private void UnlockSpellsForLevel(int level)
    {
        Dictionary<int, List<string>> spellUnlocks = new Dictionary<int, List<string>>
        {
            { 3, new List<string> { "Expelliarmus" } },
            { 5, new List<string> { "Protego" } },
            { 10, new List<string> { "Expecto Patronum" } },
            { 15, new List<string> { "Avada Kedavra" } },
            { 20, new List<string> { "Imperio" } }
        };
        
        if (spellUnlocks.ContainsKey(level))
        {
            foreach (string spell in spellUnlocks[level])
            {
                if (!unlockedSpells.Contains(spell))
                {
                    unlockedSpells.Add(spell);
                }
            }
        }
    }
    
    public bool TakeDamage(float damage, int attackerXPLevel)
    {
        float defenseFactor = 1f - (xpLevel - attackerXPLevel) * 0.02f;
        defenseFactor = Mathf.Clamp(defenseFactor, 0.7f, 1.3f);
        
        float actualDamage = damage * defenseFactor;
        currentHealth -= actualDamage;
        
        if (currentHealth <= 0)
        {
            return OnDeath();
        }
        
        return false;
    }
    
    private bool OnDeath()
    {
        horcruxes--;
        
        if (horcruxes > 0)
        {
            currentHealth = maxHealth * 0.3f;
            return false;
        }
        else
        {
            ResetProgress();
            return true;
        }
    }
    
    private void ResetProgress()
    {
        xp = 0;
        xpLevel = 1;
        xpProgress = 0;
        galleons = 50;
        horcruxes = 1;
        maxHealth = 100f;
        currentHealth = 100f;
        
        inventory = new List<InventoryItem>();
        equipment = new EquipmentData();
        
        unlockedSpells = new List<string> { "Lumos", "Stupefy" };
    }
}

// ===== Helper Classes =====

[Serializable]
public class PlayerPosition
{
    public float x;
    public float y; 
    public float z;
}

[Serializable]
public class InventoryItem
{
    public string itemId;
    public int quantity;
}

[Serializable]
public class EquipmentData
{
    public string wandId = null;
    public string robeId = null;
    public string broomId = null;
    public string petId = null;
}

[Serializable]
public class PlayerStats
{
    public int totalKills = 0;
    public int playerKills = 0;
    public int botKills = 0;
    public int teammateKills = 0;
    public int deaths = 0;
    public int spellsCast = 0;
    public float damageDealt = 0;
    public float damageTaken = 0;
    public int questsCompleted = 0;
}

[Serializable]
public class QuestsData
{
    public List<string> active;
    public List<string> completed;
}

[Serializable]
public class SortingHatData
{
    public bool hasBeenSorted = false;
    public List<string> answers = new List<string>();
}

[Serializable]
public class CharacterAppearance
{
    public string skinTone = "fair";
    public string hairColor = "brown";
    public string eyeColor = "brown";
    public string gender = "male";
}

[Serializable]
public class PlayerJoinData
{
    public string playerId;
    public string username;
    public string house;
    public PositionData position;
}

// ✅ این کلاس را فقط در اینجا نگه می‌داریم
// ✅ از MultiplayerManager.cs حذف شده است
[Serializable]
public class PlayerPositionUpdatePayload
{
    public string playerId; 
    public PlayerPosition position; 
}

// ⚠️ کلاس PositionData در جای دیگری تعریف شده است (حذف شد تا از تکرار جلوگیری شود)
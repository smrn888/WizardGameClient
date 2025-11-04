using UnityEngine;
using System;

/// <summary>
/// ساختارهای داده‌ای (Data Structures) برای کلیه آیتم‌ها و Spellها
/// این ساختارها برای دسترسی توسط تمام اسکریپت‌های بازی در یک فایل جداگانه قرار گرفته‌اند.
/// </summary>

// === کلاس‌های اصلی JSON ===

// کلاسی برای خواندن آرایه‌ی آیتم‌ها از ItemData.json
[System.Serializable]
public class ItemDataCollection
{
    public ItemData[] items;
}

// کلاسی برای خواندن آرایه‌ی Spellها از SpellData.json
[System.Serializable]
public class SpellDataCollection
{
    public SpellData[] spells;
}

// === کلاس داده‌های آیتم ===
[System.Serializable]
public class ItemData
{
    public string id;
    public string name;
    public string type; // wand, robe, potion, pet, special
    public int price;
    public string description;
    public int requiredLevel;
    public bool consumable;
    
    // ➕ آیکون (Sprite) آیتم. این فیلد از JSON لود نمی‌شود، بلکه توسط ItemDatabase در زمان اجرا لینک می‌شود.
    [System.NonSerialized]
    public Sprite icon; 
    
    public ItemStats stats;
    public ItemEffect effect;
}

// === زیرساختارهای آیتم ===
[System.Serializable]
public class ItemStats
{
    public float damage;
    public float defense;
    public float speed;
}

[System.Serializable]
public class ItemEffect
{
    public int healAmount;
    public float xpMultiplier;
    public int duration;
}

// === کلاس داده‌های Spell ===
[System.Serializable]
public class SpellData
{
    public string name;
    public int damage;
    public float cooldown;
    public float speed;
    public float range;
    public int manaCost;
    public int unlockLevel;
}
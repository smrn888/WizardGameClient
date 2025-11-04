using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// دیتابیس آیتم‌های بازی - نسخه نهایی و Fix شده
/// این کلاس مسئول بارگذاری داده‌های آیتم (JSON) و اسپریت‌های مرتبط است.
/// </summary>
public class ItemDatabase : MonoBehaviour
{
    [Header("JSON Files")]
    [SerializeField] private TextAsset itemDataJson;
    [SerializeField] private TextAsset spellDataJson;
    
    [Header("Auto-Load Sprites")]
    [Tooltip("خودکار بارگذاری Sprite ها از پوشه Resources/ShopItems")]
    [SerializeField] private bool autoLoadSprites = true;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;
    
    // Singleton
    public static ItemDatabase Instance { get; private set; }
    
    // دیتا (ItemDataCollection و ItemData اکنون در فایل ItemData.cs تعریف شده‌اند)
    private ItemDataCollection itemData;
    private SpellDataCollection spellData;
    
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            LogDebug("⚠️ Another ItemDatabase instance exists. Destroying this one.");
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        // از بین نرفتن شیء هنگام تغییر Scene
        DontDestroyOnLoad(gameObject);
        
        LogDebug("🔧 ItemDatabase Awake - Initializing...");
        LoadData();
    }
    
    /// <summary>
    /// بارگذاری کلیه داده‌های Item و Spell
    /// </summary>
    void LoadData()
    {
        LogDebug("▶️ LoadData: Starting item data and sprite load.");
        
        // 1. بارگذاری داده‌های JSON
        if (itemDataJson != null)
        {
            itemData = JsonUtility.FromJson<ItemDataCollection>(itemDataJson.text);
            LogDebug($"✅ Item JSON loaded. Found {itemData.items.Length} items in JSON.");
        }
        else
        {
            LogDebug("❌ ItemData JSON is null. Cannot load item data.");
            itemData = new ItemDataCollection { items = new ItemData[0] };
            return;
        }
        
        // 2. بارگذاری و لینک کردن Sprite ها
        if (autoLoadSprites)
        {
            LoadSpritesFromResources("ShopItems");
        }
        
        // 3. نمایش گزارش نهایی
        int linkedCount = itemData.items.Count(i => i.icon != null);
        LogDebug($"🏁 LoadData: Initialization complete. Total items linked with icon: {linkedCount}/{itemData.items.Length}");
        
        // 4. Load Spell Data
        if (spellDataJson != null)
        {
            spellData = JsonUtility.FromJson<SpellDataCollection>(spellDataJson.text);
        }
    }
    
    /// <summary>
    /// بارگذاری اسپریت‌ها از پوشه Resources و لینک کردن آنها به ItemData بر اساس Item ID
    /// </summary>
    void LoadSpritesFromResources(string resourcePath)
    {
        Sprite[] loadedSprites = Resources.LoadAll<Sprite>(resourcePath);
        
        if (loadedSprites.Length == 0)
        {
            LogDebug($"⚠️ No sprites found in Resources/{resourcePath}. Check folder structure.");
            return;
        }

        Dictionary<string, Sprite> spriteDictionary = new Dictionary<string, Sprite>();
        foreach (Sprite sprite in loadedSprites)
        {
            string spriteNameKey = sprite.name.ToLower();
            if (!spriteDictionary.ContainsKey(spriteNameKey))
            {
                spriteDictionary.Add(spriteNameKey, sprite);
            }
        }
        
        // لینک کردن Sprite به ItemData
        foreach (ItemData item in itemData.items)
        {
            string itemIdKey = item.id.ToLower();
            if (spriteDictionary.TryGetValue(itemIdKey, out Sprite iconSprite))
            {
                item.icon = iconSprite; // 🔗 لینک موفق
            }
            else
            {
                // این گزارش مهم است: نام آیدی آیتم (item.id) را با نام فایل اسپریت در Resources/ShopItems چک کنید.
                LogDebug($"❌ Sprite not found for item ID: {item.id} ('{resourcePath}/{item.id}'). This item won't appear in the shop.");
            }
        }
    }

    /// <summary>
    /// متد اصلی برای دریافت همه آیتم‌های فروشگاه (فیلتر شده بر اساس داشتن آیکون)
    /// </summary>
    public List<ItemData> GetAllShopItems()
    {
        if (itemData == null) 
            return new List<ItemData>();
            
        // ⚠️ مهم: فقط آیتم‌هایی که آیکون دارند را برمی‌گرداند.
        return itemData.items
            .Where(item => item.icon != null) 
            .ToList();
    }
    
    /// <summary>
    /// دریافت یک آیتم خاص بر اساس ID
    /// </summary>
    public ItemData GetItem(string itemId)
    {
        if (itemData == null) return null;
        
        return itemData.items.FirstOrDefault(item => item.id.Equals(itemId, System.StringComparison.OrdinalIgnoreCase));
    }
    
    /// <summary>
    /// دریافت Sprite یک آیتم
    /// </summary>
    public Sprite GetItemSprite(string itemId)
    {
        ItemData item = GetItem(itemId);
        return item?.icon;
    }
    
    /// <summary>
    /// دریافت آیتم‌های فروشگاه بر اساس سطح بازیکن
    /// </summary>
    public ItemData[] GetShopItems(int playerLevel)
    {
        if (itemData == null) 
            return new ItemData[0];
            
        return itemData.items
            .Where(item => item.icon != null && item.requiredLevel <= playerLevel)
            .ToArray();
    }
    
    /// <summary>
    /// دریافت آیتم‌هایی که بازیکن می‌تواند بخرد
    /// </summary>
    public ItemData[] GetAffordableItems(int playerGalleons, int playerLevel)
    {
        if (itemData == null) 
            return new ItemData[0];
            
        return itemData.items
            .Where(item => item.icon != null && 
                          item.requiredLevel <= playerLevel && 
                          item.price <= playerGalleons)
            .ToArray();
    }
    
    /// <summary>
    /// دریافت آیتم‌ها بر اساس نوع
    /// </summary>
    public ItemData[] GetItemsByType(string itemType)
    {
        if (itemData == null) 
            return new ItemData[0];
            
        return itemData.items
            .Where(item => item.icon != null && 
                          item.type.Equals(itemType, System.StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }
    
    public ItemData GetItemData(string itemId)
    {
        return GetItem(itemId);
    }
    
    void LogDebug(string msg)
    {
        if (showDebugLogs)
            Debug.Log($"[ItemDatabase] {msg}");
    }
}
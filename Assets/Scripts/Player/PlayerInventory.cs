using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// اینونتوری بازیکن
/// مدیریت آیتم‌های موجود در کیف بازیکن
/// ✅ FIXED: InventoryData structure
/// </summary>
public class PlayerInventory : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int maxSlots = 20;
    
    [Header("Equipment")]
    [SerializeField] private string equippedWandId = "basic_wand";
    [SerializeField] private string equippedRobeId = "basic_robe";
    [SerializeField] private string equippedBroomId = null;
    [SerializeField] private string equippedPetId = null;
    
    // ✅ Singleton Pattern
    public static PlayerInventory Instance { get; private set; }
    
    // ✅ استفاده از InventoryItem از PlayerData.cs
    private List<InventoryItem> items = new List<InventoryItem>();
    
    // Events
    public event System.Action OnInventoryChanged;
    public event System.Action<string> OnItemEquipped;
    public event System.Action<string, int> OnItemUsed;
    
    // References
    private NetworkManager networkManager;
    private ItemDatabase itemDatabase;
    private PlayerController playerController;
    
    void Awake()
    {
        // ✅ Singleton Setup
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        // اگر روی Player هست، DontDestroyOnLoad نزن
        // اگر مستقل هست، باید DontDestroyOnLoad بشه
        if (transform.parent == null)
        {
            DontDestroyOnLoad(gameObject);
        }
    }
    
    void Start()
    {
        networkManager = NetworkManager.Instance;
        itemDatabase = ItemDatabase.Instance;
        playerController = GetComponent<PlayerController>();
        
        // بارگذاری با تاخیر برای اطمینان از آماده بودن NetworkManager
        Invoke(nameof(LoadFromPlayerData), 0.1f);
    }
    
    void LoadFromPlayerData()
    {
        if (networkManager == null || networkManager.localPlayerData == null)
        {
            Debug.LogWarning("⚠️ NetworkManager or PlayerData not ready, retrying...");
            Invoke(nameof(LoadFromPlayerData), 0.5f);
            return;
        }
        
        PlayerData data = networkManager.localPlayerData;
        
        // ✅ FIXED: بارگذاری آیتم‌ها
        items.Clear();
        if (data.inventory != null)
        {
            foreach (var item in data.inventory)
            {
                items.Add(new InventoryItem
                {
                    itemId = item.itemId,
                    quantity = item.quantity
                });
            }
            Debug.Log($"📦 Loaded {items.Count} items from PlayerData");
        }
        
        // بارگذاری تجهیزات
        if (data.equipment != null)
        {
            equippedWandId = data.equipment.wandId;
            equippedRobeId = data.equipment.robeId;
            equippedBroomId = data.equipment.broomId;
            equippedPetId = data.equipment.petId;
        }
        
        OnInventoryChanged?.Invoke();
        Debug.Log("✅ Inventory loaded successfully");
    }
    
    // ===== Add/Remove Items =====
    
    public bool AddItem(string itemId, int quantity = 1)
    {
        if (quantity <= 0)
        {
            Debug.LogWarning("⚠️ Invalid quantity");
            return false;
        }
        
        // اگر آیتم موجود دارد، quantity را اضافه کن
        InventoryItem existingItem = items.FirstOrDefault(i => i.itemId == itemId);
        if (existingItem != null)
        {
            existingItem.quantity += quantity;
            
            // ✅ آپدیت PlayerData
            SyncToPlayerData();
            
            OnInventoryChanged?.Invoke();
            Debug.Log($"📦 Added {quantity}x {itemId} (Total: {existingItem.quantity})");
            return true;
        }
        
        // چک کردن فضای خالی برای آیتم جدید
        if (items.Count >= maxSlots)
        {
            Debug.LogWarning("⚠️ Inventory full!");
            return false;
        }
        
        // اضافه کردن آیتم جدید
        items.Add(new InventoryItem
        {
            itemId = itemId,
            quantity = quantity
        });
        
        // ✅ آپدیت PlayerData
        SyncToPlayerData();
        
        OnInventoryChanged?.Invoke();
        Debug.Log($"📦 Added {quantity}x {itemId}");
        return true;
    }
    
    public bool RemoveItem(string itemId, int quantity = 1)
    {
        if (quantity <= 0)
        {
            Debug.LogWarning("⚠️ Invalid quantity");
            return false;
        }
        
        InventoryItem item = items.FirstOrDefault(i => i.itemId == itemId);
        if (item == null)
        {
            Debug.LogWarning($"⚠️ Item not found: {itemId}");
            return false;
        }
        
        if (item.quantity < quantity)
        {
            Debug.LogWarning($"⚠️ Not enough quantity: {itemId} (Has: {item.quantity}, Need: {quantity})");
            return false;
        }
        
        item.quantity -= quantity;
        if (item.quantity <= 0)
        {
            items.Remove(item);
        }
        
        // ✅ آپدیت PlayerData
        SyncToPlayerData();
        
        OnInventoryChanged?.Invoke();
        Debug.Log($"📦 Removed {quantity}x {itemId}");
        return true;
    }
    
    // ✅ Sync Inventory به PlayerData
    void SyncToPlayerData()
    {
        if (networkManager == null || networkManager.localPlayerData == null)
        {
            Debug.LogWarning("⚠️ Cannot sync - NetworkManager not ready");
            return;
        }
        
        PlayerData data = networkManager.localPlayerData;
        
        // ✅ FIXED: پاک کردن لیست قبلی
        data.inventory.Clear();
        
        // اضافه کردن آیتم‌های جدید
        foreach (var item in items)
        {
            data.inventory.Add(new InventoryItem
            {
                itemId = item.itemId,
                quantity = item.quantity
            });
        }
        
        Debug.Log($"📄 Synced {items.Count} items to PlayerData");
    }
    
    // ===== Item Usage =====
    
    public void UseItem(string itemId)
    {
        if (itemDatabase == null)
        {
            Debug.LogError("❌ ItemDatabase not found!");
            return;
        }
        
        ItemData itemData = itemDatabase.GetItem(itemId);
        if (itemData == null)
        {
            Debug.LogError($"❌ Item not found in database: {itemId}");
            return;
        }
        
        if (!itemData.consumable)
        {
            Debug.LogWarning($"⚠️ Item not consumable: {itemId}");
            return;
        }
        
        if (!HasItem(itemId))
        {
            Debug.LogWarning($"⚠️ Item not in inventory: {itemId}");
            return;
        }
        
        // اعمال اثرات آیتم
        ApplyItemEffect(itemData);
        
        // حذف آیتم
        RemoveItem(itemId, 1);
        
        // ارسال به سرور
        if (networkManager != null)
        {
            networkManager.SavePlayerData();
        }
        
        OnItemUsed?.Invoke(itemId, 1);
    }
    
    void ApplyItemEffect(ItemData itemData)
    {
        if (itemData.effect == null)
        {
            Debug.LogWarning($"⚠️ Item has no effect: {itemData.name}");
            return;
        }
        
        // Health Potion
        if (itemData.effect.healAmount > 0)
        {
            if (playerController == null)
            {
                playerController = GetComponent<PlayerController>();
            }
            
            if (playerController != null && networkManager != null)
            {
                PlayerData data = networkManager.localPlayerData;
                float newHealth = Mathf.Min(
                    data.currentHealth + itemData.effect.healAmount,
                    data.maxHealth
                );
                
                float healed = newHealth - data.currentHealth;
                data.currentHealth = newHealth;
                
                Debug.Log($"💚 Healed {healed} HP (Now: {newHealth}/{data.maxHealth})");
                
                // ذخیره تغییرات
                networkManager.SavePlayerData();
            }
        }
        
        // XP Boost
        if (itemData.effect.xpMultiplier > 1f)
        {
            // TODO: Implement XP boost system
            Debug.Log($"⚡ XP Boost: {itemData.effect.xpMultiplier}x for {itemData.effect.duration}s");
        }
    }
    
    // ===== Equipment =====
    
    public void EquipItem(string itemId)
    {
        if (itemDatabase == null)
        {
            Debug.LogError("❌ ItemDatabase not found!");
            return;
        }
        
        ItemData itemData = itemDatabase.GetItem(itemId);
        if (itemData == null)
        {
            Debug.LogError($"❌ Item not found: {itemId}");
            return;
        }
        
        // چک کردن مالکیت آیتم (برای تجهیزات غیر پایه)
        if (!HasItem(itemId) && !IsStartingEquipment(itemId))
        {
            Debug.LogWarning($"⚠️ You don't own this item: {itemData.name}");
            return;
        }
        
        string previousEquipment = null;
        
        switch (itemData.type)
        {
            case "wand":
                previousEquipment = equippedWandId;
                equippedWandId = itemId;
                break;
            case "robe":
                previousEquipment = equippedRobeId;
                equippedRobeId = itemId;
                break;
            case "broom":
                previousEquipment = equippedBroomId;
                equippedBroomId = itemId;
                break;
            case "pet":
                previousEquipment = equippedPetId;
                equippedPetId = itemId;
                break;
            default:
                Debug.LogWarning($"⚠️ Cannot equip item type: {itemData.type}");
                return;
        }
        
        // ✅ آپدیت PlayerData
        if (networkManager != null && networkManager.localPlayerData != null)
        {
            PlayerData data = networkManager.localPlayerData;
            data.equipment.wandId = equippedWandId;
            data.equipment.robeId = equippedRobeId;
            data.equipment.broomId = equippedBroomId;
            data.equipment.petId = equippedPetId;
            
            networkManager.SavePlayerData();
        }
        
        OnItemEquipped?.Invoke(itemId);
        Debug.Log($"🎒 Equipped: {itemData.name}" + 
                  (previousEquipment != null ? $" (Replaced: {previousEquipment})" : ""));
    }
    
    bool IsStartingEquipment(string itemId)
    {
        return itemId == "basic_wand" || itemId == "basic_robe";
    }
    
    // ===== Query Methods =====
    
    public bool HasItem(string itemId)
    {
        return items.Any(i => i.itemId == itemId);
    }
    
    public int GetItemQuantity(string itemId)
    {
        InventoryItem item = items.FirstOrDefault(i => i.itemId == itemId);
        return item != null ? item.quantity : 0;
    }
    
    public List<InventoryItem> GetAllItems()
    {
        return new List<InventoryItem>(items);
    }
    
    public int GetUsedSlots()
    {
        return items.Count;
    }
    
    public int GetMaxSlots()
    {
        return maxSlots;
    }
    
    public bool IsFull()
    {
        return items.Count >= maxSlots;
    }
    
    // ===== Equipment Getters =====
    
    public string GetEquippedWand() => equippedWandId;
    public string GetEquippedRobe() => equippedRobeId;
    public string GetEquippedBroom() => equippedBroomId;
    public string GetEquippedPet() => equippedPetId;
    
    // ===== Clear =====
    
    public void ClearInventory()
    {
        items.Clear();
        SyncToPlayerData();
        OnInventoryChanged?.Invoke();
        Debug.Log("🗑️ Inventory cleared");
    }
}
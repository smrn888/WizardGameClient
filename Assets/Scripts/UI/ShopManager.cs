using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// مدیریت فروشگاه - Backend Only
/// خرید و فروش فقط به Backend ارسال میشه
/// Inventory در Scene جداگانه مدیریت میشه
/// </summary>
public class ShopManager : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float sellPriceMultiplier = 0.5f;
    
    // Singleton
    public static ShopManager Instance { get; private set; }
    
    // References
    private NetworkManager networkManager;
    private ItemDatabase itemDatabase;
    
    // Events
    public event System.Action<ItemData, int> OnItemPurchased;
    public event System.Action<ItemData, int> OnItemSold;
    public event System.Action<string> OnTransactionFailed;
    
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    
    void Start()
    {
        networkManager = NetworkManager.Instance;
        itemDatabase = ItemDatabase.Instance;
        
        if (networkManager == null)
        {
            Debug.LogWarning("⚠️ NetworkManager.Instance not found! Will retry...");
            Invoke(nameof(RetryFindNetworkManager), 0.5f);
        }
        
        if (itemDatabase == null)
        {
            Debug.LogError("❌ ItemDatabase.Instance not found!");
        }
        
        Debug.Log("✅ ShopManager initialized (Backend Only Mode)");
    }
    
    void RetryFindNetworkManager()
    {
        networkManager = NetworkManager.Instance;
        if (networkManager == null)
        {
            Debug.LogError("❌ NetworkManager still not found!");
        }
        else
        {
            Debug.Log("✅ NetworkManager found!");
        }
    }
    
    // ===== Purchase =====
    
    /// <summary>
    /// خرید آیتم - فقط به Backend ارسال میشه
    /// Inventory بعداً در Backend و سپس در Inventory Scene بروز میشه
    /// </summary>
    public void PurchaseItem(string itemId, int quantity = 1)
    {
        if (networkManager == null || !networkManager.isAuthenticated)
        {
            OnTransactionFailed?.Invoke("Not authenticated");
            return;
        }
        
        ItemData item = itemDatabase.GetItem(itemId);
        if (item == null)
        {
            OnTransactionFailed?.Invoke("Item not found");
            return;
        }
        
        PlayerData playerData = networkManager.localPlayerData;
        
        // چک سطح
        if (item.requiredLevel > playerData.xpLevel)
        {
            OnTransactionFailed?.Invoke($"Requires level {item.requiredLevel}");
            return;
        }
        
        // چک پول
        int totalCost = item.price * quantity;
        if (playerData.galleons < totalCost)
        {
            OnTransactionFailed?.Invoke("Insufficient galleons");
            return;
        }
        
        // ✅ ساخت request برای Backend
        PurchaseRequest purchaseData = new PurchaseRequest
        {
            playerId = networkManager.playerId,
            itemId = itemId,
            quantity = quantity
        };
        
        Debug.Log($"🛒 Sending purchase request to Backend:");
        Debug.Log($"   PlayerId: {purchaseData.playerId}");
        Debug.Log($"   ItemId: {purchaseData.itemId}");
        Debug.Log($"   Quantity: {purchaseData.quantity}");
        
        // ارسال به Backend
        networkManager.apiClient.Post("/api/shop/purchase", purchaseData, (success, response) =>
        {
            if (success)
            {
                Debug.Log($"✅ Purchase successful: {quantity}x {item.name}");
                
                // 💰 آپدیت پول محلی (Backend این کار رو انجام داده)
                playerData.galleons -= totalCost;
                
                // 🎉 اعلان خرید موفق
                OnItemPurchased?.Invoke(item, quantity);
                
                // 💾 ذخیره تغییرات روی سرور
                // (توجه: Inventory در Backend بروز شده، نیازی به بروزرسانی محلی نیست)
                networkManager.SavePlayerData();
                
                Debug.Log("📦 Item added to player inventory on Backend");
                Debug.Log("   Inventory will be updated when player opens Inventory Scene");
            }
            else
            {
                Debug.LogError($"❌ Purchase failed: {response}");
                OnTransactionFailed?.Invoke("Purchase failed: " + response);
            }
        }, networkManager.sessionToken);
    }
    
    // ===== Sell =====
    
    /// <summary>
    /// فروش آیتم - فقط به Backend ارسال میشه
    /// چک موجودی Inventory در Backend انجام میشه
    /// </summary>
    public void SellItem(string itemId, int quantity = 1)
    {
        if (networkManager == null || !networkManager.isAuthenticated)
        {
            OnTransactionFailed?.Invoke("Not authenticated");
            return;
        }
        
        ItemData item = itemDatabase.GetItem(itemId);
        if (item == null)
        {
            OnTransactionFailed?.Invoke("Item not found");
            return;
        }
        
        // ✅ ساخت request برای Backend
        SellRequest sellData = new SellRequest
        {
            playerId = networkManager.playerId,
            itemId = itemId,
            quantity = quantity
        };
        
        Debug.Log($"💵 Sending sell request to Backend:");
        Debug.Log($"   PlayerId: {sellData.playerId}");
        Debug.Log($"   ItemId: {sellData.itemId}");
        Debug.Log($"   Quantity: {sellData.quantity}");
        
        // ارسال به Backend
        networkManager.apiClient.Post("/api/shop/sell", sellData, (success, response) =>
        {
            if (success)
            {
                int sellPrice = Mathf.FloorToInt(item.price * sellPriceMultiplier);
                int totalEarned = sellPrice * quantity;
                
                Debug.Log($"✅ Sell successful: {quantity}x {item.name} for {totalEarned} galleons");
                
                // 💰 آپدیت پول محلی
                PlayerData playerData = networkManager.localPlayerData;
                playerData.galleons += totalEarned;
                
                // 🎉 اعلان فروش موفق
                OnItemSold?.Invoke(item, quantity);
                
                // 💾 ذخیره تغییرات روی سرور
                networkManager.SavePlayerData();
                
                Debug.Log("📦 Item removed from player inventory on Backend");
            }
            else
            {
                Debug.LogError($"❌ Sell failed: {response}");
                OnTransactionFailed?.Invoke("Sell failed: " + response);
            }
        }, networkManager.sessionToken);
    }
    
    // ===== Query Methods =====
    
    public ItemData[] GetAvailableItems()
    {
        if (networkManager == null || networkManager.localPlayerData == null)
        {
            return new ItemData[0];
        }
        
        int playerLevel = networkManager.localPlayerData.xpLevel;
        return itemDatabase.GetShopItems(playerLevel);
    }
    
    public ItemData[] GetAffordableItems()
    {
        if (networkManager == null || networkManager.localPlayerData == null)
        {
            return new ItemData[0];
        }
        
        PlayerData playerData = networkManager.localPlayerData;
        return itemDatabase.GetAffordableItems(playerData.galleons, playerData.xpLevel);
    }
    
    public ItemData[] GetItemsByType(string type)
    {
        ItemData[] allItems = GetAvailableItems();
        return allItems.Where(i => i.type == type).ToArray();
    }
    
    public int GetSellPrice(string itemId)
    {
        ItemData item = itemDatabase.GetItem(itemId);
        if (item == null) return 0;
        
        return Mathf.FloorToInt(item.price * sellPriceMultiplier);
    }
    
    public bool CanAfford(string itemId, int quantity = 1)
    {
        if (networkManager == null || networkManager.localPlayerData == null)
        {
            return false;
        }
        
        ItemData item = itemDatabase.GetItem(itemId);
        if (item == null) return false;
        
        int totalCost = item.price * quantity;
        return networkManager.localPlayerData.galleons >= totalCost;
    }
    
    public bool MeetsLevelRequirement(string itemId)
    {
        if (networkManager == null || networkManager.localPlayerData == null)
        {
            return false;
        }
        
        ItemData item = itemDatabase.GetItem(itemId);
        if (item == null) return false;
        
        return networkManager.localPlayerData.xpLevel >= item.requiredLevel;
    }
}

// ===== ✅ Serializable Request Classes =====

[System.Serializable]
public class PurchaseRequest
{
    public string playerId;
    public string itemId;
    public int quantity;
}

[System.Serializable]
public class SellRequest
{
    public string playerId;
    public string itemId;
    public int quantity;
}
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

/// <summary>
/// نمایش Shop UI با قابلیت Auto-Setup - نسخه کاملاً بازنویسی شده
/// </summary>
public class ShopUI : MonoBehaviour
{
    [Header("🔧 === AUTO-SETUP SETTINGS ===")]
    [SerializeField] private bool autoSetupUI = true;
    [SerializeField] private bool autoOpenOnStart = true;
    
    [Header("🎨 === MAIN PANEL ===")]
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button backButton;
    [SerializeField] private CanvasGroup canvasGroup;
    
    [Header("📦 === ITEMS CONTAINER ===")]
    [SerializeField] private Transform itemsParent;
    [SerializeField] private GameObject shopItemPrefab;
    
    private List<ShopItem> createdShopItems = new List<ShopItem>(); 
    
    [Header("🔘 === TAB BUTTONS ===")]
    [SerializeField] private Button allTabButton;
    [SerializeField] private Button wandsTabButton;
    [SerializeField] private Button robesTabButton;
    [SerializeField] private Button broomsTabButton;
    [SerializeField] private Button potionsTabButton;
    [SerializeField] private Button petsTabButton;
    [SerializeField] private Button specialTabButton;
    
    [Header("ℹ️ === PLAYER INFO ===")]
    [SerializeField] private TextMeshProUGUI galleonsText;
    [SerializeField] private TextMeshProUGUI levelText;
    
    [Header("💬 === CONFIRMATION DIALOG ===")]
    [SerializeField] private GameObject confirmPanel;
    [SerializeField] private TextMeshProUGUI confirmText;
    [SerializeField] private Button confirmYesButton;
    [SerializeField] private Button confirmNoButton;
    
    [Header("⚙️ === UTILS ===")]
    [SerializeField] private float fadeDuration = 0.3f;
    [SerializeField] private bool showDebugLogs = true;
    
    private ItemData currentItemToBuy;
    
    #region === UNITY LIFECYCLE ===
    
    void Start()
    {
        if (autoSetupUI)
        {
            // اگر از ShopUIBuilder استفاده شده، این بخش توسط Builder انجام شده است
            // در غیر این صورت، AutoSetupUI باید پیاده‌سازی شود
        }
        
        SetupListeners();
        
        // ✨ مهمترین بخش: ایجاد آیتم‌ها
        InstantiateShopItems();
        
        UpdatePlayerInfo(); 
        
        if (autoOpenOnStart)
        {
            ShowShop(true);
        }
        else
        {
            ShowShop(false, true); 
        }
    }
    
    #endregion

    #region === SETUP & INITIALIZATION ===

    /// <summary>
    /// بارگذاری آیتم‌ها و ساخت Prefabهای ShopItem در ItemsParent
    /// </summary>
    void InstantiateShopItems()
    {
        // پاکسازی آیتم‌های قبلی
        foreach (var item in createdShopItems)
        {
            Destroy(item.gameObject);
        }
        createdShopItems.Clear();

        if (ItemDatabase.Instance == null || itemsParent == null || shopItemPrefab == null)
        {
            Log("❌ Cannot instantiate shop items. Check ItemDatabase Instance, ItemsParent, or ShopItemPrefab reference.");
            return;
        }

        // ⚠️ مهم: دریافت آیتم‌ها از دیتابیس (فقط آیتم‌هایی که آیکون دارند)
        List<ItemData> allItems = ItemDatabase.Instance.GetAllShopItems();
        
        Log($"📦 Found {allItems.Count} shop items with valid icons to instantiate.");
        
        foreach (ItemData itemData in allItems)
        {
            // ساخت شیء آیتم
            GameObject itemObj = Instantiate(shopItemPrefab, itemsParent);
            itemObj.name = $"ShopItem_{itemData.id}";
            
            // گرفتن کامپوننت ShopItem
            ShopItem shopItem = itemObj.GetComponent<ShopItem>();
            if (shopItem != null)
            {
                // ست کردن ItemData (که icon دارد)
                shopItem.Setup(itemData, this); 
                createdShopItems.Add(shopItem);
            }
        }
        
        // نمایش تب "All" به صورت پیش‌فرض
        FilterItems("all"); 
    }

    /// <summary>
    /// تنظیم لیسنرها برای دکمه‌ها
    /// </summary>
    void SetupListeners()
    {
        closeButton?.onClick.AddListener(() => ShowShop(false));
        backButton?.onClick.AddListener(() => ShowShop(false));

        allTabButton?.onClick.AddListener(() => FilterItems("all"));
        wandsTabButton?.onClick.AddListener(() => FilterItems("wand"));
        robesTabButton?.onClick.AddListener(() => FilterItems("robe"));
        broomsTabButton?.onClick.AddListener(() => FilterItems("broom"));
        potionsTabButton?.onClick.AddListener(() => FilterItems("potion"));
        petsTabButton?.onClick.AddListener(() => FilterItems("pet"));
        specialTabButton?.onClick.AddListener(() => FilterItems("special"));
        
        confirmYesButton?.onClick.AddListener(ConfirmPurchase);
        confirmNoButton?.onClick.AddListener(HideConfirmation);
    }
    
    #endregion

    #region === SHOP LOGIC & UTILS ===

    void FilterItems(string itemType)
    {
        Log($"🏷️ Filtering items by type: {itemType}");
        
        foreach (ShopItem shopItem in createdShopItems)
        {
            // اطمینان از بررسی بر اساس نوع آیتم
            bool show = itemType.Equals("all", System.StringComparison.OrdinalIgnoreCase) || 
                        shopItem.ItemData.type.Equals(itemType, System.StringComparison.OrdinalIgnoreCase);
            
            shopItem.gameObject.SetActive(show);
        }
    }

    public void ShowConfirmation(ItemData item)
    {
        if (confirmPanel == null || confirmText == null) return;
            
        currentItemToBuy = item;
        confirmText.text = $"Are you sure you want to buy {item.name} for 💰{item.price}?";
        confirmText.color = Color.white;
        confirmPanel.SetActive(true);
    }
    
        /// <summary>
    /// Public method to show the shop (called by external scripts)
    /// </summary>
    public void Show()
    {
        ShowShop(true);
    }

    /// <summary>
    /// Public method to hide the shop (called by external scripts)
    /// </summary>
    public void Hide()
    {
        ShowShop(false);
    }
    void HideConfirmation()
    {
        if (confirmPanel != null)
            confirmPanel.SetActive(false);
        currentItemToBuy = null;
    }

    void ConfirmPurchase()
    {
        if (currentItemToBuy == null) return;
        
        // ... (منطق خرید اینجا انجام می‌شود)
        bool success = true; 
        
        ShowPurchaseResult(success, "Insufficient Galleons"); 
        
        // HideConfirmation بعد از نمایش نتیجه
        Invoke(nameof(HideConfirmation), 3f);
        currentItemToBuy = null;
    }
    
    void ShowPurchaseResult(bool success, string reason)
    {
        if (confirmPanel == null || confirmText == null) return;
        
        if (success)
        {
            confirmPanel.SetActive(true);
            confirmText.text = "✅ Purchase successful!";
            confirmText.color = Color.green;
        }
        else
        {
            confirmPanel.SetActive(true);
            confirmText.text = $"❌ Purchase failed!\n{reason}";
            confirmText.color = Color.red;
        }
        
        Invoke(nameof(ResetConfirmTextColor), 2f);
    }
    
    void ResetConfirmTextColor()
    {
        if (confirmText != null)
            confirmText.color = Color.white;
    }

    public void ShowShop(bool show, bool instant = false)
    {
        if (canvasGroup == null)
        {
            shopPanel.SetActive(show);
            return;
        }

        if (show)
        {
            shopPanel.SetActive(true);
            UpdatePlayerInfo();
            StartCoroutine(FadeCanvasGroup(canvasGroup, 0f, 1f, instant ? 0f : fadeDuration));
        }
        else
        {
            StartCoroutine(FadeCanvasGroup(canvasGroup, 1f, 0f, instant ? 0f : fadeDuration, () => shopPanel.SetActive(false)));
            HideConfirmation();
        }
    }
    
    void UpdatePlayerInfo()
    {
        if (galleonsText != null) galleonsText.text = "💰 1250"; 
        if (levelText != null) levelText.text = "⭐ Level 15";
    }

    IEnumerator FadeCanvasGroup(CanvasGroup cg, float start, float end, float duration, System.Action onComplete = null)
    {
        float startTime = Time.time;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed = Time.time - startTime;
            cg.alpha = Mathf.Lerp(start, end, elapsed / duration);
            yield return null;
        }
        cg.alpha = end;
        cg.interactable = (end == 1);
        cg.blocksRaycasts = (end == 1);
        onComplete?.Invoke();
    }
    
    void Log(string msg)
    {
        if (showDebugLogs)
            Debug.Log($"[ShopUI] {msg}");
    }
    
    #endregion
}
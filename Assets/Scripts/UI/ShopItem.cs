using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// کامپوننت ShopItem - نمایش یک آیتم در Shop
/// با قابلیت Auto-Setup برای پیدا کردن UI Elements
/// </summary>
public class ShopItem : MonoBehaviour
{
    [Header("🔧 AUTO-SETUP")]
    [SerializeField] private bool autoSetupUI = true;
    
    [Header("🖼️ UI Elements")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI priceText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private Button buyButton;
    [SerializeField] private TextMeshProUGUI buyButtonText;
    
    [Header("🎨 Visual Feedback")]
    [SerializeField] private GameObject lockedOverlay;
    [SerializeField] private Image backgroundImage;
    
    [Header("⚙️ Colors")]
    [SerializeField] private Color affordableColor = new Color(0.2f, 0.8f, 0.3f, 1f);
    [SerializeField] private Color unaffordableColor = new Color(0.8f, 0.2f, 0.2f, 1f);
    [SerializeField] private Color lockedColor = new Color(0.5f, 0.5f, 0.5f, 1f);
    [SerializeField] private Color normalBackgroundColor = new Color(0.12f, 0.12f, 0.18f, 1f);
    [SerializeField] private Color highlightBackgroundColor = new Color(0.15f, 0.15f, 0.22f, 1f);
    
    [Header("🛠 Debug")]
    [SerializeField] private bool showDebugLogs = false;
    
    // Private
    private ItemData itemData;
    private ShopUI shopUIReference;
    private Action<ItemData> onBuyCallback;
    private bool canAfford;
    private bool meetsLevel;
    
    // ✅ PUBLIC PROPERTY - Required by ShopUI.cs
    public ItemData ItemData => itemData;
    
    void Awake()
    {
        if (autoSetupUI)
        {
            AutoSetupUI();
        }
    }
    
    #region === 🔧 AUTO SETUP ===
    
    void AutoSetupUI()
    {
        LogDebug("🔍 Auto-Setup: Searching for UI elements...");
        
        // Background Image
        if (backgroundImage == null)
        {
            backgroundImage = GetComponent<Image>();
            LogFound("BackgroundImage", backgroundImage);
        }
        
        // Icon
        if (iconImage == null)
        {
            Transform icon = transform.Find("Icon");
            if (icon != null)
            {
                iconImage = icon.GetComponent<Image>();
            }
            LogFound("IconImage", iconImage);
        }
        
        // Name Text
        if (nameText == null)
        {
            nameText = FindTextComponent("NameText", "Name", "ItemName");
            LogFound("NameText", nameText);
        }
        
        // Price Text
        if (priceText == null)
        {
            priceText = FindTextComponent("PriceText", "Price");
            LogFound("PriceText", priceText);
        }
        
        // Description Text
        if (descriptionText == null)
        {
            descriptionText = FindTextComponent("DescriptionText", "Description", "Desc");
            LogFound("DescriptionText", descriptionText);
        }
        
        // Level Text
        if (levelText == null)
        {
            levelText = FindTextComponent("LevelText", "Level", "RequiredLevel");
            LogFound("LevelText", levelText);
        }
        
        // Buy Button
        if (buyButton == null)
        {
            Transform btn = transform.Find("BuyButton");
            if (btn == null) btn = transform.Find("Button");
            if (btn != null)
            {
                buyButton = btn.GetComponent<Button>();
            }
            LogFound("BuyButton", buyButton);
        }
        
        // Buy Button Text
        if (buyButtonText == null && buyButton != null)
        {
            buyButtonText = buyButton.GetComponentInChildren<TextMeshProUGUI>();
            LogFound("BuyButtonText", buyButtonText);
        }
        
        // Locked Overlay
        if (lockedOverlay == null)
        {
            Transform overlay = transform.Find("LockedOverlay");
            if (overlay == null) overlay = transform.Find("Locked");
            if (overlay != null)
            {
                lockedOverlay = overlay.gameObject;
            }
            LogFound("LockedOverlay", lockedOverlay);
        }
        
        LogDebug("✅ Auto-Setup Complete!");
    }
    
    TextMeshProUGUI FindTextComponent(params string[] names)
    {
        foreach (string name in names)
        {
            Transform found = transform.Find(name);
            if (found != null)
            {
                TextMeshProUGUI tmp = found.GetComponent<TextMeshProUGUI>();
                if (tmp != null) return tmp;
            }
        }
        return null;
    }
    
    void LogFound(string elementName, UnityEngine.Object obj)
    {
        if (showDebugLogs)
        {
            if (obj != null)
                Debug.Log($"[ShopItem] ✅ Found: {elementName}");
            else
                Debug.Log($"[ShopItem] ❌ Not Found: {elementName}");
        }
    }
    
    void LogDebug(string msg)
    {
        if (showDebugLogs)
            Debug.Log($"[ShopItem] {msg}");
    }
    
    #endregion
    
    #region === INITIALIZATION ===
    
    /// <summary>
    /// ✅ Setup method - Required by ShopUI.cs
    /// This is the main entry point called by ShopUI when creating shop items
    /// </summary>
    public void Setup(ItemData item, ShopUI shopUI)
    {
        itemData = item;
        shopUIReference = shopUI;
        
        // Check if player can afford and meets level requirement
        NetworkManager networkManager = NetworkManager.Instance;
        if (networkManager != null && networkManager.localPlayerData != null)
        {
            PlayerData playerData = networkManager.localPlayerData;
            canAfford = playerData.galleons >= item.price;
            meetsLevel = playerData.xpLevel >= item.requiredLevel;
        }
        else
        {
            // Default values if no player data
            canAfford = false;
            meetsLevel = false;
        }
        
        LogDebug($"📦 Setup ShopItem: {item.name} | Affordable: {canAfford} | Level Met: {meetsLevel}");
        
        // Setup the buy callback to show confirmation dialog
        onBuyCallback = (itemToBuy) => 
        {
            if (shopUIReference != null)
            {
                shopUIReference.ShowConfirmation(itemToBuy);
            }
        };
        
        UpdateUI(item.icon);
        SetupButton();
    }
    
    /// <summary>
    /// مقداردهی اولیه ShopItem (Alternative initialization method)
    /// </summary>
    public void Initialize(ItemData item, Sprite icon, bool affordable, bool levelMet, Action<ItemData> buyCallback)
    {
        itemData = item;
        canAfford = affordable;
        meetsLevel = levelMet;
        onBuyCallback = buyCallback;
        
        LogDebug($"📦 Initializing ShopItem: {item.name}");
        
        UpdateUI(icon);
        SetupButton();
    }
    
    void UpdateUI(Sprite icon)
    {
        // آیکون
        if (iconImage != null)
        {
            if (icon != null)
            {
                iconImage.sprite = icon;
                iconImage.color = Color.white;
            }
            else
            {
                // اگه آیکون نداشت، یه رنگ پیش‌فرض بزار
                iconImage.sprite = null;
                iconImage.color = GetDefaultColorForType(itemData.type);
            }
        }
        
        // نام
        if (nameText != null)
        {
            nameText.text = itemData.name;
        }
        
        // قیمت
        if (priceText != null)
        {
            if (itemData.price == 0)
            {
                priceText.text = "FREE";
                priceText.color = Color.green;
            }
            else
            {
                priceText.text = $"💰 {itemData.price}";
                priceText.color = canAfford ? Color.yellow : Color.red;
            }
        }
        
        // توضیحات
        if (descriptionText != null)
        {
            descriptionText.text = itemData.description;
        }
        
        // سطح مورد نیاز
        if (levelText != null)
        {
            if (itemData.requiredLevel > 0)
            {
                levelText.text = $"Requires Level {itemData.requiredLevel}";
                levelText.color = meetsLevel ? new Color(0.5f, 1f, 0.5f) : new Color(1f, 0.5f, 0.5f);
                levelText.gameObject.SetActive(true);
            }
            else
            {
                levelText.gameObject.SetActive(false);
            }
        }
        
        // Background Color
        if (backgroundImage != null)
        {
            if (!meetsLevel)
            {
                backgroundImage.color = new Color(0.15f, 0.1f, 0.1f, 1f); // قرمز تیره
            }
            else if (!canAfford)
            {
                backgroundImage.color = new Color(0.12f, 0.12f, 0.15f, 1f); // خاکستری تیره
            }
            else
            {
                backgroundImage.color = normalBackgroundColor; // عادی
            }
        }
        
        // Locked Overlay
        if (lockedOverlay != null)
        {
            lockedOverlay.SetActive(!meetsLevel);
        }
    }
    
    void SetupButton()
    {
        if (buyButton == null)
        {
            LogDebug("⚠️ Buy button is null!");
            return;
        }
        
        // حذف listener های قبلی
        buyButton.onClick.RemoveAllListeners();
        
        // وضعیت دکمه
        bool canBuy = canAfford && meetsLevel;
        buyButton.interactable = canBuy;
        
        // متن دکمه
        if (buyButtonText != null)
        {
            if (!meetsLevel)
            {
                buyButtonText.text = "🔒 LOCKED";
                buyButtonText.color = Color.gray;
            }
            else if (!canAfford)
            {
                buyButtonText.text = "💰 TOO EXPENSIVE";
                buyButtonText.color = new Color(1f, 0.5f, 0.5f);
            }
            else
            {
                buyButtonText.text = "✅ BUY";
                buyButtonText.color = Color.white;
            }
        }
        
        // رنگ دکمه
        ColorBlock colors = buyButton.colors;
        if (canBuy)
        {
            colors.normalColor = affordableColor;
            colors.highlightedColor = new Color(0.3f, 1f, 0.4f);
            colors.pressedColor = new Color(0.1f, 0.6f, 0.2f);
        }
        else
        {
            colors.normalColor = new Color(0.3f, 0.3f, 0.3f);
            colors.disabledColor = new Color(0.2f, 0.2f, 0.2f);
        }
        buyButton.colors = colors;
        
        // اضافه کردن listener
        if (canBuy)
        {
            buyButton.onClick.AddListener(OnBuyClicked);
        }
        
        LogDebug($"🔘 Button setup: canBuy={canBuy}, affordable={canAfford}, level={meetsLevel}");
    }
    
    #endregion
    
    #region === EVENTS ===
    
    void OnBuyClicked()
    {
        LogDebug($"💰 Buy button clicked for: {itemData.name}");
        
        if (itemData != null && onBuyCallback != null)
        {
            onBuyCallback.Invoke(itemData);
        }
        else
        {
            LogDebug("⚠️ Cannot buy - itemData or callback is null!");
        }
    }
    
    #endregion
    
    #region === UTILITY ===
    
    Color GetDefaultColorForType(string type)
    {
        switch (type.ToLower())
        {
            case "wand":
                return new Color(0.8f, 0.6f, 0.2f); // طلایی
            case "robe":
                return new Color(0.2f, 0.2f, 0.8f); // آبی
            case "broom":
                return new Color(0.6f, 0.4f, 0.2f); // قهوه‌ای
            case "potion":
                return new Color(0.8f, 0.2f, 0.8f); // بنفش
            case "pet":
                return new Color(0.8f, 0.5f, 0.3f); // نارنجی
            case "special":
                return new Color(1f, 0.84f, 0f); // طلایی روشن
            default:
                return Color.gray;
        }
    }
    
    /// <summary>
    /// بروزرسانی وضعیت آیتم (برای وقتی پول بازیکن تغییر می‌کنه)
    /// </summary>
    public void RefreshState(bool affordable, bool levelMet)
    {
        canAfford = affordable;
        meetsLevel = levelMet;
        
        if (itemData != null)
        {
            UpdateUI(iconImage?.sprite);
            SetupButton();
        }
    }
    
    /// <summary>
    /// تست مستقیم از Inspector
    /// </summary>
    [ContextMenu("Test - Show Item Info")]
    void TestShowInfo()
    {
        if (itemData != null)
        {
            Debug.Log($"=== ShopItem Info ===");
            Debug.Log($"Name: {itemData.name}");
            Debug.Log($"Type: {itemData.type}");
            Debug.Log($"Price: {itemData.price}");
            Debug.Log($"Level Required: {itemData.requiredLevel}");
            Debug.Log($"Can Afford: {canAfford}");
            Debug.Log($"Meets Level: {meetsLevel}");
        }
        else
        {
            Debug.LogWarning("⚠️ ItemData is null - Item not initialized yet!");
        }
    }
    
    #endregion
}
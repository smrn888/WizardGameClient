using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// فروشگاه ویژه چوب جادویی
/// UI زیباتر و جزئیات بیشتر برای wands
/// </summary>
public class WandShop : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject wandShopPanel;
    [SerializeField] private Button closeButton;
    
    [Header("Wand Display")]
    [SerializeField] private Image wandImage;
    [SerializeField] private TextMeshProUGUI wandName;
    [SerializeField] private TextMeshProUGUI wandDescription;
    [SerializeField] private TextMeshProUGUI wandStats;
    [SerializeField] private TextMeshProUGUI wandPrice;
    [SerializeField] private TextMeshProUGUI wandLevel;
    
    [Header("Selection")]
    [SerializeField] private Button previousButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button buyButton;
    [SerializeField] private Button equipButton;
    
    [Header("Player Info")]
    [SerializeField] private TextMeshProUGUI playerGalleons;
    [SerializeField] private TextMeshProUGUI currentWandText;
    
    // References
    private ItemDatabase itemDatabase;
    private ShopManager shopManager;
    private PlayerInventory playerInventory;
    private NetworkManager networkManager;
    
    // State
    private ItemData[] wands;
    private int currentWandIndex = 0;
    
    void Start()
    {
        itemDatabase = ItemDatabase.Instance;
        shopManager = ShopManager.Instance;
        playerInventory = FindObjectOfType<PlayerInventory>();
        networkManager = NetworkManager.Instance;
        
        // Setup buttons
        if (closeButton != null)
            closeButton.onClick.AddListener(Hide);
        
        if (previousButton != null)
            previousButton.onClick.AddListener(ShowPreviousWand);
        
        if (nextButton != null)
            nextButton.onClick.AddListener(ShowNextWand);
        
        if (buyButton != null)
            buyButton.onClick.AddListener(BuyCurrentWand);
        
        if (equipButton != null)
            equipButton.onClick.AddListener(EquipCurrentWand);
        
        LoadWands();
        Hide();
    }
    
    void LoadWands()
    {
        if (itemDatabase == null) return;
        
        wands = itemDatabase.GetItemsByType("wand");
        
        if (wands != null && wands.Length > 0)
        {
            currentWandIndex = 0;
            ShowCurrentWand();
        }
    }
    
    // ===== Show/Hide =====
    
    public void Show()
    {
        if (wandShopPanel != null)
        {
            wandShopPanel.SetActive(true);
        }
        
        ShowCurrentWand();
        UpdatePlayerInfo();
    }
    
    public void Hide()
    {
        if (wandShopPanel != null)
        {
            wandShopPanel.SetActive(false);
        }
    }
    
    public void Toggle()
    {
        if (wandShopPanel != null && wandShopPanel.activeSelf)
        {
            Hide();
        }
        else
        {
            Show();
        }
    }
    
    // ===== Navigation =====
    
    void ShowPreviousWand()
    {
        if (wands == null || wands.Length == 0) return;
        
        currentWandIndex--;
        if (currentWandIndex < 0)
        {
            currentWandIndex = wands.Length - 1;
        }
        
        ShowCurrentWand();
    }
    
    void ShowNextWand()
    {
        if (wands == null || wands.Length == 0) return;
        
        currentWandIndex++;
        if (currentWandIndex >= wands.Length)
        {
            currentWandIndex = 0;
        }
        
        ShowCurrentWand();
    }
    
    void ShowCurrentWand()
    {
        if (wands == null || wands.Length == 0 || currentWandIndex < 0 || currentWandIndex >= wands.Length)
        {
            return;
        }
        
        ItemData wand = wands[currentWandIndex];
        
        // Image
        if (wandImage != null)
        {
            Sprite sprite = itemDatabase.GetItemSprite(wand.id);
            wandImage.sprite = sprite;
            wandImage.enabled = sprite != null;
        }
        
        // Name
        if (wandName != null)
        {
            wandName.text = wand.name;
        }
        
        // Description
        if (wandDescription != null)
        {
            wandDescription.text = wand.description;
        }
        
        // Stats
        if (wandStats != null && wand.stats != null)
        {
            string stats = $"⚔️ Damage Bonus: +{(wand.stats.damage - 1f) * 100:F0}%";
            wandStats.text = stats;
        }
        
        // Price
        if (wandPrice != null)
        {
            wandPrice.text = $"💰 {wand.price} Galleons";
        }
        
        // Level requirement
        if (wandLevel != null)
        {
            if (wand.requiredLevel > 0)
            {
                wandLevel.text = $"Requires Level {wand.requiredLevel}";
                wandLevel.gameObject.SetActive(true);
                
                bool meetsLevel = networkManager != null && 
                                 networkManager.localPlayerData != null &&
                                 networkManager.localPlayerData.xpLevel >= wand.requiredLevel;
                
                wandLevel.color = meetsLevel ? Color.green : Color.red;
            }
            else
            {
                wandLevel.gameObject.SetActive(false);
            }
        }
        
        // Update buttons
        UpdateButtons(wand);
    }
    
    void UpdateButtons(ItemData wand)
    {
        bool hasItem = playerInventory != null && playerInventory.HasItem(wand.id);
        bool canAfford = shopManager != null && shopManager.CanAfford(wand.id);
        bool meetsLevel = shopManager != null && shopManager.MeetsLevelRequirement(wand.id);
        
        // Buy button
        if (buyButton != null)
        {
            buyButton.gameObject.SetActive(!hasItem);
            buyButton.interactable = canAfford && meetsLevel;
        }
        
        // Equip button
        if (equipButton != null)
        {
            equipButton.gameObject.SetActive(hasItem);
            
            string currentWand = playerInventory != null ? playerInventory.GetEquippedWand() : null;
            bool isEquipped = currentWand == wand.id;
            
            equipButton.interactable = !isEquipped;
            
            TextMeshProUGUI buttonText = equipButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = isEquipped ? "✓ Equipped" : "Equip";
            }
        }
    }
    
    void UpdatePlayerInfo()
    {
        if (networkManager == null || networkManager.localPlayerData == null) return;
        
        PlayerData data = networkManager.localPlayerData;
        
        // Galleons
        if (playerGalleons != null)
        {
            playerGalleons.text = $"💰 {data.galleons}";
        }
        
        // Current wand
        if (currentWandText != null && playerInventory != null)
        {
            string currentWandId = playerInventory.GetEquippedWand();
            ItemData currentWandData = itemDatabase.GetItem(currentWandId);
            
            if (currentWandData != null)
            {
                currentWandText.text = $"Current: {currentWandData.name}";
            }
        }
    }
    
    // ===== Actions =====
    
    void BuyCurrentWand()
    {
        if (wands == null || currentWandIndex < 0 || currentWandIndex >= wands.Length)
        {
            return;
        }
        
        ItemData wand = wands[currentWandIndex];
        
        if (shopManager != null)
        {
            shopManager.PurchaseItem(wand.id, 1);
            
            // Refresh after purchase
            Invoke(nameof(ShowCurrentWand), 0.5f);
            Invoke(nameof(UpdatePlayerInfo), 0.5f);
        }
    }
    
    void EquipCurrentWand()
    {
        if (wands == null || currentWandIndex < 0 || currentWandIndex >= wands.Length)
        {
            return;
        }
        
        ItemData wand = wands[currentWandIndex];
        
        if (playerInventory != null)
        {
            playerInventory.EquipItem(wand.id);
            
            // Refresh UI
            ShowCurrentWand();
            UpdatePlayerInfo();
        }
    }
}
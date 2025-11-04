using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// رابط کاربری اینونتوری
/// نمایش و مدیریت آیتم‌های بازیکن
/// ✅ Tab برای باز/بسته کردن
/// ✅ بازی پشت‌زمینه متوقف نمی‌شود
/// </summary>
public class InventoryUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private Transform slotsParent;
    [SerializeField] private GameObject slotPrefab;
    [SerializeField] private Button closeButton;
    
    [Header("Details Panel")]
    [SerializeField] private GameObject detailsPanel;
    [SerializeField] private Image detailIcon;
    [SerializeField] private TextMeshProUGUI detailName;
    [SerializeField] private TextMeshProUGUI detailDescription;
    [SerializeField] private TextMeshProUGUI detailStats;
    [SerializeField] private Button useButton;
    [SerializeField] private Button equipButton;
    [SerializeField] private Button sellButton;
    
    [Header("Info")]
    [SerializeField] private TextMeshProUGUI slotsText;
    [SerializeField] private TextMeshProUGUI galleonsText;
    
    [Header("Tab Settings")]
    [SerializeField] private KeyCode toggleKey = KeyCode.I;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float fadeDuration = 0.2f;
    
    // References
    private PlayerInventory playerInventory;
    private ItemDatabase itemDatabase;
    private ShopManager shopManager;
    private NetworkManager networkManager;
    
    // State
    private List<InventorySlot> slots = new List<InventorySlot>();
    private ItemData selectedItem;
    private string selectedItemId;
    private bool isOpen = false;
    private Coroutine fadeCoroutine;
    
    void Start()
    {
        playerInventory = FindObjectOfType<PlayerInventory>();
        itemDatabase = ItemDatabase.Instance;
        shopManager = ShopManager.Instance;
        networkManager = NetworkManager.Instance;
        
        // ✅ اگر CanvasGroup نباشد، خودکار اضافه کن
        if (canvasGroup == null)
        {
            canvasGroup = inventoryPanel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = inventoryPanel.AddComponent<CanvasGroup>();
            }
        }
        
        // Setup UI
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(Close);
        }
        
        if (useButton != null)
        {
            useButton.onClick.AddListener(OnUseButtonClicked);
        }
        
        if (equipButton != null)
        {
            equipButton.onClick.AddListener(OnEquipButtonClicked);
        }
        
        if (sellButton != null)
        {
            sellButton.onClick.AddListener(OnSellButtonClicked);
        }
        
        // Subscribe to events
        if (playerInventory != null)
        {
            playerInventory.OnInventoryChanged += RefreshUI;
        }
        
        // Create slots
        CreateSlots();
        
        // Hide by default
        Close();
    }
    
    void Update()
    {
        // ✅ Tab برای باز/بسته کردن
        if (Input.GetKeyDown(toggleKey))
        {
            Toggle();
        }
    }
    
    void CreateSlots()
    {
        if (slotPrefab == null || slotsParent == null) return;
        
        int maxSlots = playerInventory != null ? playerInventory.GetMaxSlots() : 20;
        
        for (int i = 0; i < maxSlots; i++)
        {
            GameObject slotObj = Instantiate(slotPrefab, slotsParent);
            InventorySlot slot = slotObj.GetComponent<InventorySlot>();
            
            if (slot != null)
            {
                slot.Initialize(i, this);
                slots.Add(slot);
            }
        }
    }
    
    // ===== Show/Hide with Animation =====
    
    public void Open()
    {
        if (isOpen) return;
        
        isOpen = true;
        
        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(true);
        }
        
        // ✅ Fade In Animation
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeIn());
        
        RefreshUI();
        
        Debug.Log("📦 Inventory Opened");
    }
    
    public void Close()
    {
        if (!isOpen) return;
        
        isOpen = false;
        HideDetails();
        
        // ✅ Fade Out Animation
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeOut());
        
        Debug.Log("📦 Inventory Closed");
    }
    
    public void Toggle()
    {
        if (isOpen)
        {
            Close();
        }
        else
        {
            Open();
        }
    }
    
    System.Collections.IEnumerator FadeIn()
    {
        float elapsed = 0f;
        
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Clamp01(elapsed / fadeDuration);
                yield return null;
            }
            
            canvasGroup.alpha = 1f;
        }
    }
    
    System.Collections.IEnumerator FadeOut()
    {
        float elapsed = 0f;
        
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / fadeDuration);
                yield return null;
            }
            
            canvasGroup.alpha = 0f;
            
            if (inventoryPanel != null)
            {
                inventoryPanel.SetActive(false);
            }
        }
    }
    
    // ===== Refresh UI =====
    
    void RefreshUI()
    {
        if (playerInventory == null) return;
        
        // ✅ FIXED: استفاده از InventoryItem به جای PlayerInventorySlot
        List<InventoryItem> items = playerInventory.GetAllItems();
        
        // Clear all slots
        foreach (var slot in slots)
        {
            slot.Clear();
        }
        
        // Fill slots with items
        for (int i = 0; i < items.Count && i < slots.Count; i++)
        {
            InventoryItem item = items[i];
            ItemData itemData = itemDatabase.GetItem(item.itemId);
            
            if (itemData != null)
            {
                Sprite icon = itemDatabase.GetItemSprite(item.itemId);
                slots[i].SetItem(item.itemId, itemData.name, icon, item.quantity);
            }
        }
        
        // Update info
        UpdateInfo();
    }
    
    void UpdateInfo()
    {
        if (playerInventory != null && slotsText != null)
        {
            int used = playerInventory.GetUsedSlots();
            int max = playerInventory.GetMaxSlots();
            slotsText.text = $"Slots: {used}/{max}";
        }
        
        if (networkManager != null && networkManager.localPlayerData != null && galleonsText != null)
        {
            int galleons = networkManager.localPlayerData.galleons;
            galleonsText.text = $"💰 {galleons}";
        }
    }
    
    // ===== Item Selection =====
    
    public void OnSlotClicked(string itemId)
    {
        if (string.IsNullOrEmpty(itemId))
        {
            HideDetails();
            return;
        }
        
        selectedItemId = itemId;
        selectedItem = itemDatabase.GetItem(itemId);
        
        if (selectedItem != null)
        {
            ShowDetails(selectedItem);
        }
    }
    
    void ShowDetails(ItemData item)
    {
        if (detailsPanel == null) return;
        
        detailsPanel.SetActive(true);
        
        // Icon
        if (detailIcon != null)
        {
            Sprite icon = itemDatabase.GetItemSprite(item.id);
            detailIcon.sprite = icon;
            detailIcon.enabled = icon != null;
        }
        
        // Name
        if (detailName != null)
        {
            detailName.text = item.name;
        }
        
        // Description
        if (detailDescription != null)
        {
            detailDescription.text = item.description;
        }
        
        // Stats
        if (detailStats != null)
        {
            string stats = "";
            
            if (item.stats != null)
            {
                if (item.stats.damage > 0)
                    stats += $"⚔️ Damage: +{item.stats.damage * 100}%\n";
                if (item.stats.defense > 0)
                    stats += $"🛡️ Defense: +{item.stats.defense * 100}%\n";
                if (item.stats.speed > 0)
                    stats += $"⚡ Speed: +{item.stats.speed * 100}%\n";
            }
            
            if (item.effect != null)
            {
                if (item.effect.healAmount > 0)
                    stats += $"💚 Heal: {item.effect.healAmount} HP\n";
                if (item.effect.xpMultiplier > 1)
                    stats += $"⭐ XP Boost: {item.effect.xpMultiplier}x\n";
            }
            
            stats += $"\n💰 Sell Price: {shopManager.GetSellPrice(item.id)}";
            
            detailStats.text = stats;
        }
        
        // Buttons
        if (useButton != null)
        {
            useButton.gameObject.SetActive(item.consumable);
        }
        
        if (equipButton != null)
        {
            bool isEquippable = item.type == "wand" || item.type == "robe" || 
                               item.type == "broom" || item.type == "pet";
            equipButton.gameObject.SetActive(isEquippable);
        }
        
        if (sellButton != null)
        {
            sellButton.gameObject.SetActive(true);
        }
    }
    
    void HideDetails()
    {
        if (detailsPanel != null)
        {
            detailsPanel.SetActive(false);
        }
        
        selectedItem = null;
        selectedItemId = null;
    }
    
    // ===== Button Handlers =====
    
    void OnUseButtonClicked()
    {
        if (string.IsNullOrEmpty(selectedItemId)) return;
        
        if (playerInventory != null)
        {
            playerInventory.UseItem(selectedItemId);
            HideDetails();
        }
    }
    
    void OnEquipButtonClicked()
    {
        if (string.IsNullOrEmpty(selectedItemId)) return;
        
        if (playerInventory != null)
        {
            playerInventory.EquipItem(selectedItemId);
            HideDetails();
        }
    }
    
    void OnSellButtonClicked()
    {
        if (string.IsNullOrEmpty(selectedItemId)) return;
        
        if (shopManager != null)
        {
            shopManager.SellItem(selectedItemId, 1);
            HideDetails();
        }
    }
    
    void OnDestroy()
    {
        if (playerInventory != null)
        {
            playerInventory.OnInventoryChanged -= RefreshUI;
        }
    }
}

// ===== Inventory Slot Component =====

public class InventorySlot : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI quantityText;
    [SerializeField] private GameObject selectedBorder;
    [SerializeField] private Button button;
    
    private int slotIndex;
    private string itemId;
    private InventoryUI inventoryUI;
    
    public void Initialize(int index, InventoryUI ui)
    {
        slotIndex = index;
        inventoryUI = ui;
        
        if (button != null)
        {
            button.onClick.AddListener(OnClicked);
        }
        
        Clear();
    }
    
    public void SetItem(string id, string itemName, Sprite icon, int quantity)
    {
        itemId = id;
        
        if (iconImage != null)
        {
            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
        }
        
        if (quantityText != null)
        {
            quantityText.text = quantity > 1 ? quantity.ToString() : "";
        }
        
        if (button != null)
        {
            button.interactable = true;
        }
    }
    
    public void Clear()
    {
        itemId = null;
        
        if (iconImage != null)
        {
            iconImage.sprite = null;
            iconImage.enabled = false;
        }
        
        if (quantityText != null)
        {
            quantityText.text = "";
        }
        
        if (button != null)
        {
            button.interactable = false;
        }
        
        if (selectedBorder != null)
        {
            selectedBorder.SetActive(false);
        }
    }
    
    void OnClicked()
    {
        if (inventoryUI != null)
        {
            inventoryUI.OnSlotClicked(itemId);
        }
    }
}
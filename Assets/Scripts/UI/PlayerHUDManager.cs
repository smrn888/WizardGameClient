using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// مدیریت HUD بازیکن - ساخت خودکار UI در Editor
/// نمایش: Health, XP, Level, Galleons, House
/// ✅ AUTO-SETUP: خودش در Editor همه چیز رو می‌سازه
/// </summary>
public class PlayerHUDManager : MonoBehaviour
{
    [Header("Auto-Created UI Elements")]
    [SerializeField] private GameObject hudPanel;
    [SerializeField] private Image healthFillImage;
    [SerializeField] private Image xpFillImage;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private TextMeshProUGUI xpText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI galleonsText;
    [SerializeField] private TextMeshProUGUI houseText;
    [SerializeField] private Image houseBadge;
    
    [Header("House Colors")]
    [SerializeField] private Color gryffindorColor = new Color(0.7f, 0.1f, 0.1f);
    [SerializeField] private Color slytherinColor = new Color(0.1f, 0.6f, 0.2f);
    [SerializeField] private Color ravenclawColor = new Color(0.1f, 0.3f, 0.7f);
    [SerializeField] private Color hufflepuffColor = new Color(0.9f, 0.8f, 0.2f);
    
    [Header("Settings")]
    [SerializeField] private bool smoothTransitions = true;
    [SerializeField] private float transitionSpeed = 5f;
    [SerializeField] private bool showDamageFlash = true;
    [SerializeField] private float updateInterval = 0.5f;
    
    // References
    private NetworkManager networkManager;
    private Canvas mainCanvas;
    
    // Animation
    private float currentHealthFill = 1f;
    private float currentXPFill = 0f;
    private float lastUpdateTime;
    
    // ================== SETUP ==================
    
    void Awake()
    {
        // 🔧 اگر UI وجود نداره، بساز
        if (hudPanel == null)
        {
            Debug.Log("🔨 HUD Panel not found. Creating new UI...");
            CreateHUDFromScratch();
        }
    }
    
    void Start()
    {
        networkManager = NetworkManager.Instance;
        
        if (networkManager == null)
        {
            Debug.LogError("❌ NetworkManager not found!");
            enabled = false;
            return;
        }
        
        // Subscribe to player data updates
        networkManager.OnPlayerDataUpdated += UpdateHUD;
        
        // Initial update
        UpdateHUD(networkManager.localPlayerData);
        
        Debug.Log("✅ PlayerHUDManager initialized");
    }
    
    void Update()
    {
        // Smooth transitions
        if (smoothTransitions)
        {
            AnimateUI();
        }
        
        // Periodic updates
        if (Time.time - lastUpdateTime > updateInterval)
        {
            lastUpdateTime = Time.time;
            if (networkManager != null && networkManager.localPlayerData != null)
            {
                UpdateHUD(networkManager.localPlayerData);
            }
        }
    }
    
    // ================== CREATE UI FROM SCRATCH ==================
    
    [ContextMenu("🔨 Create HUD UI")]
    void CreateHUDFromScratch()
    {
        // 1️⃣ Find or Create Canvas
        mainCanvas = FindObjectOfType<Canvas>();
        if (mainCanvas == null)
        {
            GameObject canvasObj = new GameObject("MainCanvas");
            mainCanvas = canvasObj.AddComponent<Canvas>();
            mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            
            canvasObj.AddComponent<GraphicRaycaster>();
            
            Debug.Log("✅ Canvas created");
        }
        
        // 2️⃣ Create HUD Panel (Top Left)
        hudPanel = CreatePanel("PlayerHUD", mainCanvas.transform, new Vector2(400, 200));
        RectTransform hudRect = hudPanel.GetComponent<RectTransform>();
        hudRect.anchorMin = new Vector2(0, 1);
        hudRect.anchorMax = new Vector2(0, 1);
        hudRect.pivot = new Vector2(0, 1);
        hudRect.anchoredPosition = new Vector2(20, -20);
        
        Image hudBg = hudPanel.GetComponent<Image>();
        hudBg.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
        
        // 3️⃣ Create Health Bar
        CreateHealthBar(hudPanel.transform);
        
        // 4️⃣ Create XP Bar
        CreateXPBar(hudPanel.transform);
        
        // 5️⃣ Create Galleons Display
        CreateGalleonsDisplay(hudPanel.transform);
        
        // 6️⃣ Create House Badge
        CreateHouseBadge(hudPanel.transform);
        
        Debug.Log("✅ Player HUD created successfully!");
        
        #if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(gameObject);
        #endif
    }
    
    void CreateHealthBar(Transform parent)
    {
        // Container
        GameObject container = new GameObject("HealthBar");
        container.transform.SetParent(parent, false);
        RectTransform rect = container.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(1, 1);
        rect.pivot = new Vector2(0.5f, 1);
        rect.anchoredPosition = new Vector2(0, -20);
        rect.sizeDelta = new Vector2(-40, 30);
        
        // Background
        GameObject bg = new GameObject("Background");
        bg.transform.SetParent(container.transform, false);
        RectTransform bgRect = bg.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        Image bgImage = bg.AddComponent<Image>();
        bgImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        
        // Fill
        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(container.transform, false);
        RectTransform fillRect = fill.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = new Vector2(1, 1);
        fillRect.pivot = new Vector2(0, 0.5f);
        fillRect.sizeDelta = Vector2.zero;
        healthFillImage = fill.AddComponent<Image>();
        healthFillImage.color = new Color(0.8f, 0.2f, 0.2f, 1f);
        healthFillImage.type = Image.Type.Filled;
        healthFillImage.fillMethod = Image.FillMethod.Horizontal;
        healthFillImage.fillAmount = 1f;
        
        // Text
        GameObject text = new GameObject("HealthText");
        text.transform.SetParent(container.transform, false);
        RectTransform textRect = text.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        healthText = text.AddComponent<TextMeshProUGUI>();
        healthText.text = "100 / 100";
        healthText.fontSize = 18;
        healthText.alignment = TextAlignmentOptions.Center;
        healthText.color = Color.white;
        healthText.fontStyle = FontStyles.Bold;
        
        // Icon
        GameObject icon = new GameObject("Icon");
        icon.transform.SetParent(container.transform, false);
        RectTransform iconRect = icon.AddComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0, 0.5f);
        iconRect.anchorMax = new Vector2(0, 0.5f);
        iconRect.pivot = new Vector2(0, 0.5f);
        iconRect.anchoredPosition = new Vector2(5, 0);
        iconRect.sizeDelta = new Vector2(20, 20);
        TextMeshProUGUI iconText = icon.AddComponent<TextMeshProUGUI>();
        iconText.text = "❤️";
        iconText.fontSize = 16;
        iconText.alignment = TextAlignmentOptions.Left;
    }
    
    void CreateXPBar(Transform parent)
    {
        // Container
        GameObject container = new GameObject("XPBar");
        container.transform.SetParent(parent, false);
        RectTransform rect = container.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(1, 1);
        rect.pivot = new Vector2(0.5f, 1);
        rect.anchoredPosition = new Vector2(0, -60);
        rect.sizeDelta = new Vector2(-40, 30);
        
        // Background
        GameObject bg = new GameObject("Background");
        bg.transform.SetParent(container.transform, false);
        RectTransform bgRect = bg.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        Image bgImage = bg.AddComponent<Image>();
        bgImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        
        // Fill
        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(container.transform, false);
        RectTransform fillRect = fill.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = new Vector2(1, 1);
        fillRect.pivot = new Vector2(0, 0.5f);
        fillRect.sizeDelta = Vector2.zero;
        xpFillImage = fill.AddComponent<Image>();
        xpFillImage.color = new Color(0.3f, 0.7f, 1f, 1f);
        xpFillImage.type = Image.Type.Filled;
        xpFillImage.fillMethod = Image.FillMethod.Horizontal;
        xpFillImage.fillAmount = 0f;
        
        // XP Text
        GameObject xpTextObj = new GameObject("XPText");
        xpTextObj.transform.SetParent(container.transform, false);
        RectTransform xpTextRect = xpTextObj.AddComponent<RectTransform>();
        xpTextRect.anchorMin = new Vector2(0.3f, 0);
        xpTextRect.anchorMax = Vector2.one;
        xpTextRect.sizeDelta = Vector2.zero;
        xpText = xpTextObj.AddComponent<TextMeshProUGUI>();
        xpText.text = "0 / 100 XP";
        xpText.fontSize = 16;
        xpText.alignment = TextAlignmentOptions.Center;
        xpText.color = Color.white;
        
        // Level Text
        GameObject levelTextObj = new GameObject("LevelText");
        levelTextObj.transform.SetParent(container.transform, false);
        RectTransform levelTextRect = levelTextObj.AddComponent<RectTransform>();
        levelTextRect.anchorMin = Vector2.zero;
        levelTextRect.anchorMax = new Vector2(0.3f, 1);
        levelTextRect.sizeDelta = Vector2.zero;
        levelText = levelTextObj.AddComponent<TextMeshProUGUI>();
        levelText.text = "Lvl 1";
        levelText.fontSize = 16;
        levelText.alignment = TextAlignmentOptions.Center;
        levelText.color = new Color(1f, 0.9f, 0.3f);
        levelText.fontStyle = FontStyles.Bold;
        
        // Icon
        GameObject icon = new GameObject("Icon");
        icon.transform.SetParent(container.transform, false);
        RectTransform iconRect = icon.AddComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0, 0.5f);
        iconRect.anchorMax = new Vector2(0, 0.5f);
        iconRect.pivot = new Vector2(0, 0.5f);
        iconRect.anchoredPosition = new Vector2(5, 0);
        iconRect.sizeDelta = new Vector2(20, 20);
        TextMeshProUGUI iconText = icon.AddComponent<TextMeshProUGUI>();
        iconText.text = "⭐";
        iconText.fontSize = 16;
        iconText.alignment = TextAlignmentOptions.Left;
    }
    
    void CreateGalleonsDisplay(Transform parent)
    {
        GameObject container = new GameObject("GalleonsDisplay");
        container.transform.SetParent(parent, false);
        RectTransform rect = container.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(1, 1);
        rect.pivot = new Vector2(0.5f, 1);
        rect.anchoredPosition = new Vector2(0, -100);
        rect.sizeDelta = new Vector2(-40, 30);
        
        // Background
        GameObject bg = new GameObject("Background");
        bg.transform.SetParent(container.transform, false);
        RectTransform bgRect = bg.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        Image bgImage = bg.AddComponent<Image>();
        bgImage.color = new Color(0.3f, 0.25f, 0.1f, 0.8f);
        
        // Text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(container.transform, false);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        galleonsText = textObj.AddComponent<TextMeshProUGUI>();
        galleonsText.text = "100 🪙";
        galleonsText.fontSize = 20;
        galleonsText.alignment = TextAlignmentOptions.Center;
        galleonsText.color = new Color(1f, 0.9f, 0.3f);
        galleonsText.fontStyle = FontStyles.Bold;
    }
    
    void CreateHouseBadge(Transform parent)
    {
        GameObject container = new GameObject("HouseBadge");
        container.transform.SetParent(parent, false);
        RectTransform rect = container.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(1, 1);
        rect.pivot = new Vector2(0.5f, 1);
        rect.anchoredPosition = new Vector2(0, -140);
        rect.sizeDelta = new Vector2(-40, 30);
        
        // Badge Image
        GameObject badgeObj = new GameObject("Badge");
        badgeObj.transform.SetParent(container.transform, false);
        RectTransform badgeRect = badgeObj.AddComponent<RectTransform>();
        badgeRect.anchorMin = new Vector2(0, 0.5f);
        badgeRect.anchorMax = new Vector2(0, 0.5f);
        badgeRect.pivot = new Vector2(0, 0.5f);
        badgeRect.anchoredPosition = new Vector2(10, 0);
        badgeRect.sizeDelta = new Vector2(25, 25);
        houseBadge = badgeObj.AddComponent<Image>();
        houseBadge.color = Color.red;
        
        // House Name
        GameObject textObj = new GameObject("HouseName");
        textObj.transform.SetParent(container.transform, false);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0, 0);
        textRect.anchorMax = new Vector2(1, 1);
        textRect.sizeDelta = Vector2.zero;
        textRect.anchoredPosition = new Vector2(20, 0);
        houseText = textObj.AddComponent<TextMeshProUGUI>();
        houseText.text = "Gryffindor";
        houseText.fontSize = 18;
        houseText.alignment = TextAlignmentOptions.Center;
        houseText.color = Color.white;
        houseText.fontStyle = FontStyles.Bold;
    }
    
    GameObject CreatePanel(string name, Transform parent, Vector2 size)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);
        
        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.sizeDelta = size;
        
        Image image = panel.AddComponent<Image>();
        image.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
        
        return panel;
    }
    
    // ================== UPDATE HUD ==================
    
    void UpdateHUD(PlayerData data)
    {
        if (data == null) return;
        
        // Update Health
        if (healthText != null && healthFillImage != null)
        {
            healthText.text = $"{Mathf.CeilToInt(data.currentHealth)} / {Mathf.CeilToInt(data.maxHealth)}";
            float targetFill = data.maxHealth > 0 ? data.currentHealth / data.maxHealth : 0;
            
            if (!smoothTransitions)
            {
                healthFillImage.fillAmount = targetFill;
            }
            else
            {
                currentHealthFill = targetFill;
            }
            
            // Color based on health percentage
            if (targetFill > 0.5f)
                healthFillImage.color = new Color(0.2f, 0.8f, 0.2f); // Green
            else if (targetFill > 0.25f)
                healthFillImage.color = new Color(0.9f, 0.7f, 0.2f); // Yellow
            else
                healthFillImage.color = new Color(0.9f, 0.2f, 0.2f); // Red
        }
        
        // Update XP
        if (xpText != null && xpFillImage != null && levelText != null)
        {
            int xpForNextLevel = data.xpLevel * 100;
            int currentXP = data.xp % 100;
            
            xpText.text = $"{currentXP} / {xpForNextLevel} XP";
            levelText.text = $"Lvl {data.xpLevel}";
            
            float targetXPFill = xpForNextLevel > 0 ? (float)currentXP / xpForNextLevel : 0;
            
            if (!smoothTransitions)
            {
                xpFillImage.fillAmount = targetXPFill;
            }
            else
            {
                currentXPFill = targetXPFill;
            }
        }
        
        // Update Galleons
        if (galleonsText != null)
        {
            galleonsText.text = $"{data.galleons} 🪙";
        }
        
        // Update House
        if (houseText != null && houseBadge != null)
        {
            houseText.text = data.house;
            houseBadge.color = GetHouseColor(data.house);
        }
    }
    
    void AnimateUI()
    {
        if (healthFillImage != null)
        {
            healthFillImage.fillAmount = Mathf.Lerp(
                healthFillImage.fillAmount,
                currentHealthFill,
                Time.deltaTime * transitionSpeed
            );
        }
        
        if (xpFillImage != null)
        {
            xpFillImage.fillAmount = Mathf.Lerp(
                xpFillImage.fillAmount,
                currentXPFill,
                Time.deltaTime * transitionSpeed
            );
        }
    }
    
    Color GetHouseColor(string house)
    {
        switch (house.ToLower())
        {
            case "gryffindor": return gryffindorColor;
            case "slytherin": return slytherinColor;
            case "ravenclaw": return ravenclawColor;
            case "hufflepuff": return hufflepuffColor;
            default: return Color.white;
        }
    }
    
    // ================== PUBLIC API ==================
    
    public void ShowDamage(float damage)
    {
        if (showDamageFlash && healthFillImage != null)
        {
            StartCoroutine(FlashDamage());
        }
    }
    
    System.Collections.IEnumerator FlashDamage()
    {
        Color original = healthFillImage.color;
        healthFillImage.color = Color.white;
        yield return new WaitForSeconds(0.1f);
        healthFillImage.color = original;
    }
    
    public void ShowXPGain(int amount)
    {
        // TODO: Show floating +XP text
        Debug.Log($"⭐ +{amount} XP");
    }
    
    public void SetVisibility(bool visible)
    {
        if (hudPanel != null)
        {
            hudPanel.SetActive(visible);
        }
    }
    
    // ================== CLEANUP ==================
    
    void OnDestroy()
    {
        if (networkManager != null)
        {
            networkManager.OnPlayerDataUpdated -= UpdateHUD;
        }
    }
}
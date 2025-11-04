using UnityEngine;
using UnityEngine.UI;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;

/// <summary>
/// ساخت خودکار Shop UI در Unity Editor
/// استفاده: Tools → Build Shop UI
/// </summary>
public class ShopUIBuilder : EditorWindow
{
    [MenuItem("Tools/Build Shop UI")]
    public static void BuildShopUI()
    {
        // 1. چک کردن و حذف UI قبلی
        GameObject oldCanvas = GameObject.Find("ShopCanvas");
        if (oldCanvas != null)
        {
            if (EditorUtility.DisplayDialog("Warning", 
                "ShopCanvas already exists. Replace it?", "Yes", "No"))
            {
                DestroyImmediate(oldCanvas);
            }
            else
            {
                return;
            }
        }

        Debug.Log("🛒 Building Shop UI...");

        // 2. ساخت Canvas اصلی
        GameObject canvas = CreateCanvas("ShopCanvas");
        
        // 3. ساخت Shop Panel اصلی
        GameObject shopPanel = CreateShopPanel(canvas.transform);
        
        // 4. ساخت Header (Player Info)
        GameObject header = CreateHeader(shopPanel.transform);
        
        // 5. ساخت Tab Buttons
        GameObject tabsContainer = CreateTabsContainer(shopPanel.transform);
        
        // 6. ساخت Items Container (ScrollView)
        GameObject scrollView = CreateItemsScrollView(shopPanel.transform);
        // نام Content برای سازگاری با AutoSetup در ShopUI.cs
        Transform itemsParent = scrollView.transform.Find("Viewport/ItemsGridContent");
        
        // 7. ساخت Confirmation Dialog
        GameObject confirmPanel = CreateConfirmationDialog(canvas.transform);
        
        // 8. ساخت Shop Item Prefab
        GameObject shopItemPrefab = CreateShopItemPrefab();
        
        // 9. اضافه کردن ShopUI Component
        ShopUI shopUI = canvas.AddComponent<ShopUI>();
        
        // 10. تنظیم References
        SetupShopUIReferences(shopUI, shopPanel, header, tabsContainer, 
            itemsParent, confirmPanel, shopItemPrefab);
        
        // 11. غیرفعال کردن Confirm Panel
        confirmPanel.SetActive(false);
        
        Debug.Log("✅ Shop UI built successfully!");
        Selection.activeGameObject = canvas;
    }
    
    // ===== Canvas =====
    
    static GameObject CreateCanvas(string name)
    {
        GameObject canvasObj = new GameObject(name);
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10; 
        
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        
        canvasObj.AddComponent<GraphicRaycaster>();
        
        CanvasGroup canvasGroup = canvasObj.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 1;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
        
        // EventSystem
        if (GameObject.Find("EventSystem") == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }
        
        return canvasObj;
    }
    
    // ===== Shop Panel =====
    
    static GameObject CreateShopPanel(Transform parent)
    {
        GameObject panel = CreatePanel("ShopPanel", parent);
        
        Image bg = panel.GetComponent<Image>();
        bg.color = new Color(0.05f, 0.05f, 0.1f, 0.98f);
        
        // Title
        GameObject title = CreateText("ShopTitle", panel.transform, "🛒 Diagon Alley Shop", 50);
        RectTransform titleRect = title.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0, -60);
        titleRect.sizeDelta = new Vector2(600, 80);
        
        TextMeshProUGUI titleTmp = title.GetComponent<TextMeshProUGUI>();
        titleTmp.color = new Color(1f, 0.84f, 0f); 
        titleTmp.fontStyle = FontStyles.Bold;
        
        // Close Button
        GameObject closeBtn = CreateButton("CloseButton", panel.transform, "✖", 60, 60);
        RectTransform closeRect = closeBtn.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(1, 1);
        closeRect.anchorMax = new Vector2(1, 1);
        closeRect.pivot = new Vector2(1, 1);
        closeRect.anchoredPosition = new Vector2(-20, -20);
        
        Button closeBtnComp = closeBtn.GetComponent<Button>();
        ColorBlock closeColors = closeBtnComp.colors;
        closeColors.normalColor = new Color(0.8f, 0.2f, 0.2f);
        closeColors.highlightedColor = new Color(1f, 0.3f, 0.3f);
        closeColors.pressedColor = new Color(0.6f, 0.1f, 0.1f);
        closeBtnComp.colors = closeColors;
        
        return panel;
    }
    
    // ===== Header (Player Info) =====
    
    static GameObject CreateHeader(Transform parent)
    {
        GameObject header = CreatePanel("Header", parent);
        
        RectTransform headerRect = header.GetComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0, 1);
        headerRect.anchorMax = new Vector2(1, 1);
        headerRect.pivot = new Vector2(0.5f, 1);
        headerRect.anchoredPosition = new Vector2(0, -130);
        headerRect.sizeDelta = new Vector2(-40, 80);
        
        Image headerBg = header.GetComponent<Image>();
        headerBg.color = new Color(0.1f, 0.1f, 0.2f, 0.8f);
        
        // Galleons Text
        GameObject galleonsText = CreateText("GalleonsText", header.transform, "💰 1000", 32);
        RectTransform galleonsRect = galleonsText.GetComponent<RectTransform>();
        galleonsRect.anchorMin = new Vector2(0, 0.5f);
        galleonsRect.anchorMax = new Vector2(0, 0.5f);
        galleonsRect.pivot = new Vector2(0, 0.5f);
        galleonsRect.anchoredPosition = new Vector2(30, 0);
        galleonsRect.sizeDelta = new Vector2(300, 50);
        
        TextMeshProUGUI galleonsTmp = galleonsText.GetComponent<TextMeshProUGUI>();
        galleonsTmp.alignment = TextAlignmentOptions.Left;
        galleonsTmp.color = Color.yellow;
        galleonsTmp.fontStyle = FontStyles.Bold;
        
        // Level Text
        GameObject levelText = CreateText("LevelText", header.transform, "⭐ Level 1", 28);
        RectTransform levelRect = levelText.GetComponent<RectTransform>();
        levelRect.anchorMin = new Vector2(1, 0.5f);
        levelRect.anchorMax = new Vector2(1, 0.5f);
        levelRect.pivot = new Vector2(1, 0.5f);
        levelRect.anchoredPosition = new Vector2(-30, 0);
        levelRect.sizeDelta = new Vector2(300, 50);
        
        TextMeshProUGUI levelTmp = levelText.GetComponent<TextMeshProUGUI>();
        levelTmp.alignment = TextAlignmentOptions.Right;
        levelTmp.color = new Color(0.5f, 1f, 0.5f);
        
        return header;
    }
    
    // ===== Tabs Container =====
    
    static GameObject CreateTabsContainer(Transform parent)
    {
        GameObject tabsContainer = new GameObject("TabsContainer");
        tabsContainer.transform.SetParent(parent);
        
        RectTransform tabsRect = tabsContainer.AddComponent<RectTransform>();
        tabsRect.anchorMin = new Vector2(0, 1);
        tabsRect.anchorMax = new Vector2(1, 1);
        tabsRect.pivot = new Vector2(0.5f, 1);
        tabsRect.anchoredPosition = new Vector2(0, -230);
        tabsRect.sizeDelta = new Vector2(-40, 70);
        
        HorizontalLayoutGroup layout = tabsContainer.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 10;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        
        CreateTabButton("AllButton", tabsContainer.transform, "📦 All");
        CreateTabButton("WandsButton", tabsContainer.transform, "🪄 Wands");
        CreateTabButton("RobesButton", tabsContainer.transform, "👘 Robes");
        CreateTabButton("BroomsButton", tabsContainer.transform, "🧹 Brooms");
        CreateTabButton("PotionsButton", tabsContainer.transform, "🧪 Potions");
        CreateTabButton("PetsButton", tabsContainer.transform, "🦉 Pets");
        CreateTabButton("SpecialButton", tabsContainer.transform, "⭐ Special");
        
        return tabsContainer;
    }
    
    static void CreateTabButton(string name, Transform parent, string text)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent);
        
        RectTransform rect = btnObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0, 60);
        
        Image img = btnObj.AddComponent<Image>();
        img.color = new Color(0.2f, 0.2f, 0.3f);
        
        Button btn = btnObj.AddComponent<Button>();
        ColorBlock colors = btn.colors;
        colors.normalColor = new Color(0.2f, 0.2f, 0.3f);
        colors.highlightedColor = new Color(0.3f, 0.3f, 0.5f);
        colors.pressedColor = new Color(0.15f, 0.15f, 0.25f);
        colors.selectedColor = new Color(0.2f, 0.6f, 0.8f);
        btn.colors = colors;
        
        // Text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform);
        
        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 20;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
    }
    
    // ===== Items ScrollView (Content renamed to ItemsGridContent) =====
    
    static GameObject CreateItemsScrollView(Transform parent)
    {
        GameObject scrollView = new GameObject("ItemsScrollView");
        scrollView.transform.SetParent(parent);
        
        RectTransform scrollRect = scrollView.AddComponent<RectTransform>();
        scrollRect.anchorMin = new Vector2(0, 0);
        scrollRect.anchorMax = new Vector2(1, 1);
        scrollRect.pivot = new Vector2(0.5f, 1);
        scrollRect.anchoredPosition = new Vector2(0, -320);
        scrollRect.sizeDelta = new Vector2(-40, -360);
        
        Image scrollBg = scrollView.AddComponent<Image>();
        scrollBg.color = new Color(0.08f, 0.08f, 0.12f, 0.5f);
        
        ScrollRect scroll = scrollView.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.scrollSensitivity = 30;
        
        // Viewport
        GameObject viewport = new GameObject("Viewport");
        viewport.transform.SetParent(scrollView.transform);
        
        RectTransform viewportRect = viewport.AddComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;
        
        Image viewportImg = viewport.AddComponent<Image>();
        viewportImg.color = Color.clear;
        
        Mask mask = viewport.AddComponent<Mask>();
        mask.showMaskGraphic = false;
        
        scroll.viewport = viewportRect;
        
        // Content (Renamed for clarity and consistency)
        GameObject content = new GameObject("ItemsGridContent");
        content.transform.SetParent(viewport.transform);
        
        RectTransform contentRect = content.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0, 800);
        
        // Grid Layout
        GridLayoutGroup grid = content.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(280, 320);
        grid.spacing = new Vector2(20, 20);
        grid.padding = new RectOffset(20, 20, 20, 20);
        grid.constraint = GridLayoutGroup.Constraint.Flexible; // Flexible constraint is fine
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.UpperCenter;
        
        ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        
        scroll.content = contentRect;
        
        // Scrollbar
        GameObject scrollbar = CreateScrollbar(scrollView.transform);
        scroll.verticalScrollbar = scrollbar.GetComponent<Scrollbar>();
        
        return scrollView;
    }
    
    static GameObject CreateScrollbar(Transform parent)
    {
        GameObject scrollbar = new GameObject("Scrollbar");
        scrollbar.transform.SetParent(parent);
        
        RectTransform scrollbarRect = scrollbar.AddComponent<RectTransform>();
        scrollbarRect.anchorMin = new Vector2(1, 0);
        scrollbarRect.anchorMax = new Vector2(1, 1);
        scrollbarRect.pivot = new Vector2(1, 1);
        scrollbarRect.anchoredPosition = Vector2.zero;
        scrollbarRect.sizeDelta = new Vector2(20, 0);
        
        Image scrollbarBg = scrollbar.AddComponent<Image>();
        scrollbarBg.color = new Color(0.1f, 0.1f, 0.15f);
        
        Scrollbar scrollbarComp = scrollbar.AddComponent<Scrollbar>();
        scrollbarComp.direction = Scrollbar.Direction.BottomToTop;
        
        // Handle
        GameObject handle = new GameObject("Handle");
        handle.transform.SetParent(scrollbar.transform);
        
        RectTransform handleRect = handle.AddComponent<RectTransform>();
        handleRect.anchorMin = Vector2.zero;
        handleRect.anchorMax = Vector2.one;
        handleRect.offsetMin = new Vector2(5, 5);
        handleRect.offsetMax = new Vector2(-5, -5);
        
        Image handleImg = handle.AddComponent<Image>();
        handleImg.color = new Color(0.3f, 0.3f, 0.4f);
        
        scrollbarComp.handleRect = handleRect;
        scrollbarComp.targetGraphic = handleImg;
        
        return scrollbar;
    }
    
    // ===== Confirmation Dialog =====
    
    static GameObject CreateConfirmationDialog(Transform parent)
    {
        GameObject panel = CreatePanel("ConfirmPanel", parent);
        
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(500, 300);
        
        Image bg = panel.GetComponent<Image>();
        bg.color = new Color(0.15f, 0.15f, 0.2f, 0.98f);
        
        // Shadow/Border
        Outline outline = panel.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 0.84f, 0f, 0.5f);
        outline.effectDistance = new Vector2(3, -3);
        
        // Confirm Text
        GameObject confirmText = CreateText("ConfirmText", panel.transform, 
            "Buy Item Name for 💰100?", 28);
        RectTransform textRect = confirmText.GetComponent<RectTransform>();
        textRect.anchoredPosition = new Vector2(0, 50);
        textRect.sizeDelta = new Vector2(450, 150);
        
        TextMeshProUGUI textTmp = confirmText.GetComponent<TextMeshProUGUI>();
        textTmp.alignment = TextAlignmentOptions.Center;
        textTmp.enableWordWrapping = true;
        
        // Yes Button
        GameObject yesBtn = CreateButton("YesButton", panel.transform, "✅ YES", 180, 60);
        RectTransform yesRect = yesBtn.GetComponent<RectTransform>();
        yesRect.anchoredPosition = new Vector2(-100, -70);
        
        Button yesBtnComp = yesBtn.GetComponent<Button>();
        ColorBlock yesColors = yesBtnComp.colors;
        yesColors.normalColor = new Color(0.2f, 0.6f, 0.2f);
        yesColors.highlightedColor = new Color(0.3f, 0.8f, 0.3f);
        yesColors.pressedColor = new Color(0.1f, 0.4f, 0.1f);
        yesBtnComp.colors = yesColors;
        
        // No Button
        GameObject noBtn = CreateButton("NoButton", panel.transform, "❌ NO", 180, 60);
        RectTransform noRect = noBtn.GetComponent<RectTransform>();
        noRect.anchoredPosition = new Vector2(100, -70);
        
        Button noBtnComp = noBtn.GetComponent<Button>();
        ColorBlock noColors = noBtnComp.colors;
        noColors.normalColor = new Color(0.6f, 0.2f, 0.2f);
        noColors.highlightedColor = new Color(0.8f, 0.3f, 0.3f);
        noColors.pressedColor = new Color(0.4f, 0.1f, 0.1f);
        noBtnComp.colors = noColors;
        
        return panel;
    }
    
    // ===== Shop Item Prefab =====
    
    static GameObject CreateShopItemPrefab()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        
        if (!AssetDatabase.IsValidFolder("Assets/Resources/Prefabs"))
            AssetDatabase.CreateFolder("Assets/Resources", "Prefabs");
        
        GameObject itemObj = new GameObject("ShopItem");
        
        RectTransform rect = itemObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(280, 320);
        
        // Background
        Image bg = itemObj.AddComponent<Image>();
        bg.color = new Color(0.12f, 0.12f, 0.18f);
        
        // Border
        Outline outline = itemObj.AddComponent<Outline>();
        outline.effectColor = new Color(0.3f, 0.3f, 0.4f);
        outline.effectDistance = new Vector2(2, -2);
        
        // Icon
        GameObject icon = new GameObject("Icon");
        icon.transform.SetParent(itemObj.transform);
        
        RectTransform iconRect = icon.AddComponent<RectTransform>();
        iconRect.anchoredPosition = new Vector2(0, 80);
        iconRect.sizeDelta = new Vector2(120, 120);
        
        Image iconImg = icon.AddComponent<Image>();
        iconImg.color = Color.white;
        
        // Name
        GameObject name = CreateText("NameText", itemObj.transform, "Item Name", 22);
        RectTransform nameRect = name.GetComponent<RectTransform>();
        nameRect.anchoredPosition = new Vector2(0, -10);
        nameRect.sizeDelta = new Vector2(260, 40);
        
        TextMeshProUGUI nameTmp = name.GetComponent<TextMeshProUGUI>();
        nameTmp.color = new Color(1f, 0.84f, 0f);
        nameTmp.fontStyle = FontStyles.Bold;
        
        // Description
        GameObject desc = CreateText("DescriptionText", itemObj.transform, "Item description", 16);
        RectTransform descRect = desc.GetComponent<RectTransform>();
        descRect.anchoredPosition = new Vector2(0, -60);
        descRect.sizeDelta = new Vector2(260, 60);
        
        TextMeshProUGUI descTmp = desc.GetComponent<TextMeshProUGUI>();
        descTmp.color = new Color(0.7f, 0.7f, 0.7f);
        descTmp.fontSize = 16;
        descTmp.enableWordWrapping = true;
        
        // Price
        GameObject price = CreateText("PriceText", itemObj.transform, "💰 100", 20);
        RectTransform priceRect = price.GetComponent<RectTransform>();
        priceRect.anchoredPosition = new Vector2(0, -110);
        priceRect.sizeDelta = new Vector2(260, 30);
        
        TextMeshProUGUI priceTmp = price.GetComponent<TextMeshProUGUI>();
        priceTmp.color = Color.yellow;
        priceTmp.fontStyle = FontStyles.Bold;
        
        // Level Text
        GameObject level = CreateText("LevelText", itemObj.transform, "Requires Level 5", 16);
        RectTransform levelRect = level.GetComponent<RectTransform>();
        levelRect.anchoredPosition = new Vector2(0, -135);
        levelRect.sizeDelta = new Vector2(260, 25);
        
        TextMeshProUGUI levelTmp = level.GetComponent<TextMeshProUGUI>();
        levelTmp.color = new Color(1f, 0.5f, 0.5f);
        levelTmp.fontSize = 16;
        
        // Buy Button
        GameObject buyBtn = CreateButton("BuyButton", itemObj.transform, "BUY", 200, 50);
        RectTransform buyRect = buyBtn.GetComponent<RectTransform>();
        buyRect.anchoredPosition = new Vector2(0, -145);
        
        Button buyBtnComp = buyBtn.GetComponent<Button>();
        ColorBlock colors = buyBtnComp.colors;
        colors.normalColor = new Color(0.2f, 0.6f, 0.2f);
        colors.highlightedColor = new Color(0.3f, 0.8f, 0.3f);
        colors.pressedColor = new Color(0.1f, 0.4f, 0.1f);
        colors.disabledColor = new Color(0.3f, 0.3f, 0.3f);
        buyBtnComp.colors = colors;
        
        // Locked Overlay
        GameObject lockedOverlay = CreatePanel("LockedOverlay", itemObj.transform);
        RectTransform lockedRect = lockedOverlay.GetComponent<RectTransform>();
        lockedRect.anchorMin = Vector2.zero;
        lockedRect.anchorMax = Vector2.one;
        lockedRect.offsetMin = Vector2.zero;
        lockedRect.offsetMax = Vector2.zero;
        
        Image lockedImg = lockedOverlay.GetComponent<Image>();
        lockedImg.color = new Color(0, 0, 0, 0.7f);
        
        GameObject lockIcon = CreateText("LockIcon", lockedOverlay.transform, "🔒", 60);
        RectTransform lockRect = lockIcon.GetComponent<RectTransform>();
        lockRect.anchoredPosition = Vector2.zero;
        
        lockedOverlay.SetActive(false);
        
        // Add ShopItem Component
        itemObj.AddComponent<ShopItem>();
        
        // Save as Prefab
        string prefabPath = "Assets/Resources/Prefabs/ShopItem.prefab";
        PrefabUtility.SaveAsPrefabAsset(itemObj, prefabPath);
        
        Debug.Log($"✅ ShopItem Prefab created at: {prefabPath}");
        
        return itemObj;
    }
    
    // ===== Helper Methods (Unchanged) =====
    
    static GameObject CreatePanel(string name, Transform parent)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent);
        
        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        
        Image img = panel.AddComponent<Image>();
        img.color = new Color(0.2f, 0.2f, 0.25f, 0.9f);
        
        return panel;
    }
    
    static GameObject CreateText(string name, Transform parent, string text, int fontSize)
    {
        GameObject textObj = new GameObject(name);
        textObj.transform.SetParent(parent);
        
        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        
        RectTransform rect = textObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(400, fontSize + 20);
        
        return textObj;
    }
    
    static GameObject CreateButton(string name, Transform parent, string text, float width, float height)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent);
        
        RectTransform rect = btnObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(width, height);
        
        Image img = btnObj.AddComponent<Image>();
        img.color = new Color(0.2f, 0.4f, 0.8f, 1f);
        
        Button btn = btnObj.AddComponent<Button>();
        
        // Text child
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform);
        
        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 24;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.fontStyle = FontStyles.Bold;
        
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        
        return btnObj;
    }
    
    // ===== Setup ShopUI References (Unchanged logic) =====
    
    static void SetupShopUIReferences(ShopUI shopUI, GameObject shopPanel, 
        GameObject header, GameObject tabsContainer, Transform itemsParent,
        GameObject confirmPanel, GameObject shopItemPrefab)
    {
        SerializedObject so = new SerializedObject(shopUI);
        
        // Main Panel
        so.FindProperty("shopPanel").objectReferenceValue = shopPanel;
        
        // Items Container
        so.FindProperty("itemsParent").objectReferenceValue = itemsParent;
        so.FindProperty("shopItemPrefab").objectReferenceValue = shopItemPrefab;
        
        // Tab Buttons
        so.FindProperty("allTabButton").objectReferenceValue = 
            tabsContainer.transform.Find("AllButton").GetComponent<Button>();
        so.FindProperty("wandsTabButton").objectReferenceValue = 
            tabsContainer.transform.Find("WandsButton").GetComponent<Button>();
        so.FindProperty("robesTabButton").objectReferenceValue = 
            tabsContainer.transform.Find("RobesButton").GetComponent<Button>();
        so.FindProperty("broomsTabButton").objectReferenceValue = 
            tabsContainer.transform.Find("BroomsButton").GetComponent<Button>();
        so.FindProperty("potionsTabButton").objectReferenceValue = 
            tabsContainer.transform.Find("PotionsButton").GetComponent<Button>();
        so.FindProperty("petsTabButton").objectReferenceValue = 
            tabsContainer.transform.Find("PetsButton").GetComponent<Button>();
        so.FindProperty("specialTabButton").objectReferenceValue = 
            tabsContainer.transform.Find("SpecialButton").GetComponent<Button>();
        
        // Player Info
        so.FindProperty("galleonsText").objectReferenceValue = 
            header.transform.Find("GalleonsText").GetComponent<TextMeshProUGUI>();
        so.FindProperty("levelText").objectReferenceValue = 
            header.transform.Find("LevelText").GetComponent<TextMeshProUGUI>();
        
        // Confirmation Dialog
        so.FindProperty("confirmPanel").objectReferenceValue = confirmPanel;
        so.FindProperty("confirmText").objectReferenceValue = 
            confirmPanel.transform.Find("ConfirmText").GetComponent<TextMeshProUGUI>();
        so.FindProperty("confirmYesButton").objectReferenceValue = 
            confirmPanel.transform.Find("YesButton").GetComponent<Button>();
        so.FindProperty("confirmNoButton").objectReferenceValue = 
            confirmPanel.transform.Find("NoButton").GetComponent<Button>();
        
        // Close/Back Buttons
        so.FindProperty("closeButton").objectReferenceValue = 
            shopPanel.transform.Find("CloseButton").GetComponent<Button>();
        so.FindProperty("backButton").objectReferenceValue = 
            shopPanel.transform.Find("CloseButton").GetComponent<Button>();
        
        // Canvas Group
        so.FindProperty("canvasGroup").objectReferenceValue = 
            shopUI.GetComponent<CanvasGroup>();
        
        // Settings
        so.FindProperty("autoSetupUI").boolValue = true;
        so.FindProperty("autoOpenOnStart").boolValue = true;
        so.FindProperty("showDebugLogs").boolValue = true;
        so.FindProperty("fadeDuration").floatValue = 0.3f;
        
        so.ApplyModifiedProperties();
        
        Debug.Log("✅ All ShopUI references set successfully!");
    }
}
#endif
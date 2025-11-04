#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// ابزار ایجاد Inventory UI خودکار
/// استفاده: Tools → Build Inventory UI
/// </summary>
public class InventoryUIBuilder
{
    [MenuItem("Tools/Build Inventory UI")]
    public static void BuildInventoryUI()
    {
        // چک کردن Canvas
        Canvas[] allCanvases = Resources.FindObjectsOfTypeAll<Canvas>();
        Canvas targetCanvas = null;
        
        foreach (Canvas c in allCanvases)
        {
            if (c.gameObject.scene.isLoaded)
            {
                targetCanvas = c;
                break;
            }
        }
        
        if (targetCanvas == null)
        {
            EditorUtility.DisplayDialog("Error", "Canvas not found in scene!", "OK");
            return;
        }

        // چک کردن UI قبلی
        GameObject oldCanvas = GameObject.Find("InventoryCanvas");
        if (oldCanvas != null)
        {
            if (EditorUtility.DisplayDialog("Warning", 
                "InventoryCanvas already exists. Replace it?", "Yes", "No"))
            {
                UnityEngine.Object.DestroyImmediate(oldCanvas);
            }
            else
            {
                return;
            }
        }

        Debug.Log("📦 Building Inventory UI...");

        // ریشه Inventory
        GameObject inventoryRoot = new GameObject("Inventory");
        RectTransform inventoryRect = inventoryRoot.AddComponent<RectTransform>();
        inventoryRect.SetParent(targetCanvas.transform, false);
        inventoryRect.anchorMin = Vector2.zero;
        inventoryRect.anchorMax = Vector2.one;
        inventoryRect.offsetMin = Vector2.zero;
        inventoryRect.offsetMax = Vector2.zero;
        
        // Background Image
        Image inventoryBg = inventoryRoot.AddComponent<Image>();
        inventoryBg.color = new Color(0, 0, 0, 0.3f);
        
        // CanvasGroup برای fade
        CanvasGroup canvasGroup = inventoryRoot.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        
        // Main Container (Horizontal Layout)
        GameObject mainContainer = new GameObject("MainContainer");
        RectTransform mainRect = mainContainer.AddComponent<RectTransform>();
        mainRect.SetParent(inventoryRect);
        mainRect.anchorMin = new Vector2(0, 0.5f);
        mainRect.anchorMax = new Vector2(1, 0.5f);
        mainRect.pivot = new Vector2(0.5f, 0.5f);
        mainRect.offsetMin = Vector2.zero;
        mainRect.offsetMax = Vector2.zero;
        
        // LEFT: SLOTS SECTION
        GameObject leftSection = new GameObject("SlotsSection");
        RectTransform leftRect = leftSection.AddComponent<RectTransform>();
        leftRect.SetParent(mainRect);
        leftRect.anchorMin = new Vector2(0, 0);
        leftRect.anchorMax = new Vector2(1, 1);
        leftRect.offsetMin = Vector2.zero;
        leftRect.offsetMax = Vector2.zero;
        
        // Slots Panel Background
        GameObject slotsPanel = new GameObject("SlotsPanel");
        RectTransform slotsPanelRect = slotsPanel.AddComponent<RectTransform>();
        slotsPanelRect.SetParent(leftRect);
        slotsPanelRect.anchorMin = new Vector2(0.5f, 0.5f);
        slotsPanelRect.anchorMax = new Vector2(0.5f, 0.5f);
        slotsPanelRect.pivot = new Vector2(0.5f, 0.5f);
        
        float totalWidth = (5 * 80) + (4 * 10);
        int totalRows = 4;
        float totalHeight = (totalRows * 80) + (3 * 10);
        
        slotsPanelRect.sizeDelta = new Vector2(totalWidth + 20, totalHeight + 20);
        slotsPanelRect.anchoredPosition = new Vector2(-200, 0);
        
        Image slotsPanelImage = slotsPanel.AddComponent<Image>();
        slotsPanelImage.color = new Color(0.15f, 0.15f, 0.15f, 0.95f);
        
        // Slots Grid Layout
        GameObject slotsContainer = new GameObject("Slots");
        RectTransform slotsContainerRect = slotsContainer.AddComponent<RectTransform>();
        slotsContainerRect.SetParent(slotsPanelRect);
        slotsContainerRect.anchorMin = Vector2.zero;
        slotsContainerRect.anchorMax = Vector2.one;
        slotsContainerRect.offsetMin = new Vector2(10, 10);
        slotsContainerRect.offsetMax = new Vector2(-10, -10);
        
        GridLayoutGroup gridLayout = slotsContainer.AddComponent<GridLayoutGroup>();
        gridLayout.cellSize = new Vector2(80, 80);
        gridLayout.spacing = new Vector2(10, 10);
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = 5;
        gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
        
        // Slot Info Text
        GameObject infoPanel = new GameObject("InfoPanel");
        RectTransform infoPanelRect = infoPanel.AddComponent<RectTransform>();
        infoPanelRect.SetParent(slotsPanelRect);
        infoPanelRect.anchorMin = new Vector2(0, 0);
        infoPanelRect.anchorMax = new Vector2(1, 0);
        infoPanelRect.pivot = new Vector2(0.5f, 0);
        infoPanelRect.offsetMin = Vector2.zero;
        infoPanelRect.offsetMax = Vector2.zero;
        infoPanelRect.anchoredPosition = new Vector2(0, -40);
        infoPanelRect.sizeDelta = new Vector2(0, 35);
        
        TextMeshProUGUI slotsText = infoPanel.AddComponent<TextMeshProUGUI>();
        slotsText.text = "Slots: 0/20";
        slotsText.fontSize = 28;
        slotsText.alignment = TextAlignmentOptions.Center;
        slotsText.color = Color.white;
        
        // RIGHT: DETAILS SECTION
        GameObject detailsSection = new GameObject("DetailsSection");
        RectTransform detailsRect = detailsSection.AddComponent<RectTransform>();
        detailsRect.SetParent(mainRect);
        detailsRect.anchorMin = new Vector2(1, 0);
        detailsRect.anchorMax = new Vector2(1, 1);
        detailsRect.pivot = new Vector2(1, 0.5f);
        detailsRect.offsetMin = Vector2.zero;
        detailsRect.offsetMax = Vector2.zero;
        detailsRect.sizeDelta = new Vector2(350, 0);
        
        Image detailsImage = detailsSection.AddComponent<Image>();
        detailsImage.color = new Color(0.2f, 0.2f, 0.2f, 0.95f);
        
        VerticalLayoutGroup detailsLayout = detailsSection.AddComponent<VerticalLayoutGroup>();
        detailsLayout.padding = new RectOffset(15, 15, 15, 15);
        detailsLayout.spacing = 10;
        detailsLayout.childForceExpandHeight = false;
        detailsLayout.childForceExpandWidth = true;
        
        // Icon
        GameObject iconObj = new GameObject("Icon");
        RectTransform iconRect = iconObj.AddComponent<RectTransform>();
        iconRect.SetParent(detailsRect);
        iconRect.sizeDelta = new Vector2(100, 100);
        
        Image detailIcon = iconObj.AddComponent<Image>();
        detailIcon.color = new Color(0.25f, 0.25f, 0.25f, 0.9f);
        
        LayoutElement iconLayout = iconObj.AddComponent<LayoutElement>();
        iconLayout.preferredWidth = 100;
        iconLayout.preferredHeight = 100;
        
        // Name
        GameObject nameObj = new GameObject("Name");
        RectTransform nameRect = nameObj.AddComponent<RectTransform>();
        nameRect.SetParent(detailsRect);
        
        TextMeshProUGUI detailName = nameObj.AddComponent<TextMeshProUGUI>();
        detailName.text = "Item Name";
        detailName.fontSize = 32;
        detailName.fontStyle = FontStyles.Bold;
        detailName.alignment = TextAlignmentOptions.TopLeft;
        detailName.color = Color.white;
        
        LayoutElement nameLayout = nameObj.AddComponent<LayoutElement>();
        nameLayout.preferredHeight = 40;
        
        // Description
        GameObject descObj = new GameObject("Description");
        RectTransform descRect = descObj.AddComponent<RectTransform>();
        descRect.SetParent(detailsRect);
        
        TextMeshProUGUI detailDescription = descObj.AddComponent<TextMeshProUGUI>();
        detailDescription.text = "Item description goes here...";
        detailDescription.fontSize = 20;
        detailDescription.alignment = TextAlignmentOptions.TopLeft;
        detailDescription.color = new Color(0.8f, 0.8f, 0.8f);
        
        LayoutElement descLayout = descObj.AddComponent<LayoutElement>();
        descLayout.preferredHeight = 60;
        
        // Stats
        GameObject statsObj = new GameObject("Stats");
        RectTransform statsRect = statsObj.AddComponent<RectTransform>();
        statsRect.SetParent(detailsRect);
        
        TextMeshProUGUI detailStats = statsObj.AddComponent<TextMeshProUGUI>();
        detailStats.text = "⚔️ Damage: +10%\n🛡️ Defense: +5%";
        detailStats.fontSize = 18;
        detailStats.alignment = TextAlignmentOptions.TopLeft;
        detailStats.color = new Color(1, 0.9f, 0.3f);
        
        LayoutElement statsLayout = statsObj.AddComponent<LayoutElement>();
        statsLayout.preferredHeight = 80;
        statsLayout.flexibleHeight = 1;
        
        // Buttons Container
        GameObject buttonsContainer = new GameObject("Buttons");
        RectTransform buttonsRect = buttonsContainer.AddComponent<RectTransform>();
        buttonsRect.SetParent(detailsRect);
        
        HorizontalLayoutGroup buttonsLayout = buttonsContainer.AddComponent<HorizontalLayoutGroup>();
        buttonsLayout.spacing = 10;
        buttonsLayout.childForceExpandWidth = true;
        buttonsLayout.childForceExpandHeight = true;
        
        LayoutElement buttonsLayoutElement = buttonsContainer.AddComponent<LayoutElement>();
        buttonsLayoutElement.preferredHeight = 50;
        
        // Use Button
        GameObject useBtn = CreateButton("Use", buttonsContainer.transform, new Color(0.2f, 0.7f, 0.2f));
        
        // Equip Button
        GameObject equipBtn = CreateButton("Equip", buttonsContainer.transform, new Color(0.3f, 0.5f, 0.8f));
        
        // Sell Button
        GameObject sellBtn = CreateButton("Sell", buttonsContainer.transform, new Color(0.8f, 0.3f, 0.3f));
        
        // Galleons Info
        GameObject galleonsObj = new GameObject("Galleons");
        RectTransform galleonsRect = galleonsObj.AddComponent<RectTransform>();
        galleonsRect.SetParent(detailsRect);
        
        TextMeshProUGUI galleonsText = galleonsObj.AddComponent<TextMeshProUGUI>();
        galleonsText.text = "💰 100 Galleons";
        galleonsText.fontSize = 20;
        galleonsText.alignment = TextAlignmentOptions.BottomRight;
        galleonsText.color = new Color(1, 0.85f, 0.3f);
        
        LayoutElement galleonsLayout = galleonsObj.AddComponent<LayoutElement>();
        galleonsLayout.preferredHeight = 30;
        
        // TOP: CLOSE BUTTON
        // TOP: CLOSE BUTTON
        GameObject closeBtn = new GameObject("CloseButton");
        RectTransform closeBtnRect = closeBtn.AddComponent<RectTransform>();
        closeBtnRect.SetParent(inventoryRect);
        closeBtnRect.anchorMin = new Vector2(1, 1);
        closeBtnRect.anchorMax = new Vector2(1, 1);
        closeBtnRect.pivot = new Vector2(1, 1);
        closeBtnRect.sizeDelta = new Vector2(50, 50);
        closeBtnRect.anchoredPosition = new Vector2(-10, -10);

        Image closeBtnImage = closeBtn.AddComponent<Image>();
        closeBtnImage.color = new Color(0.8f, 0.2f, 0.2f);

        Button closeBtnComponent = closeBtn.AddComponent<Button>();
        closeBtnComponent.targetGraphic = closeBtnImage;

        // TextMeshPro فرزند
        GameObject closeTextObj = new GameObject("Text");
        RectTransform closeTextRect = closeTextObj.AddComponent<RectTransform>();
        closeTextRect.SetParent(closeBtnRect, false);
        closeTextRect.anchorMin = Vector2.zero;
        closeTextRect.anchorMax = Vector2.one;
        closeTextRect.offsetMin = Vector2.zero;
        closeTextRect.offsetMax = Vector2.zero;

        TextMeshProUGUI closeBtnText = closeTextObj.AddComponent<TextMeshProUGUI>();
        closeBtnText.text = "✕";
        closeBtnText.fontSize = 32;
        closeBtnText.alignment = TextAlignmentOptions.Center;
        closeBtnText.color = Color.white;

        
        // Slot Prefab
        GameObject slotPrefab = CreateSlotPrefab();
        
        // ساخت Slot‌ها
        for (int i = 0; i < 20; i++)
        {
            GameObject slot = Object.Instantiate(slotPrefab, slotsContainerRect);
            slot.name = $"Slot_{i}";
        }
        
        // ADD INVENTORY UI SCRIPT
        InventoryUI inventoryUI = inventoryRoot.AddComponent<InventoryUI>();
        
        // Assign all references
        SerializedObject so = new SerializedObject(inventoryUI);
        
        so.FindProperty("inventoryPanel").objectReferenceValue = inventoryRoot;
        so.FindProperty("slotsParent").objectReferenceValue = slotsContainerRect;
        so.FindProperty("slotPrefab").objectReferenceValue = slotPrefab;
        so.FindProperty("closeButton").objectReferenceValue = closeBtnComponent;
        
        so.FindProperty("detailsPanel").objectReferenceValue = detailsSection;
        so.FindProperty("detailIcon").objectReferenceValue = detailIcon;
        so.FindProperty("detailName").objectReferenceValue = detailName;
        so.FindProperty("detailDescription").objectReferenceValue = detailDescription;
        so.FindProperty("detailStats").objectReferenceValue = detailStats;
        so.FindProperty("useButton").objectReferenceValue = useBtn.GetComponent<Button>();
        so.FindProperty("equipButton").objectReferenceValue = equipBtn.GetComponent<Button>();
        so.FindProperty("sellButton").objectReferenceValue = sellBtn.GetComponent<Button>();
        
        so.FindProperty("slotsText").objectReferenceValue = slotsText;
        so.FindProperty("galleonsText").objectReferenceValue = galleonsText;
        
        so.FindProperty("canvasGroup").objectReferenceValue = canvasGroup;
        
        so.ApplyModifiedProperties();
        
        Debug.Log("✅ Inventory UI built successfully! 20 slots created");
        EditorUtility.DisplayDialog("✅ موفق", "Inventory UI ساخته شد!\n20 slot ایجاد شد", "OK");
        EditorGUIUtility.PingObject(inventoryRoot);
    }
    
    static GameObject CreateButton(string buttonName, Transform parent, Color color)
    {
        GameObject btnObj = new GameObject(buttonName);
        RectTransform btnRect = btnObj.AddComponent<RectTransform>();
        btnRect.SetParent(parent);

        Image btnImage = btnObj.AddComponent<Image>();
        btnImage.color = color;

        Button btnComponent = btnObj.AddComponent<Button>();
        btnComponent.targetGraphic = btnImage;

        // TextMeshPro فرزند
        GameObject textObj = new GameObject("Text");
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.SetParent(btnRect, false);
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI btnText = textObj.AddComponent<TextMeshProUGUI>();
        btnText.text = buttonName;
        btnText.fontSize = 24;
        btnText.fontStyle = FontStyles.Bold;
        btnText.alignment = TextAlignmentOptions.Center;
        btnText.color = Color.white;

        LayoutElement btnLayout = btnObj.AddComponent<LayoutElement>();
        btnLayout.preferredHeight = 50;

        return btnObj;
    }

    
    static GameObject CreateSlotPrefab()
    {
        GameObject slotPrefab = new GameObject("SlotPrefab");
        RectTransform slotRect = slotPrefab.AddComponent<RectTransform>();
        slotRect.sizeDelta = new Vector2(80, 80);
        
        Image slotImage = slotPrefab.AddComponent<Image>();
        slotImage.color = new Color(0.25f, 0.25f, 0.25f, 0.9f);
        
        Button slotButton = slotPrefab.AddComponent<Button>();
        slotButton.targetGraphic = slotImage;
        
        // Icon
        GameObject iconObj = new GameObject("Icon");
        RectTransform iconRect = iconObj.AddComponent<RectTransform>();
        iconRect.SetParent(slotRect);
        iconRect.anchorMin = Vector2.zero;
        iconRect.anchorMax = Vector2.one;
        iconRect.offsetMin = Vector2.zero;
        iconRect.offsetMax = Vector2.zero;
        
        Image iconImage = iconObj.AddComponent<Image>();
        iconImage.color = new Color(1, 1, 1, 0.7f);
        
        // Quantity Text
        GameObject quantityObj = new GameObject("Quantity");
        RectTransform quantityRect = quantityObj.AddComponent<RectTransform>();
        quantityRect.SetParent(slotRect);
        quantityRect.anchorMin = new Vector2(1, 0);
        quantityRect.anchorMax = new Vector2(1, 0);
        quantityRect.pivot = new Vector2(1, 0);
        quantityRect.sizeDelta = new Vector2(30, 30);
        quantityRect.anchoredPosition = new Vector2(-5, 5);
        
        TextMeshProUGUI quantityText = quantityObj.AddComponent<TextMeshProUGUI>();
        quantityText.text = "1";
        quantityText.fontSize = 18;
        quantityText.alignment = TextAlignmentOptions.BottomRight;
        quantityText.color = Color.white;
        
        // Selected Border
        GameObject borderObj = new GameObject("SelectedBorder");
        RectTransform borderRect = borderObj.AddComponent<RectTransform>();
        borderRect.SetParent(slotRect);
        borderRect.anchorMin = Vector2.zero;
        borderRect.anchorMax = Vector2.one;
        borderRect.offsetMin = Vector2.zero;
        borderRect.offsetMax = Vector2.zero;
        
        Image borderImage = borderObj.AddComponent<Image>();
        borderImage.color = new Color(1, 1, 0, 0);
        
        // Outline
        Outline outline = borderObj.AddComponent<Outline>();
        outline.effectColor = new Color(1, 1, 0, 1);
        outline.effectDistance = new Vector2(3, 3);
        
        // InventorySlot Script
        InventorySlot inventorySlot = slotPrefab.AddComponent<InventorySlot>();
        SerializedObject slotSO = new SerializedObject(inventorySlot);
        slotSO.FindProperty("iconImage").objectReferenceValue = iconImage;
        slotSO.FindProperty("quantityText").objectReferenceValue = quantityText;
        slotSO.FindProperty("selectedBorder").objectReferenceValue = borderObj;
        slotSO.FindProperty("button").objectReferenceValue = slotButton;
        slotSO.ApplyModifiedProperties();
        
        return slotPrefab;
    }
}
#endif
using UnityEngine;
using UnityEngine.UI;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// ابزار ساخت خودکار UI برای PauseMenuManager
/// استفاده: Menu -> Tools -> Build Pause Menu UI
/// </summary>
public class PauseMenuBuilder : MonoBehaviour
{
#if UNITY_EDITOR
    [MenuItem("Tools/Build Pause Menu UI")]
    public static void BuildPauseMenuUI()
    {
        // پیدا کردن PauseMenuManager
        PauseMenuManager pauseManager = FindObjectOfType<PauseMenuManager>();
        
        if (pauseManager == null)
        {
            Debug.LogError("❌ PauseMenuManager not found! Please create one first.");
            return;
        }
        
        Debug.Log("🔨 Building Pause Menu UI...");
        
        // ساخت Canvas اصلی
        GameObject canvasObj = CreateCanvas(pauseManager.transform);
        Canvas canvas = canvasObj.GetComponent<Canvas>();
        canvas.sortingOrder = 100;
        
        // ساخت Panel‌ها
        GameObject pauseMenuPanel = CreatePauseMenuPanel(canvasObj.transform);
        GameObject settingsPanel = CreateSettingsPanel(canvasObj.transform);
        GameObject confirmDialog = CreateConfirmDialog(canvasObj.transform);
        
        // اساین کردن References
        SerializedObject so = new SerializedObject(pauseManager);
        
        // Main Pause Menu
        so.FindProperty("pauseMenuPanel").objectReferenceValue = pauseMenuPanel;
        so.FindProperty("settingsPanel").objectReferenceValue = settingsPanel;
        so.FindProperty("confirmDialog").objectReferenceValue = confirmDialog;
        
        // Pause Menu Buttons
        Transform pauseContent = pauseMenuPanel.transform.Find("Background/Content");
        so.FindProperty("resumeButton").objectReferenceValue = pauseContent.Find("ResumeButton").GetComponent<Button>();
        so.FindProperty("settingsButton").objectReferenceValue = pauseContent.Find("SettingsButton").GetComponent<Button>();
        so.FindProperty("returnToMenuButton").objectReferenceValue = pauseContent.Find("ReturnToMenuButton").GetComponent<Button>();
        so.FindProperty("quitGameButton").objectReferenceValue = pauseContent.Find("QuitButton").GetComponent<Button>();
        
        // Settings Panel
        Transform settingsContent = settingsPanel.transform.Find("Background/ScrollView/Viewport/Content");
        so.FindProperty("masterVolumeSlider").objectReferenceValue = settingsContent.Find("AudioGroup/MasterVolume/Slider").GetComponent<Slider>();
        so.FindProperty("musicVolumeSlider").objectReferenceValue = settingsContent.Find("AudioGroup/MusicVolume/Slider").GetComponent<Slider>();
        so.FindProperty("sfxVolumeSlider").objectReferenceValue = settingsContent.Find("AudioGroup/SFXVolume/Slider").GetComponent<Slider>();
        so.FindProperty("brightnessSlider").objectReferenceValue = settingsContent.Find("GraphicsGroup/Brightness/Slider").GetComponent<Slider>();
        so.FindProperty("qualityDropdown").objectReferenceValue = settingsContent.Find("GraphicsGroup/Quality/Dropdown").GetComponent<TMP_Dropdown>();
        so.FindProperty("vsyncToggle").objectReferenceValue = settingsContent.Find("GraphicsGroup/VSync/Toggle").GetComponent<Toggle>();
        so.FindProperty("settingsBackButton").objectReferenceValue = settingsPanel.transform.Find("Background/BackButton").GetComponent<Button>();
        
        // Confirm Dialog
        Transform confirmContent = confirmDialog.transform.Find("Background");
        so.FindProperty("confirmText").objectReferenceValue = confirmContent.Find("MessageText").GetComponent<TextMeshProUGUI>();
        so.FindProperty("confirmYesButton").objectReferenceValue = confirmContent.Find("ButtonGroup/YesButton").GetComponent<Button>();
        so.FindProperty("confirmNoButton").objectReferenceValue = confirmContent.Find("ButtonGroup/NoButton").GetComponent<Button>();
        
        so.ApplyModifiedProperties();
        
        // پنهان کردن پنل‌ها
        pauseMenuPanel.SetActive(false);
        settingsPanel.SetActive(false);
        confirmDialog.SetActive(false);
        
        EditorUtility.SetDirty(pauseManager);
        
        Debug.Log("✅ Pause Menu UI built successfully!");
        Debug.Log("📋 All references assigned to PauseMenuManager");
    }
    
    // ===== Canvas =====
    
    static GameObject CreateCanvas(Transform parent)
    {
        GameObject canvasObj = new GameObject("PauseMenuCanvas");
        canvasObj.transform.SetParent(parent);
        
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        
        canvasObj.AddComponent<GraphicRaycaster>();
        
        return canvasObj;
    }
    
    // ===== Pause Menu Panel =====
    
    static GameObject CreatePauseMenuPanel(Transform parent)
    {
        GameObject panel = new GameObject("PauseMenuPanel");
        panel.transform.SetParent(parent);
        
        RectTransform rt = panel.AddComponent<RectTransform>();
        StretchRect(rt);
        
        // Dark Overlay
        Image overlay = panel.AddComponent<Image>();
        overlay.color = new Color(0, 0, 0, 0.8f);
        
        // Background
        GameObject bg = CreateBox("Background", panel.transform, new Vector2(600, 500), Color.white);
        Image bgImage = bg.GetComponent<Image>();
        bgImage.color = new Color(0.15f, 0.15f, 0.2f, 0.95f);
        
        // Title
        GameObject title = CreateText("Title", bg.transform, "PAUSED", 48, TextAlignmentOptions.Center);
        RectTransform titleRT = title.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0, 1);
        titleRT.anchorMax = new Vector2(1, 1);
        titleRT.anchoredPosition = new Vector2(0, -50);
        titleRT.sizeDelta = new Vector2(-40, 60);
        
        // Content
        GameObject content = new GameObject("Content");
        content.transform.SetParent(bg.transform);
        RectTransform contentRT = content.AddComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0.5f, 0.5f);
        contentRT.anchorMax = new Vector2(0.5f, 0.5f);
        contentRT.anchoredPosition = new Vector2(0, -20);
        contentRT.sizeDelta = new Vector2(400, 300);
        
        // Buttons
        CreateMenuButton("ResumeButton", content.transform, "Resume Game", 0);
        CreateMenuButton("SettingsButton", content.transform, "Settings", 1);
        CreateMenuButton("ReturnToMenuButton", content.transform, "Return to Menu", 2);
        CreateMenuButton("QuitButton", content.transform, "Quit Game", 3);
        
        return panel;
    }
    
    // ===== Settings Panel =====
    
    static GameObject CreateSettingsPanel(Transform parent)
    {
        GameObject panel = new GameObject("SettingsPanel");
        panel.transform.SetParent(parent);
        
        RectTransform rt = panel.AddComponent<RectTransform>();
        StretchRect(rt);
        
        // Dark Overlay
        Image overlay = panel.AddComponent<Image>();
        overlay.color = new Color(0, 0, 0, 0.8f);
        
        // Background
        GameObject bg = CreateBox("Background", panel.transform, new Vector2(800, 700), Color.white);
        Image bgImage = bg.GetComponent<Image>();
        bgImage.color = new Color(0.15f, 0.15f, 0.2f, 0.95f);
        
        // Title
        GameObject title = CreateText("Title", bg.transform, "SETTINGS", 42, TextAlignmentOptions.Center);
        RectTransform titleRT = title.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0, 1);
        titleRT.anchorMax = new Vector2(1, 1);
        titleRT.anchoredPosition = new Vector2(0, -40);
        titleRT.sizeDelta = new Vector2(-40, 50);
        
        // Scroll View
        GameObject scrollView = CreateScrollView("ScrollView", bg.transform);
        RectTransform scrollRT = scrollView.GetComponent<RectTransform>();
        scrollRT.anchorMin = new Vector2(0, 0);
        scrollRT.anchorMax = new Vector2(1, 1);
        scrollRT.offsetMin = new Vector2(20, 80);
        scrollRT.offsetMax = new Vector2(-20, -100);
        
        Transform content = scrollView.transform.Find("Viewport/Content");
        
        // Audio Group
        GameObject audioGroup = CreateSettingsGroup("AudioGroup", content, "AUDIO", 0);
        CreateSliderSetting("MasterVolume", audioGroup.transform, "Master Volume", 0);
        CreateSliderSetting("MusicVolume", audioGroup.transform, "Music Volume", 1);
        CreateSliderSetting("SFXVolume", audioGroup.transform, "SFX Volume", 2);
        
        // Graphics Group
        GameObject graphicsGroup = CreateSettingsGroup("GraphicsGroup", content, "GRAPHICS", 1);
        CreateSliderSetting("Brightness", graphicsGroup.transform, "Brightness", 0);
        CreateDropdownSetting("Quality", graphicsGroup.transform, "Quality", 1);
        CreateToggleSetting("VSync", graphicsGroup.transform, "VSync", 2);
        
        // Back Button
        GameObject backBtn = CreateMenuButton("BackButton", bg.transform, "Back", 0);
        RectTransform backRT = backBtn.GetComponent<RectTransform>();
        backRT.anchorMin = new Vector2(0.5f, 0);
        backRT.anchorMax = new Vector2(0.5f, 0);
        backRT.anchoredPosition = new Vector2(0, 30);
        backRT.sizeDelta = new Vector2(200, 50);
        
        return panel;
    }
    
    // ===== Confirm Dialog =====
    
    static GameObject CreateConfirmDialog(Transform parent)
    {
        GameObject panel = new GameObject("ConfirmDialog");
        panel.transform.SetParent(parent);
        
        RectTransform rt = panel.AddComponent<RectTransform>();
        StretchRect(rt);
        
        // Dark Overlay
        Image overlay = panel.AddComponent<Image>();
        overlay.color = new Color(0, 0, 0, 0.9f);
        
        // Background
        GameObject bg = CreateBox("Background", panel.transform, new Vector2(500, 250), Color.white);
        Image bgImage = bg.GetComponent<Image>();
        bgImage.color = new Color(0.2f, 0.2f, 0.25f, 1f);
        
        // Message Text
        GameObject msgText = CreateText("MessageText", bg.transform, "Are you sure?", 28, TextAlignmentOptions.Center);
        RectTransform msgRT = msgText.GetComponent<RectTransform>();
        msgRT.anchorMin = new Vector2(0, 0.4f);
        msgRT.anchorMax = new Vector2(1, 1);
        msgRT.offsetMin = new Vector2(30, 0);
        msgRT.offsetMax = new Vector2(-30, -30);
        
        // Button Group
        GameObject btnGroup = new GameObject("ButtonGroup");
        btnGroup.transform.SetParent(bg.transform);
        RectTransform btnGroupRT = btnGroup.AddComponent<RectTransform>();
        btnGroupRT.anchorMin = new Vector2(0.5f, 0);
        btnGroupRT.anchorMax = new Vector2(0.5f, 0);
        btnGroupRT.anchoredPosition = new Vector2(0, 50);
        btnGroupRT.sizeDelta = new Vector2(400, 60);
        
        // Yes Button
        GameObject yesBtn = CreateButton("YesButton", btnGroup.transform, "Yes", new Color(0.8f, 0.2f, 0.2f));
        RectTransform yesRT = yesBtn.GetComponent<RectTransform>();
        yesRT.anchorMin = new Vector2(0, 0.5f);
        yesRT.anchorMax = new Vector2(0, 0.5f);
        yesRT.anchoredPosition = new Vector2(90, 0);
        yesRT.sizeDelta = new Vector2(150, 50);
        
        // No Button
        GameObject noBtn = CreateButton("NoButton", btnGroup.transform, "No", new Color(0.2f, 0.6f, 0.3f));
        RectTransform noRT = noBtn.GetComponent<RectTransform>();
        noRT.anchorMin = new Vector2(1, 0.5f);
        noRT.anchorMax = new Vector2(1, 0.5f);
        noRT.anchoredPosition = new Vector2(-90, 0);
        noRT.sizeDelta = new Vector2(150, 50);
        
        return panel;
    }
    
    // ===== Helper Functions =====
    
    static GameObject CreateBox(string name, Transform parent, Vector2 size, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent);
        
        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = size;
        
        Image img = obj.AddComponent<Image>();
        img.color = color;
        
        return obj;
    }
    
    static GameObject CreateText(string name, Transform parent, string text, int fontSize, TextAlignmentOptions alignment)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent);
        
        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(200, 50);
        
        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = alignment;
        tmp.color = Color.white;
        
        return obj;
    }
    
    static GameObject CreateButton(string name, Transform parent, string label, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent);
        
        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(200, 50);
        
        Image img = obj.AddComponent<Image>();
        img.color = color;
        
        Button btn = obj.AddComponent<Button>();
        btn.targetGraphic = img;
        
        GameObject txtObj = CreateText("Text", obj.transform, label, 24, TextAlignmentOptions.Center);
        StretchRect(txtObj.GetComponent<RectTransform>());
        
        return obj;
    }
    
    static GameObject CreateMenuButton(string name, Transform parent, string label, int index)
    {
        GameObject btn = CreateButton(name, parent, label, new Color(0.25f, 0.25f, 0.35f));
        
        RectTransform rt = btn.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1);
        rt.anchorMax = new Vector2(0.5f, 1);
        rt.anchoredPosition = new Vector2(0, -70 * index - 40);
        rt.sizeDelta = new Vector2(350, 60);
        
        return btn;
    }
    
    static GameObject CreateScrollView(string name, Transform parent)
    {
        GameObject scrollView = new GameObject(name);
        scrollView.transform.SetParent(parent);
        
        RectTransform rt = scrollView.AddComponent<RectTransform>();
        
        Image img = scrollView.AddComponent<Image>();
        img.color = new Color(0.1f, 0.1f, 0.15f, 0.8f);
        
        ScrollRect scroll = scrollView.AddComponent<ScrollRect>();
        
        // Viewport
        GameObject viewport = new GameObject("Viewport");
        viewport.transform.SetParent(scrollView.transform);
        RectTransform vpRT = viewport.AddComponent<RectTransform>();
        StretchRect(vpRT);
        viewport.AddComponent<Image>().color = new Color(0, 0, 0, 0);
        viewport.AddComponent<Mask>().showMaskGraphic = false;
        
        // Content
        GameObject content = new GameObject("Content");
        content.transform.SetParent(viewport.transform);
        RectTransform contentRT = content.AddComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 1);
        contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot = new Vector2(0.5f, 1);
        contentRT.anchoredPosition = Vector2.zero;
        contentRT.sizeDelta = new Vector2(0, 800);
        
        VerticalLayoutGroup vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.childForceExpandHeight = false;
        vlg.childControlHeight = false;
        vlg.spacing = 10;
        vlg.padding = new RectOffset(20, 20, 20, 20);
        
        ContentSizeFitter csf = content.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        
        scroll.viewport = vpRT;
        scroll.content = contentRT;
        scroll.horizontal = false;
        scroll.vertical = true;
        
        return scrollView;
    }
    
    static GameObject CreateSettingsGroup(string name, Transform parent, string title, int index)
    {
        GameObject group = new GameObject(name);
        group.transform.SetParent(parent);
        
        RectTransform rt = group.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 200);
        
        VerticalLayoutGroup vlg = group.AddComponent<VerticalLayoutGroup>();
        vlg.childForceExpandHeight = false;
        vlg.childControlHeight = false;
        vlg.spacing = 10;
        vlg.padding = new RectOffset(10, 10, 10, 10);
        
        // Title
        GameObject titleObj = CreateText("Title", group.transform, title, 32, TextAlignmentOptions.Left);
        RectTransform titleRT = titleObj.GetComponent<RectTransform>();
        titleRT.sizeDelta = new Vector2(0, 40);
        TextMeshProUGUI titleTMP = titleObj.GetComponent<TextMeshProUGUI>();
        titleTMP.color = new Color(0.8f, 0.8f, 1f);
        
        LayoutElement le = group.AddComponent<LayoutElement>();
        le.preferredHeight = 200;
        
        return group;
    }
    
    static void CreateSliderSetting(string name, Transform parent, string label, int index)
    {
        GameObject container = new GameObject(name);
        container.transform.SetParent(parent);
        
        RectTransform rt = container.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 40);
        
        // Label
        GameObject labelObj = CreateText("Label", container.transform, label, 20, TextAlignmentOptions.Left);
        RectTransform labelRT = labelObj.GetComponent<RectTransform>();
        labelRT.anchorMin = new Vector2(0, 0.5f);
        labelRT.anchorMax = new Vector2(0, 0.5f);
        labelRT.anchoredPosition = new Vector2(10, 0);
        labelRT.sizeDelta = new Vector2(200, 30);
        
        // Slider
        GameObject sliderObj = new GameObject("Slider");
        sliderObj.transform.SetParent(container.transform);
        
        RectTransform sliderRT = sliderObj.AddComponent<RectTransform>();
        sliderRT.anchorMin = new Vector2(0.4f, 0.5f);
        sliderRT.anchorMax = new Vector2(1, 0.5f);
        sliderRT.anchoredPosition = new Vector2(0, 0);
        sliderRT.sizeDelta = new Vector2(0, 20);
        
        Slider slider = sliderObj.AddComponent<Slider>();
        slider.minValue = 0;
        slider.maxValue = 1;
        slider.value = 0.8f;
        
        // Background
        GameObject bg = new GameObject("Background");
        bg.transform.SetParent(sliderObj.transform);
        RectTransform bgRT = bg.AddComponent<RectTransform>();
        StretchRect(bgRT);
        Image bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0.2f, 0.2f, 0.3f);
        
        // Fill Area
        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderObj.transform);
        RectTransform fillAreaRT = fillArea.AddComponent<RectTransform>();
        fillAreaRT.anchorMin = Vector2.zero;
        fillAreaRT.anchorMax = Vector2.one;
        fillAreaRT.offsetMin = new Vector2(5, 5);
        fillAreaRT.offsetMax = new Vector2(-5, -5);
        
        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform);
        RectTransform fillRT = fill.AddComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = Vector2.one;
        Image fillImg = fill.AddComponent<Image>();
        fillImg.color = new Color(0.3f, 0.6f, 0.9f);
        
        slider.fillRect = fillRT;
        
        // Handle
        GameObject handleArea = new GameObject("Handle Slide Area");
        handleArea.transform.SetParent(sliderObj.transform);
        RectTransform handleAreaRT = handleArea.AddComponent<RectTransform>();
        StretchRect(handleAreaRT);
        
        GameObject handle = new GameObject("Handle");
        handle.transform.SetParent(handleArea.transform);
        RectTransform handleRT = handle.AddComponent<RectTransform>();
        handleRT.sizeDelta = new Vector2(20, 20);
        Image handleImg = handle.AddComponent<Image>();
        handleImg.color = Color.white;
        
        slider.handleRect = handleRT;
        slider.targetGraphic = handleImg;
        
        LayoutElement le = container.AddComponent<LayoutElement>();
        le.preferredHeight = 40;
    }
    
    static void CreateDropdownSetting(string name, Transform parent, string label, int index)
    {
        GameObject container = new GameObject(name);
        container.transform.SetParent(parent);
        
        RectTransform rt = container.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 40);
        
        // Label
        GameObject labelObj = CreateText("Label", container.transform, label, 20, TextAlignmentOptions.Left);
        RectTransform labelRT = labelObj.GetComponent<RectTransform>();
        labelRT.anchorMin = new Vector2(0, 0.5f);
        labelRT.anchorMax = new Vector2(0, 0.5f);
        labelRT.anchoredPosition = new Vector2(10, 0);
        labelRT.sizeDelta = new Vector2(200, 30);
        
        // Dropdown
        GameObject dropdownObj = new GameObject("Dropdown");
        dropdownObj.transform.SetParent(container.transform);
        
        RectTransform dropdownRT = dropdownObj.AddComponent<RectTransform>();
        dropdownRT.anchorMin = new Vector2(0.4f, 0.5f);
        dropdownRT.anchorMax = new Vector2(1, 0.5f);
        dropdownRT.anchoredPosition = new Vector2(0, 0);
        dropdownRT.sizeDelta = new Vector2(0, 35);
        
        Image dropdownImg = dropdownObj.AddComponent<Image>();
        dropdownImg.color = new Color(0.2f, 0.2f, 0.3f);
        
        TMP_Dropdown dropdown = dropdownObj.AddComponent<TMP_Dropdown>();
        dropdown.options.Add(new TMP_Dropdown.OptionData("Low"));
        dropdown.options.Add(new TMP_Dropdown.OptionData("Medium"));
        dropdown.options.Add(new TMP_Dropdown.OptionData("High"));
        dropdown.value = 2;
        
        // Label
        GameObject dropLabelObj = CreateText("Label", dropdownObj.transform, "High", 18, TextAlignmentOptions.Left);
        RectTransform dropLabelRT = dropLabelObj.GetComponent<RectTransform>();
        dropLabelRT.anchorMin = new Vector2(0, 0);
        dropLabelRT.anchorMax = new Vector2(1, 1);
        dropLabelRT.offsetMin = new Vector2(10, 0);
        dropLabelRT.offsetMax = new Vector2(-30, 0);
        dropdown.captionText = dropLabelObj.GetComponent<TextMeshProUGUI>();
        
        // Arrow
        GameObject arrow = new GameObject("Arrow");
        arrow.transform.SetParent(dropdownObj.transform);
        RectTransform arrowRT = arrow.AddComponent<RectTransform>();
        arrowRT.anchorMin = new Vector2(1, 0.5f);
        arrowRT.anchorMax = new Vector2(1, 0.5f);
        arrowRT.anchoredPosition = new Vector2(-15, 0);
        arrowRT.sizeDelta = new Vector2(20, 20);
        Image arrowImg = arrow.AddComponent<Image>();
        arrowImg.color = Color.white;
        
        LayoutElement le = container.AddComponent<LayoutElement>();
        le.preferredHeight = 40;
    }
    
    static void CreateToggleSetting(string name, Transform parent, string label, int index)
    {
        GameObject container = new GameObject(name);
        container.transform.SetParent(parent);
        
        RectTransform rt = container.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 40);
        
        // Label
        GameObject labelObj = CreateText("Label", container.transform, label, 20, TextAlignmentOptions.Left);
        RectTransform labelRT = labelObj.GetComponent<RectTransform>();
        labelRT.anchorMin = new Vector2(0, 0.5f);
        labelRT.anchorMax = new Vector2(0, 0.5f);
        labelRT.anchoredPosition = new Vector2(10, 0);
        labelRT.sizeDelta = new Vector2(200, 30);
        
        // Toggle
        GameObject toggleObj = new GameObject("Toggle");
        toggleObj.transform.SetParent(container.transform);
        
        RectTransform toggleRT = toggleObj.AddComponent<RectTransform>();
        toggleRT.anchorMin = new Vector2(1, 0.5f);
        toggleRT.anchorMax = new Vector2(1, 0.5f);
        toggleRT.anchoredPosition = new Vector2(-40, 0);
        toggleRT.sizeDelta = new Vector2(60, 30);
        
        Image toggleBg = toggleObj.AddComponent<Image>();
        toggleBg.color = new Color(0.2f, 0.2f, 0.3f);
        
        Toggle toggle = toggleObj.AddComponent<Toggle>();
        toggle.isOn = true;
        
        // Checkmark
        GameObject checkmark = new GameObject("Checkmark");
        checkmark.transform.SetParent(toggleObj.transform);
        RectTransform checkRT = checkmark.AddComponent<RectTransform>();
        StretchRect(checkRT);
        checkRT.offsetMin = new Vector2(5, 5);
        checkRT.offsetMax = new Vector2(-5, -5);
        
        Image checkImg = checkmark.AddComponent<Image>();
        checkImg.color = new Color(0.3f, 0.8f, 0.4f);
        
        toggle.graphic = checkImg;
        toggle.targetGraphic = toggleBg;
        
        LayoutElement le = container.AddComponent<LayoutElement>();
        le.preferredHeight = 40;
    }
    
    static void StretchRect(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
#endif
}
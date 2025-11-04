using UnityEngine;
using UnityEngine.UI;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;

/// <summary>
/// ساخت خودکار UI منوی اصلی
/// استفاده: Tools → Build Main Menu UI
/// </summary>
public class MenuUIBuilder : EditorWindow
{
    [MenuItem("Tools/Build Main Menu UI")]
    public static void BuildMainMenuUI()
    {
        // پاک کردن UI قبلی (اختیاری)
        GameObject oldCanvas = GameObject.Find("MainMenuCanvas");
        if (oldCanvas != null)
        {
            if (EditorUtility.DisplayDialog("Warning", 
                "MainMenuCanvas already exists. Replace it?", "Yes", "No"))
            {
                DestroyImmediate(oldCanvas);
            }
            else
            {
                return;
            }
        }

        Debug.Log("🏗️ Building Main Menu UI...");

        // 1. ساخت Canvas اصلی
        GameObject canvas = CreateCanvas("MainMenuCanvas");
        
        // 2. ساخت پنل‌ها
        GameObject welcomePanel = CreateWelcomePanel(canvas.transform);
        GameObject loginPanel = CreateLoginPanel(canvas.transform);
        GameObject registerPanel = CreateRegisterPanel(canvas.transform);
        GameObject mainHubPanel = CreateMainHubPanel(canvas.transform);
        GameObject loadingPanel = CreateLoadingPanel(canvas.transform);
        GameObject settingsPanel = CreateSettingsPanel(canvas.transform);
        GameObject messageBox = CreateMessageBox(canvas.transform);
        
        // 3. غیرفعال کردن پنل‌ها (به جز Welcome)
        welcomePanel.SetActive(true);
        loginPanel.SetActive(false);
        registerPanel.SetActive(false);
        mainHubPanel.SetActive(false);
        loadingPanel.SetActive(false);
        settingsPanel.SetActive(false);
        messageBox.SetActive(false);
        
        // 4. اضافه کردن MainMenuManager
        MainMenuManager manager = canvas.AddComponent<MainMenuManager>();
        
        // 5. تنظیم References
        SetupReferences(manager, welcomePanel, loginPanel, registerPanel, 
                       mainHubPanel, loadingPanel, settingsPanel, messageBox);
        
        Debug.Log("✅ Main Menu UI built successfully!");
        Selection.activeGameObject = canvas;
    }
    
    // ===== Canvas =====
    
    static GameObject CreateCanvas(string name)
    {
        GameObject canvasObj = new GameObject(name);
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        
        canvasObj.AddComponent<GraphicRaycaster>();
        
        // EventSystem
        if (GameObject.Find("EventSystem") == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }
        
        return canvasObj;
    }
    
    // ===== Welcome Panel =====
    
    static GameObject CreateWelcomePanel(Transform parent)
    {
        GameObject panel = CreatePanel("WelcomePanel", parent);
        
        // Background
        Image bg = panel.GetComponent<Image>();
        bg.color = new Color(0.1f, 0.1f, 0.15f, 0.95f);
        
        // Title
        GameObject title = CreateText("Title", panel.transform, "🧙 Welcome to Wizard Game", 60);
        RectTransform titleRect = title.GetComponent<RectTransform>();
        titleRect.anchoredPosition = new Vector2(0, 300);
        
        // Login Button
        GameObject loginBtn = CreateButton("LoginButton", panel.transform, "Login", 200, 60);
        RectTransform loginRect = loginBtn.GetComponent<RectTransform>();
        loginRect.anchoredPosition = new Vector2(0, 50);
        
        // Register Button
        GameObject regBtn = CreateButton("RegisterButton", panel.transform, "Register", 200, 60);
        RectTransform regRect = regBtn.GetComponent<RectTransform>();
        regRect.anchoredPosition = new Vector2(0, -50);
        
        return panel;
    }
    
    // ===== Login Panel =====
    
    static GameObject CreateLoginPanel(Transform parent)
    {
        GameObject panel = CreatePanel("LoginPanel", parent);
        
        Image bg = panel.GetComponent<Image>();
        bg.color = new Color(0.1f, 0.1f, 0.15f, 0.95f);
        
        // Title
        GameObject title = CreateText("Title", panel.transform, "Login", 50);
        RectTransform titleRect = title.GetComponent<RectTransform>();
        titleRect.anchoredPosition = new Vector2(0, 300);
        
        // Username Field
        GameObject usernameField = CreateInputField("UsernameField", panel.transform, "Username");
        RectTransform usernameRect = usernameField.GetComponent<RectTransform>();
        usernameRect.sizeDelta = new Vector2(400, 50);
        usernameRect.anchoredPosition = new Vector2(0, 150);
        
        // Password Field
        GameObject passwordField = CreateInputField("PasswordField", panel.transform, "Password");
        RectTransform passwordRect = passwordField.GetComponent<RectTransform>();
        passwordRect.sizeDelta = new Vector2(400, 50);
        passwordRect.anchoredPosition = new Vector2(0, 80);
        TMP_InputField passInput = passwordField.GetComponent<TMP_InputField>();
        passInput.contentType = TMP_InputField.ContentType.Password;
        
        // Remember Me Toggle
        GameObject rememberToggle = CreateToggle("RememberMeToggle", panel.transform, "Remember Me");
        RectTransform toggleRect = rememberToggle.GetComponent<RectTransform>();
        toggleRect.anchoredPosition = new Vector2(0, 10);
        
        // Login Button
        GameObject loginBtn = CreateButton("LoginButton", panel.transform, "Login", 200, 60);
        RectTransform loginRect = loginBtn.GetComponent<RectTransform>();
        loginRect.anchoredPosition = new Vector2(0, -80);
        
        // Back Button
        GameObject backBtn = CreateButton("BackButton", panel.transform, "Back", 150, 50);
        RectTransform backRect = backBtn.GetComponent<RectTransform>();
        backRect.anchoredPosition = new Vector2(0, -160);
        
        return panel;
    }
    
    // ===== Register Panel =====
    
    static GameObject CreateRegisterPanel(Transform parent)
    {
        GameObject panel = CreatePanel("RegisterPanel", parent);
        
        Image bg = panel.GetComponent<Image>();
        bg.color = new Color(0.1f, 0.1f, 0.15f, 0.95f);
        
        // Title
        GameObject title = CreateText("Title", panel.transform, "Register", 50);
        RectTransform titleRect = title.GetComponent<RectTransform>();
        titleRect.anchoredPosition = new Vector2(0, 320);
        
        // Username Field
        GameObject usernameField = CreateInputField("UsernameField", panel.transform, "Username");
        RectTransform usernameRect = usernameField.GetComponent<RectTransform>();
        usernameRect.sizeDelta = new Vector2(400, 50);
        usernameRect.anchoredPosition = new Vector2(0, 220);
        
        // Email Field
        GameObject emailField = CreateInputField("EmailField", panel.transform, "Email");
        RectTransform emailRect = emailField.GetComponent<RectTransform>();
        emailRect.sizeDelta = new Vector2(400, 50);
        emailRect.anchoredPosition = new Vector2(0, 150);
        
        // Password Field
        GameObject passwordField = CreateInputField("PasswordField", panel.transform, "Password");
        RectTransform passwordRect = passwordField.GetComponent<RectTransform>();
        passwordRect.sizeDelta = new Vector2(400, 50);
        passwordRect.anchoredPosition = new Vector2(0, 80);
        TMP_InputField passInput = passwordField.GetComponent<TMP_InputField>();
        passInput.contentType = TMP_InputField.ContentType.Password;
        
        // Confirm Password Field
        GameObject confirmField = CreateInputField("ConfirmPasswordField", panel.transform, "Confirm Password");
        RectTransform confirmRect = confirmField.GetComponent<RectTransform>();
        confirmRect.sizeDelta = new Vector2(400, 50);
        confirmRect.anchoredPosition = new Vector2(0, 10);
        TMP_InputField confirmInput = confirmField.GetComponent<TMP_InputField>();
        confirmInput.contentType = TMP_InputField.ContentType.Password;
        
        // Register Button
        GameObject registerBtn = CreateButton("RegisterButton", panel.transform, "Register", 200, 60);
        RectTransform registerRect = registerBtn.GetComponent<RectTransform>();
        registerRect.anchoredPosition = new Vector2(0, -90);
        
        // Back Button
        GameObject backBtn = CreateButton("BackButton", panel.transform, "Back", 150, 50);
        RectTransform backRect = backBtn.GetComponent<RectTransform>();
        backRect.anchoredPosition = new Vector2(0, -170);
        
        return panel;
    }
    
    // ===== Main Hub Panel =====
    
    static GameObject CreateMainHubPanel(Transform parent)
    {
        GameObject panel = CreatePanel("MainHubPanel", parent);
        
        Image bg = panel.GetComponent<Image>();
        bg.color = new Color(0.05f, 0.05f, 0.1f, 0.98f);
        
        // Header Panel
        GameObject header = CreatePanel("Header", panel.transform);
        RectTransform headerRect = header.GetComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0, 0.85f);
        headerRect.anchorMax = new Vector2(1, 1);
        headerRect.offsetMin = Vector2.zero;
        headerRect.offsetMax = Vector2.zero;
        Image headerBg = header.GetComponent<Image>();
        headerBg.color = new Color(0.1f, 0.1f, 0.2f, 0.8f);
        
        // Player Name
        GameObject playerName = CreateText("PlayerName", header.transform, "Player Name", 32);
        RectTransform nameRect = playerName.GetComponent<RectTransform>();
        nameRect.anchoredPosition = new Vector2(-700, -40);
        
        // Player Level
        GameObject playerLevel = CreateText("PlayerLevel", header.transform, "Level 1", 28);
        RectTransform levelRect = playerLevel.GetComponent<RectTransform>();
        levelRect.anchoredPosition = new Vector2(-700, -80);
        
        // Player Galleons
        GameObject playerGalleons = CreateText("PlayerGalleons", header.transform, "0 🪙", 28);
        RectTransform galleonsRect = playerGalleons.GetComponent<RectTransform>();
        galleonsRect.anchoredPosition = new Vector2(700, -60);
        
        // House Icon
        GameObject houseIcon = new GameObject("HouseIcon");
        houseIcon.transform.SetParent(header.transform);
        Image icon = houseIcon.AddComponent<Image>();
        RectTransform iconRect = houseIcon.GetComponent<RectTransform>();
        iconRect.sizeDelta = new Vector2(100, 100);
        iconRect.anchoredPosition = new Vector2(-850, -60);
        
        // Play Button (Large, Center)
        GameObject playBtn = CreateButton("PlayButton", panel.transform, "▶ PLAY", 300, 100);
        RectTransform playRect = playBtn.GetComponent<RectTransform>();
        playRect.anchoredPosition = new Vector2(0, 100);
        
        // Shop Button
        GameObject shopBtn = CreateButton("ShopButton", panel.transform, "🛒 Shop", 250, 70);
        RectTransform shopRect = shopBtn.GetComponent<RectTransform>();
        shopRect.anchoredPosition = new Vector2(-300, -50);
        
        // Inventory Button
        GameObject invBtn = CreateButton("InventoryButton", panel.transform, "🎒 Inventory", 250, 70);
        RectTransform invRect = invBtn.GetComponent<RectTransform>();
        invRect.anchoredPosition = new Vector2(300, -50);
        
        // Settings Button
        GameObject settingsBtn = CreateButton("SettingsButton", panel.transform, "⚙️ Settings", 250, 70);
        RectTransform settingsRect = settingsBtn.GetComponent<RectTransform>();
        settingsRect.anchoredPosition = new Vector2(-300, -150);
        
        // Logout Button
        GameObject logoutBtn = CreateButton("LogoutButton", panel.transform, "🚪 Logout", 250, 70);
        RectTransform logoutRect = logoutBtn.GetComponent<RectTransform>();
        logoutRect.anchoredPosition = new Vector2(300, -150);
        
        return panel;
    }
    
    // ===== Settings Panel =====
    
    static GameObject CreateSettingsPanel(Transform parent)
    {
        GameObject panel = CreatePanel("SettingsPanel", parent);
        
        Image bg = panel.GetComponent<Image>();
        bg.color = new Color(0.1f, 0.1f, 0.15f, 0.95f);
        
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.sizeDelta = new Vector2(800, 600);
        
        // Title
        GameObject title = CreateText("Title", panel.transform, "Settings", 45);
        RectTransform titleRect = title.GetComponent<RectTransform>();
        titleRect.anchoredPosition = new Vector2(0, 250);
        
        // Audio Section
        GameObject audioLabel = CreateText("AudioLabel", panel.transform, "Audio", 30);
        RectTransform audioLabelRect = audioLabel.GetComponent<RectTransform>();
        audioLabelRect.anchoredPosition = new Vector2(-300, 170);
        
        // Master Volume
        GameObject masterVol = CreateSlider("MasterVolumeSlider", panel.transform);
        RectTransform masterRect = masterVol.GetComponent<RectTransform>();
        masterRect.sizeDelta = new Vector2(500, 30);
        masterRect.anchoredPosition = new Vector2(0, 120);
        
        // Music Volume
        GameObject musicVol = CreateSlider("MusicVolumeSlider", panel.transform);
        RectTransform musicRect = musicVol.GetComponent<RectTransform>();
        musicRect.sizeDelta = new Vector2(500, 30);
        musicRect.anchoredPosition = new Vector2(0, 70);
        
        // SFX Volume
        GameObject sfxVol = CreateSlider("SFXVolumeSlider", panel.transform);
        RectTransform sfxRect = sfxVol.GetComponent<RectTransform>();
        sfxRect.sizeDelta = new Vector2(500, 30);
        sfxRect.anchoredPosition = new Vector2(0, 20);
        
        // Graphics Section
        GameObject graphicsLabel = CreateText("GraphicsLabel", panel.transform, "Graphics", 30);
        RectTransform graphicsLabelRect = graphicsLabel.GetComponent<RectTransform>();
        graphicsLabelRect.anchoredPosition = new Vector2(-300, -40);
        
        // Quality Dropdown
        GameObject qualityDropdown = CreateDropdown("QualityDropdown", panel.transform);
        RectTransform qualityRect = qualityDropdown.GetComponent<RectTransform>();
        qualityRect.sizeDelta = new Vector2(400, 40);
        qualityRect.anchoredPosition = new Vector2(0, -90);
        
        // VSync Toggle
        GameObject vsyncToggle = CreateToggle("VsyncToggle", panel.transform, "VSync");
        RectTransform vsyncRect = vsyncToggle.GetComponent<RectTransform>();
        vsyncRect.anchoredPosition = new Vector2(-150, -140);
        
        // Fullscreen Toggle
        GameObject fullscreenToggle = CreateToggle("FullscreenToggle", panel.transform, "Fullscreen");
        RectTransform fullscreenRect = fullscreenToggle.GetComponent<RectTransform>();
        fullscreenRect.anchoredPosition = new Vector2(150, -140);
        
        // Close Button
        GameObject closeBtn = CreateButton("CloseButton", panel.transform, "Close", 200, 50);
        RectTransform closeRect = closeBtn.GetComponent<RectTransform>();
        closeRect.anchoredPosition = new Vector2(0, -230);
        
        return panel;
    }
    
    // ===== Loading Panel =====
    
    static GameObject CreateLoadingPanel(Transform parent)
    {
        GameObject panel = CreatePanel("LoadingPanel", parent);
        
        Image bg = panel.GetComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.9f);
        
        // Loading Text
        GameObject loadingText = CreateText("LoadingText", panel.transform, "Loading...", 40);
        RectTransform textRect = loadingText.GetComponent<RectTransform>();
        textRect.anchoredPosition = new Vector2(0, 50);
        
        // Loading Progress
        GameObject progressBar = CreateSlider("LoadingProgress", panel.transform);
        RectTransform progressRect = progressBar.GetComponent<RectTransform>();
        progressRect.sizeDelta = new Vector2(600, 30);
        progressRect.anchoredPosition = new Vector2(0, -20);
        Slider slider = progressBar.GetComponent<Slider>();
        slider.interactable = false;
        
        // Loading Spinner
        GameObject spinner = new GameObject("LoadingSpinner");
        spinner.transform.SetParent(panel.transform);
        Image spinnerImg = spinner.AddComponent<Image>();
        spinnerImg.color = Color.white;
        RectTransform spinnerRect = spinner.GetComponent<RectTransform>();
        spinnerRect.sizeDelta = new Vector2(80, 80);
        spinnerRect.anchoredPosition = new Vector2(0, 150);
        
        return panel;
    }
    
    // ===== Message Box =====
    
    static GameObject CreateMessageBox(Transform parent)
    {
        GameObject panel = CreatePanel("MessageBox", parent);
        
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.sizeDelta = new Vector2(500, 250);
        
        Image bg = panel.GetComponent<Image>();
        bg.color = new Color(0.15f, 0.15f, 0.2f, 0.95f);
        
        // Message Text
        GameObject messageText = CreateText("MessageText", panel.transform, "Message", 28);
        RectTransform textRect = messageText.GetComponent<RectTransform>();
        textRect.sizeDelta = new Vector2(450, 150);
        textRect.anchoredPosition = new Vector2(0, 30);
        
        // Close Button
        GameObject closeBtn = CreateButton("CloseButton", panel.transform, "OK", 150, 50);
        RectTransform closeRect = closeBtn.GetComponent<RectTransform>();
        closeRect.anchoredPosition = new Vector2(0, -70);
        
        return panel;
    }
    
    // ===== Helper Methods =====
    
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
        rect.sizeDelta = new Vector2(600, fontSize + 20);
        
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
        
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        
        return btnObj;
    }
    
    static GameObject CreateInputField(string name, Transform parent, string placeholder)
    {
        GameObject fieldObj = new GameObject(name);
        fieldObj.transform.SetParent(parent);
        
        RectTransform rect = fieldObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(400, 50);
        
        Image img = fieldObj.AddComponent<Image>();
        img.color = new Color(0.15f, 0.15f, 0.2f, 1f);
        
        TMP_InputField input = fieldObj.AddComponent<TMP_InputField>();
        
        // Text area
        GameObject textArea = new GameObject("TextArea");
        textArea.transform.SetParent(fieldObj.transform);
        RectTransform textAreaRect = textArea.AddComponent<RectTransform>();
        textAreaRect.anchorMin = Vector2.zero;
        textAreaRect.anchorMax = Vector2.one;
        textAreaRect.offsetMin = new Vector2(10, 5);
        textAreaRect.offsetMax = new Vector2(-10, -5);
        
        // Placeholder
        GameObject placeholderObj = new GameObject("Placeholder");
        placeholderObj.transform.SetParent(textArea.transform);
        TextMeshProUGUI placeholderTmp = placeholderObj.AddComponent<TextMeshProUGUI>();
        placeholderTmp.text = placeholder;
        placeholderTmp.fontSize = 20;
        placeholderTmp.color = new Color(0.5f, 0.5f, 0.5f, 0.7f);
        RectTransform placeholderRect = placeholderObj.GetComponent<RectTransform>();
        placeholderRect.anchorMin = Vector2.zero;
        placeholderRect.anchorMax = Vector2.one;
        placeholderRect.offsetMin = Vector2.zero;
        placeholderRect.offsetMax = Vector2.zero;
        
        // Text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(textArea.transform);
        TextMeshProUGUI textTmp = textObj.AddComponent<TextMeshProUGUI>();
        textTmp.fontSize = 20;
        textTmp.color = Color.white;
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        
        input.textViewport = textAreaRect;
        input.textComponent = textTmp;
        input.placeholder = placeholderTmp;
        
        return fieldObj;
    }
    
    static GameObject CreateToggle(string name, Transform parent, string label)
    {
        GameObject toggleObj = new GameObject(name);
        toggleObj.transform.SetParent(parent);
        
        RectTransform rect = toggleObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(200, 30);
        
        Toggle toggle = toggleObj.AddComponent<Toggle>();
        
        // Background
        GameObject bg = new GameObject("Background");
        bg.transform.SetParent(toggleObj.transform);
        Image bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0.2f, 0.2f, 0.25f, 1f);
        RectTransform bgRect = bg.GetComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0, 0.5f);
        bgRect.anchorMax = new Vector2(0, 0.5f);
        bgRect.sizeDelta = new Vector2(30, 30);
        bgRect.anchoredPosition = new Vector2(15, 0);
        
        // Checkmark
        GameObject checkmark = new GameObject("Checkmark");
        checkmark.transform.SetParent(bg.transform);
        Image checkImg = checkmark.AddComponent<Image>();
        checkImg.color = Color.green;
        RectTransform checkRect = checkmark.GetComponent<RectTransform>();
        checkRect.anchorMin = Vector2.zero;
        checkRect.anchorMax = Vector2.one;
        checkRect.offsetMin = new Vector2(5, 5);
        checkRect.offsetMax = new Vector2(-5, -5);
        
        // Label
        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(toggleObj.transform);
        TextMeshProUGUI labelTmp = labelObj.AddComponent<TextMeshProUGUI>();
        labelTmp.text = label;
        labelTmp.fontSize = 20;
        labelTmp.color = Color.white;
        RectTransform labelRect = labelObj.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0, 0);
        labelRect.anchorMax = new Vector2(1, 1);
        labelRect.offsetMin = new Vector2(40, 0);
        labelRect.offsetMax = Vector2.zero;
        
        toggle.targetGraphic = bgImg;
        toggle.graphic = checkImg;
        
        return toggleObj;
    }
    
    static GameObject CreateSlider(string name, Transform parent)
    {
        GameObject sliderObj = new GameObject(name);
        sliderObj.transform.SetParent(parent);
        
        RectTransform rect = sliderObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(400, 20);
        
        Slider slider = sliderObj.AddComponent<Slider>();
        slider.minValue = 0;
        slider.maxValue = 1;
        slider.value = 1;
        
        // Background
        GameObject bg = new GameObject("Background");
        bg.transform.SetParent(sliderObj.transform);
        Image bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0.2f, 0.2f, 0.25f, 1f);
        RectTransform bgRect = bg.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        
        // Fill Area
        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderObj.transform);
        RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.offsetMin = new Vector2(10, 0);
        fillAreaRect.offsetMax = new Vector2(-10, 0);
        
        // Fill
        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform);
        Image fillImg = fill.AddComponent<Image>();
        fillImg.color = new Color(0.2f, 0.6f, 1f, 1f);
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        
        // Handle Slide Area
        GameObject handleArea = new GameObject("Handle Slide Area");
        handleArea.transform.SetParent(sliderObj.transform);
        RectTransform handleAreaRect = handleArea.AddComponent<RectTransform>();
        handleAreaRect.anchorMin = Vector2.zero;
        handleAreaRect.anchorMax = Vector2.one;
        handleAreaRect.offsetMin = new Vector2(10, 0);
        handleAreaRect.offsetMax = new Vector2(-10, 0);
        
        // Handle
        GameObject handle = new GameObject("Handle");
        handle.transform.SetParent(handleArea.transform);
        Image handleImg = handle.AddComponent<Image>();
        handleImg.color = Color.white;
        RectTransform handleRect = handle.GetComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(20, 30);
        
        slider.fillRect = fillRect;
        slider.handleRect = handleRect;
        
        return sliderObj;
    }
    
    static GameObject CreateDropdown(string name, Transform parent)
    {
        GameObject dropdownObj = new GameObject(name);
        dropdownObj.transform.SetParent(parent);
        
        RectTransform rect = dropdownObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(400, 40);
        
        Image img = dropdownObj.AddComponent<Image>();
        img.color = new Color(0.15f, 0.15f, 0.2f, 1f);
        
        TMP_Dropdown dropdown = dropdownObj.AddComponent<TMP_Dropdown>();
        dropdown.options.Add(new TMP_Dropdown.OptionData("Low"));
        dropdown.options.Add(new TMP_Dropdown.OptionData("Medium"));
        dropdown.options.Add(new TMP_Dropdown.OptionData("High"));
        dropdown.options.Add(new TMP_Dropdown.OptionData("Ultra"));
        
        // Label
        GameObject label = new GameObject("Label");
        label.transform.SetParent(dropdownObj.transform);
        TextMeshProUGUI labelTmp = label.AddComponent<TextMeshProUGUI>();
        labelTmp.text = "Quality";
        labelTmp.fontSize = 20;
        labelTmp.color = Color.white;
        RectTransform labelRect = label.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(10, 0);
        labelRect.offsetMax = new Vector2(-30, 0);
        
        dropdown.captionText = labelTmp;
        
        return dropdownObj;
    }
    
    // ===== Setup References =====
    
    static void SetupReferences(MainMenuManager manager, GameObject welcomePanel, 
        GameObject loginPanel, GameObject registerPanel, GameObject mainHubPanel,
        GameObject loadingPanel, GameObject settingsPanel, GameObject messageBox)
    {
        SerializedObject so = new SerializedObject(manager);
        
        // Main Panels
        so.FindProperty("welcomePanel").objectReferenceValue = welcomePanel;
        so.FindProperty("loginPanel").objectReferenceValue = loginPanel;
        so.FindProperty("registerPanel").objectReferenceValue = registerPanel;
        so.FindProperty("mainHubPanel").objectReferenceValue = mainHubPanel;
        so.FindProperty("loadingPanel").objectReferenceValue = loadingPanel;
        so.FindProperty("settingsPanel").objectReferenceValue = settingsPanel;
        so.FindProperty("messageBox").objectReferenceValue = messageBox;
        
        // Welcome UI
        so.FindProperty("welcomeLoginButton").objectReferenceValue = welcomePanel.transform.Find("LoginButton").GetComponent<Button>();
        so.FindProperty("welcomeRegisterButton").objectReferenceValue = welcomePanel.transform.Find("RegisterButton").GetComponent<Button>();
        so.FindProperty("welcomeTitle").objectReferenceValue = welcomePanel.transform.Find("Title").GetComponent<TextMeshProUGUI>();
        
        // Login UI
        so.FindProperty("loginUsername").objectReferenceValue = loginPanel.transform.Find("UsernameField").GetComponent<TMP_InputField>();
        so.FindProperty("loginPassword").objectReferenceValue = loginPanel.transform.Find("PasswordField").GetComponent<TMP_InputField>();
        so.FindProperty("loginButton").objectReferenceValue = loginPanel.transform.Find("LoginButton").GetComponent<Button>();
        so.FindProperty("loginBackButton").objectReferenceValue = loginPanel.transform.Find("BackButton").GetComponent<Button>();
        so.FindProperty("rememberMeToggle").objectReferenceValue = loginPanel.transform.Find("RememberMeToggle").GetComponent<Toggle>();
        
        // Register UI
        so.FindProperty("registerUsername").objectReferenceValue = registerPanel.transform.Find("UsernameField").GetComponent<TMP_InputField>();
        so.FindProperty("registerEmail").objectReferenceValue = registerPanel.transform.Find("EmailField").GetComponent<TMP_InputField>();
        so.FindProperty("registerPassword").objectReferenceValue = registerPanel.transform.Find("PasswordField").GetComponent<TMP_InputField>();
        so.FindProperty("registerConfirmPassword").objectReferenceValue = registerPanel.transform.Find("ConfirmPasswordField").GetComponent<TMP_InputField>();
        so.FindProperty("registerButton").objectReferenceValue = registerPanel.transform.Find("RegisterButton").GetComponent<Button>();
        so.FindProperty("registerBackButton").objectReferenceValue = registerPanel.transform.Find("BackButton").GetComponent<Button>();
        
        // Main Hub UI
        so.FindProperty("playButton").objectReferenceValue = mainHubPanel.transform.Find("PlayButton").GetComponent<Button>();
        so.FindProperty("shopButton").objectReferenceValue = mainHubPanel.transform.Find("ShopButton").GetComponent<Button>();
        so.FindProperty("inventoryButton").objectReferenceValue = mainHubPanel.transform.Find("InventoryButton").GetComponent<Button>();
        so.FindProperty("settingsButton").objectReferenceValue = mainHubPanel.transform.Find("SettingsButton").GetComponent<Button>();
        so.FindProperty("logoutButton").objectReferenceValue = mainHubPanel.transform.Find("LogoutButton").GetComponent<Button>();
        so.FindProperty("playerNameText").objectReferenceValue = mainHubPanel.transform.Find("Header/PlayerName").GetComponent<TextMeshProUGUI>();
        so.FindProperty("playerLevelText").objectReferenceValue = mainHubPanel.transform.Find("Header/PlayerLevel").GetComponent<TextMeshProUGUI>();
        so.FindProperty("playerGalleonsText").objectReferenceValue = mainHubPanel.transform.Find("Header/PlayerGalleons").GetComponent<TextMeshProUGUI>();
        so.FindProperty("playerHouseIcon").objectReferenceValue = mainHubPanel.transform.Find("Header/HouseIcon").GetComponent<Image>();
        
        // Loading UI
        so.FindProperty("loadingText").objectReferenceValue = loadingPanel.transform.Find("LoadingText").GetComponent<TextMeshProUGUI>();
        so.FindProperty("loadingProgress").objectReferenceValue = loadingPanel.transform.Find("LoadingProgress").GetComponent<Slider>();
        so.FindProperty("loadingSpinner").objectReferenceValue = loadingPanel.transform.Find("LoadingSpinner").GetComponent<Image>();
        
        // Settings UI
        so.FindProperty("settingsCloseButton").objectReferenceValue = settingsPanel.transform.Find("CloseButton").GetComponent<Button>();
        so.FindProperty("masterVolumeSlider").objectReferenceValue = settingsPanel.transform.Find("MasterVolumeSlider").GetComponent<Slider>();
        so.FindProperty("musicVolumeSlider").objectReferenceValue = settingsPanel.transform.Find("MusicVolumeSlider").GetComponent<Slider>();
        so.FindProperty("sfxVolumeSlider").objectReferenceValue = settingsPanel.transform.Find("SFXVolumeSlider").GetComponent<Slider>();
        so.FindProperty("qualityDropdown").objectReferenceValue = settingsPanel.transform.Find("QualityDropdown").GetComponent<TMP_Dropdown>();
        so.FindProperty("vsyncToggle").objectReferenceValue = settingsPanel.transform.Find("VsyncToggle").GetComponent<Toggle>();
        so.FindProperty("fullscreenToggle").objectReferenceValue = settingsPanel.transform.Find("FullscreenToggle").GetComponent<Toggle>();
        
        // Message Box
        so.FindProperty("messageText").objectReferenceValue = messageBox.transform.Find("MessageText").GetComponent<TextMeshProUGUI>();
        so.FindProperty("messageCloseButton").objectReferenceValue = messageBox.transform.Find("CloseButton").GetComponent<Button>();
        
        so.ApplyModifiedProperties();
        
        Debug.Log("✅ All references set!");
    }
}
#endif
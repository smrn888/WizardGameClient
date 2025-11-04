using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using System;
using System.Net.Mail;

/// <summary>
/// مدیریت منوی اصلی - لاگین، ثبت‌نام، و Main Hub
/// ✅ FIX: پاک کردن Scene قدیمی قبل از لود Scene جدید
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    [Header("Main Panels")]
    [SerializeField] private GameObject welcomePanel;
    [SerializeField] private GameObject loginPanel;
    [SerializeField] private GameObject registerPanel;
    [SerializeField] private GameObject mainHubPanel;
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private GameObject settingsPanel;
    
    [Header("Welcome Screen UI")]
    [SerializeField] private Button welcomeLoginButton;
    [SerializeField] private Button welcomeRegisterButton;
    [SerializeField] private TextMeshProUGUI welcomeTitle;
    
    [Header("Login UI")]
    [SerializeField] private TMP_InputField loginUsername;
    [SerializeField] private TMP_InputField loginPassword;
    [SerializeField] private Button loginButton;
    [SerializeField] private Button loginBackButton;
    [SerializeField] private Toggle rememberMeToggle;
    
    [Header("Register UI")]
    [SerializeField] private TMP_InputField registerUsername;
    [SerializeField] private TMP_InputField registerEmail;
    [SerializeField] private TMP_InputField registerPassword;
    [SerializeField] private TMP_InputField registerConfirmPassword;
    [SerializeField] private Button registerButton;
    [SerializeField] private Button registerBackButton;
    
    [Header("Main Hub UI")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button shopButton;
    [SerializeField] private Button inventoryButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button logoutButton;
    [SerializeField] private TextMeshProUGUI playerNameText;
    [SerializeField] private TextMeshProUGUI playerLevelText;
    [SerializeField] private TextMeshProUGUI playerGalleonsText;
    [SerializeField] private Image playerHouseIcon;
    [SerializeField] private TextMeshProUGUI hubUsernameText;
    [SerializeField] private TextMeshProUGUI hubHouseText;

    [Header("Loading UI")]
    [SerializeField] private TextMeshProUGUI loadingText;
    [SerializeField] private Slider loadingProgress;
    [SerializeField] private Image loadingSpinner;
    
    [Header("Settings UI")]
    [SerializeField] private Button settingsCloseButton;
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private TMP_Dropdown qualityDropdown;
    [SerializeField] private Toggle vsyncToggle;
    [SerializeField] private Toggle fullscreenToggle;
    
    [Header("Message Box")]
    [SerializeField] private GameObject messageBox;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private Button messageCloseButton;
    
    [Header("Scene Settings")]
    [SerializeField] private string sortingHatSceneName = "SortingHat";
    [SerializeField] private string gameSceneName = "GameScene";
    [SerializeField] private string shopSceneName = "ShopScene";
    
    [Header("House Icons")]
    [SerializeField] private Sprite gryffindorIcon;
    [SerializeField] private Sprite slytherinIcon;
    [SerializeField] private Sprite ravenclawIcon;
    [SerializeField] private Sprite hufflepuffIcon;
    
    private NetworkManager networkManager;
    private SaveManager saveManager;
    private GameSettings currentSettings;
    private bool isLoading = false;
    private Action onMessageClosed;

    void Start()
    {
        networkManager = NetworkManager.Instance;
        saveManager = SaveManager.Instance;
        
        SetupButtons();
        LoadSettings();
        CheckAutoLogin();
        
        if (networkManager != null)
        {
            networkManager.OnPlayerDataUpdated += OnPlayerDataUpdated;
        }
        
        Debug.Log("🎮 MainMenu initialized");
    }
    
    void OnDestroy()
    {
        if (networkManager != null)
        {
            networkManager.OnPlayerDataUpdated -= OnPlayerDataUpdated;
        }
    }
    
    void Update()
    {
        if (loadingPanel != null && loadingPanel.activeSelf && loadingSpinner != null)
        {
            loadingSpinner.transform.Rotate(0, 0, -180f * Time.deltaTime);
        }
        
        if (loginPanel != null && loginPanel.activeSelf && Input.GetKeyDown(KeyCode.Return))
        {
            OnLoginButtonClicked();
        }
    }
    
    void SetupButtons()
    {
        welcomeLoginButton?.onClick.AddListener(ShowLoginPanel);
        welcomeRegisterButton?.onClick.AddListener(ShowRegisterPanel);
        loginButton?.onClick.AddListener(OnLoginButtonClicked);
        loginBackButton?.onClick.AddListener(ShowWelcomePanel);
        registerButton?.onClick.AddListener(OnRegisterButtonClicked);
        registerBackButton?.onClick.AddListener(ShowWelcomePanel);
        playButton?.onClick.AddListener(OnPlayButtonClicked);
        shopButton?.onClick.AddListener(OnShopButtonClicked);
        inventoryButton?.onClick.AddListener(OnInventoryButtonClicked);
        settingsButton?.onClick.AddListener(ShowSettingsPanel);
        logoutButton?.onClick.AddListener(OnLogoutButtonClicked);
        settingsCloseButton?.onClick.AddListener(HideSettingsPanel);
        messageCloseButton?.onClick.AddListener(HideMessage);
        masterVolumeSlider?.onValueChanged.AddListener(OnMasterVolumeChanged);
        musicVolumeSlider?.onValueChanged.AddListener(OnMusicVolumeChanged);
        sfxVolumeSlider?.onValueChanged.AddListener(OnSFXVolumeChanged);
        qualityDropdown?.onValueChanged.AddListener(OnQualityChanged);
        vsyncToggle?.onValueChanged.AddListener(OnVSyncChanged);
        fullscreenToggle?.onValueChanged.AddListener(OnFullscreenChanged);
    }
    
    void CheckAutoLogin()
    {
        // ✅ CRITICAL: همیشه ابتدا authentication رو reset کن
        if (networkManager != null)
        {
            networkManager.isAuthenticated = false;
        }
        
        // ✅ اکنون صفحه Welcome رو نشون بده
        ShowWelcomePanel();
        
        Debug.Log("🔓 Auto-login disabled. Please login manually.");
    }

    // ⭐ متد جدید
    void ValidateSession()
    {
        ShowLoadingPanel("Validating session...");
        
        networkManager.LoadPlayerData(() => 
        {
            if (networkManager.localPlayerData != null)
            {
                ShowMainHub();
            }
            else
            {
                // Token منقضی شده
                networkManager.Logout();
                ShowWelcomePanel();
                ShowMessage("Session expired. Please login again.", true);
            }
        });
    }
    
    void HideAllPanels()
    {
        welcomePanel?.SetActive(false);
        loginPanel?.SetActive(false);
        registerPanel?.SetActive(false);
        mainHubPanel?.SetActive(false);
        loadingPanel?.SetActive(false);
        settingsPanel?.SetActive(false);
        HideMessage();
        isLoading = false;
    }
    
    void ShowWelcomePanel()
    {
        HideAllPanels();
        welcomePanel?.SetActive(true);
    }
    
    void ShowLoginPanel()
    {
        HideAllPanels();
        loginPanel?.SetActive(true);
    }
    
    void ShowRegisterPanel()
    {
        HideAllPanels();
        registerPanel?.SetActive(true);
    }
    
    public void ShowMainHub()
    {
        HideAllPanels();
        if (mainHubPanel != null)
        {
            mainHubPanel.SetActive(true);
            UpdateHubInfo();
        }
    }
    
    void ShowLoadingPanel(string message = "Loading...")
    {
        HideAllPanels();
        loadingPanel?.SetActive(true);
        if (loadingText != null) loadingText.text = message;
        if (loadingProgress != null) loadingProgress.value = 0;
        isLoading = true;
    }
    
    void ShowSettingsPanel()
    {
        settingsPanel?.SetActive(true);
        if (settingsPanel != null) settingsPanel.transform.SetAsLastSibling();
        ApplySettingsToUI();
    }
    
    void HideSettingsPanel()
    {
        settingsPanel?.SetActive(false);
        SaveSettings();
    }
    
    void UpdatePlayerInfo()
    {
        if (networkManager == null || networkManager.localPlayerData == null) return;
        
        PlayerData data = networkManager.localPlayerData;
        
        if (playerNameText != null)
            playerNameText.text = data.username;
        
        if (playerLevelText != null)
            playerLevelText.text = $"Level {data.xpLevel}";
        
        if (playerGalleonsText != null)
            playerGalleonsText.text = $"{data.galleons} 🪙";
        
        if (playerHouseIcon != null)
            playerHouseIcon.sprite = GetHouseIcon(data.house);
        
        if (hubUsernameText != null)
            hubUsernameText.text = data.username;
        
        if (hubHouseText != null)
            hubHouseText.text = data.house;
    }
    
    public void UpdateHubInfo()
    {
        if (networkManager == null || !networkManager.isAuthenticated || networkManager.localPlayerData == null) return;
        UpdatePlayerInfo();
    }
    
    Sprite GetHouseIcon(string house)
    {
        if (string.IsNullOrEmpty(house)) return null;
        switch (house.ToLower())
        {
            case "gryffindor": return gryffindorIcon;
            case "slytherin": return slytherinIcon;
            case "ravenclaw": return ravenclawIcon;
            case "hufflepuff": return hufflepuffIcon;
            default: return null;
        }
    }
    
    void OnPlayerDataUpdated(PlayerData data)
    {
        if (mainHubPanel != null && mainHubPanel.activeSelf) UpdateHubInfo();
    }
    
    void OnLoginButtonClicked()
    {
        if (isLoading) return;
        
        string username = loginUsername.text.Trim();
        string password = loginPassword.text;
        
        if (string.IsNullOrEmpty(username))
        {
            ShowMessage("Please enter username", true);
            return;
        }
        
        if (string.IsNullOrEmpty(password))
        {
            ShowMessage("Please enter password", true);
            return;
        }
        
        SetUIInteractable(false);
        ShowLoadingPanel("Logging in...");
        
        networkManager.Login(username, password, (success, message) =>
        {
            SetUIInteractable(true);
            isLoading = false;
            loadingPanel?.SetActive(false);
            
            if (success)
            {
                Debug.Log("✅ Login successful");
                ShowMainHub();
            }
            else
            {
                Debug.LogError($"❌ Login failed: {message}");
                ShowLoginPanel();
                ShowMessage($"Login failed: {message}", true);
            }
        });
    }
    
    void OnRegisterButtonClicked()
    {
        if (isLoading) return;
        
        string username = registerUsername.text.Trim();
        string email = registerEmail.text.Trim();
        string password = registerPassword.text;
        string confirmPassword = registerConfirmPassword.text;
        
        if (string.IsNullOrEmpty(username) || username.Length < 3)
        {
            ShowMessage("Username must be at least 3 characters", true);
            return;
        }
        
        if (!IsValidEmail(email))
        {
            ShowMessage("Please enter a valid email", true);
            return;
        }
        
        if (string.IsNullOrEmpty(password) || password.Length < 6)
        {
            ShowMessage("Password must be at least 6 characters", true);
            return;
        }
        
        if (password != confirmPassword)
        {
            ShowMessage("Passwords don't match", true);
            return;
        }
        
        SetUIInteractable(false);
        ShowLoadingPanel("Creating account...");
        
        networkManager.Register(username, email, password, (success, message) =>
        {
            SetUIInteractable(true);
            isLoading = false;
            loadingPanel?.SetActive(false);
            
            if (success)
            {
                Debug.Log("✅ Registration successful");
                LoadScene(sortingHatSceneName);
            }
            else
            {
                Debug.LogError($"❌ Registration failed: {message}");
                ShowRegisterPanel();
                ShowMessage($"Registration failed: {message}", true);
            }
        });
    }
    
    void SetUIInteractable(bool interactable)
    {
        if (loginButton != null) loginButton.interactable = interactable;
        if (registerButton != null) registerButton.interactable = interactable;
    }
    
    void OnShopButtonClicked()
    {
        Debug.Log("🛒 Shop button clicked");
        LoadScene(shopSceneName);
    }

    void OnInventoryButtonClicked()
    {
        ShowMessage("Inventory coming soon!", false);
    }
    
    void OnLogoutButtonClicked()
    {
        ShowMessage("Are you sure you want to logout?", false, () =>
        {
            networkManager?.Logout();
            ShowWelcomePanel();
        }, "Yes", "No");
    }
    
    void LoadSettings()
    {
        currentSettings = saveManager != null ? saveManager.LoadSettings() : new GameSettings();
        ApplySettings();
    }
    
    void ApplySettings()
    {
        if (currentSettings != null)
        {
            AudioListener.volume = currentSettings.masterVolume;
            QualitySettings.SetQualityLevel(currentSettings.graphicsQuality);
            QualitySettings.vSyncCount = currentSettings.enableVSync ? 1 : 0;
            Screen.fullScreen = currentSettings.fullscreen;
        }
    }
    
    void ApplySettingsToUI()
    {
        if (currentSettings == null) return;
        
        if (masterVolumeSlider != null) masterVolumeSlider.value = currentSettings.masterVolume;
        if (musicVolumeSlider != null) musicVolumeSlider.value = currentSettings.musicVolume;
        if (sfxVolumeSlider != null) sfxVolumeSlider.value = currentSettings.sfxVolume;
        if (qualityDropdown != null) qualityDropdown.value = currentSettings.graphicsQuality;
        if (vsyncToggle != null) vsyncToggle.isOn = currentSettings.enableVSync;
        if (fullscreenToggle != null) fullscreenToggle.isOn = currentSettings.fullscreen;
    }
    
    void SaveSettings()
    {
        if (saveManager != null && currentSettings != null)
        {
            saveManager.SaveSettings(currentSettings);
            ApplySettings();
        }
    }
    
    void OnMasterVolumeChanged(float value)
    {
        if (currentSettings != null)
        {
            currentSettings.masterVolume = value;
            AudioListener.volume = value;
        }
    }
    
    void OnMusicVolumeChanged(float value)
    {
        if (currentSettings != null) currentSettings.musicVolume = value;
    }
    
    void OnSFXVolumeChanged(float value)
    {
        if (currentSettings != null) currentSettings.sfxVolume = value;
    }
    
    void OnQualityChanged(int value)
    {
        if (currentSettings != null)
        {
            currentSettings.graphicsQuality = value;
            QualitySettings.SetQualityLevel(value);
        }
    }
    
    void OnVSyncChanged(bool value)
    {
        if (currentSettings != null)
        {
            currentSettings.enableVSync = value;
            QualitySettings.vSyncCount = value ? 1 : 0;
        }
    }
    
    void OnFullscreenChanged(bool value)
    {
        if (currentSettings != null)
        {
            currentSettings.fullscreen = value;
            Screen.fullScreen = value;
        }
    }

    public void ShowMessage(string message, bool isError = false, Action onConfirm = null, string confirmButtonText = "OK", string cancelButtonText = null)
    {
        if (messageBox == null) return;
        
        onMessageClosed = onConfirm;
        messageBox.SetActive(true);
        messageBox.transform.SetAsLastSibling();
        
        if (messageText != null)
        {
            messageText.text = message;
            messageText.color = isError ? Color.red : Color.white;
        }
        
        if (messageCloseButton != null)
        {
            TextMeshProUGUI buttonText = messageCloseButton.GetComponentInChildren<TextMeshProUGUI>(true);
            if (buttonText != null) buttonText.text = confirmButtonText;
            messageCloseButton.interactable = true;
        }
        
        Debug.Log($"💬 Message: {message}");
    }
    
    public void HideMessage()
    {
        messageBox?.SetActive(false);
        Action actionToExecute = onMessageClosed;
        onMessageClosed = null;
        actionToExecute?.Invoke();
    }
    
    // ✅ FIX: پاک کردن Scene قدیمی قبل از لود جدید
    void LoadScene(string sceneName)
    {
        if (!IsSceneInBuild(sceneName))
        {
            Debug.LogError($"❌ Scene '{sceneName}' not found in Build Settings!");
            ShowMessage($"Scene '{sceneName}' is not available!", true);
            return;
        }
        
        HideAllPanels();
        
        // ✅ CRITICAL: استفاده از LoadSceneMode.Single برای جایگزین کامل
        Debug.Log($"🎬 Loading scene '{sceneName}' (Single mode - will replace current scene)");
        StartCoroutine(LoadSceneWithCleanup(sceneName));
    }

    // ✅ NEW: پاک کردن Scene قدیمی و لود Scene جدید
    IEnumerator LoadSceneWithCleanup(string sceneName)
    {
        ShowLoadingPanel($"Loading {sceneName}...");
        
        // ✅ پاک کردن تمام GameObject های DontDestroyOnLoad که نباید بمانند
        CleanupUnnecessaryObjects();
        
        yield return new WaitForSeconds(0.5f);
        
        // ✅ استفاده از LoadSceneMode.Single برای جایگزینی کامل
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        asyncLoad.allowSceneActivation = false;
        
        while (!asyncLoad.isDone)
        {
            float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
            
            if (loadingProgress != null)
            {
                loadingProgress.value = progress;
            }
            
            if (loadingText != null)
            {
                loadingText.text = $"Loading... {(progress * 100):F0}%";
            }
            
            if (asyncLoad.progress >= 0.9f)
            {
                yield return new WaitForSeconds(0.5f);
                asyncLoad.allowSceneActivation = true;
            }
            
            yield return null;
        }
        
        Debug.Log($"✅ Scene '{sceneName}' loaded successfully");
    }
    
    // ✅ NEW: پاک کردن GameObject های غیر ضروری
    void CleanupUnnecessaryObjects()
    {
        // پیدا کردن تمام GameObject هایی که در DontDestroyOnLoad هستند
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        
        foreach (GameObject obj in allObjects)
        {
            // ✅ Player, Enemies, Map objects باید پاک شوند
            if (obj.scene.name == "DontDestroyOnLoad")
            {
                // اما NetworkManager, SaveManager باید بمانند
                if (obj.GetComponent<NetworkManager>() != null ||
                    obj.GetComponent<SaveManager>() != null ||
                    obj.GetComponent<ItemDatabase>() != null)
                {
                    continue; // نگه دار
                }
                
                // پلیر، دشمن، و map objects رو پاک کن
                if (obj.GetComponent<PlayerController>() != null ||
                    obj.GetComponent<EnemyController>() != null ||
                    obj.GetComponent<MapManager>() != null ||
                    obj.GetComponent<GameManager>() != null ||
                    obj.name.Contains("Player") ||
                    obj.name.Contains("Enemy") ||
                    obj.name.Contains("Map"))
                {
                    Debug.Log($"🧹 Cleaning up: {obj.name}");
                    Destroy(obj);
                }
            }
        }
    }

    bool IsSceneInBuild(string sceneName)
    {
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            string sceneNameInBuild = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            
            if (sceneNameInBuild == sceneName)
            {
                Debug.Log($"✅ Scene '{sceneName}' found at index {i}");
                return true;
            }
        }
        
        return false;
    }

    void OnPlayButtonClicked()
    {
        Debug.Log("▶️ Play button clicked");
        
        if (networkManager == null || !networkManager.isAuthenticated)
        {
            ShowMessage("Not logged in!", true);
            return;
        }
        
        if (!IsSceneInBuild(gameSceneName))
        {
            Debug.LogError($"❌ GameScene '{gameSceneName}' not in build!");
            ShowMessage("Game scene is not available. Please rebuild the game.", true);
            return;
        }
        
        Debug.Log($"🎮 Loading game scene: {gameSceneName}");
        LoadScene(gameSceneName);
    }
    
    bool IsValidEmail(string email)
    {
        try
        {
            var addr = new MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
    
    public void QuitGame()
    {
        ShowMessage("Are you sure you want to quit?", false, () =>
        {
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }, "Yes", "No");
    }
}
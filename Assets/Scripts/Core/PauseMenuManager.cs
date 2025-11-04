using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// ✅ FIXED: Resume bug - Player can move after ESC
/// ✅ FIXED: Return to Menu without logout - Goes to MainMenuHub scene
/// </summary>
public class PauseMenuManager : MonoBehaviour
{
    public static PauseMenuManager Instance { get; private set; }
    
    [Header("UI References")]
    [SerializeField] private GameObject pauseMenuPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject confirmDialog;
    
    [Header("Main Pause Menu")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button returnToMenuButton;
    [SerializeField] private Button quitGameButton;
    
    [Header("Settings")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private Slider brightnessSlider;
    [SerializeField] private TMP_Dropdown qualityDropdown;
    [SerializeField] private Toggle vsyncToggle;
    [SerializeField] private Button settingsBackButton;
    
    [Header("Confirm Dialog")]
    [SerializeField] private TextMeshProUGUI confirmText;
    [SerializeField] private Button confirmYesButton;
    [SerializeField] private Button confirmNoButton;
    
    [Header("Brightness Control")]
    [SerializeField] private Image brightnessOverlay;
    
    private bool isPauseMenuOpen = false;
    private GameSettings currentSettings;
    private SaveManager saveManager;
    private NetworkManager networkManager;
    private System.Action onConfirmAction;
    
    // ✅ NEW: Track if player input should be blocked
    private PlayerController playerController;
    
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        // یافتن یا ایجاد Brightness Overlay
        if (brightnessOverlay == null)
        {
            GameObject overlayObj = new GameObject("BrightnessOverlay");
            overlayObj.transform.SetParent(transform.root);
            Canvas canvas = overlayObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            
            GameObject imageObj = new GameObject("Overlay");
            imageObj.transform.SetParent(overlayObj.transform);
            brightnessOverlay = imageObj.AddComponent<Image>();
            brightnessOverlay.color = new Color(0, 0, 0, 0);
            
            RectTransform rt = brightnessOverlay.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
    
    void Start()
    {
        saveManager = SaveManager.Instance;
        networkManager = NetworkManager.Instance;
        
        // ✅ Find PlayerController
        playerController = FindFirstObjectByType<PlayerController>();
        
        SetupButtons();
        LoadSettings();
        
        // پنهان کردن همه پنل‌ها در شروع
        HideAllPanels();
        
        Debug.Log("✅ PauseMenuManager initialized");
    }
    
    void Update()
    {
        // بررسی دکمه Escape
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (confirmDialog != null && confirmDialog.activeSelf)
            {
                // اگر دیالوگ تایید باز است، آن را ببند
                HideConfirmDialog();
            }
            else if (settingsPanel != null && settingsPanel.activeSelf)
            {
                // اگر تنظیمات باز است، به منوی اصلی برگرد
                ShowMainPauseMenu();
            }
            else
            {
                // Toggle منوی Pause
                TogglePauseMenu();
            }
        }
    }
    
    void SetupButtons()
    {
        // Main Pause Menu
        resumeButton?.onClick.AddListener(ResumeGame);
        settingsButton?.onClick.AddListener(ShowSettings);
        returnToMenuButton?.onClick.AddListener(OnReturnToMenuClicked);
        quitGameButton?.onClick.AddListener(OnQuitGameClicked);
        
        // Settings
        settingsBackButton?.onClick.AddListener(ShowMainPauseMenu);
        masterVolumeSlider?.onValueChanged.AddListener(OnMasterVolumeChanged);
        musicVolumeSlider?.onValueChanged.AddListener(OnMusicVolumeChanged);
        sfxVolumeSlider?.onValueChanged.AddListener(OnSFXVolumeChanged);
        brightnessSlider?.onValueChanged.AddListener(OnBrightnessChanged);
        qualityDropdown?.onValueChanged.AddListener(OnQualityChanged);
        vsyncToggle?.onValueChanged.AddListener(OnVSyncChanged);
        
        // Confirm Dialog
        confirmYesButton?.onClick.AddListener(OnConfirmYes);
        confirmNoButton?.onClick.AddListener(HideConfirmDialog);
    }
    
    // ===== Pause Menu Control =====
    
    public void TogglePauseMenu()
    {
        if (isPauseMenuOpen)
        {
            ResumeGame();
        }
        else
        {
            OpenPauseMenu();
        }
    }
    
    public void OpenPauseMenu()
    {
        isPauseMenuOpen = true;
        
        // ✅ CRITICAL: Disable player input when menu is open
        if (playerController != null)
        {
            playerController.enabled = false;
        }
        
        ShowMainPauseMenu();
        
        Debug.Log("⏸️ Pause menu opened (game still running)");
    }
    
    public void ResumeGame()
    {
        isPauseMenuOpen = false;
        HideAllPanels();
        
        // ✅ CRITICAL: Re-enable player input when menu closes
        if (playerController != null)
        {
            playerController.enabled = true;
        }
        
        // ✅ Ensure timeScale is normal
        Time.timeScale = 1f;
        
        Debug.Log("▶️ Game resumed");
    }
    
    // ===== Panel Management =====
    
    void HideAllPanels()
    {
        pauseMenuPanel?.SetActive(false);
        settingsPanel?.SetActive(false);
        confirmDialog?.SetActive(false);
    }
    
    void ShowMainPauseMenu()
    {
        HideAllPanels();
        pauseMenuPanel?.SetActive(true);
        SaveSettings(); // ذخیره تنظیمات هنگام بازگشت
    }
    
    void ShowSettings()
    {
        HideAllPanels();
        settingsPanel?.SetActive(true);
        ApplySettingsToUI();
    }
    
    // ===== Settings Management =====
    
    void LoadSettings()
    {
        currentSettings = saveManager != null ? saveManager.LoadSettings() : new GameSettings();
        ApplySettings();
    }
    
    void ApplySettings()
    {
        if (currentSettings == null) return;
        
        AudioListener.volume = currentSettings.masterVolume;
        QualitySettings.SetQualityLevel(currentSettings.graphicsQuality);
        QualitySettings.vSyncCount = currentSettings.enableVSync ? 1 : 0;
        
        // اعمال Brightness
        if (brightnessOverlay != null)
        {
            float alpha = Mathf.Lerp(0.5f, 0f, currentSettings.brightness);
            brightnessOverlay.color = new Color(0, 0, 0, alpha);
        }
    }
    
    void ApplySettingsToUI()
    {
        if (currentSettings == null) return;
        
        if (masterVolumeSlider != null) masterVolumeSlider.value = currentSettings.masterVolume;
        if (musicVolumeSlider != null) musicVolumeSlider.value = currentSettings.musicVolume;
        if (sfxVolumeSlider != null) sfxVolumeSlider.value = currentSettings.sfxVolume;
        if (brightnessSlider != null) brightnessSlider.value = currentSettings.brightness;
        if (qualityDropdown != null) qualityDropdown.value = currentSettings.graphicsQuality;
        if (vsyncToggle != null) vsyncToggle.isOn = currentSettings.enableVSync;
    }
    
    void SaveSettings()
    {
        if (saveManager != null && currentSettings != null)
        {
            saveManager.SaveSettings(currentSettings);
            ApplySettings();
            Debug.Log("💾 Settings saved");
        }
    }
    
    // ===== Settings Callbacks =====
    
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
        if (currentSettings != null)
        {
            currentSettings.musicVolume = value;
            // TODO: اعمال به Music Audio Source
        }
    }
    
    void OnSFXVolumeChanged(float value)
    {
        if (currentSettings != null)
        {
            currentSettings.sfxVolume = value;
            // TODO: اعمال به SFX Audio Sources
        }
    }
    
    void OnBrightnessChanged(float value)
    {
        if (currentSettings != null)
        {
            currentSettings.brightness = value;
            
            // اعمال فوری
            if (brightnessOverlay != null)
            {
                float alpha = Mathf.Lerp(0.5f, 0f, value);
                brightnessOverlay.color = new Color(0, 0, 0, alpha);
            }
        }
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
    
    // ===== Actions =====
    
    void OnReturnToMenuClicked()
    {
        ShowConfirmDialog(
            "Are you sure you want to return to main menu?\nYour progress will be saved.",
            ReturnToMainMenu
        );
    }
    
    void OnQuitGameClicked()
    {
        ShowConfirmDialog(
            "Are you sure you want to quit?\nYour progress will be saved.",
            QuitGame
        );
    }
    
    void ReturnToMainMenu()
    {
        Debug.Log("🔙 Returning to Main Menu (WITHOUT LOGOUT)...");
        
        // ✅ Re-enable player first (to avoid issues)
        if (playerController != null)
        {
            playerController.enabled = true;
        }
        
        // ✅ Ensure timeScale is normal before scene change
        Time.timeScale = 1f;
        
        // ✅ Save game before returning
        if (networkManager != null && networkManager.isAuthenticated)
        {
            networkManager.SavePlayerData((success) =>
            {
                if (success)
                {
                    Debug.Log("✅ Game saved successfully");
                }
                else
                {
                    Debug.LogWarning("⚠️ Failed to save game");
                }
                
                // ✅ IMPORTANT: Load MainMenuHub scene (NOT MainMenu)
                // Player stays logged in
                Debug.Log("🔄 Loading MainMenuHub scene...");
                SceneManager.LoadScene("MainMenuHub");
            });
        }
        else
        {
            // If not authenticated, go to login screen
            Debug.Log("⚠️ Not authenticated, going to MainMenu");
            SceneManager.LoadScene("MainMenu");
        }
    }
    
    void QuitGame()
    {
        Debug.Log("💾 Saving game before quitting...");
        
        // ✅ Ensure timeScale is normal
        Time.timeScale = 1f;
        
        // ذخیره بازی
        if (networkManager != null && networkManager.isAuthenticated)
        {
            networkManager.SavePlayerData((success) =>
            {
                if (success)
                {
                    Debug.Log("✅ Game saved successfully");
                }
                
                // خروج از بازی
                #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
                #else
                Application.Quit();
                #endif
            });
        }
        else
        {
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }
    }
    
    // ===== Confirm Dialog =====
    
    void ShowConfirmDialog(string message, System.Action onConfirm)
    {
        if (confirmDialog == null) return;
        
        onConfirmAction = onConfirm;
        confirmDialog.SetActive(true);
        
        if (confirmText != null)
        {
            confirmText.text = message;
        }
    }
    
    void HideConfirmDialog()
    {
        confirmDialog?.SetActive(false);
        onConfirmAction = null;
    }
    
    void OnConfirmYes()
    {
        HideConfirmDialog();
        onConfirmAction?.Invoke();
    }
    
    // ===== Public API =====
    
    public bool IsPauseMenuOpen()
    {
        return isPauseMenuOpen;
    }
}
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// مدیریت صفحه Login و Register
/// ✅ FIXED: تمام خطاهای CS1501 برطرف شد + Fixed JSON serialization issue
/// </summary>
public class LoginManager : MonoBehaviour
{
    [Header("=== Panels ===")]
    [SerializeField] private GameObject loginPanel;
    [SerializeField] private GameObject registerPanel;
    [SerializeField] private GameObject playerInfoPanel;
    [SerializeField] private GameObject loadingPanel;
    
    [Header("=== Login UI ===")]
    [SerializeField] private TMP_InputField loginUsernameInput;
    [SerializeField] private TMP_InputField loginPasswordInput;
    [SerializeField] private Button loginButton;
    [SerializeField] private Button showRegisterButton;
    [SerializeField] private TextMeshProUGUI loginStatusText;
    
    [Header("=== Register UI ===")]
    [SerializeField] private TMP_InputField registerUsernameInput;
    [SerializeField] private TMP_InputField registerEmailInput;
    [SerializeField] private TMP_InputField registerPasswordInput;
    [SerializeField] private Button registerButton;
    [SerializeField] private Button backToLoginButton;
    [SerializeField] private TextMeshProUGUI registerStatusText;
    
    [Header("=== Player Info UI ===")]
    [SerializeField] private TextMeshProUGUI playerNameText;
    [SerializeField] private TextMeshProUGUI houseText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI xpText;
    [SerializeField] private Slider xpSlider;
    [SerializeField] private TextMeshProUGUI galleonsText;
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private TextMeshProUGUI horcruxesText;
    [SerializeField] private Button logoutButton;
    
    [Header("=== Test Buttons (Optional) ===")]
    [SerializeField] private Button addXPButton;
    [SerializeField] private Button addGalleonsButton;
    [SerializeField] private Button takeDamageButton;
    
    private NetworkManager networkManager;
    
    void Start()
    {
        if (!ValidateReferences())
        {
            Debug.LogError("❌ LoginManager: Critical UI references missing!");
            enabled = false;
            return;
        }
        
        networkManager = NetworkManager.Instance;
        
        if (networkManager == null)
        {
            Debug.LogError("❌ NetworkManager not found!");
            UpdateLoginStatus("Error: NetworkManager not found", Color.red);
            return;
        }
        
        SetupButtons();
        networkManager.OnPlayerDataUpdated += UpdatePlayerInfo;
        ShowLoginPanel();
        
        ConnectToServer();
    }
    
    bool ValidateReferences()
    {
        bool valid = true;
        
        if (loginPanel == null)
        {
            Debug.LogError("❌ LoginPanel not assigned!");
            valid = false;
        }
        
        if (registerPanel == null)
        {
            Debug.LogError("❌ RegisterPanel not assigned!");
            valid = false;
        }
        
        if (playerInfoPanel == null)
        {
            Debug.LogError("❌ PlayerInfoPanel not assigned!");
            valid = false;
        }
        
        if (loginUsernameInput == null)
        {
            Debug.LogError("❌ Login Username Input not assigned!");
            valid = false;
        }
        
        if (loginPasswordInput == null)
        {
            Debug.LogError("❌ Login Password Input not assigned!");
            valid = false;
        }
        
        if (loginButton == null)
        {
            Debug.LogError("❌ Login Button not assigned!");
            valid = false;
        }
        
        return valid;
    }
    
    void SetupButtons()
    {
        if (loginButton != null)
        {
            loginButton.onClick.AddListener(OnLoginClicked);
            Debug.Log("✅ Login button connected");
        }
        
        if (showRegisterButton != null)
            showRegisterButton.onClick.AddListener(ShowRegisterPanel);
        
        if (registerButton != null)
            registerButton.onClick.AddListener(OnRegisterClicked);
        
        if (backToLoginButton != null)
            backToLoginButton.onClick.AddListener(ShowLoginPanel);
        
        if (logoutButton != null)
            logoutButton.onClick.AddListener(OnLogout);
        
        // Test buttons
        if (addXPButton != null)
            addXPButton.onClick.AddListener(() => TestAddXP(100));
        
        if (addGalleonsButton != null)
            addGalleonsButton.onClick.AddListener(() => TestAddGalleons(50));
        
        if (takeDamageButton != null)
            takeDamageButton.onClick.AddListener(() => TestTakeDamage(20));
    }
    
    void ConnectToServer()
    {
        UpdateLoginStatus("🔌 Connecting to server...", Color.yellow);
            
            // 💡 کلید حل مشکل: دادن یک تابع به NetworkManager برای اجرا بعد از اتصال.
            networkManager.ConnectToServer(() => 
            {
                // این بلاک فقط زمانی اجرا می‌شود که NetworkManager کارش را تمام کرده باشد.
                
                // چک کنید که آیا NetworkManager موفق به احراز هویت شده است؟
                if (networkManager.isAuthenticated)
                {
                    UpdateLoginStatus("✅ Session restored! Entering Game...", Color.green);
                    Debug.Log("✅ Auto-Login successful based on saved session.");
                    
                    // ✅ پنل را سوییچ کن
                    Invoke(nameof(ShowPlayerInfoPanel), 0.5f);
                }
                else
                {
                    // اگر توکن منقضی شده یا وجود نداشت
                    UpdateLoginStatus("✅ Connected! Enter your credentials", Color.green);
                    Debug.Log("✅ Connected to server. Waiting for credentials.");
                }
            });
    }
    
    void OnLoginClicked()
    {
        if (loginUsernameInput == null || loginPasswordInput == null)
        {
            Debug.LogError("❌ Login inputs not assigned!");
            return;
        }
        
        string username = loginUsernameInput.text.Trim();
        string password = loginPasswordInput.text.Trim();
        
        if (string.IsNullOrEmpty(username))
        {
            UpdateLoginStatus("⚠️ Please enter username", Color.yellow);
            return;
        }
        
        if (string.IsNullOrEmpty(password))
        {
            UpdateLoginStatus("⚠️ Please enter password", Color.yellow);
            return;
        }
        
        if (password.Length < 6)
        {
            UpdateLoginStatus("⚠️ Password must be at least 6 characters", Color.yellow);
            return;
        }
        
        ShowLoading(true);
        UpdateLoginStatus("🔐 Logging in...", Color.cyan);
        
        // ✅ FIXED: Pass the object directly, not the JSON string
        networkManager.Login(username, password, (success, message) =>
        {
            ShowLoading(false);
            
            if (success)
            {
                UpdateLoginStatus($"✅ {message}", Color.green);
                Debug.Log($"✅ Login successful: {username}");
                
                if (loginPasswordInput != null)
                    loginPasswordInput.text = "";
                
                Invoke(nameof(ShowPlayerInfoPanel), 0.5f);
            }
            else
            {
                UpdateLoginStatus($"❌ {message}", Color.red);
                Debug.LogWarning($"❌ Login failed: {message}");
            }
        });
    }
    
    void OnRegisterClicked()
    {
        if (registerUsernameInput == null || registerEmailInput == null || registerPasswordInput == null)
        {
            Debug.LogError("❌ Register inputs not assigned!");
            return;
        }
        
        string username = registerUsernameInput.text.Trim();
        string email = registerEmailInput.text.Trim();
        string password = registerPasswordInput.text.Trim();
        
        if (string.IsNullOrEmpty(username))
        {
            UpdateRegisterStatus("⚠️ Please enter username", Color.yellow);
            return;
        }
        
        if (username.Length < 3)
        {
            UpdateRegisterStatus("⚠️ Username must be at least 3 characters", Color.yellow);
            return;
        }
        
        if (string.IsNullOrEmpty(email) || !email.Contains("@"))
        {
            UpdateRegisterStatus("⚠️ Please enter valid email", Color.yellow);
            return;
        }
        
        if (string.IsNullOrEmpty(password))
        {
            UpdateRegisterStatus("⚠️ Please enter password", Color.yellow);
            return;
        }
        
        if (password.Length < 6)
        {
            UpdateRegisterStatus("⚠️ Password must be at least 6 characters", Color.yellow);
            return;
        }
        
        ShowLoading(true);
        UpdateRegisterStatus("🔐 Creating account...", Color.cyan);
        
        // ✅ FIXED: Pass parameters directly
        networkManager.Register(username, email, password, (success, message) =>
        {
            ShowLoading(false);
            
            if (success)
            {
                UpdateRegisterStatus($"✅ {message}", Color.green);
                Debug.Log($"✅ Registration successful: {username}");
                
                if (loginUsernameInput != null)
                    loginUsernameInput.text = username;
                if (loginPasswordInput != null)
                    loginPasswordInput.text = password;
                
                Invoke(nameof(ShowLoginPanel), 1.5f);
            }
            else
            {
                UpdateRegisterStatus($"❌ {message}", Color.red);
                Debug.LogWarning($"❌ Registration failed: {message}");
            }
        });
    }
    
    void OnLogout()
    {
        networkManager.Logout();
        
        if (loginUsernameInput != null)
            loginUsernameInput.text = "";
        if (loginPasswordInput != null)
            loginPasswordInput.text = "";
        
        ShowLoginPanel();
        UpdateLoginStatus("👋 Logged out successfully", Color.white);
    }
    
    void UpdatePlayerInfo(PlayerData data)
    {
        if (data == null) return;
        
        if (playerNameText != null)
            playerNameText.text = $"⚡ {data.username}";
        
        if (houseText != null)
        {
            string house = string.IsNullOrEmpty(data.house) ? "Not Sorted" : data.house;
            houseText.text = $"🏠 House: {house}";
        }
        
        if (levelText != null)
            levelText.text = $"⭐ Level {data.xpLevel}";
        
        if (xpText != null)
        {
            int xpNeeded = data.xpLevel * 100;
            xpText.text = $"XP: {data.xp}/{xpNeeded}";
            
            if (xpSlider != null)
            {
                xpSlider.maxValue = xpNeeded;
                xpSlider.value = data.xp;
            }
        }
        
        if (galleonsText != null)
            galleonsText.text = $"💰 {data.galleons} Galleons";
        
        if (hpText != null)
            hpText.text = $"❤️ {data.currentHealth}/{data.maxHealth} HP";
        
        if (horcruxesText != null)
            horcruxesText.text = $"🔮 {data.horcruxes}/7 Horcruxes";
        
        Debug.Log($"📊 Player Info Updated: {data.username} | Lvl {data.xpLevel} | {data.galleons}G");
    }
    
    void ShowLoginPanel()
    {
        if (loginPanel != null)
            loginPanel.SetActive(true);
        if (registerPanel != null)
            registerPanel.SetActive(false);
        if (playerInfoPanel != null)
            playerInfoPanel.SetActive(false);
        
        Debug.Log("🔐 Showing Login Panel");
    }
    
    void ShowRegisterPanel()
    {
        if (loginPanel != null)
            loginPanel.SetActive(false);
        if (registerPanel != null)
            registerPanel.SetActive(true);
        if (playerInfoPanel != null)
            playerInfoPanel.SetActive(false);
        
        if (registerUsernameInput != null)
            registerUsernameInput.text = "";
        if (registerEmailInput != null)
            registerEmailInput.text = "";
        if (registerPasswordInput != null)
            registerPasswordInput.text = "";
        
        UpdateRegisterStatus("Fill in the form to create account", Color.white);
        
        Debug.Log("🔐 Showing Register Panel");
    }
    
    void ShowPlayerInfoPanel()
    {
        if (loginPanel != null)
            loginPanel.SetActive(false);
        if (registerPanel != null)
            registerPanel.SetActive(false);
        if (playerInfoPanel != null)
            playerInfoPanel.SetActive(true);
        
        Debug.Log("👤 Showing Player Info Panel");
    }
    
    void ShowLoading(bool show)
    {
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(show);
        }
        
        if (loginButton != null)
            loginButton.interactable = !show;
        
        if (showRegisterButton != null)
            showRegisterButton.interactable = !show;
        
        if (registerButton != null)
            registerButton.interactable = !show;
        
        if (backToLoginButton != null)
            backToLoginButton.interactable = !show;
    }
    
    void UpdateLoginStatus(string message, Color color)
    {
        if (loginStatusText != null)
        {
            loginStatusText.text = message;
            loginStatusText.color = color;
        }
        else
        {
            Debug.Log($"Login Status: {message}");
        }
    }
    
    void UpdateRegisterStatus(string message, Color color)
    {
        if (registerStatusText != null)
        {
            registerStatusText.text = message;
            registerStatusText.color = color;
        }
        else
        {
            Debug.Log($"Register Status: {message}");
        }
    }
    
    // ===== Test Functions =====
    
    void TestAddXP(int amount)
    {
        networkManager.AddXP(amount);
        Debug.Log($"✅ Added {amount} XP");
    }
    
    void TestAddGalleons(int amount)
    {
        networkManager.AddGalleons(amount);
        Debug.Log($"✅ Added {amount} Galleons");
    }
    
    void TestTakeDamage(int damage)
    {
        networkManager.TakeDamage(damage, "Test");
        Debug.Log($"💔 Took {damage} damage");
    }
    
    void OnDestroy()
    {
        if (networkManager != null)
        {
            networkManager.OnPlayerDataUpdated -= UpdatePlayerInfo;
        }
        
        if (loginButton != null)
            loginButton.onClick.RemoveAllListeners();
        if (showRegisterButton != null)
            showRegisterButton.onClick.RemoveAllListeners();
        if (registerButton != null)
            registerButton.onClick.RemoveAllListeners();
        if (backToLoginButton != null)
            backToLoginButton.onClick.RemoveAllListeners();
        if (logoutButton != null)
            logoutButton.onClick.RemoveAllListeners();
        if (addXPButton != null)
            addXPButton.onClick.RemoveAllListeners();
        if (addGalleonsButton != null)
            addGalleonsButton.onClick.RemoveAllListeners();
        if (takeDamageButton != null)
            takeDamageButton.onClick.RemoveAllListeners();
    }
}
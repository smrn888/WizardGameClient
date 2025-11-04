using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI Manager برای تست اتصال به Backend
/// </summary>
public class UITestManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject loginPanel;
    [SerializeField] private GameObject registerPanel;
    [SerializeField] private GameObject playerInfoPanel;
    [SerializeField] private GameObject loadingPanel;

    [Header("Login Panel")]
    [SerializeField] private TMP_InputField loginUsername;
    [SerializeField] private TMP_InputField loginPassword;
    [SerializeField] private Button loginButton;
    [SerializeField] private Button showRegisterButton;
    [SerializeField] private TextMeshProUGUI loginStatusText;

    [Header("Register Panel")]
    [SerializeField] private TMP_InputField registerUsername;
    [SerializeField] private TMP_InputField registerEmail;
    [SerializeField] private TMP_InputField registerPassword;
    [SerializeField] private Button registerButton;
    [SerializeField] private Button showLoginButton;
    [SerializeField] private TextMeshProUGUI registerStatusText;

    [Header("Player Info Panel")]
    [SerializeField] private TextMeshProUGUI playerNameText;
    [SerializeField] private TextMeshProUGUI playerHouseText;
    [SerializeField] private TextMeshProUGUI playerLevelText;
    [SerializeField] private TextMeshProUGUI playerXPText;
    [SerializeField] private TextMeshProUGUI playerGalleonsText;
    [SerializeField] private TextMeshProUGUI playerHPText;
    [SerializeField] private TextMeshProUGUI playerHorcruxesText;
    [SerializeField] private Slider playerXPBar;
    [SerializeField] private Slider playerHPBar;
    [SerializeField] private Button logoutButton;
    [SerializeField] private Button addXPButton;
    [SerializeField] private Button addGalleonButton;
    [SerializeField] private Button takeDamageButton;

    [Header("Connection Status")]
    [SerializeField] private TextMeshProUGUI connectionStatusText;
    [SerializeField] private TextMeshProUGUI pingText;
    [SerializeField] private Image connectionIndicator;
    [SerializeField] private Color connectedColor = Color.green;
    [SerializeField] private Color disconnectedColor = Color.red;

    private NetworkManager networkManager;

    void Start()
    {
        // دریافت NetworkManager
        networkManager = NetworkManager.Instance;

        if (networkManager == null)
        {
            Debug.LogError("❌ NetworkManager not found! Creating one...");
            GameObject nm = new GameObject("NetworkManager");
            networkManager = nm.AddComponent<NetworkManager>();
        }

        // تنظیم Event Listeners
        SetupButtons();
        SetupNetworkEvents();

        // اتصال به سرور
        ConnectToServer();

        // نمایش پنل اولیه
        if (networkManager.isAuthenticated)
        {
            ShowPlayerInfoPanel();
        }
        else
        {
            ShowLoginPanel();
        }
    }

    void Update()
    {
        // آپدیت وضعیت اتصال
        UpdateConnectionStatus();
    }

    // ===== Setup =====

    private void SetupButtons()
    {
        // Login Panel
        loginButton.onClick.AddListener(OnLoginClicked);
        showRegisterButton.onClick.AddListener(ShowRegisterPanel);

        // Register Panel
        registerButton.onClick.AddListener(OnRegisterClicked);
        showLoginButton.onClick.AddListener(ShowLoginPanel);

        // Player Info Panel
        logoutButton.onClick.AddListener(OnLogoutClicked);
        addXPButton.onClick.AddListener(() => OnAddXPClicked(100));
        addGalleonButton.onClick.AddListener(() => OnAddGalleonClicked(50));
        takeDamageButton.onClick.AddListener(() => OnTakeDamageClicked(30));
    }

    private void SetupNetworkEvents()
    {
        networkManager.OnConnected += OnServerConnected;
        networkManager.OnDisconnected += OnServerDisconnected;
        networkManager.OnPlayerDataUpdated += OnPlayerDataUpdated;
        networkManager.OnError += OnNetworkError;
    }

    // ===== Connection =====

    private void ConnectToServer()
    {
        ShowLoadingPanel("Connecting to server...");

        // ✅ FIXED: حذف parameter
        networkManager.ConnectToServer(() =>
        {
            HideLoadingPanel();
            Debug.Log("✅ Connected to server");
            UpdateStatusText(connectionStatusText, "Connected", Color.green);
        });
    }

    // ===== Login =====

    private void OnLoginClicked()
    {
        string username = loginUsername.text.Trim();
        string password = loginPassword.text.Trim();

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            UpdateStatusText(loginStatusText, "Please fill all fields", Color.red);
            return;
        }

        ShowLoadingPanel("Logging in...");
        UpdateStatusText(loginStatusText, "Logging in...", Color.yellow);

        networkManager.Login(username, password, (success, message) =>
        {
            HideLoadingPanel();

            if (success)
            {
                Debug.Log("✅ Login successful");
                UpdateStatusText(loginStatusText, "Login successful!", Color.green);
                ShowPlayerInfoPanel();
            }
            else
            {
                Debug.LogError($"❌ Login failed: {message}");
                UpdateStatusText(loginStatusText, $"Login failed: {message}", Color.red);
            }
        });
    }

    // ===== Register =====

    private void OnRegisterClicked()
    {
        string username = registerUsername.text.Trim();
        string email = registerEmail.text.Trim();
        string password = registerPassword.text.Trim();

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            UpdateStatusText(registerStatusText, "Please fill all fields", Color.red);
            return;
        }

        ShowLoadingPanel("Registering...");
        UpdateStatusText(registerStatusText, "Registering...", Color.yellow);

        // ✅ FIXED: اضافه کردن email و callback
        networkManager.Register(username, email, password, (success, message) =>
        {
            HideLoadingPanel();

            if (success)
            {
                Debug.Log("✅ Registration successful");
                UpdateStatusText(registerStatusText, "Registration successful!", Color.green);
                ShowPlayerInfoPanel();
            }
            else
            {
                Debug.LogError($"❌ Registration failed: {message}");
                UpdateStatusText(registerStatusText, $"Registration failed: {message}", Color.red);
            }
        });
    }

    // ===== Logout =====

    private void OnLogoutClicked()
    {
        networkManager.Logout();
        ShowLoginPanel();
        Debug.Log("✅ Logged out");
    }

    // ===== Player Actions =====

    private void OnAddXPClicked(int amount)
    {
        networkManager.AddXP(amount, (success) =>
        {
            if (success)
            {
                Debug.Log($"✅ Added {amount} XP");
            }
        });
    }

    private void OnAddGalleonClicked(int amount)
    {
        networkManager.AddGalleons(amount, (success) =>
        {
            if (success)
            {
                Debug.Log($"✅ Added {amount} Galleons");
            }
        });
    }

    private void OnTakeDamageClicked(int damage)
    {
        // ✅ FIXED: int damage, string source, Action<bool> callback
        networkManager.TakeDamage(damage, "Test", (success) =>
        {
            if (success)
            {
                Debug.Log($"💔 Took {damage} damage");
                if (networkManager.localPlayerData.currentHealth <= 0)
                {
                    Debug.Log("💀 Player died!");
                    UpdateStatusText(playerNameText, "YOU DIED!", Color.red);
                }
            }
        });
    }

    // ===== Network Events =====

    private void OnServerConnected()
    {
        UpdateStatusText(connectionStatusText, "Connected", Color.green);
        if (connectionIndicator != null)
        {
            connectionIndicator.color = connectedColor;
        }
    }

    private void OnServerDisconnected()
    {
        UpdateStatusText(connectionStatusText, "Disconnected", Color.red);
        if (connectionIndicator != null)
        {
            connectionIndicator.color = disconnectedColor;
        }
    }

    private void OnPlayerDataUpdated(PlayerData playerData)
    {
        UpdatePlayerInfoUI(playerData);
    }

    private void OnNetworkError(string error)
    {
        Debug.LogError($"Network Error: {error}");
    }

    // ===== UI Updates =====

    private void UpdatePlayerInfoUI(PlayerData playerData)
    {
        if (playerData == null) return;

        // Basic Info
        playerNameText.text = $"Player: {playerData.username}";
        playerHouseText.text = $"House: {(string.IsNullOrEmpty(playerData.house) ? "Not Sorted" : playerData.house)}";
        playerLevelText.text = $"Level: {playerData.xpLevel}";
        playerXPText.text = $"XP: {playerData.xp}";
        playerGalleonsText.text = $"Galleons: {playerData.galleons}";
        playerHPText.text = $"HP: {playerData.currentHealth:F0}/{playerData.maxHealth:F0}";
        playerHorcruxesText.text = $"Horcruxes: {playerData.horcruxes}";

        // XP Bar
        if (playerXPBar != null)
        {
            float xpProgress = playerData.xpProgress / 5f; // 5 segments per level
            playerXPBar.value = xpProgress;
        }

        // HP Bar
        if (playerHPBar != null)
        {
            playerHPBar.value = playerData.currentHealth / playerData.maxHealth;
        }

        Debug.Log($"📊 UI Updated: {playerData.username} | Level {playerData.xpLevel} | {playerData.galleons} Galleons");
    }

    private void UpdateConnectionStatus()
    {
        if (networkManager == null) return;

        // Update ping
        if (pingText != null)
        {
            if (networkManager.isConnected)
            {
                pingText.text = $"Ping: {networkManager.currentPing:F0}ms";
            }
            else
            {
                pingText.text = "Ping: --";
            }
        }

        // Update connection indicator
        if (connectionIndicator != null)
        {
            connectionIndicator.color = networkManager.isConnected ? connectedColor : disconnectedColor;
        }
    }

    // ===== Panel Management =====

    private void ShowLoginPanel()
    {
        HideAllPanels();
        if (loginPanel != null) loginPanel.SetActive(true);
        
        // پاک کردن فیلدها
        if (loginUsername != null) loginUsername.text = "";
        if (loginPassword != null) loginPassword.text = "";
        if (loginStatusText != null) loginStatusText.text = "";
    }

    private void ShowRegisterPanel()
    {
        HideAllPanels();
        if (registerPanel != null) registerPanel.SetActive(true);
        
        // پاک کردن فیلدها
        if (registerUsername != null) registerUsername.text = "";
        if (registerEmail != null) registerEmail.text = "";
        if (registerPassword != null) registerPassword.text = "";
        if (registerStatusText != null) registerStatusText.text = "";
    }

    private void ShowPlayerInfoPanel()
    {
        HideAllPanels();
        if (playerInfoPanel != null) playerInfoPanel.SetActive(true);
        
        // بارگذاری داده‌های بازیکن
        if (networkManager.localPlayerData != null)
        {
            UpdatePlayerInfoUI(networkManager.localPlayerData);
        }
    }

    private void ShowLoadingPanel(string message)
    {
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(true);
            
            // اگه متن loading داری، آپدیت کن
            TextMeshProUGUI loadingText = loadingPanel.GetComponentInChildren<TextMeshProUGUI>();
            if (loadingText != null)
            {
                loadingText.text = message;
            }
        }
    }

    private void HideLoadingPanel()
    {
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(false);
        }
    }

    private void HideAllPanels()
    {
        if (loginPanel != null) loginPanel.SetActive(false);
        if (registerPanel != null) registerPanel.SetActive(false);
        if (playerInfoPanel != null) playerInfoPanel.SetActive(false);
        if (loadingPanel != null) loadingPanel.SetActive(false);
    }

    // ===== Helper Methods =====

    private void UpdateStatusText(TextMeshProUGUI textComponent, string message, Color color)
    {
        if (textComponent != null)
        {
            textComponent.text = message;
            textComponent.color = color;
        }
    }

    void OnDestroy()
    {
        // پاک کردن Event Listeners
        if (networkManager != null)
        {
            networkManager.OnConnected -= OnServerConnected;
            networkManager.OnDisconnected -= OnServerDisconnected;
            networkManager.OnPlayerDataUpdated -= OnPlayerDataUpdated;
            networkManager.OnError -= OnNetworkError;
        }
    }
}
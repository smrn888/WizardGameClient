using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 🎯 مدیریت صحنه‌ی فروشگاه
/// مسئول راه‌اندازی، باز/بستن ShopUI، و هماهنگی بین ShopManager و UI.
/// </summary>
public class ShopSceneManager : MonoBehaviour
{
    [Header("🛒 === SHOP UI ===")]
    [Tooltip("ارجاع به ShopUI در صحنه (از Inspector بکشید یا خودش Auto-Find می‌کند)")]
    [SerializeField] private ShopUI shopUI;

    [Header("🎮 === PLAYER INFO UI ===")]
    [SerializeField] private TextMeshProUGUI playerNameText;
    [SerializeField] private TextMeshProUGUI galleonsText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private Button closeButton;

    [Header("🔗 === REFERENCES ===")]
    private NetworkManager networkManager;
    private ShopManager shopManager;

    [Header("⚙️ === SETTINGS ===")]
    [SerializeField] private bool openShopOnStart = true;
    [SerializeField] private bool showDebugLogs = true;

    void Awake()
    {
        DebugLog("ShopSceneManager Awake() called.");
        networkManager = NetworkManager.Instance;
        shopManager = ShopManager.Instance;
    }

    void Start()
    {
        DebugLog("🛒 Shop Scene Started");

        // پیدا کردن ShopUI در صحنه در صورت نبود ارجاع در Inspector
        if (shopUI == null)
        {
            shopUI = FindObjectOfType<ShopUI>();
            if (shopUI == null)
            {
                Debug.LogError("❌ ShopUI component not found in the scene!");
                return;
            }
        }

        // اتصال دکمه‌ها
        if (closeButton != null)
            closeButton.onClick.AddListener(CloseShop);

        // نمایش نام و اطلاعات بازیکن
        UpdatePlayerInfo();

        // باز کردن خودکار فروشگاه در شروع
        if (openShopOnStart)
            OpenShop();
    }

    // === OPEN SHOP ===
    public void OpenShop()
    {
        if (shopUI != null)
        {
            shopUI.Show(); // اجرای انیمیشن Fade In از ShopUI
            DebugLog("✅ Shop opened automatically with Fade In animation.");
        }
        else
        {
            Debug.LogError("❌ ShopUI reference is missing in ShopSceneManager!");
        }
    }

    // === CLOSE SHOP ===
    public void CloseShop()
    {
        if (shopUI != null)
        {
            shopUI.Hide(); // اجرای Fade Out
            DebugLog("🛑 Shop closed via Close button.");
        }
        else
        {
            Debug.LogWarning("⚠️ Tried to close shop but ShopUI was null!");
        }
    }

    // === UPDATE PLAYER INFO ===
    public void UpdatePlayerInfo()
    {
        if (networkManager == null || networkManager.localPlayerData == null)
        {
            Debug.LogWarning("⚠️ NetworkManager or PlayerData is missing!");
            return;
        }

        PlayerData data = networkManager.localPlayerData;

        if (playerNameText != null)
            playerNameText.text = data.username;

        if (galleonsText != null)
            galleonsText.text = $"💰 {data.galleons}";

        if (levelText != null)
            levelText.text = $"⭐ Level {data.xpLevel}";

        DebugLog($"👤 Player Info Updated → {data.username}, {data.galleons} Galleons, Level {data.xpLevel}");
    }

    // === RELOAD SHOP ITEMS ===
    public void RefreshShopItems()
    {
        if (shopUI != null)
        {
            shopUI.Show(); // می‌تونی این رو تغییر بدی به shopUI.RefreshItems() اگر متد جدا داری
            DebugLog("♻️ Shop items refreshed.");
        }
    }

    // === DEBUG LOG ===
    private void DebugLog(string message)
    {
        if (showDebugLogs)
            Debug.Log($"[ShopSceneManager] {message}");
    }
}

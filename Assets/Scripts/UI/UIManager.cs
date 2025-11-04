using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// مدیریت رابط کاربری در بازی
/// نمایش HP، XP، پول، منو، و غیره
/// </summary>
public class UIManager : MonoBehaviour
{
    [Header("HUD Elements")]
    [SerializeField] private Slider healthBar;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private Slider xpBar;
    [SerializeField] private TextMeshProUGUI xpLevelText;
    [SerializeField] private TextMeshProUGUI galleonsText;
    [SerializeField] private TextMeshProUGUI horcruxesText;
    [SerializeField] private TextMeshProUGUI zoneNameText;
    
    [Header("Player Info")]
    [SerializeField] private TextMeshProUGUI playerNameText;
    [SerializeField] private TextMeshProUGUI houseText;
    [SerializeField] private Image houseIcon;
    
    [Header("Spell Cooldowns")]
    [SerializeField] private Image[] spellIcons;
    [SerializeField] private Image[] spellCooldownOverlays;
    [SerializeField] private TextMeshProUGUI[] spellCooldownTexts;
    
    [Header("Notifications")]
    [SerializeField] private GameObject notificationPanel;
    [SerializeField] private TextMeshProUGUI notificationText;
    [SerializeField] private Image notificationIcon;
    [SerializeField] private float notificationDuration = 3f;
    
    [Header("Combat Text")]
    [SerializeField] private GameObject combatTextPrefab;
    [SerializeField] private Transform combatTextParent;
    
    [Header("Minimap")]
    [SerializeField] private RawImage minimapImage;
    [SerializeField] private RectTransform minimapPlayerIcon;
    
    [Header("Pause Menu")]
    [SerializeField] private GameObject pauseMenuPanel;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button quitButton;
    
    [Header("Level Up")]
    [SerializeField] private GameObject levelUpPanel;
    [SerializeField] private TextMeshProUGUI levelUpText;
    [SerializeField] private TextMeshProUGUI rewardsText;
    
    [Header("Death Screen")]
    [SerializeField] private GameObject deathPanel;
    [SerializeField] private TextMeshProUGUI deathMessageText;
    [SerializeField] private Button respawnButton;
    [SerializeField] private TextMeshProUGUI livesRemainingText;
    
    // References
    private PlayerController player;
    private NetworkManager networkManager;
    private MapManager mapManager;
    
    // State
    private bool isPaused = false;
    private Queue<string> notificationQueue = new Queue<string>();
    private bool isShowingNotification = false;
    
    // Singleton
    public static UIManager Instance { get; private set; }
    
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    
    void Start()
    {
        // Get references
        networkManager = NetworkManager.Instance;
        mapManager = FindObjectOfType<MapManager>();
        
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.GetComponent<PlayerController>();
        }
        
        // Setup buttons
        SetupButtons();
        
        // Initial update
        UpdateUI();
        
        // Hide panels
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        if (levelUpPanel != null) levelUpPanel.SetActive(false);
        if (deathPanel != null) deathPanel.SetActive(false);
        if (notificationPanel != null) notificationPanel.SetActive(false);
        
        Debug.Log("✅ UIManager initialized");
    }
    
    void Update()
    {
        // Update HUD every frame
        UpdateHUD();
        
        // Update spell cooldowns
        UpdateSpellCooldowns();
        
        // Update minimap
        UpdateMinimap();
        
        // Handle pause menu
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePauseMenu();
        }
        
        // Process notification queue
        if (!isShowingNotification && notificationQueue.Count > 0)
        {
            ShowNextNotification();
        }
    }
    
    void SetupButtons()
    {
        resumeButton?.onClick.AddListener(ResumeGame);
        settingsButton?.onClick.AddListener(OpenSettings);
        quitButton?.onClick.AddListener(QuitToMainMenu);
        respawnButton?.onClick.AddListener(OnRespawnClicked);
    }
    
    // ===== HUD Updates =====
    
    void UpdateUI()
    {
        if (networkManager == null || networkManager.localPlayerData == null) return;
        
        PlayerData data = networkManager.localPlayerData;
        
        // Player info
        if (playerNameText != null)
            playerNameText.text = data.username;
        
        if (houseText != null)
            houseText.text = data.house;
        
        // Stats
        if (galleonsText != null)
            galleonsText.text = data.galleons.ToString();
        
        if (horcruxesText != null)
            horcruxesText.text = $"♥ {data.horcruxes}";
        
        if (xpLevelText != null)
            xpLevelText.text = $"Level {data.xpLevel}";
        
        UpdateHealthBar();
        UpdateXPBar();
    }
    
    void UpdateHUD()
    {
        if (player == null || networkManager == null) return;
        
        UpdateHealthBar();
        UpdateZoneName();
    }
    
    void UpdateHealthBar()
    {
        if (player == null) return;
        
        int currentHP = player.GetCurrentHealth();
        int maxHP = player.GetMaxHealth();
        
        if (healthBar != null)
        {
            healthBar.maxValue = maxHP;
            healthBar.value = currentHP;
        }
        
        if (healthText != null)
        {
            healthText.text = $"{currentHP} / {maxHP}";
        }
    }
    
    void UpdateXPBar()
    {
        if (networkManager == null || networkManager.localPlayerData == null) return;
        
        PlayerData data = networkManager.localPlayerData;
        
        if (xpBar != null)
        {
            xpBar.maxValue = 5;
            xpBar.value = data.xpProgress;
        }
        
        if (xpLevelText != null)
        {
            xpLevelText.text = $"Level {data.xpLevel} ({data.xpProgress}/5)";
        }
    }
    
    void UpdateZoneName()
    {
        if (mapManager == null || player == null) return;
        
        string zoneName = mapManager.GetCurrentZoneName(player.transform.position);
        
        if (zoneNameText != null && zoneNameText.text != zoneName)
        {
            zoneNameText.text = zoneName;
            
            // FIXED: جایگزین LeanTween با Coroutine
            StartCoroutine(AnimateZoneNameScale());
        }
    }
    
    // FIXED: انیمیشن بدون LeanTween
    IEnumerator AnimateZoneNameScale()
    {
        if (zoneNameText == null) yield break;
        
        float duration = 0.3f;
        float elapsed = 0f;
        Vector3 startScale = Vector3.one * 1.2f;
        Vector3 targetScale = Vector3.one;
        
        zoneNameText.transform.localScale = startScale;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            // Ease out back
            float overshoot = 1.70158f;
            t = t - 1;
            float easeValue = t * t * ((overshoot + 1) * t + overshoot) + 1;
            
            zoneNameText.transform.localScale = Vector3.Lerp(startScale, targetScale, easeValue);
            yield return null;
        }
        
        zoneNameText.transform.localScale = targetScale;
    }
    
    void UpdateSpellCooldowns()
    {
        // TODO: Implement spell cooldown display
    }
    
    void UpdateMinimap()
    {
        // TODO: Implement minimap update
    }
    
    // ===== Notifications =====
    
    public void ShowNotification(string message, Sprite icon = null)
    {
        notificationQueue.Enqueue(message);
    }
    
    void ShowNextNotification()
    {
        if (notificationQueue.Count == 0) return;
        
        string message = notificationQueue.Dequeue();
        StartCoroutine(ShowNotificationCoroutine(message));
    }
    
    IEnumerator ShowNotificationCoroutine(string message)
    {
        isShowingNotification = true;
        
        if (notificationPanel != null)
        {
            notificationPanel.SetActive(true);
            
            if (notificationText != null)
            {
                notificationText.text = message;
            }
            
            // FIXED: Slide in animation بدون LeanTween
            RectTransform rect = notificationPanel.GetComponent<RectTransform>();
            Vector2 startPos = rect.anchoredPosition;
            Vector2 targetPos = startPos + Vector2.down * 100f;
            
            yield return StartCoroutine(AnimateRectTransform(rect, rect.anchoredPosition, targetPos, 0.3f));
            
            yield return new WaitForSeconds(notificationDuration);
            
            // Slide out
            yield return StartCoroutine(AnimateRectTransform(rect, rect.anchoredPosition, startPos, 0.3f));
            
            notificationPanel.SetActive(false);
        }
        
        isShowingNotification = false;
    }
    
    // FIXED: انیمیشن RectTransform بدون LeanTween
    IEnumerator AnimateRectTransform(RectTransform rect, Vector2 start, Vector2 end, float duration)
    {
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            rect.anchoredPosition = Vector2.Lerp(start, end, t);
            yield return null;
        }
        
        rect.anchoredPosition = end;
    }
    
    // ===== Combat Text =====
    
    public void ShowCombatText(Vector3 worldPosition, string text, Color color)
    {
        if (combatTextPrefab == null) return;
        
        Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPosition);
        
        GameObject textObj = Instantiate(combatTextPrefab, combatTextParent);
        RectTransform rect = textObj.GetComponent<RectTransform>();
        rect.position = screenPos;
        
        TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
        if (tmp != null)
        {
            tmp.text = text;
            tmp.color = color;
        }
        
        StartCoroutine(AnimateCombatText(textObj));
    }
    
    IEnumerator AnimateCombatText(GameObject textObj)
    {
        RectTransform rect = textObj.GetComponent<RectTransform>();
        TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
        
        Vector3 startPos = rect.position;
        Vector3 endPos = startPos + Vector3.up * 50f;
        Color startColor = tmp.color;
        
        float duration = 1f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            rect.position = Vector3.Lerp(startPos, endPos, t);
            
            Color c = startColor;
            c.a = 1f - t;
            tmp.color = c;
            
            yield return null;
        }
        
        Destroy(textObj);
    }
    
    // ===== Level Up =====
    
    public void ShowLevelUp(int newLevel, int galleonsReward)
    {
        if (levelUpPanel == null) return;
        
        levelUpPanel.SetActive(true);
        
        if (levelUpText != null)
        {
            levelUpText.text = $"LEVEL {newLevel}!";
        }
        
        if (rewardsText != null)
        {
            string rewards = $"+{galleonsReward} Galleons\n";
            
            if (newLevel % 10 == 0)
            {
                rewards += "+1 Horcrux\n+50 Max HP";
            }
            
            rewardsText.text = rewards;
        }
        
        StartCoroutine(HideLevelUpPanelDelayed(3f));
    }
    
    IEnumerator HideLevelUpPanelDelayed(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (levelUpPanel != null)
        {
            levelUpPanel.SetActive(false);
        }
    }
    
    // ===== Death Screen =====
    
    public void ShowDeathScreen(int livesRemaining)
    {
        if (deathPanel == null) return;
        
        deathPanel.SetActive(true);
        
        if (livesRemaining > 0)
        {
            if (deathMessageText != null)
            {
                deathMessageText.text = "You have been defeated!";
            }
            
            if (livesRemainingText != null)
            {
                livesRemainingText.text = $"Lives remaining: {livesRemaining}";
            }
            
            if (respawnButton != null)
            {
                respawnButton.gameObject.SetActive(true);
            }
        }
        else
        {
            if (deathMessageText != null)
            {
                deathMessageText.text = "GAME OVER\n\nAll progress has been lost.";
            }
            
            if (respawnButton != null)
            {
                respawnButton.gameObject.SetActive(false);
            }
        }
    }
    
    public void HideDeathScreen()
    {
        if (deathPanel != null)
        {
            deathPanel.SetActive(false);
        }
    }
    
    void OnRespawnClicked()
    {
        HideDeathScreen();
    }
    
    // ===== Pause Menu =====
    
    void TogglePauseMenu()
    {
        isPaused = !isPaused;
        
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(isPaused);
        }
        
        Time.timeScale = isPaused ? 0f : 1f;
    }
    
    void ResumeGame()
    {
        isPaused = false;
        
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
        }
        
        Time.timeScale = 1f;
    }
    
    void OpenSettings()
    {
        Debug.Log("Settings clicked");
    }
    
    void QuitToMainMenu()
    {
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }
    
    // ===== Public API =====
    
    public void UpdateGalleons(int amount)
    {
        if (galleonsText != null)
        {
            galleonsText.text = amount.ToString();
        }
    }
    
    public void UpdateHorcruxes(int amount)
    {
        if (horcruxesText != null)
        {
            horcruxesText.text = $"♥ {amount}";
        }
    }
    
    public void ShowDamage(Vector3 worldPos, int damage, bool isCritical)
    {
        Color color = isCritical ? Color.red : Color.white;
        string text = isCritical ? $"{damage}!" : damage.ToString();
        ShowCombatText(worldPos, text, color);
    }
    
    public void ShowHealing(Vector3 worldPos, int amount)
    {
        ShowCombatText(worldPos, $"+{amount}", Color.green);
    }
    
    public void ShowXPGain(Vector3 worldPos, int amount)
    {
        ShowCombatText(worldPos, $"+{amount} XP", Color.yellow);
    }
}
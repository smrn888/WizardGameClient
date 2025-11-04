using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

/// <summary>
/// صفحه انتخاب کاراکتر بعد از Sorting Hat
/// بازیکن می‌تواند ظاهر و تجهیزات اولیه را انتخاب کند
/// </summary>
public class CharacterSelectionUI : MonoBehaviour
{
    [Header("Character Preview")]
    [SerializeField] private Image characterPreview;
    [SerializeField] private TextMeshProUGUI characterNameText;
    [SerializeField] private TextMeshProUGUI houseNameText;
    [SerializeField] private Image houseBadge;
    
    [Header("Character Options")]
    [SerializeField] private Button maleButton;
    [SerializeField] private Button femaleButton;
    [SerializeField] private Slider skinToneSlider;
    [SerializeField] private Slider hairStyleSlider;
    [SerializeField] private Image hairColorPreview;
    
    [Header("House Info")]
    [SerializeField] private TextMeshProUGUI houseDescriptionText;
    [SerializeField] private TextMeshProUGUI houseTraitsText;
    [SerializeField] private Image houseColorBar;
    
    [Header("Starting Items")]
    [SerializeField] private Image wandPreview;
    [SerializeField] private TextMeshProUGUI wandNameText;
    [SerializeField] private Button[] wandButtons;
    
    [Header("Character Sprites")]
    [SerializeField] private Sprite[] maleSprites;
    [SerializeField] private Sprite[] femaleSprites;
    [SerializeField] private Sprite[] gryffindorBadges;
    [SerializeField] private Sprite[] slytherinBadges;
    [SerializeField] private Sprite[] ravenclawBadges;
    [SerializeField] private Sprite[] hufflepuffBadges;
    
    [Header("Wand Sprites")]
    [SerializeField] private Sprite[] wandSprites;
    [SerializeField] private string[] wandNames;
    
    [Header("Navigation")]
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button backButton;
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private string gameSceneName = "Hogwarts";
    
    // Character Data
    private string selectedGender = "male";
    private int selectedSkinTone = 0;
    private int selectedHairStyle = 0;
    private int selectedHairColor = 0;
    private int selectedWand = 0;
    private string playerHouse;
    
    // References
    private NetworkManager networkManager;
    
    void Start()
    {
        networkManager = NetworkManager.Instance;
        
        if (networkManager == null || networkManager.localPlayerData == null)
        {
            Debug.LogError("❌ No player data! Returning to main menu...");
            SceneManager.LoadScene("MainMenu");
            return;
        }
        
        // Get player's house from Sorting Hat
        playerHouse = networkManager.localPlayerData.house;
        
        if (string.IsNullOrEmpty(playerHouse))
        {
            Debug.LogError("❌ Player has no house! Sending to Sorting Hat...");
            SceneManager.LoadScene("SortingHat");
            return;
        }
        
        // Setup UI
        SetupButtons();
        LoadHouseInfo();
        UpdateCharacterPreview();
        
        // Hide loading panel
        if (loadingPanel != null)
            loadingPanel.SetActive(false);
        
        Debug.Log($"✅ Character Selection initialized for house: {playerHouse}");
    }
    
    void SetupButtons()
    {
        // Gender buttons
        maleButton?.onClick.AddListener(() => SelectGender("male"));
        femaleButton?.onClick.AddListener(() => SelectGender("female"));
        
        // Sliders
        skinToneSlider?.onValueChanged.AddListener(OnSkinToneChanged);
        hairStyleSlider?.onValueChanged.AddListener(OnHairStyleChanged);
        
        // Wand buttons
        for (int i = 0; i < wandButtons.Length; i++)
        {
            int index = i; // Capture for closure
            wandButtons[i]?.onClick.AddListener(() => SelectWand(index));
        }
        
        // Navigation
        confirmButton?.onClick.AddListener(OnConfirmClicked);
        backButton?.onClick.AddListener(OnBackClicked);
    }
    
    // ===== House Info =====
    
    void LoadHouseInfo()
    {
        if (houseNameText != null)
            houseNameText.text = playerHouse;
        
        // Set house badge
        if (houseBadge != null)
        {
            houseBadge.sprite = GetHouseBadge(playerHouse);
        }
        
        // Set house description
        if (houseDescriptionText != null)
        {
            houseDescriptionText.text = GetHouseDescription(playerHouse);
        }
        
        // Set house traits
        if (houseTraitsText != null)
        {
            houseTraitsText.text = GetHouseTraits(playerHouse);
        }
        
        // Set house color
        if (houseColorBar != null)
        {
            houseColorBar.color = GetHouseColor(playerHouse);
        }
    }
    
    Sprite GetHouseBadge(string house)
    {
        switch (house.ToLower())
        {
            case "gryffindor":
                return gryffindorBadges.Length > 0 ? gryffindorBadges[0] : null;
            case "slytherin":
                return slytherinBadges.Length > 0 ? slytherinBadges[0] : null;
            case "ravenclaw":
                return ravenclawBadges.Length > 0 ? ravenclawBadges[0] : null;
            case "hufflepuff":
                return hufflepuffBadges.Length > 0 ? hufflepuffBadges[0] : null;
            default:
                return null;
        }
    }
    
    string GetHouseDescription(string house)
    {
        switch (house.ToLower())
        {
            case "gryffindor":
                return "The house of the brave and daring. Gryffindors are known for their courage, chivalry, and determination.";
            case "slytherin":
                return "The house of the ambitious and cunning. Slytherins use any means to achieve their goals.";
            case "ravenclaw":
                return "The house of the wise and intelligent. Ravenclaws value learning, wit, and wisdom above all.";
            case "hufflepuff":
                return "The house of the loyal and hardworking. Hufflepuffs are known for their patience, dedication, and fair play.";
            default:
                return "";
        }
    }
    
    string GetHouseTraits(string house)
    {
        switch (house.ToLower())
        {
            case "gryffindor":
                return "Traits: Courage • Bravery • Determination • Chivalry";
            case "slytherin":
                return "Traits: Ambition • Cunning • Leadership • Resourcefulness";
            case "ravenclaw":
                return "Traits: Intelligence • Wisdom • Creativity • Wit";
            case "hufflepuff":
                return "Traits: Loyalty • Patience • Hard Work • Fairness";
            default:
                return "";
        }
    }
    
    Color GetHouseColor(string house)
    {
        switch (house.ToLower())
        {
            case "gryffindor":
                return new Color(0.74f, 0.11f, 0.11f); // Dark Red
            case "slytherin":
                return new Color(0.11f, 0.46f, 0.17f); // Dark Green
            case "ravenclaw":
                return new Color(0.11f, 0.26f, 0.65f); // Dark Blue
            case "hufflepuff":
                return new Color(0.96f, 0.77f, 0.19f); // Yellow
            default:
                return Color.white;
        }
    }
    
    // ===== Character Customization =====
    
    void SelectGender(string gender)
    {
        selectedGender = gender;
        UpdateCharacterPreview();
        
        Debug.Log($"👤 Selected gender: {gender}");
    }
    
    void OnSkinToneChanged(float value)
    {
        selectedSkinTone = Mathf.RoundToInt(value);
        UpdateCharacterPreview();
    }
    
    void OnHairStyleChanged(float value)
    {
        selectedHairStyle = Mathf.RoundToInt(value);
        UpdateCharacterPreview();
    }
    
    void UpdateCharacterPreview()
    {
        if (characterPreview == null) return;
        
        // Get appropriate sprite based on gender and customization
        Sprite[] sprites = selectedGender == "male" ? maleSprites : femaleSprites;
        
        if (sprites != null && sprites.Length > 0)
        {
            int index = Mathf.Clamp(selectedSkinTone, 0, sprites.Length - 1);
            characterPreview.sprite = sprites[index];
        }
        
        // Update name preview
        if (characterNameText != null)
        {
            string username = networkManager.localPlayerData.username;
            characterNameText.text = username;
        }
    }
    
    // ===== Wand Selection =====
    
    void SelectWand(int wandIndex)
    {
        selectedWand = wandIndex;
        
        // Update wand preview
        if (wandPreview != null && wandSprites.Length > wandIndex)
        {
            wandPreview.sprite = wandSprites[wandIndex];
        }
        
        // Update wand name
        if (wandNameText != null && wandNames.Length > wandIndex)
        {
            wandNameText.text = wandNames[wandIndex];
        }
        
        Debug.Log($"🪄 Selected wand: {wandNames[wandIndex]}");
    }
    
    // ===== Confirmation =====
    
    void OnConfirmClicked()
    {
        Debug.Log("✅ Character confirmed, saving...");
        
        // Save character customization to player data
        SaveCharacterData();
        
        // Show loading and proceed to game
        ShowLoadingAndStartGame();
    }
    
    void SaveCharacterData()
    {
        if (networkManager == null || networkManager.localPlayerData == null) return;
        
        PlayerData data = networkManager.localPlayerData;
        
        // Save character appearance (you can expand this with a custom CharacterAppearance class)
        // For now, we'll save it as simple fields
        // TODO: Create CharacterAppearance class in PlayerData
        
        // Save wand selection
        if (data.equipment != null && wandNames.Length > selectedWand)
        {
            data.equipment.wandId = wandNames[selectedWand].ToLower().Replace(" ", "_");
        }
        
        Debug.Log($"💾 Saved character: Gender={selectedGender}, Wand={wandNames[selectedWand]}");
    }
    
    void ShowLoadingAndStartGame()
    {
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(true);
        }
        
        // Save to server
        networkManager.SavePlayerData((success) =>
        {
            if (success)
            {
                Debug.Log("✅ Character data saved to server");
                LoadGameScene();
            }
            else
            {
                Debug.LogError("❌ Failed to save character data");
                // Still proceed to game, data will sync later
                LoadGameScene();
            }
        });
    }
    
    void LoadGameScene()
    {
        StartCoroutine(LoadGameSceneAsync());
    }
    
    IEnumerator LoadGameSceneAsync()
    {
        yield return new WaitForSeconds(1f);
        
        AsyncOperation operation = SceneManager.LoadSceneAsync(gameSceneName);
        
        while (!operation.isDone)
        {
            // You can update loading progress here
            yield return null;
        }
    }
    
    void OnBackClicked()
    {
        // Return to Sorting Hat
        SceneManager.LoadScene("SortingHat");
    }
}
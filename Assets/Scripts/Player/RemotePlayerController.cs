using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// کنترل بازیکنان remote (دیگران)
/// </summary>
public class RemotePlayerController : MonoBehaviour
{
    [Header("Info")]
    public string playerId;
    public string username;
    public string house;
    public float lastUpdateTime;
    
    [Header("Health")]
    public float currentHealth;
    public float maxHealth;
    
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float positionSmoothing = 0.15f;
    
    [Header("Visual")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private TextMesh nameLabel;
    [SerializeField] private GameObject healthBarPrefab;
    
    private Vector3 targetPosition;
    private Vector3 velocity = Vector3.zero;
    private GameObject healthBarObj;
    private Slider healthSlider;
    
    void Awake()
    {
        // Get or add components
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            }
        }
        
        // تنظیم Tag و Layer
        gameObject.tag = "RemotePlayer";
        gameObject.layer = LayerMask.NameToLayer("Player");
    }
    
    /// <summary>
    /// Initialize remote player
    /// </summary>
    public void Initialize(string id, string name, string houseType, Vector2Serializable position, float hp, float maxHp)
    {
        playerId = id;
        username = name;
        house = houseType;
        currentHealth = hp;
        maxHealth = maxHp;
        lastUpdateTime = Time.time;
        
        // تنظیم موقعیت اولیه
        Vector3 pos = new Vector3(position.x, position.y, 0);
        transform.position = pos;
        targetPosition = pos;
        
        // تنظیم sprite بر اساس خانه
        SetHouseSprite();
        
        // ساخت name label
        CreateNameLabel();
        
        // ساخت health bar
        CreateHealthBar();
        
        Debug.Log($"✅ Remote player initialized: {username} ({house})");
    }
    
    void Update()
    {
        // Smooth movement به سمت target position
        if (Vector3.Distance(transform.position, targetPosition) > 0.01f)
        {
            transform.position = Vector3.SmoothDamp(
                transform.position, 
                targetPosition, 
                ref velocity, 
                positionSmoothing
            );
        }
            // Smoothly move towards the new target position
        // (Ensure you have 'velocity' and 'positionSmoothing' fields defined)
        transform.position = Vector3.SmoothDamp(
            transform.position, 
            targetPosition, 
            ref velocity, 
            positionSmoothing
        );
            
        // آپدیت health bar position
        UpdateHealthBarPosition();
    }
    
    /// <summary>
    /// آپدیت موقعیت بازیکن
    /// </summary>
    public void UpdatePosition(Vector2Serializable newPosition)
    {
        targetPosition = new Vector3(newPosition.x, newPosition.y, 0);
        lastUpdateTime = Time.time;
    }
    
    /// <summary>
    /// آپدیت سلامتی
    /// </summary>
    public void UpdateHealth(float hp, float maxHp)
    {
        currentHealth = hp;
        maxHealth = maxHp;
        
        if (healthSlider != null)
        {
            healthSlider.value = maxHealth > 0 ? currentHealth / maxHealth : 0;
        }
    }
    
    /// <summary>
    /// تنظیم sprite بر اساس خانه
    /// </summary>
    void SetHouseSprite()
    {
        if (spriteRenderer == null) return;
        
        // سعی کن sprite خانه رو پیدا کنی
        string spritePath = $"Sprites/Players/{house}";
        Sprite houseSprite = Resources.Load<Sprite>(spritePath);
        
        if (houseSprite != null)
        {
            spriteRenderer.sprite = houseSprite;
        }
        else
        {
            // اگر نبود، رنگ بر اساس خانه
            switch (house.ToLower())
            {
                case "gryffindor":
                    spriteRenderer.color = new Color(0.8f, 0.2f, 0.2f); // قرمز
                    break;
                case "slytherin":
                    spriteRenderer.color = new Color(0.2f, 0.8f, 0.3f); // سبز
                    break;
                case "ravenclaw":
                    spriteRenderer.color = new Color(0.2f, 0.3f, 0.8f); // آبی
                    break;
                case "hufflepuff":
                    spriteRenderer.color = new Color(0.9f, 0.8f, 0.2f); // زرد
                    break;
                default:
                    spriteRenderer.color = Color.white;
                    break;
            }
        }
        
        spriteRenderer.sortingOrder = 10;
    }
    

        public void SetTargetPosition(Vector3 newPosition)
    {
        // targetPosition is already a private field in RemotePlayerController.cs
        targetPosition = newPosition;
    }
    /// <summary>
    /// ساخت label اسم
    /// </summary>
    void CreateNameLabel()
    {
        GameObject labelObj = new GameObject("NameLabel");
        labelObj.transform.SetParent(transform);
        labelObj.transform.localPosition = new Vector3(0, 1.5f, 0);
        
        nameLabel = labelObj.AddComponent<TextMesh>();
        nameLabel.text = username;
        nameLabel.fontSize = 20;
        nameLabel.alignment = TextAlignment.Center;
        nameLabel.anchor = TextAnchor.MiddleCenter;
        nameLabel.color = Color.white;
        
        // جلوگیری از چرخش با دوربین
        labelObj.transform.rotation = Quaternion.identity;
    }
    
    /// <summary>
    /// ساخت health bar
    /// </summary>
    void CreateHealthBar()
    {
        if (healthBarPrefab != null)
        {
            healthBarObj = Instantiate(healthBarPrefab, transform);
            healthBarObj.transform.localPosition = new Vector3(0, 1.2f, 0);
            healthSlider = healthBarObj.GetComponentInChildren<Slider>();
        }
        else
        {
            // ساخت ساده
            GameObject canvasObj = new GameObject("HealthBarCanvas");
            canvasObj.transform.SetParent(transform);
            canvasObj.transform.localPosition = new Vector3(0, 1.2f, 0);
            
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            
            RectTransform rect = canvasObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(1, 0.15f);
            rect.localScale = new Vector3(0.01f, 0.01f, 1f);
            
            GameObject sliderObj = new GameObject("HealthSlider");
            sliderObj.transform.SetParent(canvasObj.transform);
            
            healthSlider = sliderObj.AddComponent<Slider>();
            healthSlider.minValue = 0;
            healthSlider.maxValue = 1;
            healthSlider.value = 1;
            
            RectTransform sliderRect = sliderObj.GetComponent<RectTransform>();
            sliderRect.anchorMin = Vector2.zero;
            sliderRect.anchorMax = Vector2.one;
            sliderRect.sizeDelta = Vector2.zero;
            
            // Background
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(sliderObj.transform);
            Image bgImage = bgObj.AddComponent<Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.2f);
            
            // Fill
            GameObject fillAreaObj = new GameObject("Fill Area");
            fillAreaObj.transform.SetParent(sliderObj.transform);
            
            GameObject fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(fillAreaObj.transform);
            Image fillImage = fillObj.AddComponent<Image>();
            fillImage.color = new Color(0.2f, 0.8f, 0.2f);
            
            healthSlider.fillRect = fillImage.rectTransform;
            healthSlider.targetGraphic = fillImage;
        }
    }
    
    /// <summary>
    /// آپدیت موقعیت health bar
    /// </summary>
    void UpdateHealthBarPosition()
    {
        if (healthBarObj != null)
        {
            // همیشه رو به دوربین
            healthBarObj.transform.rotation = Quaternion.identity;
        }
    }
    
    /// <summary>
    /// دریافت ضرر
    /// </summary>
    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);
        
        UpdateHealth(currentHealth, maxHealth);
        
        // انیمیشن ضرر
        if (spriteRenderer != null)
        {
            StartCoroutine(FlashRed());
        }
        
        Debug.Log($"💥 {username} took {damage} damage. HP: {currentHealth}/{maxHealth}");
    }
    
    System.Collections.IEnumerator FlashRed()
    {
        Color original = spriteRenderer.color;
        spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        spriteRenderer.color = original;
    }
    
    void OnDestroy()
    {
        if (healthBarObj != null)
        {
            Destroy(healthBarObj);
        }
    }
}
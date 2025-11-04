using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections; // ✅ این خط اضافه شد

/// <summary>
/// کنترل Health Bar که بالای سر بازیکن یا دشمن نمایش داده می‌شود
/// </summary>
public class HealthBarController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform target;
    [SerializeField] private Canvas canvas;
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Image fillImage;
    [SerializeField] private TextMeshProUGUI healthText;
    
    [Header("Settings")]
    [SerializeField] private Vector3 offset = new Vector3(0, 2f, 0);
    [SerializeField] private bool showHealthText = true;
    [SerializeField] private bool alwaysVisible = false;
    [SerializeField] private float hideDelay = 3f;
    [SerializeField] private bool smoothTransition = true;
    [SerializeField] private float smoothSpeed = 5f;
    
    [Header("Colors")]
    [SerializeField] private Color fullHealthColor = Color.green;
    [SerializeField] private Color midHealthColor = Color.yellow;
    [SerializeField] private Color lowHealthColor = Color.red;
    [SerializeField] private float midHealthThreshold = 0.5f;
    [SerializeField] private float lowHealthThreshold = 0.25f;
    
    [Header("Animation")]
    [SerializeField] private bool enablePulse = true;
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private float pulseAmount = 0.1f;
    
    private Camera mainCamera;
    private float currentHealth;
    private float maxHealth;
    private float targetHealthValue;
    private float lastDamageTime;
    private CanvasGroup canvasGroup;
    private bool isInitialized = false;
    
    void Start()
    {
        Initialize();
    }
    
    void Initialize()
    {
        if (isInitialized) return;
        
        mainCamera = Camera.main;
        
        if (canvas == null)
        {
            canvas = GetComponentInChildren<Canvas>();
        }
        
        if (canvas != null)
        {
            canvas.worldCamera = mainCamera;
            canvas.sortingLayerName = "UI";
            canvas.sortingOrder = 100;
        }
        
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null && canvas != null)
        {
            canvasGroup = canvas.gameObject.AddComponent<CanvasGroup>();
        }
        
        if (!alwaysVisible && canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }
        
        isInitialized = true;
    }
    
    void Update()
    {
        if (!isInitialized) Initialize();
        
        FaceCamera();
        UpdatePosition();
        
        if (smoothTransition)
        {
            SmoothHealthUpdate();
        }
        
        if (!alwaysVisible)
        {
            UpdateVisibility();
        }
        
        if (enablePulse && GetHealthPercentage() < lowHealthThreshold)
        {
            PulseEffect();
        }
    }
    
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
    
    public void SetMaxHealth(float max)
    {
        maxHealth = max;
        
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
        }
        
        UpdateHealthBar(maxHealth);
    }
    
    public void UpdateHealthBar(float health)
    {
        currentHealth = Mathf.Clamp(health, 0, maxHealth);
        targetHealthValue = currentHealth;
        
        if (!smoothTransition)
        {
            SetHealthImmediate();
        }
        
        UpdateColor();
        UpdateText();
        
        if (!alwaysVisible)
        {
            lastDamageTime = Time.time;
            ShowBar();
        }
    }
    
    void SetHealthImmediate()
    {
        if (healthSlider != null)
        {
            healthSlider.value = currentHealth;
        }
    }
    
    void SmoothHealthUpdate()
    {
        if (healthSlider == null) return;
        
        float current = healthSlider.value;
        float target = targetHealthValue;
        
        if (Mathf.Abs(current - target) > 0.01f)
        {
            healthSlider.value = Mathf.Lerp(current, target, Time.deltaTime * smoothSpeed);
        }
    }
    
    void UpdateColor()
    {
        if (fillImage == null) return;
        
        float percentage = GetHealthPercentage();
        Color targetColor;
        
        if (percentage > midHealthThreshold)
        {
            float t = (percentage - midHealthThreshold) / (1f - midHealthThreshold);
            targetColor = Color.Lerp(midHealthColor, fullHealthColor, t);
        }
        else if (percentage > lowHealthThreshold)
        {
            float t = (percentage - lowHealthThreshold) / (midHealthThreshold - lowHealthThreshold);
            targetColor = Color.Lerp(lowHealthColor, midHealthColor, t);
        }
        else
        {
            targetColor = lowHealthColor;
        }
        
        fillImage.color = targetColor;
    }
    
    void UpdateText()
    {
        if (!showHealthText || healthText == null) return;
        healthText.text = $"{Mathf.CeilToInt(currentHealth)} / {Mathf.CeilToInt(maxHealth)}";
    }
    
    void UpdatePosition()
    {
        if (target == null) return;
        transform.position = target.position + offset;
    }
    
    void FaceCamera()
    {
        if (mainCamera == null || canvas == null) return;
        
        canvas.transform.LookAt(canvas.transform.position + mainCamera.transform.rotation * Vector3.forward,
                                mainCamera.transform.rotation * Vector3.up);
    }
    
    void UpdateVisibility()
    {
        if (canvasGroup == null) return;
        
        if (Time.time - lastDamageTime > hideDelay)
        {
            HideBar();
        }
    }
    
    void ShowBar()
    {
        if (canvasGroup == null) return;
        canvasGroup.alpha = 1f;
    }
    
    void HideBar()
    {
        if (canvasGroup == null) return;
        canvasGroup.alpha = 0f;
    }
    
    void PulseEffect()
    {
        if (fillImage == null) return;
        
        float pulse = Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
        Vector3 scale = Vector3.one * (1f + pulse);
        fillImage.transform.localScale = scale;
    }
    
    float GetHealthPercentage()
    {
        if (maxHealth <= 0) return 0f;
        return currentHealth / maxHealth;
    }
    
    public void SetAlwaysVisible(bool visible)
    {
        alwaysVisible = visible;
        
        if (canvasGroup != null)
        {
            canvasGroup.alpha = visible ? 1f : 0f;
        }
    }
    
    public void Flash(Color color, float duration = 0.2f)
    {
        if (fillImage == null) return;
        StartCoroutine(FlashCoroutine(color, duration));
    }
    
    private IEnumerator FlashCoroutine(Color color, float duration)
    {
        Color originalColor = fillImage.color;
        fillImage.color = color;
        
        yield return new WaitForSeconds(duration);
        
        fillImage.color = originalColor;
    }
    
    public void TakeDamage(float damage)
    {
        UpdateHealthBar(currentHealth - damage);
        Flash(Color.white, 0.1f);
    }
    
    public void Heal(float amount)
    {
        UpdateHealthBar(currentHealth + amount);
        Flash(Color.green, 0.2f);
    }
    
    public float GetCurrentHealth()
    {
        return currentHealth;
    }
    
    public float GetMaxHealth()
    {
        return maxHealth;
    }
}
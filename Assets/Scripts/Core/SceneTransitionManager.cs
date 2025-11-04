using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

/// <summary>
/// مدیریت انتقال بین Scene ها با انیمیشن‌های حرفه‌ای
/// - Fade In/Out
/// - Slide Transitions
/// - Loading Bar
/// </summary>
public class SceneTransitionManager : MonoBehaviour
{
    [Header("Transition Settings")]
    [SerializeField] private TransitionType transitionType = TransitionType.Fade;
    [SerializeField] private float transitionDuration = 0.5f;
    [SerializeField] private AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("UI References")]
    [SerializeField] private GameObject transitionCanvas;
    [SerializeField] private Image fadeImage;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private RectTransform slidePanel;
    [SerializeField] private TextMeshProUGUI loadingText;
    [SerializeField] private Slider loadingBar;
    [SerializeField] private Image loadingSpinner;
    
    [Header("Loading Messages")]
    [SerializeField] private string[] loadingMessages = new string[]
    {
        "Opening Diagon Alley...",
        "Preparing your vault...",
        "Polishing broomsticks...",
        "Stirring potions...",
        "Counting Galleons...",
        "Feeding Owls...",
        "Loading magical items..."
    };
    
    // Singleton
    public static SceneTransitionManager Instance { get; private set; }
    
    // State
    private bool isTransitioning = false;
    private Coroutine spinnerCoroutine;
    
    void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        // Hide transition UI
        if (transitionCanvas != null)
        {
            transitionCanvas.SetActive(false);
        }
        
        Debug.Log("✅ SceneTransitionManager initialized");
    }
    
    /// <summary>
    /// Load scene با انیمیشن
    /// </summary>
    public void LoadScene(string sceneName)
    {
        if (isTransitioning)
        {
            Debug.LogWarning("⚠️ Already transitioning!");
            return;
        }
        
        StartCoroutine(TransitionToScene(sceneName));
    }
    
    /// <summary>
    /// Load scene با انیمیشن و callback
    /// </summary>
    public void LoadScene(string sceneName, System.Action onComplete)
    {
        if (isTransitioning)
        {
            Debug.LogWarning("⚠️ Already transitioning!");
            return;
        }
        
        StartCoroutine(TransitionToScene(sceneName, onComplete));
    }
    
    IEnumerator TransitionToScene(string sceneName, System.Action onComplete = null)
    {
        isTransitioning = true;
        
        // 1️⃣ Show transition
        if (transitionCanvas != null)
        {
            transitionCanvas.SetActive(true);
        }
        
        // 2️⃣ Fade Out / Slide In
        yield return StartCoroutine(PlayTransitionIn());
        
        // 3️⃣ Load scene
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        operation.allowSceneActivation = false;
        
        // Start spinner
        if (loadingSpinner != null)
        {
            spinnerCoroutine = StartCoroutine(RotateSpinner());
        }
        
        // Show loading
        float loadProgress = 0f;
        
        while (!operation.isDone)
        {
            // Progress: 0-0.9 is loading, 0.9-1.0 is activation
            loadProgress = Mathf.Clamp01(operation.progress / 0.9f);
            
            // Update loading bar
            if (loadingBar != null)
            {
                loadingBar.value = loadProgress;
            }
            
            // Update loading text
            if (loadingText != null)
            {
                string message = loadingMessages[Random.Range(0, loadingMessages.Length)];
                loadingText.text = $"{message}\n{loadProgress * 100:F0}%";
            }
            
            // When almost done, activate scene
            if (operation.progress >= 0.9f)
            {
                // Wait a bit for visual effect
                yield return new WaitForSeconds(0.3f);
                
                if (loadingText != null)
                {
                    loadingText.text = "Ready! 🎉";
                }
                
                yield return new WaitForSeconds(0.2f);
                
                operation.allowSceneActivation = true;
            }
            
            yield return null;
        }
        
        // Stop spinner
        if (spinnerCoroutine != null)
        {
            StopCoroutine(spinnerCoroutine);
            spinnerCoroutine = null;
        }
        
        // 4️⃣ Fade In / Slide Out
        yield return StartCoroutine(PlayTransitionOut());
        
        // 5️⃣ Hide transition
        if (transitionCanvas != null)
        {
            transitionCanvas.SetActive(false);
        }
        
        isTransitioning = false;
        
        // Call callback
        onComplete?.Invoke();
        
        Debug.Log($"✅ Scene loaded: {sceneName}");
    }
    
    IEnumerator PlayTransitionIn()
    {
        switch (transitionType)
        {
            case TransitionType.Fade:
                yield return StartCoroutine(FadeIn());
                break;
                
            case TransitionType.SlideLeft:
                yield return StartCoroutine(SlideIn(Vector2.right));
                break;
                
            case TransitionType.SlideRight:
                yield return StartCoroutine(SlideIn(Vector2.left));
                break;
                
            case TransitionType.SlideUp:
                yield return StartCoroutine(SlideIn(Vector2.down));
                break;
                
            case TransitionType.SlideDown:
                yield return StartCoroutine(SlideIn(Vector2.up));
                break;
        }
    }
    
    IEnumerator PlayTransitionOut()
    {
        switch (transitionType)
        {
            case TransitionType.Fade:
                yield return StartCoroutine(FadeOut());
                break;
                
            case TransitionType.SlideLeft:
                yield return StartCoroutine(SlideOut(Vector2.left));
                break;
                
            case TransitionType.SlideRight:
                yield return StartCoroutine(SlideOut(Vector2.right));
                break;
                
            case TransitionType.SlideUp:
                yield return StartCoroutine(SlideOut(Vector2.up));
                break;
                
            case TransitionType.SlideDown:
                yield return StartCoroutine(SlideOut(Vector2.down));
                break;
        }
    }
    
    IEnumerator FadeIn()
    {
        if (canvasGroup == null) yield break;
        
        float elapsed = 0f;
        canvasGroup.alpha = 0f;
        
        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = transitionCurve.Evaluate(elapsed / transitionDuration);
            canvasGroup.alpha = t;
            yield return null;
        }
        
        canvasGroup.alpha = 1f;
    }
    
    IEnumerator FadeOut()
    {
        if (canvasGroup == null) yield break;
        
        float elapsed = 0f;
        canvasGroup.alpha = 1f;
        
        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = transitionCurve.Evaluate(elapsed / transitionDuration);
            canvasGroup.alpha = 1f - t;
            yield return null;
        }
        
        canvasGroup.alpha = 0f;
    }
    
    IEnumerator SlideIn(Vector2 direction)
    {
        if (slidePanel == null) yield break;
        
        Vector2 startPos = direction * Screen.width;
        Vector2 endPos = Vector2.zero;
        
        float elapsed = 0f;
        
        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = transitionCurve.Evaluate(elapsed / transitionDuration);
            slidePanel.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
            yield return null;
        }
        
        slidePanel.anchoredPosition = endPos;
    }
    
    IEnumerator SlideOut(Vector2 direction)
    {
        if (slidePanel == null) yield break;
        
        Vector2 startPos = Vector2.zero;
        Vector2 endPos = direction * Screen.width;
        
        float elapsed = 0f;
        
        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = transitionCurve.Evaluate(elapsed / transitionDuration);
            slidePanel.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
            yield return null;
        }
        
        slidePanel.anchoredPosition = endPos;
    }
    
    IEnumerator RotateSpinner()
    {
        while (true)
        {
            if (loadingSpinner != null)
            {
                loadingSpinner.transform.Rotate(0, 0, -360f * Time.deltaTime);
            }
            yield return null;
        }
    }
    
    /// <summary>
    /// تغییر نوع انیمیشن در runtime
    /// </summary>
    public void SetTransitionType(TransitionType type)
    {
        transitionType = type;
    }
    
    /// <summary>
    /// تغییر مدت زمان انیمیشن
    /// </summary>
    public void SetTransitionDuration(float duration)
    {
        transitionDuration = Mathf.Max(0.1f, duration);
    }
}

// ===== Enums =====

public enum TransitionType
{
    Fade,
    SlideLeft,
    SlideRight,
    SlideUp,
    SlideDown
}
using UnityEngine;
using System.Collections;

/// <summary>
/// جایگزین ساده LeanTween - برای انیمیشن‌های پایه
/// </summary>
public static class SimpleTween
{
    /// <summary>
    /// Scale کردن یک GameObject
    /// </summary>
    public static void Scale(GameObject obj, Vector3 targetScale, float duration)
    {
        MonoBehaviour mb = obj.GetComponent<MonoBehaviour>();
        if (mb != null)
        {
            mb.StartCoroutine(ScaleCoroutine(obj.transform, targetScale, duration));
        }
    }
    
    /// <summary>
    /// Move کردن یک RectTransform
    /// </summary>
    public static void Move(RectTransform rect, Vector2 targetPos, float duration, System.Action onComplete = null)
    {
        MonoBehaviour mb = rect.GetComponent<MonoBehaviour>();
        if (mb != null)
        {
            mb.StartCoroutine(MoveCoroutine(rect, targetPos, duration, onComplete));
        }
    }
    
    /// <summary>
    /// Move X کردن یک GameObject
    /// </summary>
    public static void MoveX(GameObject obj, float targetX, float duration, System.Action onComplete = null)
    {
        MonoBehaviour mb = obj.GetComponent<MonoBehaviour>();
        if (mb != null)
        {
            mb.StartCoroutine(MoveXCoroutine(obj.transform, targetX, duration, onComplete));
        }
    }
    
    // ===== Coroutines =====
    
    static IEnumerator ScaleCoroutine(Transform transform, Vector3 targetScale, float duration)
    {
        Vector3 startScale = transform.localScale;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            
            // EaseOutBack effect
            t = 1f - Mathf.Pow(1f - t, 3f);
            
            transform.localScale = Vector3.Lerp(startScale, targetScale, t);
            yield return null;
        }
        
        transform.localScale = targetScale;
    }
    
    static IEnumerator MoveCoroutine(RectTransform rect, Vector2 targetPos, float duration, System.Action onComplete)
    {
        Vector2 startPos = rect.anchoredPosition;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            
            // EaseOutBack effect
            t = 1f - Mathf.Pow(1f - t, 3f);
            
            rect.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
            yield return null;
        }
        
        rect.anchoredPosition = targetPos;
        onComplete?.Invoke();
    }
    
    static IEnumerator MoveXCoroutine(Transform transform, float targetX, float duration, System.Action onComplete)
    {
        Vector3 startPos = transform.position;
        Vector3 targetPos = new Vector3(targetX, startPos.y, startPos.z);
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            
            // EaseShake effect (for camera shake)
            float shake = Mathf.Sin(t * Mathf.PI * 6f) * (1f - t);
            Vector3 currentPos = Vector3.Lerp(startPos, targetPos, t);
            currentPos.x += shake * 0.1f;
            
            transform.position = currentPos;
            yield return null;
        }
        
        transform.position = startPos; // Reset to original for camera shake
        onComplete?.Invoke();
    }
}

/// <summary>
/// Helper برای دسترسی راحت‌تر به SimpleTween
/// </summary>
public static class LeanTween
{
    public static void scale(GameObject obj, Vector3 targetScale, float duration)
    {
        SimpleTween.Scale(obj, targetScale, duration);
    }
    
    public static TweenHelper moveX(GameObject obj, float targetX, float duration)
    {
        return new TweenHelper(obj, targetX, duration);
    }
    
    public static TweenHelper move(RectTransform rect, Vector2 targetPos, float duration)
    {
        return new TweenHelper(rect, targetPos, duration);
    }
}

/// <summary>
/// کلاس کمکی برای chain کردن متدها
/// </summary>
public class TweenHelper
{
    private GameObject gameObj;
    private RectTransform rectTransform;
    private float targetX;
    private Vector2 targetPos;
    private float duration;
    private System.Action onComplete;
    private bool isRect;
    
    public TweenHelper(GameObject obj, float x, float dur)
    {
        gameObj = obj;
        targetX = x;
        duration = dur;
        isRect = false;
    }
    
    public TweenHelper(RectTransform rect, Vector2 pos, float dur)
    {
        rectTransform = rect;
        targetPos = pos;
        duration = dur;
        isRect = true;
    }
    
    public TweenHelper setEase(LeanTweenType easeType)
    {
        // Ignore ease type for now
        return this;
    }
    
    public TweenHelper setLoopPingPong(int loops)
    {
        // Simplified: just execute once
        return this;
    }
    
    public TweenHelper setOnComplete(System.Action callback)
    {
        onComplete = callback;
        Execute();
        return this;
    }
    
    void Execute()
    {
        if (isRect && rectTransform != null)
        {
            SimpleTween.Move(rectTransform, targetPos, duration, onComplete);
        }
        else if (gameObj != null)
        {
            SimpleTween.MoveX(gameObj, targetX, duration, onComplete);
        }
    }
}

/// <summary>
/// Enum برای نوع ease (فقط برای compatibility)
/// </summary>
public enum LeanTweenType
{
    easeOutBack,
    easeInBack,
    easeShake,
    linear
}
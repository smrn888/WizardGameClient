using UnityEngine;

/// <summary>
/// مدیریت انیمیشن‌های بازیکن
/// حرکت 4 جهته، کست طلسم، آسیب، و مرگ
/// </summary>
public class PlayerAnimator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;
    
    [Header("Animation Settings")]
    [SerializeField] private bool use4DirectionalMovement = true;
    [SerializeField] private bool autoFlipSprite = true;
    [SerializeField] private float movementThreshold = 0.1f;
    
    [Header("Layer Weights")]
    [SerializeField] private float castLayerWeight = 1f;
    [SerializeField] private float hitLayerWeight = 1f;
    
    // Animation States
    private bool isMoving;
    private bool isCasting;
    private bool isHit;
    private bool isDead;
    
    // Movement direction
    private Vector2 moveDirection;
    private Vector2 lastMoveDirection = Vector2.down;
    private string currentDirection = "down";
    
    // Animation parameters (string optimization)
    private static readonly int IsMoving = Animator.StringToHash("isMoving");
    private static readonly int IsCasting = Animator.StringToHash("isCasting");
    private static readonly int IsHit = Animator.StringToHash("isHit");
    private static readonly int IsDead = Animator.StringToHash("isDead");
    private static readonly int MoveX = Animator.StringToHash("moveX");
    private static readonly int MoveY = Animator.StringToHash("moveY");
    private static readonly int LastMoveX = Animator.StringToHash("lastMoveX");
    private static readonly int LastMoveY = Animator.StringToHash("lastMoveY");
    private static readonly int CastX = Animator.StringToHash("castX");
    private static readonly int CastY = Animator.StringToHash("castY");
    private static readonly int HitTrigger = Animator.StringToHash("hit");
    private static readonly int DeathTrigger = Animator.StringToHash("death");
    private static readonly int RespawnTrigger = Animator.StringToHash("respawn");
    
    void Start()
    {
        // Get components if not assigned
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
        
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
        
        if (animator == null)
        {
            Debug.LogError("❌ Animator not found on PlayerAnimator!");
            enabled = false;
            return;
        }
        
        // Initialize
        SetInitialAnimationState();
    }
    
    void SetInitialAnimationState()
    {
        // Set default idle animation (facing down)
        SetMovement(Vector2.zero);
        lastMoveDirection = Vector2.down;
        UpdateAnimationParameters();
    }
    
    // ===== Public API =====
    
    /// <summary>
    /// تنظیم جهت حرکت
    /// </summary>
    public void SetMovement(Vector2 direction)
    {
        moveDirection = direction;
        
        isMoving = direction.magnitude > movementThreshold;
        
        if (isMoving)
        {
            lastMoveDirection = direction.normalized;
            UpdateDirection();
        }
        
        UpdateAnimationParameters();
        
        // Auto flip sprite
        if (autoFlipSprite && spriteRenderer != null)
        {
            if (direction.x < -movementThreshold)
                spriteRenderer.flipX = true;
            else if (direction.x > movementThreshold)
                spriteRenderer.flipX = false;
        }
    }
    
    /// <summary>
    /// شروع انیمیشن کست طلسم
    /// </summary>
    public void PlayCastAnimation(Vector2 castDirection)
    {
        isCasting = true;
        
        if (animator != null)
        {
            animator.SetBool(IsCasting, true);
            animator.SetFloat(CastX, castDirection.x);
            animator.SetFloat(CastY, castDirection.y);
        }
        
        // Auto stop after animation duration
        float castDuration = GetAnimationLength("Cast");
        Invoke(nameof(StopCastAnimation), castDuration);
    }
    
    /// <summary>
    /// پایان انیمیشن کست
    /// </summary>
    public void StopCastAnimation()
    {
        isCasting = false;
        
        if (animator != null)
        {
            animator.SetBool(IsCasting, false);
        }
    }
    
    /// <summary>
    /// پلی انیمیشن آسیب
    /// </summary>
    public void PlayHitAnimation()
    {
        if (isDead) return;
        
        isHit = true;
        
        if (animator != null)
        {
            animator.SetTrigger(HitTrigger);
        }
        
        // Reset after animation
        float hitDuration = GetAnimationLength("Hit");
        Invoke(nameof(ResetHitAnimation), hitDuration);
    }
    
    void ResetHitAnimation()
    {
        isHit = false;
    }
    
    /// <summary>
    /// پلی انیمیشن مرگ
    /// </summary>
    public void PlayDeathAnimation()
    {
        isDead = true;
        isMoving = false;
        isCasting = false;
        
        if (animator != null)
        {
            animator.SetBool(IsDead, true);
            animator.SetTrigger(DeathTrigger);
        }
    }
    
    /// <summary>
    /// پلی انیمیشن Respawn
    /// </summary>
    public void PlayRespawnAnimation()
    {
        isDead = false;
        
        if (animator != null)
        {
            animator.SetBool(IsDead, false);
            animator.SetTrigger(RespawnTrigger);
        }
        
        SetInitialAnimationState();
    }
    
    // ===== Animation Updates =====
    
    void UpdateAnimationParameters()
    {
        if (animator == null) return;
        
        // Movement state
        animator.SetBool(IsMoving, isMoving);
        
        // Current movement direction
        if (isMoving)
        {
            animator.SetFloat(MoveX, moveDirection.x);
            animator.SetFloat(MoveY, moveDirection.y);
        }
        else
        {
            animator.SetFloat(MoveX, 0);
            animator.SetFloat(MoveY, 0);
        }
        
        // Last movement direction (for idle facing)
        animator.SetFloat(LastMoveX, lastMoveDirection.x);
        animator.SetFloat(LastMoveY, lastMoveDirection.y);
    }
    
    /// <summary>
    /// تشخیص جهت غالب (برای انیمیشن 4 جهته)
    /// </summary>
    void UpdateDirection()
    {
        if (!use4DirectionalMovement) return;
        
        float x = lastMoveDirection.x;
        float y = lastMoveDirection.y;
        
        // Determine primary direction
        if (Mathf.Abs(x) > Mathf.Abs(y))
        {
            currentDirection = x > 0 ? "right" : "left";
        }
        else
        {
            currentDirection = y > 0 ? "up" : "down";
        }
    }
    
    // ===== Utility =====
    
    /// <summary>
    /// گرفتن طول انیمیشن
    /// </summary>
    float GetAnimationLength(string animationName)
    {
        if (animator == null) return 0.5f;
        
        RuntimeAnimatorController ac = animator.runtimeAnimatorController;
        if (ac == null) return 0.5f;
        
        foreach (AnimationClip clip in ac.animationClips)
        {
            if (clip.name.Contains(animationName))
            {
                return clip.length;
            }
        }
        
        return 0.5f; // Default duration
    }
    
    /// <summary>
    /// چک کردن اینکه آیا انیمیشن در حال پخش است
    /// </summary>
    public bool IsPlayingAnimation(string animationName)
    {
        if (animator == null) return false;
        
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        return stateInfo.IsName(animationName);
    }
    
    /// <summary>
    /// تنظیم سرعت انیمیشن
    /// </summary>
    public void SetAnimationSpeed(float speed)
    {
        if (animator != null)
        {
            animator.speed = speed;
        }
    }
    
    /// <summary>
    /// گرفتن جهت فعلی
    /// </summary>
    public string GetCurrentDirection()
    {
        return currentDirection;
    }
    
    /// <summary>
    /// گرفتن آخرین جهت حرکت
    /// </summary>
    public Vector2 GetLastMoveDirection()
    {
        return lastMoveDirection;
    }
    
    /// <summary>
    /// چک اینکه آیا بازیکن در حال حرکت است
    /// </summary>
    public bool GetIsMoving()
    {
        return isMoving;
    }
    
    /// <summary>
    /// چک اینکه آیا بازیکن در حال کست است
    /// </summary>
    public bool GetIsCasting()
    {
        return isCasting;
    }
    
    /// <summary>
    /// چک اینکه آیا بازیکن مرده است
    /// </summary>
    public bool GetIsDead()
    {
        return isDead;
    }
    
    // ===== Visual Effects =====
    
    /// <summary>
    /// فلش کردن اسپرایت
    /// </summary>
    public void FlashSprite(Color color, float duration = 0.1f)
    {
        if (spriteRenderer != null)
        {
            StartCoroutine(FlashCoroutine(color, duration));
        }
    }
    
    System.Collections.IEnumerator FlashCoroutine(Color color, float duration)
    {
        Color originalColor = spriteRenderer.color;
        spriteRenderer.color = color;
        
        yield return new WaitForSeconds(duration);
        
        spriteRenderer.color = originalColor;
    }
    
    /// <summary>
    /// محو شدن اسپرایت
    /// </summary>
    public void FadeOut(float duration = 1f)
    {
        if (spriteRenderer != null)
        {
            StartCoroutine(FadeCoroutine(0f, duration));
        }
    }
    
    /// <summary>
    /// ظاهر شدن اسپرایت
    /// </summary>
    public void FadeIn(float duration = 1f)
    {
        if (spriteRenderer != null)
        {
            StartCoroutine(FadeCoroutine(1f, duration));
        }
    }
    
    System.Collections.IEnumerator FadeCoroutine(float targetAlpha, float duration)
    {
        float startAlpha = spriteRenderer.color.a;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            Color color = spriteRenderer.color;
            color.a = Mathf.Lerp(startAlpha, targetAlpha, t);
            spriteRenderer.color = color;
            
            yield return null;
        }
        
        // Ensure final value
        Color finalColor = spriteRenderer.color;
        finalColor.a = targetAlpha;
        spriteRenderer.color = finalColor;
    }
    
    /// <summary>
    /// تنظیم رنگ اسپرایت
    /// </summary>
    public void SetSpriteColor(Color color)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = color;
        }
    }
    
    /// <summary>
    /// بازگردانی رنگ اسپرایت به حالت پیش‌فرض
    /// </summary>
    public void ResetSpriteColor()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.white;
        }
    }
    
    // ===== Animation Events =====
    // این متدها از طریق Animation Events فراخوانی می‌شوند
    
    public void OnCastAnimationComplete()
    {
        StopCastAnimation();
    }
    
    public void OnHitAnimationComplete()
    {
        ResetHitAnimation();
    }
    
    public void OnDeathAnimationComplete()
    {
        // این متد می‌تواند برای تریگر کردن رویدادهای بعد از مرگ استفاده شود
        Debug.Log("💀 Death animation complete");
    }
    
    public void OnFootstepSound()
    {
        // این متد می‌تواند برای پخش صدای قدم استفاده شود
        // TODO: Play footstep sound
    }
    
    public void OnSpellCastSound()
    {
        // این متد می‌تواند برای پخش صدای کست طلسم استفاده شود
        // TODO: Play spell cast sound
    }
    
    // ===== Debug =====
    
    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        
        // نمایش جهت حرکت
        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position, (Vector3)lastMoveDirection * 0.5f);
        
        // نمایش وضعیت
        if (isMoving)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 2f, 0.2f);
        }
        
        if (isCasting)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 2.5f, 0.2f);
        }
    }
    
    // ===== Layer Control =====
    
    /// <summary>
    /// تنظیم وزن لایه انیمیشن
    /// </summary>
    public void SetLayerWeight(int layerIndex, float weight)
    {
        if (animator != null && layerIndex < animator.layerCount)
        {
            animator.SetLayerWeight(layerIndex, weight);
        }
    }
    
    /// <summary>
    /// گرفتن وزن لایه انیمیشن
    /// </summary>
    public float GetLayerWeight(int layerIndex)
    {
        if (animator != null && layerIndex < animator.layerCount)
        {
            return animator.GetLayerWeight(layerIndex);
        }
        return 0f;
    }
}
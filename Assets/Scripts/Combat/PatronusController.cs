using UnityEngine;
using System.Collections;

/// <summary>
/// کنترلر Patronus - طلسم محافظتی که Dementor را از بین می‌برد
/// </summary>
public class PatronusController : MonoBehaviour
{
    [Header("Patronus Settings")]
    [SerializeField] private float lifetime = 10f; // مدت زمان وجود Patronus
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float rotationSpeed = 100f;
    [SerializeField] private float damagePerSecond = 50f;
    
    [Header("Behavior")]
    [SerializeField] private bool followCaster = true; // آیا کستر را دنبال می‌کند
    [SerializeField] private float followDistance = 5f;
    [SerializeField] private bool attackDementors = true;
    [SerializeField] private float attackRange = 15f;
    
    [Header("Visual")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator animator;
    [SerializeField] private ParticleSystem glowEffect;
    [SerializeField] private Color patronusColor = new Color(0.7f, 0.9f, 1f); // آبی-سفید درخشان
    
    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip summonSound;
    [SerializeField] private AudioClip ambientSound;
    
    private Transform caster;
    private Transform targetDementor;
    private Vector2 moveDirection;
    private float creationTime;
    private Rigidbody2D rb;
    
    void Start()
    {
        creationTime = Time.time;
        rb = GetComponent<Rigidbody2D>();
        
        // Setup visuals
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
        
        if (animator == null)
            animator = GetComponent<Animator>();
        
        if (spriteRenderer != null)
        {
            spriteRenderer.color = patronusColor;
        }
        

        // Setup audio
        if (audioSource != null)
        {
            if (summonSound != null)
            {
                audioSource.PlayOneShot(summonSound);
            }
            if (ambientSound != null)
            {
                audioSource.clip = ambientSound;
                audioSource.loop = true;
                audioSource.Play();
            }
        }
        
        // Find caster
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            caster = playerObj.transform;
        }
        
        // Setup layer & collision
        gameObject.layer = LayerMask.NameToLayer("Spell");
        
        CircleCollider2D collider = GetComponent<CircleCollider2D>();
        if (collider == null)
        {
            collider = gameObject.AddComponent<CircleCollider2D>();
        }
        collider.isTrigger = true;
        collider.radius = 1f;
        
        Debug.Log("✨ Patronus summoned! (Expecto Patronum)");
    }
    
    void Update()
    {
        // Check lifetime
        if (Time.time - creationTime >= lifetime)
        {
            Dismiss();
            return;
        }
        
        // Behavior
        if (attackDementors)
        {
            FindAndAttackDementor();
        }
        else if (followCaster && caster != null)
        {
            FollowCaster();
        }
        
        UpdateAnimation();
    }
    
    void FixedUpdate()
    {
        if (rb != null && moveDirection != Vector2.zero)
        {
            rb.MovePosition(rb.position + moveDirection * moveSpeed * Time.fixedDeltaTime);
        }
    }
    
    // ===== Behavior =====
    
    void FindAndAttackDementor()
    {
        DementorController[] dementors = FindObjectsOfType<DementorController>();
        
        float nearestDistance = float.MaxValue;
        targetDementor = null;
        
        foreach (DementorController dementor in dementors)
        {
            if (dementor == null) continue;
            
            float distance = Vector2.Distance(transform.position, dementor.transform.position);
            
            if (distance < attackRange && distance < nearestDistance)
            {
                nearestDistance = distance;
                targetDementor = dementor.transform;
            }
        }
        
        if (targetDementor != null)
        {
            // حرکت به سمت Dementor
            Vector2 direction = (targetDementor.position - transform.position).normalized;
            moveDirection = direction;
            
            // چرخش به سمت هدف
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            Quaternion targetRotation = Quaternion.Euler(0, 0, angle);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
        else
        {
            // 🚀 اگر دمنتور نیست، در مسیر فعلی یا جهت بازیکن ادامه بده
            if (moveDirection == Vector2.zero && caster != null)
            {
                PlayerController player = caster.GetComponent<PlayerController>();
                if (player != null)
                    moveDirection = player.GetFacingDirection().normalized;
                else
                    moveDirection = Vector2.right; // پیش‌فرض
            }

            // ✨ به آرامی محو بشه و ناپدید شه
            StartCoroutine(FadeAndDisappear());
        }
    }

    private bool isFading = false;

    private IEnumerator FadeAndDisappear()
    {
        if (isFading) yield break; // از تکرار جلوگیری می‌کنیم
        isFading = true;

        float fadeDuration = 2f;
        float elapsed = 0f;

        Color startColor = spriteRenderer.color;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            spriteRenderer.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            yield return null;
        }

        Destroy(gameObject);
    }

    
    void FollowCaster()
    {
        if (caster == null) return;
        
        float distance = Vector2.Distance(transform.position, caster.position);
        
        if (distance > followDistance)
        {
            Vector2 direction = (caster.position - transform.position).normalized;
            moveDirection = direction;
        }
        else
        {
            // Stay near caster
            moveDirection = Vector2.zero;
        }
    }
    
    // ===== Collision =====
    
    void OnTriggerEnter2D(Collider2D other)
    {
        DementorController dementor = other.GetComponent<DementorController>();
        if (dementor != null)
        {
            dementor.TakeDamageFromPatronus(damagePerSecond);
            CreateImpactEffect();
        }
    }
    
    void OnTriggerStay2D(Collider2D other)
    {
        DementorController dementor = other.GetComponent<DementorController>();
        if (dementor != null)
        {
            dementor.TakeDamageFromPatronus(damagePerSecond * Time.deltaTime);
        }
    }
    
    // ===== Effects =====
    
    void CreateImpactEffect()
    {
        GameObject effect = new GameObject("PatronusImpact");
        effect.transform.position = transform.position;
        
        ParticleSystem ps = effect.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startColor = patronusColor;
        main.startLifetime = 0.5f;
        main.startSpeed = 5f;
        main.maxParticles = 30;
        
        Destroy(effect, 1f);
    }
    
    void Dismiss()
    {
        Debug.Log("✨ Patronus dismissed");
        
        // Fade out effect
        if (spriteRenderer != null)
        {
            spriteRenderer.color = new Color(patronusColor.r, patronusColor.g, patronusColor.b, 0);
        }
        
        
        Destroy(gameObject, 0.5f);
    }
    
    // ===== Animation =====
    
    void UpdateAnimation()
    {
        if (animator == null) return;
        
        bool isMoving = moveDirection.magnitude > 0.1f;
        animator.SetBool("isMoving", isMoving);
        
        if (isMoving)
        {
            animator.SetFloat("moveX", moveDirection.x);
            animator.SetFloat("moveY", moveDirection.y);
        }
    }
    
    // ===== Gizmos =====
    
    void OnDrawGizmosSelected()
    {
        // Attack range
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        // Follow distance
        if (caster != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(caster.position, followDistance);
        }
    }
}
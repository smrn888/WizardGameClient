using UnityEngine;
using System.Collections;

/// <summary>
/// ✅ FIXED: Patronus detection without requiring "Patronus" tag
/// </summary>
public class DementorController : MonoBehaviour
{
    [Header("Dementor Settings")]
    [SerializeField] private float moveSpeed = 1.5f;
    [SerializeField] private float detectionRange = 15f;
    [SerializeField] private float attackRange = 3f;
    [SerializeField] private float drainRate = 5f;
    
    [Header("Behavior")]
    [SerializeField] private bool isInvulnerable = true;
    [SerializeField] private float chaseSpeed = 2f;
    [SerializeField] private float retreatSpeed = 3f;
    
    [Header("Visual")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator animator;
    [SerializeField] private ParticleSystem darkAura;
    [SerializeField] private Color dementorColor = new Color(0.2f, 0.2f, 0.2f);
    
    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip hoverSound;
    [SerializeField] private AudioClip screamSound;
    [SerializeField] private AudioClip deathSound;
    
    [Header("Effects")]
    [SerializeField] private GameObject disappearEffectPrefab;
    
    // State
    private Transform player;
    private Transform nearestPatronus;
    private Rigidbody2D rb;
    private bool isDraining = false;
    private bool isFleeing = false;
    private bool isDying = false;
    private Vector2 moveDirection;
    
    // Health
    private float currentHealth = 100f;
    private float maxHealth = 100f;
    
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
        
        if (animator == null)
            animator = GetComponent<Animator>();
        
        if (spriteRenderer != null)
        {
            spriteRenderer.color = dementorColor;
        }
        
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
        
        gameObject.layer = LayerMask.NameToLayer("Enemy");
        gameObject.tag = "Dementor";
        
        if (audioSource != null && hoverSound != null)
        {
            audioSource.clip = hoverSound;
            audioSource.loop = true;
            audioSource.Play();
        }
        
        Debug.Log("👻 Dementor spawned");
    }
    
    void Update()
    {
        if (isDying) return;
        
        CheckForPatronus();
        
        if (isFleeing && nearestPatronus != null)
        {
            FleeFromPatronus();
        }
        else if (player != null)
        {
            float distanceToPlayer = Vector2.Distance(transform.position, player.position);
            
            if (distanceToPlayer < detectionRange)
            {
                if (distanceToPlayer < attackRange)
                {
                    AttackPlayer();
                }
                else
                {
                    ChasePlayer();
                }
            }
            else
            {
                Wander();
            }
        }
        
        UpdateAnimation();
    }
    
    void FixedUpdate()
    {
        if (!isDying && moveDirection != Vector2.zero)
        {
            rb.MovePosition(rb.position + moveDirection * Time.fixedDeltaTime);
        }
    }
    
    // ===== رفتارها =====
    
    void ChasePlayer()
    {
        Vector2 direction = (player.position - transform.position).normalized;
        moveDirection = direction * chaseSpeed;
        
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = direction.x < 0;
        }
    }
    
    void AttackPlayer()
    {
        moveDirection = Vector2.zero;
        
        if (!isDraining)
        {
            StartCoroutine(DrainPlayerHealth());
        }
    }
    
    IEnumerator DrainPlayerHealth()
    {
        isDraining = true;
        
        if (audioSource != null && screamSound != null)
        {
            audioSource.PlayOneShot(screamSound);
        }
        
        while (Vector2.Distance(transform.position, player.position) < attackRange && player != null)
        {
            PlayerController pc = player.GetComponent<PlayerController>();
            if (pc != null)
            {
                float damage = drainRate * Time.deltaTime;
                pc.TakeDamage((int)damage);
                
                CreateDrainEffect();
            }
            
            yield return null;
        }
        
        isDraining = false;
    }
    
    void FleeFromPatronus()
    {
        if (nearestPatronus == null) return;
        
        Vector2 direction = (transform.position - nearestPatronus.position).normalized;
        moveDirection = direction * retreatSpeed;
        
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = direction.x < 0;
        }
    }
    
    void Wander()
    {
        if (Random.value < 0.02f)
        {
            Vector2 randomDir = Random.insideUnitCircle.normalized;
            moveDirection = randomDir * moveSpeed;
        }
    }
    
    // ===== ✅ FIXED: چک Patronus بدون Tag =====
    
    void CheckForPatronus()
    {
        // ✅ استفاده از FindObjectsOfType به جای FindGameObjectsWithTag
        PatronusController[] patronuses = FindObjectsOfType<PatronusController>();
        
        float nearestDistance = float.MaxValue;
        nearestPatronus = null;
        
        foreach (PatronusController patronus in patronuses)
        {
            if (patronus == null) continue;
            
            float distance = Vector2.Distance(transform.position, patronus.transform.position);
            
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestPatronus = patronus.transform;
            }
        }
        
        isFleeing = nearestPatronus != null && nearestDistance < 10f;
        
        if (nearestPatronus != null && nearestDistance < 3f)
        {
            TakeDamageFromPatronus(50f * Time.deltaTime);
        }
    }
    
    // ===== دریافت آسیب =====
    
    public void TakeDamage(float damage, string source = "")
    {
        if (isInvulnerable && source != "Patronus")
        {
            Debug.Log("👻 Dementor is invulnerable to normal attacks!");
            return;
        }
        
        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);
        
        Debug.Log($"👻 Dementor took {damage:F1} damage. Health: {currentHealth}/{maxHealth}");
        
        if (spriteRenderer != null)
        {
            StartCoroutine(FlashWhite());
        }
        
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    public void TakeDamageFromPatronus(float damage)
    {
        TakeDamage(damage, "Patronus");
    }
    
    IEnumerator FlashWhite()
    {
        if (spriteRenderer != null)
        {
            Color original = spriteRenderer.color;
            spriteRenderer.color = Color.white;
            yield return new WaitForSeconds(0.1f);
            spriteRenderer.color = original;
        }
    }
    
    // ===== مرگ =====
    
    void Die()
    {
        if (isDying) return;
        
        isDying = true;
        isDraining = false;
        moveDirection = Vector2.zero;
        
        Debug.Log("👻 Dementor destroyed!");
        
        if (audioSource != null && deathSound != null)
        {
            audioSource.Stop();
            audioSource.PlayOneShot(deathSound);
        }
        
        CreateDisappearEffect();
        
        Destroy(gameObject, 1f);
    }
    
    // ===== افکت‌ها =====
    
    void CreateDrainEffect()
    {
        GameObject effect = new GameObject("DrainEffect");
        effect.transform.position = player.position;
        
        LineRenderer line = effect.AddComponent<LineRenderer>();
        line.startWidth = 0.1f;
        line.endWidth = 0.05f;
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.startColor = Color.black;
        line.endColor = new Color(0.3f, 0.3f, 0.3f, 0);
        line.SetPosition(0, transform.position);
        line.SetPosition(1, player.position);
        
        Destroy(effect, 0.2f);
    }
    
    void CreateDisappearEffect()
    {
        if (disappearEffectPrefab != null)
        {
            GameObject effect = Instantiate(disappearEffectPrefab, transform.position, Quaternion.identity);
            Destroy(effect, 2f);
        }
        else
        {
            GameObject effect = new GameObject("DisappearEffect");
            effect.transform.position = transform.position;
            
            ParticleSystem ps = effect.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startColor = Color.black;
            main.startLifetime = 1f;
            main.startSpeed = 3f;
            main.maxParticles = 50;
            
            Destroy(effect, 2f);
        }
    }
    
    // ===== انیمیشن =====
    
    void UpdateAnimation()
    {
        if (animator == null) return;
        
        bool isMoving = moveDirection.magnitude > 0.1f;
        
        animator.SetBool("isMoving", isMoving);
        animator.SetBool("isDraining", isDraining);
        animator.SetBool("isFleeing", isFleeing);
        
        if (isMoving)
        {
            animator.SetFloat("moveX", moveDirection.x);
            animator.SetFloat("moveY", moveDirection.y);
        }
    }
    
    // ===== ✅ FIXED: Collision بدون استفاده از Tag =====
    
    void OnTriggerEnter2D(Collider2D other)
    {
        // ✅ چک کردن با GetComponent به جای CompareTag
        PatronusController patronus = other.GetComponent<PatronusController>();
        if (patronus != null)
        {
            TakeDamageFromPatronus(30f);
        }
    }
    
    void OnTriggerStay2D(Collider2D other)
    {
        // ✅ چک کردن با GetComponent به جای CompareTag
        PatronusController patronus = other.GetComponent<PatronusController>();
        if (patronus != null)
        {
            TakeDamageFromPatronus(20f * Time.deltaTime);
        }
    }
    
    // ===== Gizmos =====
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 10f);
    }
}
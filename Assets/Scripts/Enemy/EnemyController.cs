using UnityEngine;
using System.Collections;

/// <summary>
/// ✅ FIXED: Enemy spell casting now works properly
/// ✅ FIXED: Better combat range and timing
/// </summary>
public class EnemyController : MonoBehaviour
{
    [Header("Enemy Info")]
    public string enemyName;
    public string house; // slytherin, ravenclaw, deatheater, dementor
    public int maxHealth = 100;
    
    [Header("Movement")]
    public float chaseSpeed = 2f;
    public float detectionRange = 10f;
    public float attackRange = 7f; // ✅ NEW: محدوده شلیک
    public float minAttackDistance = 3f; // ✅ NEW: حداقل فاصله برای شلیک
    
    [Header("Combat")]
    public GameObject spellPrefab;
    public float fireRate = 2f;
    public int spellDamage = 12;
    
    [Header("Animation")]
    public Animator animator;
    public SpriteRenderer spriteRenderer;

    private int currentHealth;
    private Transform player;
    private float lastFireTime = -999f; // ✅ شروع با عدد منفی
    private bool isStunned = false;
    private bool isFalling = false;
    private Rigidbody2D rb;
    private Vector2 moveDirection;

    void Start()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody2D>();
        
        // اگر spriteRenderer در Inspector تنظیم نشده، خودکار پیدا کن
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
        
        if (spriteRenderer == null)
        {
            Debug.LogError($"❌ SpriteRenderer not found on {gameObject.name}");
        }
        
        // اگر animator در Inspector تنظیم نشده، خودکار پیدا کن
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
        
        // پیدا کردن بازیکن
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }

        // تنظیم Tag و Layer
        gameObject.tag = "Enemy";
        gameObject.layer = LayerMask.NameToLayer("Enemy");

        // ✅ Load spell prefab if not assigned
        if (spellPrefab == null)
        {
            spellPrefab = Resources.Load<GameObject>("Prefabs/Spell");
            if (spellPrefab == null)
            {
                Debug.LogWarning($"⚠️ Spell prefab not found for {enemyName}");
            }
        }

        Debug.Log($"👹 {enemyName} spawned");
    }

    void Update()
    {
        if (isFalling || isStunned) return;

        if (player != null)
        {
            float distance = Vector2.Distance(transform.position, player.position);

            if (distance < detectionRange)
            {
                // ✅ اگر در محدوده شلیک است
                if (distance >= minAttackDistance && distance <= attackRange)
                {
                    // توقف و شلیک
                    moveDirection = Vector2.zero;
                    
                    // رو به بازیکن
                    FacePlayer();
                    
                    // شلیک با cooldown
                    if (Time.time - lastFireTime >= fireRate)
                    {
                        FireSpell();
                    }
                }
                // اگر خیلی دور است، تعقیب کن
                else if (distance > attackRange)
                {
                    ChasePlayer();
                }
                // اگر خیلی نزدیک است، عقب‌گرد
                else if (distance < minAttackDistance)
                {
                    RetreatFromPlayer();
                }
            }
            else
            {
                // خارج از محدوده - توقف
                moveDirection = Vector2.zero;
            }
        }

        UpdateAnimation();
    }

    void FixedUpdate()
    {
        if (!isStunned && !isFalling && moveDirection != Vector2.zero)
        {
            rb.MovePosition(rb.position + moveDirection * chaseSpeed * Time.fixedDeltaTime);
        }
    }

    // ===== تعقیب =====
    void ChasePlayer()
    {
        Vector2 direction = (player.position - transform.position).normalized;
        moveDirection = direction;

        // فلیپ کردن Sprite
        if (direction.x < 0)
            spriteRenderer.flipX = true;
        else if (direction.x > 0)
            spriteRenderer.flipX = false;
    }

    void RetreatFromPlayer()
    {
        Vector2 direction = (transform.position - player.position).normalized;
        moveDirection = direction;

        // فلیپ کردن Sprite
        if (direction.x < 0)
            spriteRenderer.flipX = true;
        else if (direction.x > 0)
            spriteRenderer.flipX = false;
    }

    void FacePlayer()
    {
        if (player == null || spriteRenderer == null) return;
        
        Vector2 direction = (player.position - transform.position).normalized;
        
        if (direction.x < 0)
            spriteRenderer.flipX = true;
        else if (direction.x > 0)
            spriteRenderer.flipX = false;
    }

    // ===== شلیک طلسم =====
    void FireSpell()
    {
        if (spellPrefab == null)
        {
            Debug.LogWarning($"⚠️ {enemyName} cannot fire - no spell prefab!");
            return;
        }

        if (player == null)
        {
            Debug.LogWarning($"⚠️ {enemyName} cannot fire - no player target!");
            return;
        }

        lastFireTime = Time.time;

        Vector2 direction = (player.position - transform.position).normalized;
        
        // ✅ اضافه کردن offset برای spawn position
        Vector3 spawnPos = transform.position + (Vector3)(direction * 0.5f);
        
        GameObject spell = Instantiate(spellPrefab, spawnPos, Quaternion.identity);
        SpellController spellCtrl = spell.GetComponent<SpellController>();

        // تعیین رنگ طلسم بر اساس house
        Color spellColor = GetSpellColorByHouse();

        if (spellCtrl != null)
        {
            // ✅ IMPORTANT: Pass correct parameters
            spellCtrl.Initialize(
                direction,           // direction
                5f,                  // speed
                spellDamage,         // damage
                spellColor,          // color
                "EnemySpell",        // spell name
                "Enemy",             // caster type
                gameObject.name      // caster ID
            );
            
            Debug.Log($"⚡ {enemyName} fired spell at player!");
        }
        else
        {
            Debug.LogError($"❌ SpellController not found on spell prefab!");
            Destroy(spell);
        }
    }

    Color GetSpellColorByHouse()
    {
        switch (house.ToLower())
        {
            case "deatheater":
                return new Color(0f, 0.8f, 0f); // Green (Avada Kedavra style)
            case "dementor":
                return new Color(0.2f, 0.2f, 0.2f); // Dark gray
            case "slytherin":
                return new Color(0.1f, 0.8f, 0.1f); // Green
            case "ravenclaw":
                return new Color(0.2f, 0.4f, 1f); // Blue
            case "hufflepuff":
                return new Color(1f, 0.9f, 0.2f); // Yellow
            case "gryffindor":
                return Color.red; // Red
            default:
                return new Color(0.8f, 0f, 0.8f); // Purple (default)
        }
    }

    // ===== دریافت آسیب =====
    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);

        Debug.Log($"💔 {enemyName} took {damage} damage. Health: {currentHealth}/{maxHealth}");

        // افکت آسیب
        StartCoroutine(FlashRed());

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    IEnumerator FlashRed()
    {
        if (spriteRenderer != null)
        {
            Color original = spriteRenderer.color;
            spriteRenderer.color = Color.red;
            yield return new WaitForSeconds(0.1f);
            spriteRenderer.color = original;
        }
    }

    // ===== مرگ =====
    void Die()
    {
        Debug.Log($"💀 {enemyName} defeated!");

        // اضافه کردن XP
        XPManager xpManager = XPManager.Instance;
        if (xpManager != null)
        {
            int enemyLevel = 1;
            xpManager.AwardEnemyKillXP(house, enemyLevel);
        }

        // ثبت kill در آمار
        NetworkManager networkManager = NetworkManager.Instance;
        if (networkManager != null && networkManager.localPlayerData != null)
        {
            networkManager.localPlayerData.stats.totalKills++;
            networkManager.localPlayerData.stats.botKills++;
            networkManager.SavePlayerData();
        }

        // Animation و Effect
        if (house == "slytherin")
        {
            StartFallAnimation();
        }
        else
        {
            CreateDeathEffect();
            Destroy(gameObject);
        }
    }

    void StartFallAnimation()
    {
        isFalling = true;
        
        if (animator != null)
        {
            animator.SetTrigger("Fall");
        }

        Destroy(gameObject, 2f);
    }

    void CreateDeathEffect()
    {
        GameObject effect = new GameObject("DeathEffect");
        effect.transform.position = transform.position;

        ParticleSystem ps = effect.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startColor = new Color(0.5f, 0, 0.5f);
        main.startLifetime = 1f;
        main.maxParticles = 30;

        Destroy(effect, 2f);
    }

    // ===== استان شدن =====
    public void Stun(float duration)
    {
        StartCoroutine(StunCoroutine(duration));
    }

    IEnumerator StunCoroutine(float duration)
    {
        isStunned = true;
        moveDirection = Vector2.zero;
        
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.yellow;
        }

        yield return new WaitForSeconds(duration);

        isStunned = false;
        
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.white;
        }
    }

    // ===== انیمیشن =====
    void UpdateAnimation()
    {
        if (animator == null) return;

        bool isMoving = moveDirection.magnitude > 0.1f;
        animator.SetBool("isMoving", isMoving);
        animator.SetBool("isStunned", isStunned);
        animator.SetFloat("moveX", moveDirection.x);
        animator.SetFloat("moveY", moveDirection.y);
    }

    // ===== Gizmos =====
    void OnDrawGizmosSelected()
    {
        // نمایش محدوده تشخیص
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        
        // نمایش محدوده شلیک
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        // نمایش حداقل فاصله
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, minAttackDistance);
    }
}
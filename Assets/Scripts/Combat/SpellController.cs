using UnityEngine;

public class SpellController : MonoBehaviour
{
    [Header("Spell Properties")]
    public string spellName;
    public int damage;
    public float speed;
    public Color color;
    public string source; // "player", "enemy", "remote_player"
    public string casterId; // ID بازیکنی که کست کرده
    public float maxRange = 50f;

    [Header("Visual")]
    public TrailRenderer trail;
    public ParticleSystem particles;
    
    [Header("Network")]
    public bool isNetworked = false;

    private Vector2 direction;
    private float traveledDistance = 0f;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private CombatNetworkSync networkSync;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        networkSync = FindObjectOfType<CombatNetworkSync>();

        networkSync = FindObjectOfType<CombatNetworkSync>();
        if (networkSync == null)
        {
            Debug.LogWarning("⚠️ CombatNetworkSync not found in scene");
        }
            // 🔥 مطمئن شو Collider داری
        CircleCollider2D col = GetComponent<CircleCollider2D>();
        if (col == null) col = gameObject.AddComponent<CircleCollider2D>();
        
        col.isTrigger = true;
        col.radius = 0.3f; // 👈 بسته به سایز Spell، مثلاً 0.3 تا 0.5
    }

    public void Initialize(Vector2 dir, float spd, int dmg, Color col, string name, string src, string caster = null)
    {
        direction = dir.normalized;
        speed = spd;
        damage = dmg;
        color = col;
        spellName = name;
        source = src;
        casterId = caster;

        // تنظیم بصری
        if (spriteRenderer != null)
        {
            spriteRenderer.color = color;
        }

        if (trail != null)
        {
            trail.startColor = color;
            trail.endColor = new Color(color.r, color.g, color.b, 0);
        }

        if (particles != null)
        {
            var main = particles.main;
            main.startColor = color;
        }

        gameObject.layer = LayerMask.NameToLayer("Spell");

        CircleCollider2D col2d = GetComponent<CircleCollider2D>();
        if (col2d != null)
        {
            col2d.isTrigger = true;
        }

        Debug.Log($"✨ {spellName} created by {source} (Caster: {casterId}, Networked: {isNetworked})");
    }

    void FixedUpdate()
    {
        if (rb != null)
        {
            Vector2 movement = direction * speed * Time.fixedDeltaTime;
            rb.MovePosition(rb.position + movement);

            traveledDistance += movement.magnitude;

            if (traveledDistance >= maxRange)
            {
                DestroySpell();
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // برخورد با دیوار
        if (other.gameObject.layer == LayerMask.NameToLayer("Wall"))
        {
            DestroySpell();
            return;
        }

        // ===== طلسم بازیکن local به دشمن =====
        if (source == "player" && other.CompareTag("Enemy"))
        {
            EnemyController enemy = other.GetComponent<EnemyController>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
                CreateHitEffect();
                DestroySpell();
            }
            return;
        }

        // ===== طلسم دشمن به بازیکن local =====
        if (source == "enemy" && other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                player.TakeDamage(damage);
                CreateHitEffect();
                DestroySpell();
            }
            return;
        }
        
        // ===== 🆕 طلسم بازیکن local به بازیکن remote =====
        if (source == "player" && other.CompareTag("RemotePlayer"))
        {
            RemotePlayerController remotePlayer = other.GetComponent<RemotePlayerController>();
            if (remotePlayer != null)
            {
                // ✅ ارسال damage به سرور
                if (networkSync != null)
                {
                    NetworkManager nm = NetworkManager.Instance;
                    if (nm != null && nm.isAuthenticated)
                    {
                        networkSync.SendAttack(remotePlayer.playerId, damage, spellName);
                        Debug.Log($"⚔️ Hit remote player: {remotePlayer.username} with {spellName}");
                    }
                }
                
                // نمایش visual local
                remotePlayer.TakeDamage(damage);
                CreateHitEffect();
                DestroySpell();
            }
            return;
        }
        
        // ===== 🆕 طلسم بازیکن remote به بازیکن local =====
        if (source == "remote_player" && other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                player.TakeDamage(damage);
                CreateHitEffect();
                
                // ✅ اطلاع به سرور که ما damage خوردیم
                if (networkSync != null && !string.IsNullOrEmpty(casterId))
                {
                    NetworkManager nm = NetworkManager.Instance;
                    if (nm != null && nm.isAuthenticated)
                    {
                        // می‌تونی اینجا یه confirmation بفرستی به سرور
                        Debug.Log($"💥 Took {damage} damage from {casterId}");
                    }
                }
                
                DestroySpell();
            }
            return;
        }

        // ===== برخورد طلسم‌ها با یکدیگر =====
        SpellController otherSpell = other.GetComponent<SpellController>();
        if (otherSpell != null && otherSpell.source != source)
        {
            CreateCollisionEffect(otherSpell);
            
            if (damage <= otherSpell.damage)
            {
                DestroySpell();
            }
        }
    }

    void CreateHitEffect()
    {
        GameObject hitEffect = new GameObject("HitEffect");
        hitEffect.transform.position = transform.position;

        ParticleSystem ps = hitEffect.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startColor = color;
        main.startLifetime = 0.5f;
        main.startSpeed = 5f;
        main.maxParticles = 20;

        Destroy(hitEffect, 1f);
    }

    void CreateCollisionEffect(SpellController other)
    {
        GameObject collisionEffect = new GameObject("SpellCollision");
        collisionEffect.transform.position = (transform.position + other.transform.position) / 2f;

        LineRenderer line = collisionEffect.AddComponent<LineRenderer>();
        line.startWidth = 0.2f;
        line.endWidth = 0.2f;
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.startColor = color;
        line.endColor = other.color;
        line.SetPosition(0, transform.position);
        line.SetPosition(1, other.transform.position);

        Destroy(collisionEffect, 0.5f);

        Debug.Log($"💥 Spell collision: {spellName} vs {other.spellName}");
    }

    void DestroySpell()
    {
        Destroy(gameObject);
    }
}
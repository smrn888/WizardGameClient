using UnityEngine;
using System;

/// <summary>
/// ⚔️ سینک کردن Combat با سرور
/// ✅ FIXED: تمام خطاهای CS1061 برطرف شد
/// </summary>
public class CombatNetworkSync : MonoBehaviour
{
    public static CombatNetworkSync Instance { get; private set; }

    private PlayerController playerController;
    private NetworkManager networkManager;
    private MultiplayerManager multiplayerManager;

    [Header("Spell Sync")]
    [Tooltip("پرفب برای ساخت طلسم‌های remote")]
    [SerializeField] private GameObject spellPrefab;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        playerController = FindObjectOfType<PlayerController>();
        networkManager = NetworkManager.Instance;
        multiplayerManager = MultiplayerManager.Instance;

        if (networkManager == null || multiplayerManager == null)
        {
            Debug.LogError("❌ NetworkManager or MultiplayerManager not found!");
            enabled = false;
            return;
        }

        // ✅ ثبت نام در Eventها
        SubscribeToNetworkEvents();

        Debug.Log($"✅ CombatNetworkSync initialized (Event-Driven mode)");
    }

    private void SubscribeToNetworkEvents()
    {
        networkManager.OnConnected += OnNetworkConnected;
        networkManager.OnDisconnected += OnNetworkDisconnected;
        
        // ✅ استفاده از Eventهای موجود در NetworkManager
        networkManager.OnSpellCasted += HandleRemoteSpellCast;
        networkManager.OnDamageReceived += HandleRemoteDamageReceived;
        networkManager.OnPlayerDied += HandleRemotePlayerDied;
    }

    void OnNetworkConnected()
    {
        Debug.Log("Combat Sync is listening to Socket.IO events.");
    }

    void OnNetworkDisconnected()
    {
        Debug.Log("Combat Sync stopped listening (disconnected).");
    }

    // ================== Event Handlers ==================

    public void HandleRemoteSpellCast(SpellCastData spellData)
    {
        // فیلتر کردن طلسم‌های خودمان
        if (spellData.casterId == networkManager.playerId)
        {
            return; 
        }

        SpawnRemoteSpell(spellData);
    }
    
    void SpawnRemoteSpell(SpellCastData spellData)
    {
        if (spellPrefab == null)
        {
            Debug.LogWarning("⚠️ Spell prefab not assigned!");
            return;
        }

        try
        {
            Vector3 spawnPos = spellData.position.ToVector3();
            Vector2 direction = spellData.direction.ToVector2();
            Color spellColor = spellData.color.ToColor();
            
            GameObject spellObj = Instantiate(spellPrefab, spawnPos, Quaternion.identity);
            SpellController spellCtrl = spellObj.GetComponent<SpellController>();
            
            if (spellCtrl != null)
            {
                spellCtrl.Initialize(
                    direction,
                    spellData.speed,
                    spellData.damage,
                    spellColor,
                    spellData.spellName,
                    "Remote_Spell_Tag",
                    spellData.casterId
                );
            }
            
            Debug.Log($"✨ Remote spell spawned: {spellData.spellName} by {spellData.casterName}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"❌ Failed to spawn remote spell: {ex.Message}");
        }
    }

    public void HandleRemoteDamageReceived(DamageReceivedData damageData)
    {
        if (playerController == null || damageData.targetId != networkManager.playerId) return;
        
        Debug.Log($"💔 Local Player took {damageData.damage:F1} damage from {damageData.attackerId} ({damageData.source})");
        
        playerController.TakeDamage((int)damageData.damage);
    }
    
    public void HandleRemotePlayerDied(PlayerDeathData deathData)
    {
        if (deathData.playerId == networkManager.playerId)
        {
            Debug.Log($"💀 Local player death confirmed by server.");
        }
        else
        {
            RemotePlayerController remote = multiplayerManager.GetRemotePlayer(deathData.playerId);
            if (remote != null)
            {
                Debug.Log($"💀 Remote player {remote.username} died (Killer: {deathData.killerId}).");
                multiplayerManager.RemoveRemotePlayer(deathData.playerId);
            }
        }
    }

    // ================== Send Methods ==================

    public void BroadcastSpell(Vector3 position, Vector2 direction, string spellName, Color color, int damage, float speed)
    {
        if (networkManager == null || !networkManager.isAuthenticated)
        {
            Debug.LogWarning("⚠️ NetworkManager not ready for spell broadcast");
            return;
        }
        
        // ✅ استفاده از متد موجود در NetworkManager
        networkManager.SendSpellCast(
            spellName, 
            position, 
            direction, 
            color, 
            damage, 
            speed
        );
        
        Debug.Log($"📤 Sent spell: {spellName}");
    }

    public void SendAttack(string targetPlayerId, float damage, string spellName)
    {
        if (networkManager == null || !networkManager.isAuthenticated)
        {
            Debug.LogWarning("⚠️ Not authenticated - cannot send damage");
            return;
        }
        
        // ✅ استفاده از متد موجود در NetworkManager
        networkManager.SendDamageDealt(targetPlayerId, damage, spellName);
        
        Debug.Log($"⚔️ Dealt {damage:F1} to {targetPlayerId} with {spellName}");
    }
    
    // ================== HTTP API Calls ==================
    
    public void GetCombatStatus(Action<CombatStatus> callback)
    {
        if (networkManager == null || !networkManager.isAuthenticated)
        {
            Debug.LogWarning("⚠️ Not authenticated for GetCombatStatus");
            callback?.Invoke(null);
            return;
        }
        
        try
        {
            networkManager.apiClient.Get(
                $"/api/combat/status/{networkManager.playerId}",
                (success, response) =>
                {
                    if (success)
                    {
                        try
                        {
                            CombatStatus status = JsonUtility.FromJson<CombatStatus>(response);
                            callback?.Invoke(status);
                            Debug.Log($"📊 Combat Status received: Active Combats={status.activeCombats}");
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"❌ Failed to parse combat status: {ex.Message}");
                            callback?.Invoke(null);
                        }
                    }
                    else
                    {
                        Debug.LogError($"❌ Failed to get combat status: {response}");
                        callback?.Invoke(null);
                    }
                },
                networkManager.sessionToken
            );
        }
        catch (Exception ex)
        {
            Debug.LogError($"❌ Error in GetCombatStatus: {ex.Message}");
            callback?.Invoke(null);
        }
    }

    // ================== Cleanup ==================

    void OnDestroy()
    {
        if (networkManager != null)
        {
            networkManager.OnConnected -= OnNetworkConnected;
            networkManager.OnDisconnected -= OnNetworkDisconnected;
            networkManager.OnSpellCasted -= HandleRemoteSpellCast;
            networkManager.OnDamageReceived -= HandleRemoteDamageReceived;
            networkManager.OnPlayerDied -= HandleRemotePlayerDied;
        }
    }
}
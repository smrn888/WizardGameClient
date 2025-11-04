using UnityEngine;
using System.IO;
using System;

/// <summary>
/// مدیریت ذخیره و بارگذاری داده‌های محلی
/// از PlayerPrefs و فایل‌های JSON استفاده می‌کند
/// ✅ FIXED: Proper Singleton with DontDestroyOnLoad
/// </summary>
public class SaveManager : MonoBehaviour
{
    // Singleton
    public static SaveManager Instance { get; private set; }

    private const string SESSION_KEY = "session";
    private const string PLAYER_DATA_FILE = "playerdata.json";
    private const string SETTINGS_FILE = "settings.json";

    private string savePath;

    void Awake()
    {
        // ✅ FIXED: Proper Singleton pattern with DontDestroyOnLoad
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        // مسیر ذخیره‌سازی
        savePath = Application.persistentDataPath;
        Debug.Log($"💾 SaveManager initialized. Save path: {savePath}");
    }

    // ===== Session Management =====

    /// <summary>
    /// ذخیره Session کامل (با username و house)
    /// </summary>
    public void SaveSession(string token, string playerId)
    {
        var session = new SessionData
        {
            token = token,
            playerId = playerId
        };
        
        string json = JsonUtility.ToJson(session);
        PlayerPrefs.SetString(SESSION_KEY, json);
        PlayerPrefs.Save();
        
        Debug.Log($"💾 Session saved: {playerId}");
    }

    /// <summary>
    /// بارگذاری Session - استفاده از out parameters
    /// </summary>
    public bool LoadSession(out string token, out string playerId)
    {
        token = null;
        playerId = null;

        if (PlayerPrefs.HasKey(SESSION_KEY))
        {
            string json = PlayerPrefs.GetString(SESSION_KEY);
            
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    SessionData session = JsonUtility.FromJson<SessionData>(json);
                    token = session.token;
                    playerId = session.playerId;
                    Debug.Log($"💾 Session loaded: {playerId}");
                    return true;
                }
                catch (Exception e)
                {
                    Debug.LogError($"❌ Failed to parse session: {e.Message}");
                    return false;
                }
            }
        }
        
        Debug.Log("💾 No session found");
        return false;
    }

    /// <summary>
    /// پاک کردن Session
    /// </summary>
    public void ClearSession()
    {
        PlayerPrefs.DeleteKey(SESSION_KEY);
        PlayerPrefs.Save();
        
        Debug.Log("💾 Session cleared");
    }

    // ===== Player Data Management =====

    /// <summary>
    /// ذخیره داده‌های بازیکن (به صورت محلی)
    /// </summary>
    public void SavePlayerData(PlayerData playerData)
    {
        try
        {
            string json = JsonUtility.ToJson(playerData, true);
            string filePath = Path.Combine(savePath, PLAYER_DATA_FILE);
            
            File.WriteAllText(filePath, json);
            
            Debug.Log($"💾 Player data saved locally: {playerData.username}");
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Failed to save player data: {e.Message}");
        }
    }

    /// <summary>
    /// بارگذاری داده‌های بازیکن (از فایل محلی)
    /// </summary>
    public PlayerData LoadPlayerData()
    {
        try
        {
            string filePath = Path.Combine(savePath, PLAYER_DATA_FILE);
            
            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                PlayerData playerData = JsonUtility.FromJson<PlayerData>(json);
                
                Debug.Log($"💾 Player data loaded from cache: {playerData.username}");
                return playerData;
            }
            else
            {
                Debug.Log("💾 No cached player data found");
                return null;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Failed to load player data: {e.Message}");
            return null;
        }
    }

    /// <summary>
    /// پاک کردن داده‌های بازیکن
    /// </summary>
    public void ClearPlayerData()
    {
        try
        {
            string filePath = Path.Combine(savePath, PLAYER_DATA_FILE);
            
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                Debug.Log("💾 Player data cleared");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Failed to clear player data: {e.Message}");
        }
    }

    // ===== Settings Management =====

    /// <summary>
    /// ذخیره تنظیمات بازی
    /// </summary>
    public void SaveSettings(GameSettings settings)
    {
        try
        {
            string json = JsonUtility.ToJson(settings, true);
            string filePath = Path.Combine(savePath, SETTINGS_FILE);
            
            File.WriteAllText(filePath, json);
            
            Debug.Log("💾 Settings saved");
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Failed to save settings: {e.Message}");
        }
    }

    /// <summary>
    /// بارگذاری تنظیمات بازی
    /// </summary>
    public GameSettings LoadSettings()
    {
        try
        {
            string filePath = Path.Combine(savePath, SETTINGS_FILE);
            
            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                GameSettings settings = JsonUtility.FromJson<GameSettings>(json);
                
                Debug.Log("💾 Settings loaded");
                return settings;
            }
            else
            {
                Debug.Log("💾 No settings found, using defaults");
                return new GameSettings();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Failed to load settings: {e.Message}");
            return new GameSettings();
        }
    }

    // ===== Quick Save/Load =====

    public void QuickSave(string key, string value)
    {
        PlayerPrefs.SetString(key, value);
        PlayerPrefs.Save();
    }

    public string QuickLoad(string key, string defaultValue = "")
    {
        return PlayerPrefs.GetString(key, defaultValue);
    }

    public void SaveInt(string key, int value)
    {
        PlayerPrefs.SetInt(key, value);
        PlayerPrefs.Save();
    }

    public int LoadInt(string key, int defaultValue = 0)
    {
        return PlayerPrefs.GetInt(key, defaultValue);
    }

    public void SaveFloat(string key, float value)
    {
        PlayerPrefs.SetFloat(key, value);
        PlayerPrefs.Save();
    }

    public float LoadFloat(string key, float defaultValue = 0f)
    {
        return PlayerPrefs.GetFloat(key, defaultValue);
    }

    public void SaveBool(string key, bool value)
    {
        PlayerPrefs.SetInt(key, value ? 1 : 0);
        PlayerPrefs.Save();
    }

    public bool LoadBool(string key, bool defaultValue = false)
    {
        return PlayerPrefs.GetInt(key, defaultValue ? 1 : 0) == 1;
    }

    // ===== Utility Methods =====

    public void ClearAllData()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();

        try
        {
            string[] files = { PLAYER_DATA_FILE, SETTINGS_FILE };
            
            foreach (string file in files)
            {
                string filePath = Path.Combine(savePath, file);
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
            
            Debug.Log("💾 All data cleared");
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Failed to clear all data: {e.Message}");
        }
    }

    public bool HasSaveData()
    {
        string filePath = Path.Combine(savePath, PLAYER_DATA_FILE);
        return File.Exists(filePath);
    }

    public long GetSaveFileSize()
    {
        string filePath = Path.Combine(savePath, PLAYER_DATA_FILE);
        
        if (File.Exists(filePath))
        {
            FileInfo fileInfo = new FileInfo(filePath);
            return fileInfo.Length;
        }
        
        return 0;
    }

    public void BackupPlayerData()
    {
        try
        {
            string filePath = Path.Combine(savePath, PLAYER_DATA_FILE);
            string backupPath = Path.Combine(savePath, $"playerdata_backup_{DateTime.Now:yyyyMMdd_HHmmss}.json");
            
            if (File.Exists(filePath))
            {
                File.Copy(filePath, backupPath);
                Debug.Log($"💾 Backup created: {backupPath}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Failed to create backup: {e.Message}");
        }
    }

    public void RestoreFromBackup(string backupFileName)
    {
        try
        {
            string backupPath = Path.Combine(savePath, backupFileName);
            string filePath = Path.Combine(savePath, PLAYER_DATA_FILE);
            
            if (File.Exists(backupPath))
            {
                File.Copy(backupPath, filePath, true);
                Debug.Log($"💾 Restored from backup: {backupFileName}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Failed to restore backup: {e.Message}");
        }
    }
}
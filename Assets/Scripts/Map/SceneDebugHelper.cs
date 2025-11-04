using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// ابزار Debug برای بررسی مشکلات Scene Management
/// استفاده: اضافه کنید به یک Empty GameObject در هر Scene
/// کلید F3 برای نمایش اطلاعات
/// </summary>
public class SceneDebugHelper : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private bool autoLogOnSceneLoad = true;
    [SerializeField] private KeyCode debugKey = KeyCode.F3;
    
    void Start()
    {
        if (autoLogOnSceneLoad)
        {
            LogSceneInfo();
        }
    }
    
    void Update()
    {
        if (Input.GetKeyDown(debugKey))
        {
            LogSceneInfo();
        }
    }
    
    void LogSceneInfo()
    {
        Debug.Log("========== SCENE DEBUG INFO ==========");
        Debug.Log($"🎬 Current Scene: {SceneManager.GetActiveScene().name}");
        Debug.Log($"📊 Total Scenes Loaded: {SceneManager.sceneCount}");
        
        // لیست تمام Scene های لود شده
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            Debug.Log($"  Scene {i}: {scene.name} (Loaded: {scene.isLoaded}, Valid: {scene.IsValid()})");
        }
        
        Debug.Log("\n🎮 GAME MANAGERS:");
        LogManager<GameManager>("GameManager");
        LogManager<MapManager>("MapManager");
        LogManager<NetworkManager>("NetworkManager");
        LogManager<SaveManager>("SaveManager");
        LogManager<MultiplayerManager>("MultiplayerManager");
        LogManager<EnemySpawnerManager>("EnemySpawnerManager");
        LogManager<PauseMenuManager>("PauseMenuManager");
        
        Debug.Log("\n👤 PLAYER & ENEMIES:");
        LogObjects<PlayerController>("PlayerController");
        LogObjects<EnemyController>("EnemyController");
        
        Debug.Log("\n📦 DONTDESTROYONLOAD OBJECTS:");
        LogDontDestroyOnLoadObjects();
        
        Debug.Log("======================================");
    }
    
    void LogManager<T>(string name) where T : MonoBehaviour
    {
        T manager = FindFirstObjectByType<T>();
        if (manager != null)
        {
            Scene scene = manager.gameObject.scene;
            Debug.Log($"  ✅ {name} found in scene: {scene.name}");
        }
        else
        {
            Debug.Log($"  ❌ {name} NOT FOUND!");
        }
    }
    
    void LogObjects<T>(string name) where T : MonoBehaviour
    {
        T[] objects = FindObjectsOfType<T>();
        if (objects.Length > 0)
        {
            Debug.Log($"  ✅ Found {objects.Length} {name}(s):");
            foreach (T obj in objects)
            {
                Scene scene = obj.gameObject.scene;
                Debug.Log($"     - {obj.gameObject.name} in scene: {scene.name}");
            }
        }
        else
        {
            Debug.Log($"  ℹ️ No {name} objects found");
        }
    }
    
    void LogDontDestroyOnLoadObjects()
    {
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        int count = 0;
        
        foreach (GameObject obj in allObjects)
        {
            if (obj.scene.name == "DontDestroyOnLoad")
            {
                count++;
                Debug.Log($"  - {obj.name}");
            }
        }
        
        if (count == 0)
        {
            Debug.Log("  ℹ️ No objects in DontDestroyOnLoad");
        }
        else
        {
            Debug.Log($"  Total: {count} objects");
        }
    }
    
    // متد عمومی برای فراخوانی از جای دیگر
    public static void LogCurrentSceneState()
    {
        SceneDebugHelper helper = FindFirstObjectByType<SceneDebugHelper>();
        if (helper != null)
        {
            helper.LogSceneInfo();
        }
        else
        {
            Debug.LogWarning("⚠️ SceneDebugHelper not found in scene!");
        }
    }
}
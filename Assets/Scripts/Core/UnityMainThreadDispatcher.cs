// ایجاد فایل جدید: Assets/Scripts/Utilities/UnityMainThreadDispatcher.cs

using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// این class باید تو MAIN MENU یا SCENE MANAGER باشه (DontDestroyOnLoad)
/// یا می‌تونید تو هر صحنه‌ای استفاده کنید
/// </summary>
public class UnityMainThreadDispatcher : MonoBehaviour
{
    private static UnityMainThreadDispatcher _instance;
    private Queue<System.Action> actionQueue = new Queue<System.Action>();

    public static UnityMainThreadDispatcher Instance()
    {
        if (_instance == null)
        {
            GameObject obj = new GameObject("UnityMainThreadDispatcher");
            _instance = obj.AddComponent<UnityMainThreadDispatcher>();
            DontDestroyOnLoad(obj);
            Debug.Log("✅ UnityMainThreadDispatcher created");
        }
        return _instance;
    }

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void Enqueue(System.Action action)
    {
        if (action == null) return;
        
        lock (actionQueue)
        {
            actionQueue.Enqueue(action);
        }
    }

    void Update()
    {
        lock (actionQueue)
        {
            while (actionQueue.Count > 0)
            {
                try
                {
                    actionQueue.Dequeue()?.Invoke();
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"❌ Error in main thread action: {ex.Message}");
                }
            }
        }
    }
}
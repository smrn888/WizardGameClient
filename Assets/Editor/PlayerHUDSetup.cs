#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

/// <summary>
/// 🔧 ابزار خودکار برای ساخت PlayerHUD در بازی
/// این اسکریپت باید در پوشه Editor قرار بگیرد
/// </summary>
public class PlayerHUDSetup : EditorWindow
{
    private string hudObjectName = "PlayerHUD";
    private bool attachToCanvas = true;
    private bool createNewCanvas = false;
    
    [MenuItem("Tools/🎮 Create Player HUD")]
    public static void ShowWindow()
    {
        GetWindow<PlayerHUDSetup>("Player HUD Setup");
    }
    
    void OnGUI()
    {
        GUILayout.Label("🎯 Player HUD Auto-Setup", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        hudObjectName = EditorGUILayout.TextField("HUD Object Name:", hudObjectName);
        attachToCanvas = EditorGUILayout.Toggle("Attach to Canvas:", attachToCanvas);
        
        if (attachToCanvas)
        {
            Canvas existingCanvas = FindObjectOfType<Canvas>();
            if (existingCanvas == null)
            {
                EditorGUILayout.HelpBox("⚠️ No Canvas found in scene. Will create new Canvas.", MessageType.Warning);
                createNewCanvas = true;
            }
            else
            {
                EditorGUILayout.HelpBox($"✅ Found Canvas: {existingCanvas.name}", MessageType.Info);
                createNewCanvas = false;
            }
        }
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("🔨 Create Player HUD", GUILayout.Height(40)))
        {
            CreatePlayerHUD();
        }
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("🔍 Find Existing HUD"))
        {
            FindExistingHUD();
        }
        
        if (GUILayout.Button("🗑️ Delete All HUDs"))
        {
            DeleteAllHUDs();
        }
    }
    
    void CreatePlayerHUD()
    {
        // بررسی وجود HUD قبلی
        PlayerHUDManager existingHUD = FindObjectOfType<PlayerHUDManager>();
        if (existingHUD != null)
        {
            bool replace = EditorUtility.DisplayDialog(
                "HUD Exists",
                "A PlayerHUD already exists. Replace it?",
                "Replace",
                "Cancel"
            );
            
            if (replace)
            {
                DestroyImmediate(existingHUD.gameObject);
            }
            else
            {
                return;
            }
        }
        
        // ساخت GameObject اصلی
        GameObject hudObject = new GameObject(hudObjectName);
        PlayerHUDManager hudManager = hudObject.AddComponent<PlayerHUDManager>();
        
        // اگر باید به Canvas وصل شود
        if (attachToCanvas)
        {
            Canvas canvas = FindObjectOfType<Canvas>();
            
            if (canvas == null && createNewCanvas)
            {
                canvas = CreateCanvas();
            }
            
            if (canvas != null)
            {
                hudObject.transform.SetParent(canvas.transform, false);
            }
        }
        
        // ثبت در Undo برای امکان برگشت
        Undo.RegisterCreatedObjectUndo(hudObject, "Create Player HUD");
        
        // انتخاب آبجکت ساخته شده
        Selection.activeGameObject = hudObject;
        
        // فراخوانی متد ساخت UI با Reflection
        var method = typeof(PlayerHUDManager).GetMethod("CreateHUDFromScratch", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (method != null)
        {
            method.Invoke(hudManager, null);
        }
        
        // ذخیره تغییرات
        EditorUtility.SetDirty(hudObject);
        
        Debug.Log($"✅ PlayerHUD created successfully: {hudObject.name}");
        EditorUtility.DisplayDialog(
            "Success!",
            $"Player HUD created at: {hudObject.name}\n\n" +
            "UI elements have been auto-generated.\n" +
            "Check the Inspector to customize settings.",
            "OK"
        );
    }
    
    Canvas CreateCanvas()
    {
        GameObject canvasObj = new GameObject("MainCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        
        canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        
        Undo.RegisterCreatedObjectUndo(canvasObj, "Create Canvas");
        
        Debug.Log("✅ Canvas created");
        return canvas;
    }
    
    void FindExistingHUD()
    {
        PlayerHUDManager[] allHUDs = FindObjectsOfType<PlayerHUDManager>();
        
        if (allHUDs.Length == 0)
        {
            EditorUtility.DisplayDialog("Not Found", "No PlayerHUD found in scene.", "OK");
            return;
        }
        
        string message = $"Found {allHUDs.Length} HUD(s):\n\n";
        foreach (var hud in allHUDs)
        {
            message += $"• {hud.gameObject.name}\n";
        }
        
        EditorUtility.DisplayDialog("HUD Found", message, "OK");
        
        // انتخاب اولین HUD
        Selection.activeGameObject = allHUDs[0].gameObject;
    }
    
    void DeleteAllHUDs()
    {
        PlayerHUDManager[] allHUDs = FindObjectsOfType<PlayerHUDManager>();
        
        if (allHUDs.Length == 0)
        {
            EditorUtility.DisplayDialog("Not Found", "No PlayerHUD found in scene.", "OK");
            return;
        }
        
        bool confirm = EditorUtility.DisplayDialog(
            "Delete All HUDs?",
            $"This will delete {allHUDs.Length} HUD object(s). Continue?",
            "Delete",
            "Cancel"
        );
        
        if (confirm)
        {
            foreach (var hud in allHUDs)
            {
                Undo.DestroyObjectImmediate(hud.gameObject);
            }
            
            Debug.Log($"🗑️ Deleted {allHUDs.Length} HUD(s)");
        }
    }
    
    [MenuItem("GameObject/UI/Player HUD", false, 10)]
    static void CreatePlayerHUDFromMenu(MenuCommand menuCommand)
    {
        // ساخت از منوی راست کلیک
        GameObject hudObject = new GameObject("PlayerHUD");
        PlayerHUDManager hudManager = hudObject.AddComponent<PlayerHUDManager>();
        
        // اگر Parent انتخاب شده است
        GameObjectUtility.SetParentAndAlign(hudObject, menuCommand.context as GameObject);
        
        // ثبت Undo
        Undo.RegisterCreatedObjectUndo(hudObject, "Create Player HUD");
        
        // انتخاب
        Selection.activeObject = hudObject;
        
        // ساخت UI
        hudManager.SendMessage("CreateHUDFromScratch", SendMessageOptions.DontRequireReceiver);
        
        Debug.Log("✅ PlayerHUD created from menu");
    }
    
    [MenuItem("CONTEXT/PlayerHUDManager/🔨 Rebuild UI")]
    static void RebuildUI(MenuCommand command)
    {
        PlayerHUDManager hudManager = command.context as PlayerHUDManager;
        
        if (hudManager != null)
        {
            bool confirm = EditorUtility.DisplayDialog(
                "Rebuild UI?",
                "This will destroy and recreate all UI elements. Continue?",
                "Rebuild",
                "Cancel"
            );
            
            if (confirm)
            {
                hudManager.SendMessage("CreateHUDFromScratch", SendMessageOptions.DontRequireReceiver);
                EditorUtility.SetDirty(hudManager.gameObject);
                Debug.Log("✅ UI rebuilt successfully");
            }
        }
    }
}

/// <summary>
/// 🎯 Quick Setup - دکمه سریع در Scene
/// </summary>
[InitializeOnLoad]
public class PlayerHUDQuickSetup
{
    static PlayerHUDQuickSetup()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }
    
    static void OnSceneGUI(SceneView sceneView)
    {
        Handles.BeginGUI();
        
        GUILayout.BeginArea(new Rect(10, 10, 200, 100));
        
        PlayerHUDManager existingHUD = Object.FindObjectOfType<PlayerHUDManager>();
        
        if (existingHUD == null)
        {
            if (GUILayout.Button("🎮 Quick Setup HUD", GUILayout.Height(30)))
            {
                PlayerHUDSetup.ShowWindow();
            }
        }
        else
        {
            GUILayout.Label($"HUD: {existingHUD.name}", EditorStyles.boldLabel);
            if (GUILayout.Button("Select HUD"))
            {
                Selection.activeGameObject = existingHUD.gameObject;
            }
        }
        
        GUILayout.EndArea();
        
        Handles.EndGUI();
    }
}
#endif
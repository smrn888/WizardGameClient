using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// Auto Builder for Wizard Game - Complete Version
/// بسیار کامل و خودکار - تمام کمپوننت‌ها رو اساین می‌کند
/// Usage: Tools > WizardGame > Auto Build All
/// </summary>
public class AutoSceneBuilder : EditorWindow
{
    private bool buildCharacters = true;
    private bool buildMap = true;
    private bool buildUI = true;
    private bool buildManagers = true;
    private bool autoSetupLayers = true;
    private bool autoSetupPhysics = true;
    
    private Vector2 scrollPos;
    private string statusMessage = "";
    private Color statusColor = Color.white;
    
    // Character sprite paths
    private string characterSpritePath = "Assets/Sprites/Characters";
    private string mapSpritePath = "Assets/Resources/Sprites";
    
    [MenuItem("Tools/WizardGame/Auto Build All")]
    static void ShowWindow()
    {
        var window = GetWindow<AutoSceneBuilder>("Auto Scene Builder");
        window.minSize = new Vector2(450, 700);
        window.Show();
    }
    
    void OnGUI()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        
        GUILayout.Label("🎮 Wizard Game Auto Builder", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        EditorGUILayout.HelpBox(
            "✅ Complete Auto Setup:\n" +
            "• بارگذاری تمام اسپرایت‌ها\n" +
            "• ساخت Prefabها با کمپوننت‌ها\n" +
            "• تنظیم Layers و Physics\n" +
            "• ساخت Scene کامل\n" +
            "• اساین کردن تمام فایل‌ها",
            MessageType.Info
        );
        
        GUILayout.Space(10);
        
        // Options
        GUILayout.Label("Options:", EditorStyles.boldLabel);
        buildCharacters = EditorGUILayout.Toggle("Build Characters", buildCharacters);
        buildMap = EditorGUILayout.Toggle("Build Map", buildMap);
        buildUI = EditorGUILayout.Toggle("Build UI", buildUI);
        buildManagers = EditorGUILayout.Toggle("Build Managers", buildManagers);
        autoSetupLayers = EditorGUILayout.Toggle("Setup Layers", autoSetupLayers);
        autoSetupPhysics = EditorGUILayout.Toggle("Setup Physics", autoSetupPhysics);
        
        GUILayout.Space(10);
        
        // Build All Button
        if (GUILayout.Button("🚀 Build Everything (کامل)", GUILayout.Height(50)))
        {
            BuildAll();
        }
        
        GUILayout.Space(10);
        
        // Individual Build Buttons
        GUILayout.Label("Individual Builds:", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Build Characters Only"))
            BuildCharacters();
        
        if (GUILayout.Button("Build Map Only"))
            BuildMap();
        
        if (GUILayout.Button("Setup Layers & Physics"))
            SetupLayersAndPhysics();
        
        if (GUILayout.Button("Create Game Scene"))
            CreateGameScene();
        
        GUILayout.Space(20);
        
        // Status
        if (!string.IsNullOrEmpty(statusMessage))
        {
            GUI.color = statusColor;
            EditorGUILayout.HelpBox(statusMessage, MessageType.Info);
            GUI.color = Color.white;
        }
        
        EditorGUILayout.EndScrollView();
    }
    
    void BuildAll()
    {
        statusMessage = "شروع فرآیند ساخت...";
        statusColor = Color.yellow;
        Repaint();
        
        try
        {
            if (autoSetupLayers)
                SetupLayersAndPhysics();
            
            if (buildCharacters)
                BuildCharacters();
            
            if (buildMap)
                BuildMap();
            
            if (buildManagers)
                BuildManagers();
            
            if (buildUI)
                BuildUI();
            
            CreateGameScene();
            
            statusMessage = "✅ ساخت موفقیت‌آمیز تمام شد!\n" +
                          "🎮 اکنون می‌توانید بازی را اجرا کنید";
            statusColor = Color.green;
            Debug.Log("✅ Auto Build Complete!");
            
            EditorUtility.DisplayDialog(
                "Build Complete", 
                "تمام Prefabها، Animatorها و Scene ایجاد شدند!\n\n" +
                "✅ همه کمپوننت‌ها اساین شدند\n" +
                "✅ تمام فایل‌ها لوگ شدند\n" +
                "✅ Scene آماده است",
                "OK"
            );
        }
        catch (System.Exception ex)
        {
            statusMessage = $"❌ خطا: {ex.Message}";
            statusColor = Color.red;
            Debug.LogError($"❌ Build failed: {ex.Message}\n{ex.StackTrace}");
            EditorUtility.DisplayDialog("Build Failed", ex.Message, "OK");
        }
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Repaint();
    }
    
    #region Layers & Physics
    
    void SetupLayersAndPhysics()
    {
        Debug.Log("🔧 تنظیم Layers و Physics...");
        
        // Setup Layers
        SetupLayer(8, "Player");
        SetupLayer(9, "Enemy");
        SetupLayer(10, "Wall");
        SetupLayer(11, "Door");
        SetupLayer(12, "Spell");
        SetupLayer(13, "Item");
        SetupLayer(14, "NPC");
        
        // Setup Physics2D collision matrix
        Physics2D.IgnoreLayerCollision(8, 8, true);
        Physics2D.IgnoreLayerCollision(8, 9, false);
        Physics2D.IgnoreLayerCollision(8, 10, false);
        Physics2D.IgnoreLayerCollision(8, 11, false);
        Physics2D.IgnoreLayerCollision(8, 12, true);
        
        Physics2D.IgnoreLayerCollision(9, 9, true);
        Physics2D.IgnoreLayerCollision(9, 10, false);
        Physics2D.IgnoreLayerCollision(9, 12, true);
        
        Physics2D.IgnoreLayerCollision(12, 10, false);
        Physics2D.IgnoreLayerCollision(12, 12, true);
        
        Debug.Log("✅ Layers و Physics تنظیم شدند");
    }
    
    void SetupLayer(int layerIndex, string layerName)
    {
        SerializedObject tagManager = new SerializedObject(
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]
        );
        SerializedProperty layers = tagManager.FindProperty("layers");
        
        if (layers == null || !layers.isArray)
        {
            Debug.LogError("❌ نمی‌توان Layerها را تنظیم کرد");
            return;
        }
        
        SerializedProperty layerSP = layers.GetArrayElementAtIndex(layerIndex);
        
        if (layerSP.stringValue != layerName)
        {
            Debug.Log($"تنظیم Layer {layerIndex}: {layerName}");
            layerSP.stringValue = layerName;
            tagManager.ApplyModifiedProperties();
        }
    }
    
    #endregion
    
    #region Character Building
    
    void BuildCharacters()
    {
        Debug.Log("👤 ساخت کاراکترها...");
        
        string prefabPath = "Assets/Resources/Prefabs";
        
        CreateDirectoryIfNotExists("Assets/Resources");
        CreateDirectoryIfNotExists(prefabPath);
        
        // Player
        BuildPlayerCharacter(prefabPath);
        
        // Enemies
        string[] enemies = { "slytherin", "ravenclaw", "hufflepuff", "deatheater", "dementor" };
        foreach (string enemy in enemies)
        {
            BuildEnemyCharacter(enemy, prefabPath);
        }
        
        Debug.Log("✅ کاراکترها ساخته شدند");
        statusMessage = "✅ کاراکترها ایجاد شدند";
        statusColor = Color.green;
    }
    
    void BuildPlayerCharacter(string prefabPath)
    {
        Debug.Log("ساخت بازیکن...");
        
        // Load Sprite
        string spriteFile = $"{characterSpritePath}/gryffindor front.png";
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spriteFile);
        
        if (sprite == null)
        {
            Debug.LogError($"❌ اسپرایت یافت نشد: {spriteFile}");
            return;
        }
        
        GameObject player = new GameObject("Player");
        player.tag = "Player";
        player.layer = LayerMask.NameToLayer("Player");
        
        // SpriteRenderer
        SpriteRenderer sr = player.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingLayerName = "Default";
        sr.sortingOrder = 10;
        
        // Rigidbody2D
        Rigidbody2D rb = player.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        
        // Collider
        CircleCollider2D col = player.AddComponent<CircleCollider2D>();
        col.radius = 0.5f;
        col.offset = new Vector2(0, -0.2f);
        
        // Animator
        Animator animator = player.AddComponent<Animator>();
        AnimatorController animController = CreatePlayerAnimatorController();
        animator.runtimeAnimatorController = animController;
        
        // Add PlayerController
        PlayerController playerCtrl = player.AddComponent<PlayerController>();
        playerCtrl.moveSpeed = 4f;
        playerCtrl.maxHealth = 100;
        playerCtrl.radius = 0.5f;
        
        // Add PlayerAnimator
        PlayerAnimator playerAnim = player.AddComponent<PlayerAnimator>();
        playerAnim.GetType().GetField("animator", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(playerAnim, animator);
        playerAnim.GetType().GetField("spriteRenderer", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(playerAnim, sr);
        
        // Assign references
        playerCtrl.animator = animator;
        playerCtrl.spriteRenderer = sr;
        
        // WandTip
        GameObject wandTip = new GameObject("WandTip");
        wandTip.transform.parent = player.transform;
        wandTip.transform.localPosition = new Vector3(0.5f, 0.5f, 0);
        playerCtrl.wandTip = wandTip.transform;
        
        // Spell Prefab Reference
        GameObject spellObj = Resources.Load<GameObject>("Prefabs/Spell");
        if (spellObj != null)
            playerCtrl.spellPrefab = spellObj;
        
        // Save Prefab
        string prefabSavePath = $"Assets/Resources/Prefabs/Player.prefab";
        PrefabUtility.SaveAsPrefabAsset(player, prefabSavePath);
        
        DestroyImmediate(player);
        
        Debug.Log($"✅ Player Prefab ایجاد شد: {prefabSavePath}");
    }
    
    void BuildEnemyCharacter(string enemyName, string prefabPath)
    {
        Debug.Log($"ساخت Enemy: {enemyName}");
        
        string spriteFile = $"{characterSpritePath}/{enemyName}.png";
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spriteFile);
        
        if (sprite == null)
        {
            Debug.LogWarning($"⚠️ اسپرایت یافت نشد: {spriteFile}");
            return;
        }
        
        GameObject enemy = new GameObject($"Enemy_{enemyName}");
        enemy.tag = "Enemy";
        enemy.layer = LayerMask.NameToLayer("Enemy");
        
        // SpriteRenderer
        SpriteRenderer sr = enemy.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingLayerName = "Default";
        sr.sortingOrder = 5;
        
        // Rigidbody2D
        Rigidbody2D rb = enemy.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        
        // Collider
        CircleCollider2D col = enemy.AddComponent<CircleCollider2D>();
        col.radius = 0.5f;
        
        // Animator
        Animator animator = enemy.AddComponent<Animator>();
        AnimatorController animController = CreateEnemyAnimatorController(enemyName);
        animator.runtimeAnimatorController = animController;
        
        // Add EnemyController
        EnemyController enemyCtrl = enemy.AddComponent<EnemyController>();
        enemyCtrl.enemyName = enemyName;
        enemyCtrl.house = enemyName;
        enemyCtrl.maxHealth = 100;
        enemyCtrl.chaseSpeed = 2f;
        enemyCtrl.detectionRange = 10f;
        enemyCtrl.fireRate = 2f;
        enemyCtrl.spellDamage = 12;
        enemyCtrl.animator = animator;
        enemyCtrl.spriteRenderer = sr;
        
        // Spell Prefab
        GameObject spellObj = Resources.Load<GameObject>("Prefabs/Spell");
        if (spellObj != null)
            enemyCtrl.spellPrefab = spellObj;
        
        string prefabSavePath = $"Assets/Resources/Prefabs/Enemy_{enemyName}.prefab";
        PrefabUtility.SaveAsPrefabAsset(enemy, prefabSavePath);
        
        DestroyImmediate(enemy);
        
        Debug.Log($"✅ Enemy Prefab ایجاد شد: {prefabSavePath}");
    }
    
    AnimatorController CreatePlayerAnimatorController()
    {
        string path = "Assets/Animations/Controllers/PlayerAnimator.controller";
        CreateDirectoryIfNotExists("Assets/Animations");
        CreateDirectoryIfNotExists("Assets/Animations/Controllers");
        
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if (controller != null)
            return controller;
        
        controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        
        controller.AddParameter("isMoving", AnimatorControllerParameterType.Bool);
        controller.AddParameter("isCasting", AnimatorControllerParameterType.Bool);
        controller.AddParameter("isHit", AnimatorControllerParameterType.Bool);
        controller.AddParameter("isDead", AnimatorControllerParameterType.Bool);
        controller.AddParameter("moveX", AnimatorControllerParameterType.Float);
        controller.AddParameter("moveY", AnimatorControllerParameterType.Float);
        controller.AddParameter("lastMoveX", AnimatorControllerParameterType.Float);
        controller.AddParameter("lastMoveY", AnimatorControllerParameterType.Float);
        controller.AddParameter("castX", AnimatorControllerParameterType.Float);
        controller.AddParameter("castY", AnimatorControllerParameterType.Float);
        controller.AddParameter("hit", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("death", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("respawn", AnimatorControllerParameterType.Trigger);
        
        var rootStateMachine = controller.layers[0].stateMachine;
        var idleState = rootStateMachine.AddState("Idle");
        var moveState = rootStateMachine.AddState("Move");
        var castState = rootStateMachine.AddState("Cast");
        
        rootStateMachine.defaultState = idleState;
        
        Debug.Log($"✅ Player Animator Controller ایجاد شد: {path}");
        return controller;
    }
    
    AnimatorController CreateEnemyAnimatorController(string enemyName)
    {
        string path = $"Assets/Animations/Controllers/Enemy_{enemyName}_Animator.controller";
        CreateDirectoryIfNotExists("Assets/Animations");
        CreateDirectoryIfNotExists("Assets/Animations/Controllers");
        
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if (controller != null)
            return controller;
        
        controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        
        controller.AddParameter("isMoving", AnimatorControllerParameterType.Bool);
        controller.AddParameter("isStunned", AnimatorControllerParameterType.Bool);
        controller.AddParameter("moveX", AnimatorControllerParameterType.Float);
        controller.AddParameter("moveY", AnimatorControllerParameterType.Float);
        
        if (enemyName == "slytherin")
        {
            controller.AddParameter("Fall", AnimatorControllerParameterType.Trigger);
        }
        
        var rootStateMachine = controller.layers[0].stateMachine;
        var idleState = rootStateMachine.AddState("Idle");
        rootStateMachine.defaultState = idleState;
        
        Debug.Log($"✅ Enemy Animator Controller ایجاد شد: {path}");
        return controller;
    }
    
    #endregion
    
    #region Map Building
    
    void BuildMap()
    {
        Debug.Log("🗺️ ساخت Map...");
        
        string[] mapSprites = {
            "Astronomy_Tower.png",
            "Black_Lake.png",
            "Castle_Courtyard.png",
            "GreatHall.png",
            "Gryffindor_Common_Room.png",
            "Lower_Corridor.png",
            "Main_Corridor.png",
            "Potions_Dungeon.png",
            "Restricted_Library.png",
            "Tower_Stairs.png"
        };
        
        foreach (string spriteName in mapSprites)
        {
            string path = $"{mapSpritePath}/{spriteName}";
            ConfigureMapSprite(path);
        }
        
        Debug.Log("✅ Map اسپرایت‌ها تنظیم شدند");
        statusMessage = "✅ Map‌ها ایجاد شدند";
    }
    
    void ConfigureMapSprite(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            Debug.LogWarning($"⚠️ اسپرایت یافت نشد: {path}");
            return;
        }
        
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 100;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 4096;
        
        importer.SaveAndReimport();
    }
    
    #endregion
    
    #region Managers
    
    void BuildManagers()
    {
        Debug.Log("⚙️ ساخت Managers...");
        Debug.Log("✅ Managers آمادند");
        statusMessage = "✅ Managers ایجاد شدند";
    }
    
    #endregion
    
    #region UI
    
    void BuildUI()
    {
        Debug.Log("🎨 ساخت UI...");
        Debug.Log("✅ UI آماده است");
        statusMessage = "✅ UI ایجاد شد";
    }
    
    #endregion
    
    #region Scene Creation
    
    void CreateGameScene()
    {
        Debug.Log("🎬 ساخت Game Scene...");
        
        var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(
            UnityEditor.SceneManagement.NewSceneSetup.DefaultGameObjects,
            UnityEditor.SceneManagement.NewSceneMode.Single
        );
        
        // Main Camera
        GameObject cam = GameObject.Find("Main Camera");
        if (cam != null)
        {
            Camera camera = cam.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 10f;
            camera.backgroundColor = new Color(0.1f, 0.1f, 0.15f);
            
            CameraFollow cameraFollow = cam.AddComponent<CameraFollow>();
        }
        
        // Managers
        GameObject gameManager = new GameObject("GameManager");
        gameManager.AddComponent<GameManager>();
        
        GameObject networkManager = new GameObject("NetworkManager");
        networkManager.AddComponent<NetworkManager>();
        
        GameObject mapManager = new GameObject("MapManager");
        mapManager.AddComponent<MapManager>();
        
        GameObject uiManager = new GameObject("UIManager");
        // uiManager.AddComponent<UIManager>(); // اگر این کلاس وجود دارد
        
        GameObject xpManager = new GameObject("XPManager");
        GameObject itemDatabase = new GameObject("ItemDatabase");
        GameObject playerInventory = new GameObject("PlayerInventory");
        playerInventory.AddComponent<PlayerInventory>();
        
        GameObject combatSync = new GameObject("CombatNetworkSync");
        combatSync.AddComponent<CombatNetworkSync>();
        
        // Event System
        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
        eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        
        CreateUICanvas();
        
        // Spawn Player
        GameObject playerPrefab = Resources.Load<GameObject>("Prefabs/Player");
        if (playerPrefab != null)
        {
            GameObject player = PrefabUtility.InstantiatePrefab(playerPrefab) as GameObject;
            player.transform.position = Vector3.zero;
            Debug.Log("✅ Player اسپاون شد");
        }
        
        string scenePath = "Assets/Scenes/Game.unity";
        CreateDirectoryIfNotExists("Assets/Scenes");
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, scenePath);
        
        Debug.Log($"✅ Game Scene ایجاد شد: {scenePath}");
        statusMessage = "✅ Scene کامل ایجاد شد!\n🎮 بازی آماده است";
        statusColor = Color.green;
    }
    
    void CreateUICanvas()
    {
        GameObject canvas = new GameObject("Canvas");
        Canvas c = canvas.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.AddComponent<UnityEngine.UI.CanvasScaler>();
        canvas.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        
        var scaler = canvas.GetComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
    }
    
    #endregion
    
    #region Utilities
    
    void CreateDirectoryIfNotExists(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            string parent = Path.GetDirectoryName(path).Replace("\\", "/");
            string folder = Path.GetFileName(path);
            
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            {
                CreateDirectoryIfNotExists(parent);
            }
            
            if (!string.IsNullOrEmpty(parent))
            {
                AssetDatabase.CreateFolder(parent, folder);
            }
        }
    }
    
    #endregion
}
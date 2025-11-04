#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// Custom Editor برای PauseMenuManager با دکمه Build UI
/// </summary>
[CustomEditor(typeof(PauseMenuManager))]
public class PauseMenuManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // نمایش فیلدهای عادی
        DrawDefaultInspector();
        
        EditorGUILayout.Space(20);
        EditorGUILayout.LabelField("UI Builder", EditorStyles.boldLabel);
        
        // دکمه Build UI
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("🔨 Build Pause Menu UI", GUILayout.Height(40)))
        {
            BuildUI();
        }
        GUI.backgroundColor = Color.white;
        
        EditorGUILayout.Space(10);
        
        // دکمه Clear UI
        GUI.backgroundColor = Color.red;
        if (GUILayout.Button("🗑️ Clear UI (Delete Canvas)", GUILayout.Height(30)))
        {
            ClearUI();
        }
        GUI.backgroundColor = Color.white;
        
        EditorGUILayout.Space(10);
        EditorGUILayout.HelpBox(
            "Build UI: ساخت خودکار Canvas و تمام المان‌های UI\n" +
            "Clear UI: حذف Canvas موجود برای ساخت مجدد", 
            MessageType.Info
        );
    }
    
    void BuildUI()
    {
        PauseMenuManager pauseManager = (PauseMenuManager)target;
        
        if (pauseManager == null)
        {
            EditorUtility.DisplayDialog("Error", "PauseMenuManager not found!", "OK");
            return;
        }
        
        // چک کردن وجود Canvas قبلی
        Transform existingCanvas = pauseManager.transform.Find("PauseMenuCanvas");
        if (existingCanvas != null)
        {
            bool overwrite = EditorUtility.DisplayDialog(
                "Canvas Already Exists",
                "A PauseMenuCanvas already exists. Do you want to delete it and create a new one?",
                "Yes, Rebuild",
                "Cancel"
            );
            
            if (!overwrite)
            {
                return;
            }
            
            DestroyImmediate(existingCanvas.gameObject);
        }
        
        // ساخت UI
        PauseMenuBuilder.BuildPauseMenuUI();
        
        EditorUtility.DisplayDialog(
            "Success!",
            "Pause Menu UI created successfully!\n\n" +
            "✅ Canvas created\n" +
            "✅ All panels created\n" +
            "✅ All references assigned\n\n" +
            "Press ESC in Play Mode to test!",
            "Awesome!"
        );
    }
    
    void ClearUI()
    {
        PauseMenuManager pauseManager = (PauseMenuManager)target;
        
        if (pauseManager == null) return;
        
        Transform canvas = pauseManager.transform.Find("PauseMenuCanvas");
        
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Info", "No Canvas found to delete.", "OK");
            return;
        }
        
        bool confirm = EditorUtility.DisplayDialog(
            "Confirm Delete",
            "Are you sure you want to delete the PauseMenuCanvas?",
            "Yes, Delete",
            "Cancel"
        );
        
        if (confirm)
        {
            DestroyImmediate(canvas.gameObject);
            
            // پاک کردن References
            SerializedObject so = new SerializedObject(pauseManager);
            so.FindProperty("pauseMenuPanel").objectReferenceValue = null;
            so.FindProperty("settingsPanel").objectReferenceValue = null;
            so.FindProperty("confirmDialog").objectReferenceValue = null;
            so.FindProperty("resumeButton").objectReferenceValue = null;
            so.FindProperty("settingsButton").objectReferenceValue = null;
            so.FindProperty("returnToMenuButton").objectReferenceValue = null;
            so.FindProperty("quitGameButton").objectReferenceValue = null;
            so.FindProperty("masterVolumeSlider").objectReferenceValue = null;
            so.FindProperty("musicVolumeSlider").objectReferenceValue = null;
            so.FindProperty("sfxVolumeSlider").objectReferenceValue = null;
            so.FindProperty("brightnessSlider").objectReferenceValue = null;
            so.FindProperty("qualityDropdown").objectReferenceValue = null;
            so.FindProperty("vsyncToggle").objectReferenceValue = null;
            so.FindProperty("settingsBackButton").objectReferenceValue = null;
            so.FindProperty("confirmText").objectReferenceValue = null;
            so.FindProperty("confirmYesButton").objectReferenceValue = null;
            so.FindProperty("confirmNoButton").objectReferenceValue = null;
            so.ApplyModifiedProperties();
            
            EditorUtility.DisplayDialog("Deleted", "Canvas deleted successfully!", "OK");
        }
    }
}
#endif
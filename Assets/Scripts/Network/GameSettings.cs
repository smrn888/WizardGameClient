using UnityEngine;

/// <summary>
/// تنظیمات بازی - شامل صدا، گرافیک، و Brightness
/// 📁 LOCATION: Assets/Scripts/Data/GameSettings.cs
/// </summary>
[System.Serializable]
public class GameSettings
{
    [Header("Audio")]
    [Range(0f, 1f)] public float masterVolume = 1f;
    [Range(0f, 1f)] public float musicVolume = 0.7f;
    [Range(0f, 1f)] public float sfxVolume = 0.8f;
    
    [Header("Graphics")]
    public int graphicsQuality = 2; // 0=Low, 1=Medium, 2=High
    public bool enableVSync = true;
    public bool fullscreen = true;
    [Range(0f, 1f)] public float brightness = 0.5f; // ⬅️ Brightness setting
    
    [Header("Gameplay")]
    public bool enableTutorial = true;
    public bool showDamageNumbers = true;
    public bool autoSave = true;
    
    public GameSettings()
    {
        // Default values
        masterVolume = 1f;
        musicVolume = 0.7f;
        sfxVolume = 0.8f;
        graphicsQuality = 2;
        enableVSync = true;
        fullscreen = true;
        brightness = 0.5f; // ⬅️ Initialize brightness
        enableTutorial = true;
        showDamageNumbers = true;
        autoSave = true;
    }
}
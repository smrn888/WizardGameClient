// فایل: PlayerVisibilityDebug.cs
using UnityEngine;

public class PlayerVisibilityDebug : MonoBehaviour
{
    void Start()
    {
        Debug.Log("=== 🔍 PLAYER VISIBILITY DEBUG ===");
        CheckEverything();
    }
    
    void Update()
    {
        // هر فریم چک کن
        if (Input.GetKeyDown(KeyCode.F1))
        {
            CheckEverything();
        }
    }
    
    void CheckEverything()
    {
        // 1. موقعیت
        Debug.Log($"\n📍 POSITION:");
        Debug.Log($"Player Z = {transform.position.z}");
        
        // 2. SpriteRenderer
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        Debug.Log($"\n🎨 SPRITE RENDERER:");
        if (sr == null)
        {
            Debug.LogError("❌ SpriteRenderer NOT FOUND!");
            return;
        }
        
        Debug.Log($"Enabled: {sr.enabled}");
        Debug.Log($"Sprite: {(sr.sprite != null ? sr.sprite.name : "NULL")}");
        Debug.Log($"Color: {sr.color}");
        Debug.Log($"Alpha: {sr.color.a}");
        Debug.Log($"Sorting Layer: {sr.sortingLayerName}");
        Debug.Log($"Order in Layer: {sr.sortingOrder}");
        Debug.Log($"Material: {(sr.material != null ? sr.material.name : "NULL")}");
        
        // 3. Scale
        Debug.Log($"\n📏 SCALE:");
        Debug.Log($"Local Scale: {transform.localScale}");
        Debug.Log($"Lossy Scale: {transform.lossyScale}");
        
        // 4. GameObject
        Debug.Log($"\n🎮 GAMEOBJECT:");
        Debug.Log($"Active: {gameObject.activeSelf}");
        Debug.Log($"ActiveInHierarchy: {gameObject.activeInHierarchy}");
        Debug.Log($"Layer: {LayerMask.LayerToName(gameObject.layer)}");
        
        // 5. Camera
        Camera cam = Camera.main;
        Debug.Log($"\n📷 CAMERA:");
        if (cam == null)
        {
            Debug.LogError("❌ Main Camera NOT FOUND!");
            return;
        }
        
        Debug.Log($"Camera Z: {cam.transform.position.z}");
        Debug.Log($"Orthographic: {cam.orthographic}");
        Debug.Log($"Orthographic Size: {cam.orthographicSize}");
        Debug.Log($"Culling Mask: {cam.cullingMask}");
        
        // چک کن آیا Camera پلیر را می‌بیند
        bool layerVisible = ((1 << gameObject.layer) & cam.cullingMask) != 0;
        Debug.Log($"Layer Visible to Camera: {layerVisible}");
        
        // موقعیت در viewport
        Vector3 viewportPos = cam.WorldToViewportPoint(transform.position);
        Debug.Log($"Viewport Position: {viewportPos}");
        bool inView = viewportPos.z > 0 && 
                     viewportPos.x >= 0 && viewportPos.x <= 1 &&
                     viewportPos.y >= 0 && viewportPos.y <= 1;
        Debug.Log($"In Camera View: {inView}");
        
        // 6. خلاصه مشکلات
        Debug.Log($"\n⚠️ POTENTIAL ISSUES:");
        if (!sr.enabled) Debug.LogError("❌ SpriteRenderer DISABLED!");
        if (sr.sprite == null) Debug.LogError("❌ NO SPRITE!");
        if (sr.color.a < 0.1f) Debug.LogError("❌ ALPHA TOO LOW!");
        if (transform.localScale.x < 0.01f) Debug.LogError("❌ SCALE TOO SMALL!");
        if (!gameObject.activeInHierarchy) Debug.LogError("❌ GAMEOBJECT INACTIVE!");
        if (!layerVisible) Debug.LogError("❌ LAYER NOT VISIBLE TO CAMERA!");
        if (!inView) Debug.LogWarning("⚠️ PLAYER OUTSIDE CAMERA VIEW!");
        
        Debug.Log("\n=== END DEBUG ===\n");
    }
    
    // نمایش Gizmo در Scene View
    void OnDrawGizmos()
    {
        // دایره سبز
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, 1f);
        
        // خط قرمز به بالا
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 2f);
        
        // مکعب زرد
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, Vector3.one * 0.5f);
    }
}
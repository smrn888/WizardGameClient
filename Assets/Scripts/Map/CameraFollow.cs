// فایل: CameraFollow.cs
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target Settings")]
    [SerializeField] private Transform target;
    
    [Header("Camera Settings")]
    [SerializeField] private float smoothSpeed = 0.125f;
    [SerializeField] private Vector3 offset = new Vector3(0, 0, -10f); // ⚠️ باید منفی باشد!
    
    [Header("Bounds (Optional)")]
    [SerializeField] private bool useBounds = false;
    [SerializeField] private Vector2 minBounds;
    [SerializeField] private Vector2 maxBounds;
    
    void Start()
    {
        // اطمینان از Z منفی
        if (offset.z >= 0)
        {
            offset.z = -10f;
            Debug.LogWarning("⚠️ Camera offset.z was positive! Fixed to -10");
        }
        
        // اگر Target تنظیم نشده، Player را پیدا کن
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                target = player.transform;
                Debug.Log("✅ Camera found Player");
            }
            else
            {
                Debug.LogError("❌ No target set and no Player found!");
            }
        }
        
        // Snap اولیه
        if (target != null)
        {
            SnapToTarget();
        }
    }
    
    void LateUpdate()
    {
        if (target == null) return;
        
        // موقعیت مطلوب
        Vector3 desiredPosition = target.position + offset;
        
        // اعمال Bounds
        if (useBounds)
        {
            desiredPosition.x = Mathf.Clamp(desiredPosition.x, minBounds.x, maxBounds.x);
            desiredPosition.y = Mathf.Clamp(desiredPosition.y, minBounds.y, maxBounds.y);
        }
        
        // حرکت نرم
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        
        // ⚠️ CRITICAL: اطمینان از Z منفی
        smoothedPosition.z = offset.z; // همیشه -10
        
        transform.position = smoothedPosition;
    }
    
    // Snap فوری به Target
    public void SnapToTarget()
    {
        if (target == null) return;
        
        Vector3 snapPosition = target.position + offset;
        
        if (useBounds)
        {
            snapPosition.x = Mathf.Clamp(snapPosition.x, minBounds.x, maxBounds.x);
            snapPosition.y = Mathf.Clamp(snapPosition.y, minBounds.y, maxBounds.y);
        }
        
        // ⚠️ CRITICAL: Z باید منفی باشد
        snapPosition.z = offset.z;
        
        transform.position = snapPosition;
        
        Debug.Log($"📷 Camera snapped to: {snapPosition}");
        Debug.Log($"📷 Camera Z is: {transform.position.z}");
    }
    
    // تنظیم Target جدید
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        if (newTarget != null)
        {
            SnapToTarget();
        }
    }
    
    void OnDrawGizmosSelected()
    {
        if (!useBounds) return;
        
        // نمایش Bounds در Scene
        Gizmos.color = Color.yellow;
        Vector3 center = new Vector3(
            (minBounds.x + maxBounds.x) / 2f,
            (minBounds.y + maxBounds.y) / 2f,
            0
        );
        Vector3 size = new Vector3(
            maxBounds.x - minBounds.x,
            maxBounds.y - minBounds.y,
            0
        );
        Gizmos.DrawWireCube(center, size);
    }
}
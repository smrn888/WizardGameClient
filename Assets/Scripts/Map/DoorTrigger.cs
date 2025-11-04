using UnityEngine;

public class DoorTrigger : MonoBehaviour
{
    [Header("Door Settings")]
    public string targetZoneId;
    public string exitSide;
    
    private MapManager mapManager;
    private bool isPlayerInside = false;

    void Start()
    {
        mapManager = FindObjectOfType<MapManager>();
        
        // اضافه کردن Visual برای در
        SpriteRenderer sr = gameObject.AddComponent<SpriteRenderer>();
        sr.color = new Color(1f, 0.84f, 0f, 0.2f); // طلایی شفاف
        sr.sortingOrder = 10;
        
        // ساخت یک Quad ساده برای نمایش در
        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Destroy(quad.GetComponent<MeshCollider>());
        quad.transform.parent = transform;
        quad.transform.localPosition = Vector3.zero;
        quad.transform.localScale = Vector3.one;
        
        MeshRenderer mr = quad.GetComponent<MeshRenderer>();
        mr.material = new Material(Shader.Find("Sprites/Default"));
        mr.material.color = new Color(1f, 0.84f, 0f, 0.15f);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInside = true;
            Debug.Log($"🚪 Player entered door to: {targetZoneId}");
            
            // انتقال بازیکن
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                player.TransitionToZone(targetZoneId, exitSide);
            }
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInside = false;
        }
    }

    void OnDrawGizmos()
    {
        // نمایش در در Scene View
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, Vector3.one * 2);
    }
}
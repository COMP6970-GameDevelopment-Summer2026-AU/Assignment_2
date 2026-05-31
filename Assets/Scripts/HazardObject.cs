using UnityEngine;

// ══════════════════════════════════════════════════════════════════════════════
// HazardObject
// Attach to cactus, bush, campfire, lake sprites placed in the scene.
// Tag them "Hazard".
// Add a trigger Collider2D sized to the sprite.
//
// On player collision:
//   • -10 score
//   • -0.5 life
//   • 3s extended cooldown (shows on player tooltip)
// ══════════════════════════════════════════════════════════════════════════════
public class HazardObject : MonoBehaviour
{
    [Header("Type (for debug)")]
    public string hazardType = "cactus"; // cactus / bush / campfire / lake

    void Awake()
    {
        var rb = GetComponent<Rigidbody2D>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.simulated = true;
        var col = GetComponent<Collider2D>();
        if (col == null) col = gameObject.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        GameEvents.HazardObjectHit();
        Debug.Log($"[HazardObject] Player hit {hazardType} at {transform.position}");
    }

    // ── Tooltip — always visible above object ─────────────────────────────────
    void OnGUI()
    {
        if (Camera.main == null) return;

        // Convert world position to screen position
        Vector3 screen = Camera.main.WorldToScreenPoint(transform.position);

        // Don't draw if behind camera
        if (screen.z < 0) return;

        float guiX = screen.x;
        float guiY = Screen.height - screen.y; // flip Y for GUI

        // Style
        var style = new GUIStyle();
        style.fontSize         = 13;
        style.fontStyle        = FontStyle.Bold;
        style.normal.textColor = new Color(1f, 0.3f, 0.1f);
        style.alignment        = TextAnchor.MiddleCenter;

        string label = hazardType.ToUpper() + "\n-10pts  -½ life";
        float  boxW  = 100f;
        float  boxH  = 36f;
        float  bx    = guiX - boxW / 2f;
        float  by    = guiY - 40f; // above the sprite

        // Dark background
        GUI.color = new Color(0f, 0f, 0f, 0.72f);
        GUI.DrawTexture(new Rect(bx - 3, by - 2, boxW + 6, boxH + 4),
                        Texture2D.whiteTexture);

        // Colored top border
        GUI.color = new Color(1f, 0.3f, 0.1f);
        GUI.DrawTexture(new Rect(bx - 3, by - 2, boxW + 6, 2),
                        Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUI.Label(new Rect(bx, by, boxW, boxH), label, style);
    }
}
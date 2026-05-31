using UnityEngine;

// ══════════════════════════════════════════════════════════════════════════════
// BridgeObject
// Tag: "Ground" — player can stand on it
// Name must contain "Bridge" — PlayerMovement uses name to give +10 score
//
// Two colliders added automatically in Awake():
//   1. Solid BoxCollider2D — player lands and stands on top
//   2. Trigger BoxCollider2D — fires +10 score event on touch
// ══════════════════════════════════════════════════════════════════════════════
public class BridgeObject : MonoBehaviour
{
    bool triggered = false;

    void Awake()
    {
        // Rigidbody2D kinematic — required for trigger events to fire
        var rb = gameObject.AddComponent<Rigidbody2D>();
        rb.bodyType  = RigidbodyType2D.Kinematic;
        rb.simulated = true;

        // Collider 1 — SOLID, player stands on top
        var solid    = gameObject.AddComponent<BoxCollider2D>();
        solid.isTrigger = false;
        solid.size      = new Vector2(1.5f, 0.25f);
        solid.offset    = new Vector2(0f, 0.15f); // top of sprite

        // Collider 2 — TRIGGER, detects player for +10 score
        var trigger    = gameObject.AddComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.size      = new Vector2(1.5f, 0.8f);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered || !other.CompareTag("Player")) return;
        triggered = true;
        GameEvents.BridgeTouched();
        GameEvents.HintMessage("+10 pts — Bridge crossed!");
        Debug.Log($"[Bridge] Player crossed {gameObject.name}");
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player")) triggered = false;
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
        style.normal.textColor = new Color(0.2f, 1f, 0.3f);
        style.alignment        = TextAnchor.MiddleCenter;

        string label = "+10 pts";
        float  boxW  = 90f;
        float  boxH  = 22f;
        float  bx    = guiX - boxW / 2f;
        float  by    = guiY - 40f; // above the sprite

        // Dark background
        GUI.color = new Color(0f, 0f, 0f, 0.72f);
        GUI.DrawTexture(new Rect(bx - 3, by - 2, boxW + 6, boxH + 4),
                        Texture2D.whiteTexture);

        // Colored top border
        GUI.color = new Color(0.2f, 1f, 0.3f);
        GUI.DrawTexture(new Rect(bx - 3, by - 2, boxW + 6, 2),
                        Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUI.Label(new Rect(bx, by, boxW, boxH), label, style);
    }
}
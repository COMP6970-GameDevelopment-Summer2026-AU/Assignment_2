using UnityEngine;

// ══════════════════════════════════════════════════════════════════════════════
// Checkpoint
// Attach to any GameObject tagged "Checkpoint" with a trigger Collider2D.
// When player walks through, saves position and shows tooltip.
//
// Visual: glows green when reached, grey when not yet reached.
// ══════════════════════════════════════════════════════════════════════════════
public class Checkpoint : MonoBehaviour
{
    public int checkpointNumber = 1;  // 1, 2, or 3

    SpriteRenderer sr;
    bool reached = false;

    void Awake()
    {
        // Rigidbody2D kinematic — required for trigger events to fire
        var rb = GetComponent<Rigidbody2D>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();
        rb.bodyType  = RigidbodyType2D.Kinematic;
        rb.simulated = true;

        // Collider 1 — SOLID (player can stand on / bump into this)
        var solid = gameObject.AddComponent<BoxCollider2D>();
        solid.isTrigger = false;
        solid.size      = new Vector2(2f, 0.3f);

        // Collider 2 — TRIGGER (detects player touch for game events)
        var trigger = gameObject.AddComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.size      = new Vector2(2f, 3f);
    }

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = new Color(0.6f, 0.6f, 0.6f); // grey = not yet
    }

    // Called directly by PlayerMovement as backup
    public void OnTriggerEnter2D_Manual()
    {
        if (reached) return;
        reached = true;
        Vector3 savePos = transform.position + Vector3.up * 0.5f;
        CheckpointSystem.Save(savePos);
        if (sr != null) sr.color = Color.green;
        GameEvents.HintMessage($"✓ Checkpoint {checkpointNumber} saved!");
        Debug.Log($"[Checkpoint {checkpointNumber}] Reached at {savePos}");
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (reached || !other.CompareTag("Player")) return;
        reached = true;

        // Save position slightly above ground so player doesn't spawn in floor
        Vector3 savePos = transform.position + Vector3.up * 0.5f;
        CheckpointSystem.Save(savePos);

        // Turn green
        if (sr != null) sr.color = Color.green;

        GameEvents.HintMessage($"✓ Checkpoint {checkpointNumber} saved!");
        Debug.Log($"[Checkpoint {checkpointNumber}] Reached at {savePos}");
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
        style.normal.textColor = reached
            ? new Color(0.2f, 1f, 0.4f)   // green when saved
            : new Color(0.3f, 0.8f, 1f);   // cyan when not yet
        style.alignment        = TextAnchor.MiddleCenter;

        string label = reached
            ? $"✓ CP {checkpointNumber} SAVED"
            : $"CP {checkpointNumber}";
        float  boxW  = 90f;
        float  boxH  = 22f;
        float  bx    = guiX - boxW / 2f;
        float  by    = guiY - 40f; // above the sprite

        // Dark background
        GUI.color = new Color(0f, 0f, 0f, 0.72f);
        GUI.DrawTexture(new Rect(bx - 3, by - 2, boxW + 6, boxH + 4),
                        Texture2D.whiteTexture);

        // Colored top border
        GUI.color = new Color(0.3f, 0.8f, 1f);
        GUI.DrawTexture(new Rect(bx - 3, by - 2, boxW + 6, 2),
                        Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUI.Label(new Rect(bx, by, boxW, boxH), label, style);
    }
}
using UnityEngine;

// ══════════════════════════════════════════════════════════════════════════════
// EnemyPatrol
// Attach to your Enemy GameObjects.
// Tag them "Enemy".
// Patrols left/right. Stompable from above by player.
// ══════════════════════════════════════════════════════════════════════════════
public class EnemyPatrol : MonoBehaviour
{
    public float moveSpeed    = 2f;
    public float moveDistance = 3f;
    int flipCount = 0;

    Rigidbody2D    rb;
    SpriteRenderer sr;
    Vector3        startPos;
    int            dir  = 1;
    bool           dead = false;

    void Start()
    {
        rb       = GetComponent<Rigidbody2D>();
        sr       = GetComponent<SpriteRenderer>();
        startPos = transform.position;
        Debug.Log($"[Enemy] '{gameObject.name}' patrol started at {startPos} | speed={moveSpeed} range={moveDistance}");
    }

    void Update()
    {
        if (dead) return;

        transform.position += Vector3.right * dir * moveSpeed * Time.deltaTime;

        if (Mathf.Abs(transform.position.x - startPos.x) >= moveDistance)
        { dir *= -1; flipCount++; if(flipCount%10==0) Debug.Log($"[Enemy] '{gameObject.name}' flip #{flipCount} at {transform.position}"); }

        if (sr != null) sr.flipX = dir < 0;
    }

    // Called by PlayerMovement on stomp
    public void Die()
    {
        if (dead) return;
        dead = true;
        Debug.Log($"[Enemy] '{gameObject.name}' STOMPED and died at {transform.position}");
        if (GetComponent<Collider2D>() != null)
            GetComponent<Collider2D>().enabled = false;
        if (rb != null) rb.linearVelocity = new Vector2(0, 4f);
        Destroy(gameObject, 0.4f);
    }

    // ── Tooltip — always visible above enemy ──────────────────────────────────
    void OnGUI()
    {
        if (Camera.main == null) return;

        Vector3 screen = Camera.main.WorldToScreenPoint(transform.position);
        if (screen.z < 0) return;

        float guiX = screen.x;
        float guiY = Screen.height - screen.y;

        var style = new GUIStyle();
        style.fontSize         = 13;
        style.fontStyle        = FontStyle.Bold;
        style.normal.textColor = new Color(1f, 0.3f, 0.1f); // red-orange
        style.alignment        = TextAnchor.MiddleCenter;

        string label = dead ? "✖ DEAD" : "-1 life\nStomp to kill!";
        float  boxW  = 90f;
        float  boxH  = dead ? 22f : 36f;
        float  bx    = guiX - boxW / 2f;
        float  by    = guiY - 44f;

        // Dark background
        GUI.color = new Color(0f, 0f, 0f, 0.72f);
        GUI.DrawTexture(new Rect(bx-3, by-2, boxW+6, boxH+4), Texture2D.whiteTexture);

        // Red top border
        GUI.color = new Color(1f, 0.3f, 0.1f, 1f);
        GUI.DrawTexture(new Rect(bx-3, by-2, boxW+6, 2), Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUI.Label(new Rect(bx, by, boxW, boxH), label, style);
    }
}
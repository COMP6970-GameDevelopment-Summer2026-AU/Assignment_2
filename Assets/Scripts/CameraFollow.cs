using UnityEngine;

// ══════════════════════════════════════════════════════════════════════════════
// CameraFollow
// Attach to Main Camera.
// Fixes the camera settings programmatically on Start — no manual Inspector
// tweaking needed. Then smoothly follows the player from left to right.
//
// What it fixes automatically:
//   • Orthographic size → 6 (shows ~12 units vertically — good for platformer)
//   • Scale Y → 1 (removes the squish you see in the screenshot)
//   • Scale X → 1
//   • Position Z → -10 (correct 2D camera depth)
//   • Background color → parchment (matches cartography theme)
//
// Camera behaviour:
//   • Follows player horizontally (left → right exploration)
//   • Vertical follow with gentle smoothing (not jerky)
//   • Left boundary locked — camera never shows left of spawn point
//   • Player always in lower-left third of screen (classic platformer feel)
// ══════════════════════════════════════════════════════════════════════════════
[RequireComponent(typeof(Camera))]
public class CameraFollow : MonoBehaviour
{
    [Header("Follow Settings")]
    public float smoothSpeed      = 5f;     // how fast camera catches up
    public Vector2 offset         = new Vector2(2f, 2f); // player in lower-left third
    public float lookAheadX       = 2f;     // extra X ahead of player direction

    [Header("Boundaries")]
    public float minX             = -30f;   // just before harbor x=-28
    public float maxX             =  55f;   // just past castle x=52
    public float minY             =  -3f;   // below ground fill
    public float maxY             =  28f;   // above goal flag at y=24

    [Header("Camera Settings (auto-applied on Start)")]
    public float orthographicSize = 5f;     // 5 = tighter zoom suits compact world
                                            // your current 16 shows the ENTIRE world at once

    // ── Runtime ────────────────────────────────────────────────────────────────
    Camera   cam;
    Transform player;
    float    currentVelX, currentVelY; // for SmoothDamp
    float    facingDir = 1f;           // 1 = right, -1 = left

    void Start()
    {
        cam = GetComponent<Camera>();

        // ── Fix all camera settings programmatically ───────────────────────────
        // These match what you should set in Inspector but done via code instead

        // Fix the squished Y scale (your screenshot shows Scale Y = 0.4)
        transform.localScale = Vector3.one;

        // Correct Z position for 2D
        Vector3 pos = transform.position;
        pos.z = -10f;
        transform.position = pos;

        // Fix orthographic size — 16 is way too zoomed out for a platformer
        // Size 6 = camera shows 12 units tall × ~21 units wide (at 16:9)
        cam.orthographicSize  = orthographicSize;
        cam.orthographic      = true;

        // Parchment background matching the cartography theme
        cam.backgroundColor   = new Color(0.83f, 0.74f, 0.54f);
        cam.clearFlags        = CameraClearFlags.SolidColor;

        // Find player automatically
        GameObject p = GameObject.FindWithTag("Player");
        if (p != null)
        {
            player = p.transform;
            // Snap camera to player immediately on start (no slide-in)
            SnapToPlayer();
        }
        else
            Debug.LogWarning("[CameraFollow] No GameObject tagged 'Player' found.");

        Debug.Log("[CameraFollow] Camera configured: " +
                  $"size={orthographicSize}, scale={transform.localScale}, " +
                  $"Z={transform.position.z}");
    }

    void LateUpdate()
    {
        if (player == null) return;

        // Track which way player is facing for look-ahead
        var rb = player.GetComponent<Rigidbody2D>();
        if (rb != null && Mathf.Abs(rb.linearVelocity.x) > 0.1f)
            facingDir = Mathf.Sign(rb.linearVelocity.x);

        // Target position — offset so player is in lower-left third of screen
        // This gives the player more space to see ahead (exploration feel)
        float targetX = player.position.x + offset.x + (lookAheadX * facingDir);
        float targetY = player.position.y + offset.y;

        // Clamp to level boundaries
        targetX = Mathf.Clamp(targetX, minX, maxX);
        targetY = Mathf.Clamp(targetY, minY, maxY);

        // Smooth follow using SmoothDamp for natural feel
        float newX = Mathf.SmoothDamp(transform.position.x, targetX,
                                       ref currentVelX, 1f / smoothSpeed);
        float newY = Mathf.SmoothDamp(transform.position.y, targetY,
                                       ref currentVelY, 1f / smoothSpeed);

        transform.position = new Vector3(newX, newY, -10f);
    }

    // Instantly snap camera to player (used on Start to avoid slide-in)
    void SnapToPlayer()
    {
        float x = Mathf.Clamp(player.position.x + offset.x, minX, maxX);
        float y = Mathf.Clamp(player.position.y + offset.y, minY, maxY);
        transform.position = new Vector3(x, y, -10f);
    }
}
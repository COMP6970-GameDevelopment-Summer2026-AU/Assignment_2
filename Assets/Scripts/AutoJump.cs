using UnityEngine;
using UnityEngine.InputSystem;

// ══════════════════════════════════════════════════════════════════════════════
// AutoJump — AI demo mode that plays the game automatically.
// Shows the player how the game works before they take control.
//
// HOW IT WORKS:
//   • Scans ahead for obstacles, gaps, enemies, hazards, bridges
//   • Decides to move, jump, or wait based on what it sees
//   • Uses a simple state machine: WALK → JUMP → AVOID → CELEBRATE
//
// HOW TO USE:
//   1. Attach to the Player GameObject (alongside PlayerMovement)
//   2. In Inspector: enable "autoJumpActive" OR press TAB during game
//   3. Press TAB again to return to manual control
//
// SETUP: Attach to Player. No other wiring needed.
// ══════════════════════════════════════════════════════════════════════════════
public class AutoJump : MonoBehaviour
{
    [Header("Auto Jump")]
    public bool autoJumpActive = false;

    [Header("Detection")]
    public float lookAheadDist  = 2.5f;  // how far ahead to scan
    public float jumpAheadDist  = 1.5f;  // distance to trigger jump
    public float gapCheckDist   = 1.2f;  // gap detection below feet
    public float obstacleHeight = 0.8f;  // min height to jump over

    [Header("Timing")]
    public float thinkInterval  = 0.08f; // decision frequency (seconds)

    // ── Runtime ────────────────────────────────────────────────────────────────
    Rigidbody2D rb;
    float       thinkTimer;
    bool        wantJump;
    bool        wantMove = true;
    float       moveDir  = 1f;   // always move right (world goes left→right)
    bool        isGrounded;
    float       jumpCooldown;

    enum State { Walk, Jump, Pause, Avoid }
    State state = State.Walk;

    // OnGUI
    GUIStyle labelStyle;
    bool     styleReady;

    // ── Layer masks ────────────────────────────────────────────────────────────
    int groundMask;
    int allMask;

    void Start()
    {
        rb         = GetComponent<Rigidbody2D>();
        groundMask = ~LayerMask.GetMask("Player","Enemy","Collectible");
        allMask    = ~LayerMask.GetMask("Player");

        if (autoJumpActive)
            Debug.Log("[AutoJump] ══ AUTO JUMP MODE ACTIVE ══ Press TAB to take control");
    }

    void Update()
    {
        // Toggle with TAB key
        if (Keyboard.current.tabKey.wasPressedThisFrame)
        {
            autoJumpActive = !autoJumpActive;
            Debug.Log(autoJumpActive
                ? "[AutoJump] ▶ AUTO JUMP ON  — Press TAB for manual control"
                : "[AutoJump] ⏸ MANUAL MODE  — Press TAB for auto jump");
            GameEvents.HintMessage(autoJumpActive
                ? "AUTO JUMP — watch and learn!"
                : "MANUAL MODE — you have control!");
        }

        if (!autoJumpActive) return;

        // Update grounded state
        isGrounded = Physics2D.OverlapCircle(
            (Vector2)transform.position + Vector2.down * 0.52f,
            0.18f, groundMask);

        if (jumpCooldown > 0f) jumpCooldown -= Time.deltaTime;

        thinkTimer -= Time.deltaTime;
        if (thinkTimer <= 0f)
        {
            thinkTimer = thinkInterval;
            Think();
        }

        Act();
    }

    // ══════════════════════════════════════════════════════════════════════════
    // THINK — scan environment and decide what to do
    // ══════════════════════════════════════════════════════════════════════════
    void Think()
    {
        wantJump = false;
        wantMove = true;
        moveDir  = 1f; // always head right

        Vector2 pos    = transform.position;
        Vector2 ahead  = pos + Vector2.right * lookAheadDist;
        Vector2 feet   = pos + Vector2.down  * 0.55f;

        // ── 1. Gap detection — is there a gap ahead? ───────────────────────────
        // Cast down from a point ahead to see if ground exists
        Vector2 gapCheckPos = pos + Vector2.right * jumpAheadDist + Vector2.down * 0.3f;
        bool groundAhead = Physics2D.Raycast(gapCheckPos, Vector2.down, gapCheckDist, groundMask);

        if (!groundAhead && isGrounded && jumpCooldown <= 0f)
        {
            Debug.Log("[AutoJump] GAP detected ahead — JUMPING");
            wantJump = true;
            jumpCooldown = 0.5f;
            state = State.Jump;
            return;
        }

        // ── 2. Obstacle detection — wall or tall object ahead ─────────────────
        // Raycast horizontally at player's mid and upper body
        bool wallMid = Physics2D.Raycast(pos, Vector2.right, lookAheadDist, groundMask);
        bool wallTop = Physics2D.Raycast(pos + Vector2.up * 0.4f, Vector2.right,
                                          lookAheadDist * 0.8f, groundMask);

        if ((wallMid || wallTop) && isGrounded && jumpCooldown <= 0f)
        {
            // Check if obstacle is short enough to jump over
            var hit = Physics2D.Raycast(pos, Vector2.right, lookAheadDist, groundMask);
            if (hit.collider != null)
            {
                bool isHazard = hit.collider.CompareTag("HazardObject");

                if (isHazard)
                {
                    // Jump over hazard objects
                    Debug.Log($"[AutoJump] HAZARD ahead '{hit.collider.name}' — JUMPING OVER");
                    wantJump = true;
                    jumpCooldown = 0.6f;
                    state = State.Jump;
                }
                else
                {
                    // Regular obstacle — jump over
                    Debug.Log($"[AutoJump] OBSTACLE '{hit.collider.name}' — JUMPING");
                    wantJump = true;
                    jumpCooldown = 0.5f;
                    state = State.Jump;
                }
            }
            return;
        }

        // ── 3. Enemy detection — enemy ahead ───────────────────────────────────
        // Scan for enemy colliders in a box ahead
        Collider2D enemy = Physics2D.OverlapBox(
            pos + Vector2.right * lookAheadDist,
            new Vector2(1.5f, 1f), 0f,
            LayerMask.GetMask("Enemy"));

        if (enemy != null && isGrounded && jumpCooldown <= 0f)
        {
            float eyDist = Vector2.Distance(pos, enemy.transform.position);
            if (eyDist < lookAheadDist * 1.2f)
            {
                // Jump to land on top of enemy (stomp)
                Debug.Log($"[AutoJump] ENEMY '{enemy.name}' at dist={eyDist:F1} — STOMPING");
                wantJump = true;
                jumpCooldown = 0.6f;
                state = State.Jump;
                return;
            }
        }

        // ── 4. Platform ahead and above — jump up to it ────────────────────────
        // Check if there's a platform just above and ahead
        bool platformAbove = Physics2D.Raycast(
            pos + Vector2.right * 1f,
            Vector2.up, 3f, groundMask);

        if (platformAbove && isGrounded && jumpCooldown <= 0f)
        {
            var hit2 = Physics2D.Raycast(pos + Vector2.right * 1f, Vector2.up, 3f, groundMask);
            if (hit2.distance > 0.5f && hit2.distance < 3f)
            {
                Debug.Log($"[AutoJump] PLATFORM above at dist={hit2.distance:F1} — JUMPING UP");
                wantJump = true;
                jumpCooldown = 0.8f;
                state = State.Jump;
                return;
            }
        }

        // ── 5. Default — keep walking right ───────────────────────────────────
        state    = State.Walk;
        wantMove = true;
    }

    // ══════════════════════════════════════════════════════════════════════════
    // ACT — apply decisions to Rigidbody2D directly
    // ══════════════════════════════════════════════════════════════════════════
    void Act()
    {
        if (!autoJumpActive) return;

        // Move horizontally
        float speed = wantMove ? 5f : 0f;
        rb.linearVelocity = new Vector2(moveDir * speed, rb.linearVelocity.y);

        // Flip sprite
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.flipX = moveDir < 0;

        // Jump
        if (wantJump && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 13f);
            wantJump = false;
            Debug.Log($"[AutoJump] ↑ JUMP executed at {transform.position}");
        }

        // Better fall gravity (same as PlayerMovement)
        if (rb.linearVelocity.y < 0)
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * 1.8f * Time.deltaTime;
    }

    // ══════════════════════════════════════════════════════════════════════════
    // OnGUI — show autoplay status overlay
    // ══════════════════════════════════════════════════════════════════════════
    void OnGUI()
    {
        if (!styleReady)
        {
            labelStyle = new GUIStyle();
            labelStyle.fontSize         = 14;
            labelStyle.fontStyle        = FontStyle.Bold;
            labelStyle.normal.textColor = Color.white;
            labelStyle.alignment        = TextAnchor.MiddleCenter;
            styleReady = true;
        }

        float sw = Screen.width;

        if (autoJumpActive)
        {
            // Pulsing banner at bottom
            float pulse = 0.7f + Mathf.Sin(Time.time * 3f) * 0.3f;

            GUI.color = new Color(0.1f, 0.1f, 0.6f, 0.88f);
            GUI.DrawTexture(new Rect(0, Screen.height - 42, sw, 42), Texture2D.whiteTexture);

            GUI.color = new Color(0.3f, 0.6f, 1f, pulse);
            GUI.DrawTexture(new Rect(0, Screen.height - 42, sw, 2), Texture2D.whiteTexture);

            GUI.color = Color.white;
            GUI.Label(new Rect(0, Screen.height - 38, sw, 34),
                $"▶  AUTO JUMP MODE  |  State: {state}  |  Press TAB to take control",
                labelStyle);
        }
        else
        {
            // Small hint in corner
            GUI.color = new Color(0f, 0f, 0f, 0.6f);
            GUI.DrawTexture(new Rect(sw - 220, Screen.height - 36, 218, 30),
                Texture2D.whiteTexture);
            GUI.color = new Color(0.7f, 0.7f, 0.7f, 1f);

            var small = new GUIStyle(labelStyle);
            small.fontSize = 12;
            GUI.Label(new Rect(sw - 220, Screen.height - 36, 218, 30),
                "TAB = Auto Jump demo", small);
            GUI.color = Color.white;
        }
    }

    // Gizmo — show detection rays in Scene view
    void OnDrawGizmosSelected()
    {
        if (!autoJumpActive) return;

        Vector2 pos = transform.position;

        // Look-ahead ray
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(pos, Vector2.right * lookAheadDist);

        // Gap check ray
        Gizmos.color = Color.red;
        Vector2 gapPos = pos + Vector2.right * jumpAheadDist + Vector2.down * 0.3f;
        Gizmos.DrawRay(gapPos, Vector2.down * gapCheckDist);

        // Ground check circle
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(pos + Vector2.down * 0.52f, 0.18f);
    }
}

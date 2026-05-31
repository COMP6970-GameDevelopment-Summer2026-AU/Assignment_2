using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// ══════════════════════════════════════════════════════════════════════════════
// PlayerMovement
// Attach to your Player GameObject.
// Requires: Rigidbody2D, CapsuleCollider2D, SpriteRenderer
// Tag Player as "Player"
// ══════════════════════════════════════════════════════════════════════════════
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed   = 5f;
    public float jumpForce   = 9f;
    public float fallMult    = 2.8f;
    public float lowJumpMult = 2.2f;

    [Header("Jump")]
    public int   maxJumps   = 2;      // 2 = double jump
    public float coyoteTime = 0.12f;  // grace period after leaving platform
    public float jumpBuffer = 0.15f;  // buffer before landing

    Rigidbody2D    rb;
    SpriteRenderer sr;

    int   jumpsLeft;
    bool  isGrounded;
    float coyoteCounter;
    float jumpBufferCounter;
    bool  isDead;
    float lives = 3f;
    const float MAX_LIVES = 3f;
    float damageCooldown    = 0f;
    const float DAMAGE_COOLDOWN         = 1.5f;
    const float HAZARD_OBJECT_COOLDOWN  = 3f;   // cactus/bush/campfire/lake
    bool  hazardExtended    = false;   // true = hazard object hit (3s cooldown)
    int   platformBumpCooldown = 0;    // frames before next bump penalty

    Vector3 spawnPoint;

    void Start()
    {
        rb         = GetComponent<Rigidbody2D>();
        sr         = GetComponent<SpriteRenderer>();
        jumpsLeft  = maxJumps;
        spawnPoint = transform.position;
        GameEvents.LivesChanged(Mathf.CeilToInt(lives));
        lastPosition = transform.position;

        // Subscribe to events
        GameEvents.OnBonusLife  += HandleBonusLife;
        GameEvents.OnCliffFall  += HandleCliffFall;

        Debug.Log($"[Player] ══ DEBUG MODE ══ Started at {transform.position}");
        Debug.Log($"[Player] Lives={lives} MaxJumps={maxJumps} MoveSpeed={moveSpeed} JumpForce={jumpForce}");
        Debug.Log($"[Player] CoyoteTime={coyoteTime} JumpBuffer={jumpBuffer} FallMult={fallMult}");
    }

    void Update()
    {
        if (isDead) return;
        if (damageCooldown > 0f)
        {
            damageCooldown -= Time.deltaTime;
            if (damageCooldown <= 0f) hazardExtended = false;
        }
        if (winMsgTimer   > 0f) winMsgTimer   -= Time.deltaTime;
        if (platformBumpCooldown > 0) platformBumpCooldown--;

        // Track distance and steps
        float moved = Vector3.Distance(transform.position, lastPosition);
        distanceTravelled += moved;
        lastPosition       = transform.position;

        // Count steps — every STEP_DISTANCE units of horizontal movement = 1 step
        if (Mathf.Abs(rb.linearVelocity.x) > 0.1f)
        {
            stepAccumulator += moved;
            if (stepAccumulator >= STEP_DISTANCE)
            {
                stepCount++;
                stepAccumulator -= STEP_DISTANCE;
                // Log every 10 steps so you can see it working in console
                if (stepCount % 5 == 0)
                    Debug.Log($"[Player] Steps={stepCount} | pos={transform.position} | score={ScoreSystem.Score} | lives={lives:F1}");
            }
        }
        CheckGround();  // physics-based ground detection every frame
        Move();
        CoyoteUpdate();
        JumpBufferUpdate();
        BetterJump();
        FlipSprite();
    }

    void Move()
    {
        float input = 0f;
        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)  input = -1f;
        if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) input =  1f;
        rb.linearVelocity = new Vector2(input * moveSpeed, rb.linearVelocity.y);

        // Fire walk start/stop events for footstep sounds
        bool nowWalking = Mathf.Abs(input) > 0f && isGrounded;
        if (nowWalking && !wasWalking)  PlayerSounds.WalkStart();
        if (!nowWalking && wasWalking)  PlayerSounds.WalkStop();
        wasWalking = nowWalking;
    }

    void CoyoteUpdate()
    {
        if (isGrounded)
            coyoteCounter = coyoteTime;          // refresh while on ground
        else
            coyoteCounter -= Time.deltaTime;     // count down in air
    }

    void JumpBufferUpdate()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
            jumpBufferCounter = jumpBuffer;
        else
            jumpBufferCounter -= Time.deltaTime;

        if (jumpBufferCounter > 0f)
        {
            if (coyoteCounter > 0f)
            { DoJump(); coyoteCounter = 0f; jumpBufferCounter = 0f; }
            else if (jumpsLeft > 0)
            { DoJump(); jumpBufferCounter = 0f; }
        }
    }

    void DoJump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        jumpsLeft = Mathf.Max(0, jumpsLeft - 1);
        totalJumps++;
        PlayerSounds.Jump();
        PlayerSounds.WalkStop();
        Debug.Log($"[Player] JUMP #{totalJumps} | jumpsLeft={jumpsLeft} | pos={transform.position} | vel={rb.linearVelocity}");
    }

    void BetterJump()
    {
        if (rb.linearVelocity.y < 0)
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (fallMult-1) * Time.deltaTime;
        else if (rb.linearVelocity.y > 0 && !Keyboard.current.spaceKey.isPressed)
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (lowJumpMult-1) * Time.deltaTime;
    }

    void FlipSprite()
    {
        if (rb.linearVelocity.x < -0.1f)     sr.flipX = true;
        else if (rb.linearVelocity.x > 0.1f) sr.flipX = false;
    }

    // ── Ground detection — downward raycast ───────────────────────────────────
    // Uses a short downward raycast + overlap circle.
    // Accepts ANY solid (non-trigger) collider directly below the player.
    // This means tilemap Ground, bridge solid colliders, and anything else solid.
    void CheckGround()
    {
        bool wasGrounded = isGrounded;

        // Exclude player, enemy, triggers — only hit solid colliders
        int mask = ~LayerMask.GetMask("Player", "Enemy", "Collectible");

        // Method 1: small overlap circle just under feet
        Vector2 feetPos = (Vector2)transform.position + Vector2.down * 0.52f;
        Collider2D hit = Physics2D.OverlapCircle(feetPos, 0.18f, mask);

        if (hit != null && !hit.isTrigger)
        {
            isGrounded = true;
        }
        else
        {
            // Method 2: short downward raycast as backup
            RaycastHit2D ray = Physics2D.Raycast(
                (Vector2)transform.position + Vector2.down * 0.4f,
                Vector2.down, 0.2f, mask);
            isGrounded = (ray.collider != null && !ray.collider.isTrigger);
        }

        if (isGrounded && !wasGrounded)
        {
            jumpsLeft = maxJumps;
            Debug.Log($"[Player] LANDED at {transform.position} | jumpsReset={maxJumps}");
        }
        if (isGrounded) jumpsLeft = maxJumps;
    }

    // ── Enemy stomp (kept as collision event) ─────────────────────────────────
    void OnCollisionEnter2D(Collision2D col)
    {
        if (col.gameObject.CompareTag("Enemy"))
            CheckEnemyStomp(col);

        // Platform bump penalty — hitting the side of a platform (not landing on top)
        if (col.gameObject.CompareTag("Ground") && platformBumpCooldown <= 0)
        {
            foreach (ContactPoint2D c in col.contacts)
            {
                // Side hit = normal is mostly horizontal
                if (Mathf.Abs(c.normal.x) > 0.7f)
                {
                    GameEvents.PlatformBump();
                    platformBumpCooldown = 30; // 30 frames cooldown before next bump
                    break;
                }
            }
        }
    }

    void OnCollisionStay2D(Collision2D col)
    {
        if (col.gameObject.CompareTag("Enemy"))
            CheckEnemyStomp(col);
    }

    // ── Enemy stomp ────────────────────────────────────────────────────────────
    void CheckEnemyStomp(Collision2D col)
    {
        if (rb.linearVelocity.y < -0.5f &&
            transform.position.y > col.transform.position.y + 0.3f)
        {
            col.gameObject.GetComponent<EnemyPatrol>()?.Die();
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce * 0.7f);
        }
    }

    // ── Triggers ───────────────────────────────────────────────────────────────
    void OnTriggerEnter2D(Collider2D other)
    {
        if (isDead) return;

        if (other.CompareTag("Water") || other.CompareTag("Hazard"))
        {
            waterFalls++;
            Debug.Log($"[Player] WATER/HAZARD hit #{waterFalls} at {transform.position}");
            PlayerSounds.WaterHit(); TakeDamage(0.5f); return;
        }

        if (other.CompareTag("Enemy"))
        {
            enemyHits++;
            Debug.Log($"[Player] ENEMY hit #{enemyHits} '{other.gameObject.name}' at {transform.position}");
            PlayerSounds.EnemyHit(); TakeDamage(1f); return;
        }

        // ── Hazard objects: cactus, bush, campfire, lake ───────────────────────
        // Rule: -10 score, -0.5 life, 3s cooldown shown on tooltip
        if (other.CompareTag("HazardObject"))
        {
            if (damageCooldown > 0f) return; // already in cooldown
            GameEvents.HazardObjectHit();
            hazardExtended = true;
            TakeDamage(0.5f);
            damageCooldown = HAZARD_OBJECT_COOLDOWN;
            Debug.Log($"[Player] ⚠ HAZARD OBJECT '{other.gameObject.name}' → -10pts, -0.5 life, 3s cooldown | lives={lives:F1}");
            return;
        }

        // Bridge tag is "Ground" — BridgeObject.cs handles +10 score via its own
        // OnTriggerEnter2D. No need to fire here — prevents double counting.

        // ── Checkpoint ─────────────────────────────────────────────────────────
        if (other.CompareTag("Checkpoint"))
        {
            Debug.Log($"[Player] ✦ CHECKPOINT '{other.gameObject.name}' at {other.transform.position}");
            other.GetComponent<Checkpoint>()?.OnTriggerEnter2D_Manual();
            return;
        }     // enemy = full life

        if (other.CompareTag("Goal"))
        {
            var cm = FindAnyObjectByType<CollectibleManager>();
            if (cm != null && !cm.AllCollected)
            {
                Debug.Log("[Player] Touched Goal but not all gems collected yet!");
                GameEvents.HintMessage("Collect all gems first!");
                return;
            }
            // Store stats for GameManager win screen
            GameEvents.StatJumps    = totalJumps;
            GameEvents.StatWater    = waterFalls;
            GameEvents.StatEnemies  = enemyHits;
            GameEvents.StatDistance = distanceTravelled;

            string summary =
                $"[Player] ✓ WE WIN!\n" +
                $"  Steps:       {stepCount}\n" +
                $"  Jumps:       {totalJumps}\n" +
                $"  Water falls: {waterFalls}\n" +
                $"  Enemy hits:  {enemyHits}\n" +
                $"  Distance:    {distanceTravelled:F1} units";
            Debug.Log(summary);
            Debug.Log($"[Player] ★ WIN! Final: score={GameEvents.StatJumps} jumps, {stepCount} steps, {distanceTravelled:F1} units, {waterFalls} waterFalls, {enemyHits} enemyHits");
            PlayerSounds.FlagReached();

            GameEvents.StatJumps    = totalJumps;
            GameEvents.StatWater    = waterFalls;
            GameEvents.StatEnemies  = enemyHits;
            GameEvents.StatDistance = distanceTravelled;
            GameEvents.StatSteps    = stepCount;

            winMessage  = $"Steps: {stepCount}  |  Jumps: {totalJumps}  |  Water: {waterFalls}  |  Enemies: {enemyHits}";
            winMsgTimer = 6f;
            GameEvents.PlayerWon();
        }
    }

    // OnTriggerStay handles the case where player spawns inside water
    // or slides between two water trigger objects
    void OnTriggerStay2D(Collider2D other)
    {
        if (isDead) return;
        if (other.CompareTag("Water") || other.CompareTag("Hazard"))
            TakeDamage(0.5f);
    }

    // ── Gizmo — shows ground check circle in Scene view ───────────────────────
    void OnDrawGizmosSelected()
    {
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere((Vector2)transform.position + Vector2.down * 0.55f, 0.2f);
    }

    // ── Damage / respawn ───────────────────────────────────────────────────────
    void HandleBonusLife()
    {
        if (lives < MAX_LIVES)
        {
            lives = Mathf.Min(MAX_LIVES, lives + 1f);
            GameEvents.LivesChanged(Mathf.CeilToInt(lives));
            Debug.Log("[Player] Bonus life! Lives=" + lives);
        }
    }

    void HandleCliffFall()
    {
        if (isDead) return;
        Debug.Log("[Player] Cliff fall — respawning at checkpoint");
        transform.position = CheckpointSystem.CurrentCheckpoint;
        rb.linearVelocity  = Vector2.zero;
        isGrounded         = false;
        jumpsLeft          = maxJumps;
        damageCooldown     = DAMAGE_COOLDOWN;
        GameEvents.HintMessage("Fell off! Respawned at checkpoint.");
    }

    // ── Cooldown tooltip above player ─────────────────────────────────────────
    GUIStyle cooldownStyle;
    bool     styleReady;
    string   winMessage  = "";
    float    winMsgTimer = 0f;

    bool wasWalking = false;

    // ── Journey stats ──────────────────────────────────────────────────────────
    int   totalJumps        = 0;
    int   waterFalls        = 0;
    int   enemyHits         = 0;
    float distanceTravelled = 0f;
    int   stepCount         = 0;
    float stepAccumulator   = 0f;
    const float STEP_DISTANCE = 0.5f; // every 0.5 units = 1 step
    Vector3 lastPosition;

    void OnGUI()
    {
        if (Camera.main == null) return;

        if (!styleReady)
        {
            cooldownStyle = new GUIStyle();
            cooldownStyle.fontSize         = 13;
            cooldownStyle.fontStyle        = FontStyle.Bold;
            cooldownStyle.normal.textColor = Color.black;
            cooldownStyle.alignment        = TextAnchor.MiddleCenter;
            styleReady = true;
        }

        Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position);
        float guiX = screenPos.x;
        float guiY = Screen.height - screenPos.y;

        // ── Cooldown tooltip ───────────────────────────────────────────────────
        if (damageCooldown > 0f)
        {
            float boxW = 160f, boxH = 40f;
            float boxX = guiX - boxW / 2f;
            float boxY = guiY - 70f;

            GUI.color = new Color(1f, 0.6f, 0f, 0.85f);
            GUI.DrawTexture(new Rect(boxX-4, boxY-2, boxW+8, boxH+4), Texture2D.whiteTexture);
            GUI.color = new Color(1f, 0.3f, 0f, 1f);
            GUI.DrawTexture(new Rect(boxX-4, boxY-2, boxW+8, 2), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(boxX-4, boxY+boxH+2, boxW+8, 2), Texture2D.whiteTexture);
            GUI.color = Color.white;
            string cdLine1 = hazardExtended ? "⚠ HAZARD COOLDOWN" : "COOLING DOWN";
            string cdLine2 = Mathf.Ceil(damageCooldown) + "s — Can't be hurt";
            GUI.Label(new Rect(boxX, boxY+2,  boxW, 18), cdLine1, cooldownStyle);
            GUI.Label(new Rect(boxX, boxY+20, boxW, 18), cdLine2, cooldownStyle);
        }

        // ── Win tooltip ────────────────────────────────────────────────────────
        if (winMsgTimer > 0f)
        {
            float boxW = 260f, boxH = 40f;
            float boxX = guiX - boxW / 2f;
            float boxY = guiY - 115f;

            GUI.color = new Color(0.1f, 0.7f, 0.1f, 0.92f);
            GUI.DrawTexture(new Rect(boxX-4, boxY-2, boxW+8, boxH+4), Texture2D.whiteTexture);
            GUI.color = new Color(0.2f, 1f, 0.2f, 1f);
            GUI.DrawTexture(new Rect(boxX-4, boxY-2, boxW+8, 2), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(boxX-4, boxY+boxH+2, boxW+8, 2), Texture2D.whiteTexture);
            GUI.color = Color.white;
            // Parse stats from winMessage for multi-line display
            GUI.Label(new Rect(boxX, boxY+2,  boxW, 18), "YOU WIN! Reached the flag!", cooldownStyle);
            GUI.Label(new Rect(boxX, boxY+20, boxW, 18), winMessage,                   cooldownStyle);
        }
    }

    void TakeDamage(float amount = 1f)
    {
        if (isDead) return;
        if (damageCooldown > 0f) return;

        damageCooldown = DAMAGE_COOLDOWN;
        lives -= amount;
        GameEvents.LivesChanged(Mathf.CeilToInt(lives)); // show ceiling for display

        // Track what caused the damage (checked by caller context via tag)
        // We track in OnTriggerEnter2D instead for accuracy

        if (lives <= 0f)
        {
            isDead = true;
            rb.linearVelocity = Vector2.zero;
            GameEvents.PlayerDied();
            return;
        }

        // Respawn at LATEST CHECKPOINT (not original spawn point)
        Vector3 respawnPos = CheckpointSystem.CurrentCheckpoint;
        transform.position = respawnPos;
        rb.linearVelocity  = new Vector2(0f, 3f); // small upward push to exit trigger
        isGrounded         = false;
        jumpsLeft          = maxJumps;
        damageCooldown     = DAMAGE_COOLDOWN * 2f;
        Debug.Log($"[Player] ✦ RESPAWN → checkpoint={respawnPos} | lives={lives:F1} remaining");
    }
}
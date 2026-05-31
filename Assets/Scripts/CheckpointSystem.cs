using UnityEngine;

// ══════════════════════════════════════════════════════════════════════════════
// CheckpointSystem
// Manages 3 checkpoints in the scene.
// Handles cliff fall detection (player falls below world bounds → respawn).
// Attach to _GameManager.
//
// CHECKPOINT SETUP:
//   Create 3 empty GameObjects, tag each "Checkpoint", place them in the scene.
//   OR use CartographySceneBuilder which places them automatically.
//
// CLIFF DETECTION:
//   If player Y < cliffFallY (below all ground), respawn at last checkpoint.
// ══════════════════════════════════════════════════════════════════════════════
public class CheckpointSystem : MonoBehaviour
{
    [Header("Cliff Detection")]
    public float cliffFallY = -8f;  // below this Y = fell off world

    // Current saved checkpoint position
    public static Vector3 CurrentCheckpoint { get; private set; }
    static bool checkpointSet = false;

    Transform player;
    float     checkTimer = 0f;

    void Start()
    {
        GameObject p = GameObject.FindWithTag("Player");
        if (p != null)
        {
            player = p.transform;
            // Default spawn = player start position
            CurrentCheckpoint = player.position;
            checkpointSet = true;
            Debug.Log($"[Checkpoint] ══ System initialized | defaultSpawn={CurrentCheckpoint}");
        }
    }

    void Update()
    {
        if (player == null) return;

        // Check for cliff fall every 0.1s (not every frame)
        checkTimer -= Time.deltaTime;
        if (checkTimer > 0f) return;
        checkTimer = 0.1f;

        if (player.position.y < cliffFallY)
        {
            Debug.Log($"[Checkpoint] Player fell off cliff at y={player.position.y:F1} " +
                      $"— respawning at {CurrentCheckpoint}");
            GameEvents.CliffFall();
        }
    }

    // Called by Checkpoint trigger when player touches it
    public static void Save(Vector3 pos)
    {
        if (!checkpointSet || pos.x > CurrentCheckpoint.x)
        {
            Vector3 prev = CurrentCheckpoint;
            CurrentCheckpoint = pos;
            checkpointSet = true;
            GameEvents.CheckpointReached(pos);
            Debug.Log($"[Checkpoint] ✦ SAVED at {pos} (was: {prev})");
        }
    }
}
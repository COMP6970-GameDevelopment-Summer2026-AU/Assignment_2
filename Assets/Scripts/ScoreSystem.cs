using UnityEngine;

// ══════════════════════════════════════════════════════════════════════════════
// ScoreSystem
// Manages the score counter and bridge life bonus.
// Attach to _GameManager.
//
// RULES:
//   • Touch hazard object (cactus/bush/campfire/lake) → -10 points
//   • Bump non-bridge platform tile                   → -1 point
//   • Touch bridge                                    → +10 points
//   • Every 3 bridges touched → +1 life (if below max)
// ══════════════════════════════════════════════════════════════════════════════
public class ScoreSystem : MonoBehaviour
{
    public static int   Score         { get; private set; } = 0;
    public static int   BridgesTouched{ get; private set; } = 0;
    public static float MaxLives      { get; private set; } = 3f;

    void OnEnable()
    {
        GameEvents.OnScoreChanged    += _ => { };  // handled by GameManager HUD
        GameEvents.OnBridgeTouched   += HandleBridge;
        GameEvents.OnHazardObjectHit += HandleHazard;
        GameEvents.OnPlatformBump    += HandlePlatformBump;
    }

    void OnDisable()
    {
        GameEvents.OnBridgeTouched   -= HandleBridge;
        GameEvents.OnHazardObjectHit -= HandleHazard;
        GameEvents.OnPlatformBump    -= HandlePlatformBump;
    }

    void Start() { Score = 0; Debug.Log("[ScoreSystem] ══ Initialized | MaxLives=3 | Rules: Bridge=+10, Hazard=-10, Bump=-1"); }

    void HandleBridge()
    {
        AddScore(10);
        BridgesTouched++;
        Debug.Log($"[Score] ✦ BRIDGE #{BridgesTouched} → +10pts | totalScore={Score}");

        if (BridgesTouched % 3 == 0)
        {
            GameEvents.BonusLife();
            GameEvents.HintMessage("3 bridges crossed → +1 life bonus!");
            Debug.Log($"[Score] ★ BONUS LIFE! {BridgesTouched} bridges crossed | score={Score}");
        }
    }

    void HandleHazard()
    {
        AddScore(-10);
        Debug.Log($"[Score] ⚠ HAZARD → -10pts | totalScore={Score}");
    }

    void HandlePlatformBump()
    {
        AddScore(-1);
        Debug.Log($"[Score] BUMP → -1pt | totalScore={Score}");
    }

    static void AddScore(int delta)
    {
        Score = Mathf.Max(0, Score + delta);
        GameEvents.ScoreChanged(Score);
    }
}
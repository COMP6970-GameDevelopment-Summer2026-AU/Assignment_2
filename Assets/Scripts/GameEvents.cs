using System;
using UnityEngine;


public static class GameEvents
{
    // ── Core ──────────────────────────────────────────────────────────────────
    public static event Action         OnPlayerDied;
    public static event Action         OnPlayerWon;
    public static event Action<int>    OnLivesChanged;
    public static event Action<int>    OnCollectiblePickedUp;
    public static event Action<int>    OnCollectiblesRequired;
    public static event Action<string> OnHintMessage;
    public static event Action         OnGameStarted;

    // ── Score ─────────────────────────────────────────────────────────────────
    public static event Action<int>    OnScoreChanged;
    public static event Action         OnBridgeTouched;
    public static event Action         OnHazardObjectHit;
    public static event Action         OnPlatformBump;
    public static event Action         OnBonusLife;

    // ── Checkpoint ────────────────────────────────────────────────────────────
    public static event Action<Vector3> OnCheckpointReached;
    public static event Action          OnCliffFall;

    // ── Invokers ──────────────────────────────────────────────────────────────
    public static void PlayerDied()                 => OnPlayerDied?.Invoke();
    public static void PlayerWon()                  => OnPlayerWon?.Invoke();
    public static void LivesChanged(int n)          => OnLivesChanged?.Invoke(n);
    public static void CollectiblePickedUp(int n)   => OnCollectiblePickedUp?.Invoke(n);
    public static void CollectiblesRequired(int n)  => OnCollectiblesRequired?.Invoke(n);
    public static void HintMessage(string m)        => OnHintMessage?.Invoke(m);
    public static void GameStarted()                => OnGameStarted?.Invoke();
    public static void ScoreChanged(int s)          => OnScoreChanged?.Invoke(s);
    public static void BridgeTouched()              => OnBridgeTouched?.Invoke();
    public static void HazardObjectHit()            => OnHazardObjectHit?.Invoke();
    public static void PlatformBump()               => OnPlatformBump?.Invoke();
    public static void BonusLife()                  => OnBonusLife?.Invoke();
    public static void CheckpointReached(Vector3 p) => OnCheckpointReached?.Invoke(p);
    public static void CliffFall()                  => OnCliffFall?.Invoke();

    // ── Journey stats ─────────────────────────────────────────────────────────
    public static int   StatJumps    = 0;
    public static int   StatWater    = 0;
    public static int   StatEnemies  = 0;
    public static float StatDistance = 0f;
    public static int   StatSteps    = 0;

    public static void ResetStats()
    {
        StatJumps=0; StatWater=0; StatEnemies=0; StatDistance=0f; StatSteps=0;
    }
}
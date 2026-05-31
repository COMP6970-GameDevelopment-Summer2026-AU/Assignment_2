using System;

// Static sound event bus — PlayerMovement fires these,
// SoundManager listens and plays the clips.
public static class PlayerSounds
{
    public static event Action OnWaterHit;
    public static event Action OnEnemyHit;
    public static event Action OnFlagReached;
    public static event Action OnGemCollected;
    public static event Action OnJump;
    public static event Action OnWalkStart;
    public static event Action OnWalkStop;

    public static void WaterHit()    => OnWaterHit?.Invoke();
    public static void EnemyHit()    => OnEnemyHit?.Invoke();
    public static void FlagReached() => OnFlagReached?.Invoke();
    public static void GemCollected()=> OnGemCollected?.Invoke();
    public static void Jump()        => OnJump?.Invoke();
    public static void WalkStart()   => OnWalkStart?.Invoke();
    public static void WalkStop()    => OnWalkStop?.Invoke();
}
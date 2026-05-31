using UnityEngine;

// ══════════════════════════════════════════════════════════════════════════════
// SoundManager — plays sound effects via GameEvents.
//
// Setup in Unity (one time):
//   1. Add this script to _GameManager (or any GameObject)
//   2. In Inspector drag 4 AudioClip files into the 4 slots:
//      • waterSound   — splash sound
//      • enemySound   — hit/hurt sound
//      • flagSound    — win/fanfare sound
//      • gemSound     — collect/pickup sound
//
// Free sounds from: https://kenney.nl/assets/interface-sounds
//                   https://freesound.org
// ══════════════════════════════════════════════════════════════════════════════
public class SoundManager : MonoBehaviour
{
    [Header("Sound Effects")]
    public AudioClip waterSound;    // player falls in water
    public AudioClip enemySound;    // player hit by enemy
    public AudioClip flagSound;     // player reaches goal/flag
    public AudioClip gemSound;      // player collects a gem
    public AudioClip jumpSound;     // player jumps
    public AudioClip walkSound;     // player walking footstep

    [Header("Volume")]
    [Range(0f, 1f)] public float volume     = 0.8f;
    [Range(0f, 1f)] public float walkVolume = 0.4f;

    [Header("Walk Settings")]
    public float walkStepInterval = 0.35f; // seconds between footstep sounds

    AudioSource audioSource;
    AudioSource walkAudioSource;  // separate source for looping walk
    float       walkStepTimer = 0f;
    bool        isWalking     = false;

    void Awake()
    {
        // Main audio source for one-shot sounds
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        // Separate audio source for walk sounds
        walkAudioSource = gameObject.AddComponent<AudioSource>();
        walkAudioSource.playOnAwake = false;
    }

    void OnEnable()
    {
        PlayerSounds.OnWaterHit    += PlayWater;
        PlayerSounds.OnEnemyHit    += PlayEnemy;
        PlayerSounds.OnFlagReached += PlayFlag;
        PlayerSounds.OnGemCollected+= PlayGem;
        PlayerSounds.OnJump        += PlayJump;
        PlayerSounds.OnWalkStart   += () => isWalking = true;
        PlayerSounds.OnWalkStop    += () => isWalking = false;
    }

    void OnDisable()
    {
        PlayerSounds.OnWaterHit    -= PlayWater;
        PlayerSounds.OnEnemyHit    -= PlayEnemy;
        PlayerSounds.OnFlagReached -= PlayFlag;
        PlayerSounds.OnGemCollected-= PlayGem;
        PlayerSounds.OnJump        -= PlayJump;
    }

    void Update()
    {
        // Play footstep sound at intervals while walking
        if (isWalking && walkSound != null)
        {
            walkStepTimer -= Time.deltaTime;
            if (walkStepTimer <= 0f)
            {
                walkAudioSource.PlayOneShot(walkSound, walkVolume);
                walkStepTimer = walkStepInterval;
            }
        }
        else
        {
            walkStepTimer = 0f; // reset so next step plays immediately
        }
    }

    void PlayWater()  => Play(waterSound);
    void PlayEnemy()  => Play(enemySound);
    void PlayFlag()   => Play(flagSound);
    void PlayGem()    => Play(gemSound);
    void PlayJump()   => Play(jumpSound);

    void Play(AudioClip clip)
    {
        if (clip == null) return;
        audioSource.PlayOneShot(clip, volume);
    }
}
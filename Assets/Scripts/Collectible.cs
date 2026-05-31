using UnityEngine;

// ══════════════════════════════════════════════════════════════════════════════
// Collectible
// Attach to each gem/coin sprite. Tag "Collectible". Add trigger CircleCollider2D.
// Bobs and spins. Plays gem sound on collect.
// ══════════════════════════════════════════════════════════════════════════════
public class Collectible : MonoBehaviour
{
    [Header("Animation")]
    public float bobHeight = 0.12f;
    public float bobSpeed  = 2.5f;
    public float spinSpeed = 120f;

    CollectibleManager mgr;
    Vector3 origin;
    bool    taken;

    void Start()
    {
        origin = transform.position;
        mgr    = FindAnyObjectByType<CollectibleManager>();
    }

    void Update()
    {
        if (taken) return;
        float y = origin.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = new Vector3(origin.x, y, origin.z);
        transform.Rotate(0, spinSpeed * Time.deltaTime, 0);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (taken || !other.CompareTag("Player")) return;
        taken = true;
        PlayerSounds.GemCollected(); // play gem sound
        mgr?.Register();
        Destroy(gameObject);
    }
}
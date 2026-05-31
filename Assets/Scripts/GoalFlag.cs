using UnityEngine;

// ══════════════════════════════════════════════════════════════════════════════
// GoalFlag
// Attach to your Goal/Flag GameObject.
// Tag it "Goal". Add a trigger BoxCollider2D.
// Grey until all gems collected, yellow when ready.
// ══════════════════════════════════════════════════════════════════════════════
public class GoalFlag : MonoBehaviour
{
    CollectibleManager cm;
    SpriteRenderer     sr;
    bool               triggered;

    void Start()
    {
        cm = FindAnyObjectByType<CollectibleManager>();
        sr = GetComponent<SpriteRenderer>();
        UpdateColor();
        GameEvents.OnCollectiblePickedUp += _ => UpdateColor();
    }

    void Update()
    {
        // Gentle wave animation
        transform.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(Time.time * 2f) * 6f);
    }

    void UpdateColor()
    {
        if (sr == null) return;
        sr.color = (cm == null || cm.AllCollected) ? Color.yellow : Color.gray;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered || !other.CompareTag("Player")) return;
        if (cm != null && !cm.AllCollected)
        { GameEvents.HintMessage("Collect all gems first!"); return; }
        triggered = true;
        GameEvents.PlayerWon();
    }

    void OnDestroy() => GameEvents.OnCollectiblePickedUp -= _ => UpdateColor();
}

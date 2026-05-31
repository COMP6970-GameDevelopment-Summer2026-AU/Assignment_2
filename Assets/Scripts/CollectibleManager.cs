using UnityEngine;

// Testing mode: CollectibleManager disabled.
// AllCollected always returns true so Goal works without gems.
// Re-enable when adding gems to the scene.
public class CollectibleManager : MonoBehaviour
{
    public bool AllCollected => true;

    void Start()
    {
        // Report 0 required so HUD shows Gems: 0/0
        GameEvents.CollectiblesRequired(0);
        Debug.Log("[CollectibleManager] Testing mode — gems disabled, goal is always open.");
    }

    public void Register() { }
}
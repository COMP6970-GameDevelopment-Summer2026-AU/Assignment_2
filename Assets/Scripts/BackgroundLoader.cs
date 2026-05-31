using UnityEngine;

// Loads parchmentAncient.png from Assets/Resources/ and pins it to camera.
// SETUP: drag parchmentAncient.png into Assets/Resources/
public class BackgroundLoader : MonoBehaviour
{
    void Start()
    {
        // Remove any existing background
        var old = GameObject.Find("_Background_Parchment");
        if (old != null) Destroy(old);

        var tex = Resources.Load<Texture2D>("parchmentAncient");
        if (tex == null)
        {
            Debug.LogWarning("[BackgroundLoader] parchmentAncient.png not found in Assets/Resources/");
            return;
        }

        var sprite = Sprite.Create(tex,
            new Rect(0, 0, tex.width, tex.height),
            new Vector2(0.5f, 0.5f), 100f);

        var go = new GameObject("_Background_Parchment");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite       = sprite;
        sr.sortingOrder = -10;

        // Scale to fill camera view
        var cam = Camera.main;
        float h = cam.orthographicSize * 2f;
        float w = h * cam.aspect;
        go.transform.localScale = new Vector3(
            w / sr.bounds.size.x * 1.1f,
            h / sr.bounds.size.y * 1.1f, 1f);

        // Pin to camera so it always fills the view
        go.transform.SetParent(cam.transform);
        go.transform.localPosition = new Vector3(0f, 0f, 10f);

        Debug.Log("[BackgroundLoader] parchmentAncient loaded.");
    }
}
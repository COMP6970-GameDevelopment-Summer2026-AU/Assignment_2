#if UNITY_EDITOR
// ══════════════════════════════════════════════════════════════════════════════
// ProjectSetup.cs — place in Assets/Editor/
//
// ONLY creates required tags and layers.
// Does NOT place, move, or modify any GameObjects or tilemaps.
// You design the scene — this just ensures the tags exist.
//
// HOW TO USE:
//   Unity menu → Tools → Platformer2D A2 → Create Tags & Layers
// ══════════════════════════════════════════════════════════════════════════════
using UnityEngine;
using UnityEditor;

public static class ProjectSetup
{
    static readonly string[] TAGS   = { "Player","Enemy","Collectible","Goal","Hazard","Water","Ground","HazardObject","Bridge","Checkpoint" };
    static readonly string[] LAYERS = { "Ground","Platform","Player","Enemy" };

    [MenuItem("Tools/Platformer2D A2/Fix Background Tile Imports")]
    public static void FixBackgroundImports()
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", 
            new[]{"Assets/Resources/Backgrounds"});
        int count = 0;
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null) continue;
            imp.textureType         = TextureImporterType.Sprite;
            imp.spriteImportMode    = SpriteImportMode.Single;
            imp.spritePixelsPerUnit = 24f;
            imp.filterMode          = FilterMode.Point;
            imp.textureCompression  = TextureImporterCompression.Uncompressed;
            var s = new TextureImporterSettings();
            imp.ReadTextureSettings(s);
            s.spriteMeshType = SpriteMeshType.FullRect; // fixes tiling warning
            imp.SetTextureSettings(s);
            imp.SaveAndReimport();
            count++;
        }
        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("Done", 
            $"Fixed {count} background tiles.\nNow press Play — background will paint!", "OK");
    }

    [MenuItem("Tools/Platformer2D A2/Create Tags and Layers")]
    public static void CreateTagsAndLayers()
    {
        var tm = new SerializedObject(
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);

        // Tags
        var tags = tm.FindProperty("tags");
        int tagsAdded = 0;
        foreach (string tag in TAGS)
        {
            bool exists = false;
            for (int i = 0; i < tags.arraySize; i++)
                if (tags.GetArrayElementAtIndex(i).stringValue == tag) { exists = true; break; }
            if (!exists)
            {
                tags.InsertArrayElementAtIndex(tags.arraySize);
                tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tag;
                tagsAdded++;
            }
        }

        // Layers (user slots 8–31)
        var layers = tm.FindProperty("layers");
        int layersAdded = 0;
        foreach (string layer in LAYERS)
        {
            bool exists = false;
            for (int i = 0; i < layers.arraySize; i++)
                if (layers.GetArrayElementAtIndex(i).stringValue == layer) { exists = true; break; }
            if (!exists)
            {
                for (int i = 8; i < layers.arraySize; i++)
                {
                    if (string.IsNullOrEmpty(layers.GetArrayElementAtIndex(i).stringValue))
                    {
                        layers.GetArrayElementAtIndex(i).stringValue = layer;
                        layersAdded++;
                        break;
                    }
                }
            }
        }

        tm.ApplyModifiedProperties();
        AssetDatabase.SaveAssets();

        EditorUtility.DisplayDialog("Tags & Layers Created",
            $"Tags added: {tagsAdded}\nLayers added: {layersAdded}\n\n" +
            "Tags: Player, Enemy, Collectible, Goal, Hazard, Water, Ground\n" +
            "Layers: Ground, Platform, Player, Enemy\n\n" +
            "Now design your scene and attach scripts manually.",
            "Done!");

        Debug.Log($"[Setup] Tags added: {tagsAdded}, Layers added: {layersAdded}");
    }
}
#endif

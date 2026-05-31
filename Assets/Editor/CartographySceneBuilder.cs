#if UNITY_EDITOR
// ══════════════════════════════════════════════════════════════════════════════
// CartographySceneBuilder.cs
//
// STORY: "The Last Cartographer"
//
// WORLD DESIGN:
//   • Zones are SHORT horizontally (8 tiles) — less horizontal walking
//   • BIG elevation jumps between zones (+7, -7, +11, -7, +11, -8)
//   • Zigzag stepping stone platforms — player goes UP-RIGHT-UP or DOWN-RIGHT-DOWN
//   • Each gap has 3 single-tile steps stairstepping vertically
//   • Decorations: 2 objects per zone, spaced 4+ tiles apart
//
// ELEVATION MAP:
//   ⚓ HARBOR      y=1   x=-28 to -21  (sea level, start)
//        ↑+7  zigzag up  3 steps
//   🏘 SETTLEMENT  y=8   x=-16 to  -9  (hilltop)
//        ↓-7  zigzag down 3 steps
//   🌲 FOREST      y=1   x= -4 to   3  (valley floor)
//        ↑+11 zigzag up  3 steps (big climb!)
//   ⛩ RUINS       y=12  x=  8 to  15  (high plateau)
//        ↓-7  zigzag down 3 steps
//   🌵 DESERT      y=5   x= 20 to  27  (sand dunes)
//        ↑+11 zigzag up  3 steps (big climb!)
//   ⛰ MOUNTAIN    y=16  x= 32 to  39  (summit)
//        ↓-8  zigzag down 3 steps
//   🏰 CASTLE      y=8   x= 44 to  52  (grounds → summit flag y=24)
// ══════════════════════════════════════════════════════════════════════════════
using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using System.IO;

public static class CartographySceneBuilder
{
    [MenuItem("Tools/Cartography/Build Platform Level (Default 64px)")]
    public static void BuildDefault() => Build("Assets/Cartography/Default", 64f);

    [MenuItem("Tools/Cartography/Build Platform Level (Retina 128px)")]
    public static void BuildRetina()  => Build("Assets/Cartography/Retina", 128f);

    [MenuItem("Tools/Cartography/Build Platform Level (Auto-detect)")]
    public static void BuildAuto()
    {
        if (AssetDatabase.IsValidFolder("Assets/Cartography/Default"))
            Build("Assets/Cartography/Default", 64f);
        else if (AssetDatabase.IsValidFolder("Assets/Cartography/Retina"))
            Build("Assets/Cartography/Retina", 128f);
        else
        {
            var g = AssetDatabase.FindAssets("textureStone t:Texture2D");
            if (g.Length > 0)
            {
                var p = AssetDatabase.GUIDToAssetPath(g[0]);
                var f = Path.GetDirectoryName(p).Replace("\\", "/");
                Build(f, p.ToLower().Contains("retina") ? 128f : 64f);
            }
            else EditorUtility.DisplayDialog("Tiles not found",
                "Place tiles at:\nAssets/Cartography/Default/", "OK");
        }
    }

    static string F;
    static float  P;
    static Dictionary<string, TileBase> Cache = new Dictionary<string, TileBase>();

    static void Build(string folder, float ppu)
    {
        F = folder; P = ppu;
        Cache.Clear();
        FixImports();
        AssetDatabase.Refresh();

        var cam = Camera.main;
        if (cam != null)
        {
            cam.backgroundColor  = new Color(0.83f, 0.74f, 0.54f);
            cam.clearFlags       = CameraClearFlags.SolidColor;
            cam.orthographicSize = 5f; // tighter zoom suits compact world
        }

        Tilemap map = FindMap();
        if (map == null)
        {
            EditorUtility.DisplayDialog("No Tilemap",
                "Create one:\nHierarchy → 2D Object → Tilemap → Rectangular\n" +
                "Name: 'Platform'  Tag: 'Ground'", "OK");
            return;
        }

        map.ClearAllTiles();

        var stone = T("textureStone");
        var brick = T("textureBricks");
        var gnd   = stone ?? brick;
        if (gnd == null)
        { EditorUtility.DisplayDialog("No tiles","Cannot load from:\n"+folder,"OK"); return; }

        // ══════════════════════════════════════════════════════════════════════
        // GROUND SECTIONS — short (8 tiles), big elevation differences
        // ══════════════════════════════════════════════════════════════════════

        Gnd(map, -28, -21,  1, gnd, brick);  // HARBOR      y=1
        Gnd(map, -16,  -9,  8, gnd, brick);  // SETTLEMENT  y=8
        Gnd(map,  -4,   3,  1, gnd, brick);  // FOREST      y=1
        Gnd(map,   8,  15, 12, gnd, brick);  // RUINS       y=12
        Gnd(map,  20,  27,  5, gnd, brick);  // DESERT      y=5
        Gnd(map,  32,  39, 16, gnd, brick);  // MOUNTAIN    y=16
        Gnd(map,  44,  52,  8, gnd, brick);  // CASTLE      y=8

        // ══════════════════════════════════════════════════════════════════════
        // ZIGZAG STEPPING STONE PLATFORMS
        // Each gap has 3 single-tile steps — staircase effect
        // Player steps one tile at a time, each jump is purposeful
        // ══════════════════════════════════════════════════════════════════════
        var B  = T("bridge")     ?? T("pathStraight") ?? gnd;
        var R  = T("bridgeRope") ?? T("bridge")       ?? gnd;

        // ── Gap 1: HARBOR(y=1) → SETTLEMENT(y=8)  ↑+7 ───────────────────────
        // 3 steps: y=3, y=5, y=7 — each shifts x+1 to force rightward movement
        Plat(map, -20, 3,  B);   // step 1  y=3
        Plat(map, -19, 5,  R);   // step 2  y=5  (1 tile right, 2 up)
        Plat(map, -18, 7,  B);   // step 3  y=7  (1 tile right, 2 up)
        // lands on settlement ground y=8

        // ── Gap 2: SETTLEMENT(y=8) → FOREST(y=1)  ↓-7 ───────────────────────
        // Steps down: y=6, y=4, y=2
        Plat(map,  -8, 6,  R);   // step 1  y=6  (drop from 8)
        Plat(map,  -7, 4,  B);   // step 2  y=4
        Plat(map,  -6, 2,  R);   // step 3  y=2  (drop to forest y=1)

        // ── Gap 3: FOREST(y=1) → RUINS(y=12)  ↑+11 BIG ZIGZAG ──────────────
        // 3 steps: y=4, y=7, y=10 — big jumps, exciting climb!
        Plat(map,   4,  4, B);   // step 1  y=4
        Plat(map,   5,  7, R);   // step 2  y=7   (jump up 3!)
        Plat(map,   6, 10, B);   // step 3  y=10  (jump up 3!)
        // then jump to ruins ground y=12

        // ── Gap 4: RUINS(y=12) → DESERT(y=5)  ↓-7 ───────────────────────────
        // Steps down: y=9, y=7, y=5
        Plat(map,  16, 9,  R);   // step 1  y=9
        Plat(map,  17, 7,  B);   // step 2  y=7
        Plat(map,  18, 5,  R);   // step 3  y=5   (lands on desert y=5)

        // ── Gap 5: DESERT(y=5) → MOUNTAIN(y=16)  ↑+11 BIG ZIGZAG ────────────
        // 3 big steps: y=8, y=11, y=14
        Plat(map,  28,  8, B);   // step 1  y=8
        Plat(map,  29, 11, R);   // step 2  y=11  (jump up 3!)
        Plat(map,  30, 14, B);   // step 3  y=14  (jump up 3!)
        // then jump to mountain ground y=16

        // ── Gap 6: MOUNTAIN(y=16) → CASTLE(y=8)  ↓-8 ────────────────────────
        // Steps down: y=13, y=10, y=8
        Plat(map,  40, 13, R);   // step 1  y=13
        Plat(map,  41, 10, B);   // step 2  y=10
        Plat(map,  42,  8, R);   // step 3  y=8   (lands on castle y=8)

        // ── CASTLE INTERNAL — climb to GOAL FLAG (y=24) ───────────────────────
        // Vertical zigzag within the castle itself
        Plat(map,  46, 11, B);   // rampart 1
        Plat(map,  48, 14, R);   // rampart 2
        Plat(map,  50, 17, B);   // rampart 3
        Plat(map,  51, 20, R);   // upper tower
        Plat(map,  52, 23, B);   // summit — GOAL here

        // ══════════════════════════════════════════════════════════════════════
        // DECORATIONS — 2 objects per zone, 4+ tiles apart, no crowding
        // ══════════════════════════════════════════════════════════════════════

        // ⚓ HARBOR  (8 tiles: x=-28 to -21)
        D(map, -27,  2, "compass");       // start marker
        D(map, -23,  2, "lighthouse");    // 4 tiles gap
        // (zone edge tiles -21,-20 empty — open ground before gap)

        // 🏘 SETTLEMENT  (x=-16 to -9)
        D(map, -15,  9, "church");        // landmark
        D(map, -11,  9, "mill");          // 4 tiles gap

        // 🌲 FOREST  (x=-4 to 3)
        D(map,  -3,  2, "treePineTall"); // TALL ← jump over
        D(map,   2,  2, "treeTall");     // TALL ← jump over  (5 tiles gap)

        // ⛩ RUINS  (x=8 to 15)
        D(map,   9, 13, "gate");          // ancient gate
        D(map,  13, 13, "pyramid");       // pyramid  (4 tiles gap)

        // 🌵 DESERT  (x=20 to 27)
        D(map,  21,  6, "cactusLarge");   // cactus ← jump over
        D(map,  25,  6, "palmLarge");     // palm   (4 tiles gap)

        // ⛰ MOUNTAIN  (x=32 to 39)
        D(map,  33, 17, "rocksMountain"); // rocks
        D(map,  37, 17, "watchtower");    // watchtower  (4 tiles gap)

        // 🏰 CASTLE  (x=44 to 52)
        D(map,  45,  9, "gate");          // castle gate
        D(map,  49,  9, "castle");        // castle keep  (4 tiles gap)

        // GOAL FLAG at castle summit
        D(map,  52, 24, "flag");          // ← GOAL FLAG!

        // ── CHECKPOINTS (3 total, one per major zone) ─────────────────────────
        // Placed at zone entry points where player first lands on ground
        PlaceCheckpoint(1, new Vector3(-12f, 9f,  0f));  // After forest (zone 3)
        PlaceCheckpoint(2, new Vector3( 20f, 6f,  0f));  // After ruins→desert (zone 5)
        PlaceCheckpoint(3, new Vector3( 44f, 9f,  0f));  // Castle entrance (zone 7)

        // ── HAZARD OBJECTS placed in scene (cactus, bush, campfire, lake) ─────
        // These use sprites from Assets/Cartography/Default/
        // Tag: HazardObject, trigger collider — player loses -10pts, -0.5 life
        PlaceHazardObj("cactus",   new Vector3(-3f,  3f, 0f),  "cactus");
        PlaceHazardObj("cactus2",  new Vector3( 2f,  3f, 0f),  "cactusLarge");
        PlaceHazardObj("bush1",    new Vector3(-18f, 9f, 0f),  "bush");
        PlaceHazardObj("campfire1",new Vector3( 12f, 11f,0f),  "campfire");
        PlaceHazardObj("campfire2",new Vector3( 23f, 6f, 0f),  "campfire");
        PlaceHazardObj("lake1",    new Vector3( 32f, 17f,0f),  "lake");

        // ── BRIDGE OBJECTS (trigger) — player gets +10pts ─────────────────────
        // Placed on the stepping stone platforms between zones
        PlaceBridgeObj("Bridge_1", new Vector3(-20f, 3.5f, 0f));
        PlaceBridgeObj("Bridge_2", new Vector3( -7f, 6.5f, 0f));
        PlaceBridgeObj("Bridge_3", new Vector3(  4f, 4.5f, 0f));
        PlaceBridgeObj("Bridge_4", new Vector3( 16f, 9.5f, 0f));
        PlaceBridgeObj("Bridge_5", new Vector3( 28f, 8.5f, 0f));
        PlaceBridgeObj("Bridge_6", new Vector3( 40f, 13.5f,0f));

        map.RefreshAllTiles();
        AssetDatabase.SaveAssets();

        EditorUtility.DisplayDialog("Built! 🗺️",
            "\"The Last Cartographer\" — Compact Vertical World\n\n" +
            "Zones: 8 tiles wide (50% reduction)\n" +
            "Vertical zigzag between every zone\n\n" +
            "ELEVATION:\n" +
            "  ⚓ Harbor     y=1\n" +
            "  ↑+7  zigzag 3 steps\n" +
            "  🏘 Settlement y=8\n" +
            "  ↓-7  3 steps down\n" +
            "  🌲 Forest     y=1\n" +
            "  ↑+11 BIG zigzag!\n" +
            "  ⛩ Ruins      y=12\n" +
            "  ↓-7  3 steps down\n" +
            "  🌵 Desert     y=5\n" +
            "  ↑+11 BIG zigzag!\n" +
            "  ⛰ Mountain   y=16\n" +
            "  ↓-8  3 steps down\n" +
            "  🏰 Castle     y=8 → flag y=24\n\n" +
            "Player start: (-28, 3)\n" +
            "Goal flag:    (52, 24)",
            "Adventure! ⚔️");
    }

    // ══════════════════════════════════════════════════════════════════════════
    static Tilemap FindMap()
    {
        var g = Object.FindFirstObjectByType<Grid>();
        if (g == null) return null;
        foreach (Transform c in g.transform)
        {
            var tm = c.GetComponent<Tilemap>();
            if (tm == null) continue;
            string n = c.name.ToLower();
            if (!n.Contains("bg") && !n.Contains("background")) return tm;
        }
        return g.GetComponentInChildren<Tilemap>();
    }

    // Ground: surface at y, fill at y-1 and y-2
    static void Gnd(Tilemap map, int x1, int x2, int y,
                    TileBase top, TileBase fill)
    {
        Row(map, x1, x2, y,   top);
        Row(map, x1, x2, y-1, fill ?? top);
        Row(map, x1, x2, y-2, fill ?? top);
    }

    // Single platform tile at (x, y) — 1 tile wide stepping stone
    static void Plat(Tilemap map, int x, int y, TileBase t)
    {
        if (t == null || map == null) return;
        map.SetTile(new Vector3Int(x, y, 0), t);
    }

    static void Row(Tilemap map, int x1, int x2, int y, TileBase t)
    {
        if (t == null || map == null) return;
        for (int x = x1; x <= x2; x++)
            map.SetTile(new Vector3Int(x, y, 0), t);
    }

    static void D(Tilemap map, int x, int y, string name)
    {
        var t = T(name);
        if (t != null) map.SetTile(new Vector3Int(x, y, 0), t);
        else           Debug.LogWarning($"[Cartography] Not found: {name}");
    }

    static TileBase T(string name)
    {
        if (Cache.TryGetValue(name, out var c)) return c;
        string ap = $"Assets/Tiles/Cartography/{name}.asset";
        var ex = AssetDatabase.LoadAssetAtPath<Tile>(ap);
        if (ex != null) { Cache[name] = ex; return ex; }
        var sp = Spr(name);
        if (sp == null) { Cache[name] = null; return null; }
        if (!AssetDatabase.IsValidFolder("Assets/Tiles"))
            AssetDatabase.CreateFolder("Assets", "Tiles");
        if (!AssetDatabase.IsValidFolder("Assets/Tiles/Cartography"))
            AssetDatabase.CreateFolder("Assets/Tiles", "Cartography");
        var tile = ScriptableObject.CreateInstance<Tile>();
        tile.sprite = sp;
        tile.colliderType = Tile.ColliderType.Grid;
        AssetDatabase.CreateAsset(tile, ap);
        Cache[name] = tile;
        return tile;
    }

    static Sprite Spr(string name)
    {
        string path = $"{F}/{name}.png";
        var sp = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sp != null) return sp;
        foreach (var a in AssetDatabase.LoadAllAssetsAtPath(path))
            if (a is Sprite s) return s;
        return null;
    }

    static void PlaceCheckpoint(int num, Vector3 pos)
    {
        string n = $"Checkpoint_{num}";
        // Destroy existing so we always recreate with correct settings
        var existing = GameObject.Find(n);
        if (existing != null) Object.DestroyImmediate(existing);

        var sp = Spr("flag") ?? Spr("compass");

        var go = new GameObject(n);
        go.transform.position = pos;
        go.tag = "Checkpoint";

        var sr = go.AddComponent<SpriteRenderer>();
        if (sp != null) { sr.sprite = sp; sr.color = Color.gray; }
        else { sr.color = new Color(0.2f, 1f, 0.4f); }
        sr.sortingOrder = 3;

        // Checkpoint script adds its own dual colliders (solid + trigger) in Awake()
        go.AddComponent<Checkpoint>().checkpointNumber = num;
        Debug.Log($"[Builder] Checkpoint {num} at {pos}");
    }

    static void PlaceHazardObj(string name, Vector3 pos, string spriteName)
    {
        var existing = GameObject.Find("HazObj_"+name);
        if (existing != null) Object.DestroyImmediate(existing);

        var sp = Spr(spriteName);
        var go = new GameObject("HazObj_"+name);
        go.transform.position = pos;
        go.tag = "HazardObject";

        var sr = go.AddComponent<SpriteRenderer>();
        if (sp != null) sr.sprite = sp;
        else { sr.color = new Color(1f,0.3f,0.1f); }
        sr.sortingOrder = 3;

        // HazardObject script adds its own dual colliders in Awake()
        go.AddComponent<HazardObject>().hazardType = spriteName;
        Debug.Log($"[Builder] Hazard '{spriteName}' at {pos}");
    }

    static void PlaceBridgeObj(string name, Vector3 pos)
    {
        var existing = GameObject.Find("BridgeObj_"+name);
        if (existing != null) Object.DestroyImmediate(existing);

        var sp = Spr("bridge") ?? Spr("bridgeRope");
        var go = new GameObject("BridgeObj_"+name);
        go.transform.position = pos;
        go.tag = "Ground";  // Ground tag = player can stand on it

        var sr = go.AddComponent<SpriteRenderer>();
        if (sp != null) { sr.sprite = sp; sr.color = new Color(0.8f,0.7f,0.3f); }
        sr.sortingOrder = 3;

        // BridgeObject script adds its own dual colliders in Awake()
        go.AddComponent<BridgeObject>();
        Debug.Log($"[Builder] Bridge trigger at {pos}");
    }

    static void FixImports()
    {
        var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { F });
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var imp  = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null) continue;
            imp.textureType         = TextureImporterType.Sprite;
            imp.spriteImportMode    = SpriteImportMode.Single;
            imp.spritePixelsPerUnit = P;
            imp.filterMode          = FilterMode.Bilinear;
            imp.textureCompression  = TextureImporterCompression.Uncompressed;
            imp.spritePivot         = new Vector2(0.5f, 0.5f);
            var s = new TextureImporterSettings();
            imp.ReadTextureSettings(s);
            s.spriteMeshType = SpriteMeshType.FullRect;
            imp.SetTextureSettings(s);
            imp.SaveAndReimport();
        }
        Debug.Log($"[Cartography] Fixed {guids.Length} sprites @ {P}PPU");
    }
}
#endif
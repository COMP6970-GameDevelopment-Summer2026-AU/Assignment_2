using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// ══════════════════════════════════════════════════════════════════════════════
// GameManager
// Attach to an empty GameObject named _GameManager in your scene.
// Handles: start screen, HUD, win screen, lose screen — all via OnGUI.
// Zero Canvas wiring needed.
// ══════════════════════════════════════════════════════════════════════════════
public class GameManager : MonoBehaviour
{
    enum State { Start, Playing, Win, Lose }
    State  state     = State.Start;
    float  lives     = 3f;
    int    score     = 0;
    int    checkpoint = 0;
    float  playTime  = 0f;
    string hint      = "";
    float  hintTimer = 0f;

    GUIStyle sBig, sSub, sMeta, sPrompt, sHud, sHint, sGreen, sBlue;
    bool stylesReady;

    // ── Start screen animation ─────────────────────────────────────────────────
    float slideY      = 0f;     // current Y position (0 = fully out top, 1 = settled)
    float slideTarget = 0f;
    bool  slideReady  = false;
    const float SLIDE_SPEED = 4f;
    const float PANEL_HEIGHT_RATIO = 0.52f; // panel covers 52% of screen height

    void OnEnable()
    {
        GameEvents.OnPlayerDied          += () => ChangeState(State.Lose);
        GameEvents.OnPlayerWon           += () => ChangeState(State.Win);
        GameEvents.OnLivesChanged        += n => lives     = n; // n is ceiling int, we track float internally
        GameEvents.OnHintMessage         += m => { hint = m; hintTimer = 3f; };
        GameEvents.OnScoreChanged        += n => score = n;
        GameEvents.OnCheckpointReached   += p => checkpoint++;
    }

    void OnDisable()
    {
        GameEvents.OnPlayerDied          -= () => ChangeState(State.Lose);
        GameEvents.OnPlayerWon           -= () => ChangeState(State.Win);
        GameEvents.OnLivesChanged        -= n => lives     = n;
        GameEvents.OnHintMessage         -= m => { hint = m; hintTimer = 3f; };
        GameEvents.OnScoreChanged        -= n => score = n;
        GameEvents.OnCheckpointReached   -= p => checkpoint++;
    }

    void Start()
    {
        Time.timeScale = 0f;
    }

    void Update()
    {
        if (state == State.Playing)
        {
            playTime += Time.deltaTime;
            if (hintTimer > 0f) { hintTimer -= Time.deltaTime; if (hintTimer <= 0f) hint = ""; }
        }

        // Animate panel slide
        if (state == State.Start)
        {
            if (!slideReady) { slideY = -1f; slideTarget = 0f; slideReady = true; }
            slideY = Mathf.Lerp(slideY, slideTarget, Time.unscaledDeltaTime * SLIDE_SPEED);
        }

        if (state == State.Start && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            state = State.Playing;
            Time.timeScale = 1f;
            GameEvents.GameStarted();
        }

        if ((state == State.Win || state == State.Lose) &&
            Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    void ChangeState(State s) { state = s; Time.timeScale = 0f; }

    // ── Styles ────────────────────────────────────────────────────────────────
    void InitStyles()
    {
        if (stylesReady) return;
        sBig    = Sty(52, FontStyle.Bold,   Color.white,                   TextAnchor.MiddleCenter);
        sSub    = Sty(22, FontStyle.Normal, new Color(0.85f,0.85f,0.85f),  TextAnchor.MiddleCenter);
        sMeta   = Sty(17, FontStyle.Normal, new Color(0.7f,0.7f,0.7f),     TextAnchor.MiddleCenter);
        sPrompt = Sty(28, FontStyle.Bold,   Color.yellow,                  TextAnchor.MiddleCenter);
        sHud    = Sty(18, FontStyle.Bold,   Color.white,                   TextAnchor.MiddleCenter);
        sHint   = Sty(20, FontStyle.Bold,   Color.yellow,                  TextAnchor.MiddleCenter);
        sGreen  = Sty(17, FontStyle.Normal, new Color(0.3f,1f,0.4f),       TextAnchor.MiddleLeft);
        sBlue   = Sty(17, FontStyle.Normal, new Color(0.4f,0.85f,1f),      TextAnchor.MiddleLeft);
        stylesReady = true;
    }

    GUIStyle Sty(int sz, FontStyle fs, Color c, TextAnchor a)
    {
        var s = new GUIStyle();
        s.fontSize = sz; s.fontStyle = fs; s.normal.textColor = c; s.alignment = a;
        return s;
    }

    // ── OnGUI ─────────────────────────────────────────────────────────────────
    void OnGUI()
    {
        InitStyles();
        float sw = Screen.width, sh = Screen.height;
        switch (state)
        {
            case State.Start:   DrawStart(sw, sh);  break;
            case State.Playing: DrawHUD(sw, sh);    break;
            case State.Win:     DrawWin(sw, sh);    break;
            case State.Lose:    DrawLose(sw, sh);   break;
        }
    }

    // ── Start screen ──────────────────────────────────────────────────────────
    void DrawStart(float sw, float sh)
    {
        // ── Panel slides down from top — covers top 42% of screen ─────────────
        float panelH = sh * PANEL_HEIGHT_RATIO;
        // slideY: -1 = fully above screen, 0 = settled at top
        float offsetY = slideY * panelH;

        // Semi-transparent game visible behind/below the panel
        // Subtle dark scrim over whole screen (light — game still visible)
        GUI.color = new Color(0f, 0.05f, 0f, 0.28f);
        GUI.DrawTexture(new Rect(0, 0, sw, sh), Texture2D.whiteTexture);
        GUI.color = Color.white;

        // ── Main panel ────────────────────────────────────────────────────────
        float panelY = offsetY;

        // Panel background — dark forest green
        GUI.color = new Color(0.06f, 0.14f, 0.08f, 0.97f);
        GUI.DrawTexture(new Rect(0, panelY, sw, panelH), Texture2D.whiteTexture);
        // Slightly lighter green strip at top for gradient feel
        GUI.color = new Color(0.08f, 0.20f, 0.10f, 0.6f);
        GUI.DrawTexture(new Rect(0, panelY, sw, panelH * 0.35f), Texture2D.whiteTexture);
        GUI.color = Color.white;

        // ── Accent top bar — bright green ─────────────────────────────────────
        GUI.color = new Color(0.18f, 0.72f, 0.32f, 1f);
        GUI.DrawTexture(new Rect(0, panelY, sw, 3), Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUI.color = new Color(0f, 0f, 0f, 0.6f);
        GUI.DrawTexture(new Rect(0, panelY + panelH, sw, 4), Texture2D.whiteTexture);
        GUI.color = new Color(0.18f, 0.72f, 0.32f, 0.4f);
        GUI.DrawTexture(new Rect(0, panelY + panelH + 4, sw, 1), Texture2D.whiteTexture);
        GUI.color = Color.white;

        // Remap all Y positions relative to panelY
        float sh2 = panelH; // treat panel height as effective screen height for layout

        // ── Title block ────────────────────────────────────────────────────────
        var sTitle = Sty(36, FontStyle.Bold,   new Color(0.88f,1f,0.88f),     TextAnchor.MiddleCenter);
        var sSub2  = Sty(14, FontStyle.Normal, new Color(0.55f,0.78f,0.58f),  TextAnchor.MiddleCenter);
        var sMeta2 = Sty(12, FontStyle.Normal, new Color(0.38f,0.58f,0.40f),  TextAnchor.MiddleCenter);

        GUI.Label(new Rect(0, panelY+5,  sw, 38), "PuKu's Adventure", sTitle);
        GUI.Label(new Rect(0, panelY+43, sw, 18), "Assignment 2  •  COMP 6970 Game Development  •  Summer 2026  •  Jahidul Arafat", sSub2);
        GUI.Label(new Rect(0, panelY+59, sw, 16), "Built with Kenney Cartography Pack  •  Unity 6  •  Platformer2D", sMeta2);

        GUI.color = new Color(0.18f, 0.72f, 0.32f, 0.15f);
        GUI.DrawTexture(new Rect(sw*0.05f, panelY+78, sw*0.9f, 1), Texture2D.whiteTexture);
        GUI.color = Color.white;

        // ── Three info cards ───────────────────────────────────────────────────
        float pad   = 20f;
        float cardW = (sw - pad * 4) / 3f;
        float cardH = panelH - 136f;
        float cardY = panelY + 80f;
        float[] cardX = { pad, pad*2 + cardW, pad*3 + cardW*2 };

        var sCardHdr = Sty(12, FontStyle.Bold,   new Color(0.28f,0.85f,0.45f), TextAnchor.UpperLeft);
        var sCardLbl = Sty(12, FontStyle.Bold,   new Color(0.88f,0.92f,0.86f), TextAnchor.UpperLeft);
        var sCardVal = Sty(12, FontStyle.Normal, new Color(0.60f,0.70f,0.60f), TextAnchor.UpperLeft);
        var sCardTag = Sty(11, FontStyle.Bold,   new Color(0.28f,0.42f,0.32f), TextAnchor.MiddleCenter);

        for (int i = 0; i < 3; i++)
        {
            GUI.color = new Color(0.04f, 0.11f, 0.05f, 0.92f);
            GUI.DrawTexture(new Rect(cardX[i], cardY, cardW, cardH), Texture2D.whiteTexture);
            GUI.color = new Color(0.18f, 0.72f, 0.32f, 0.18f);
            GUI.DrawTexture(new Rect(cardX[i], cardY, cardW, 1), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(cardX[i], cardY, 1, cardH), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(cardX[i]+cardW-1, cardY, 1, cardH), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(cardX[i], cardY+cardH-1, cardW, 1), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        float lh = 17f;
        float ip = 8f;

        // ── CARD 1 — CONTROLS & OBJECTIVE ─────────────────────────────────────
        float y = cardY + ip;
        float x = cardX[0] + ip;
        float cw = cardW - ip*2;

        GUI.Label(new Rect(x, y, cw, 18), "CONTROLS", sCardHdr); y += 20;
        DrawRow(x, ref y, cw, lh, "A / ←", "Move left",    sCardLbl, sCardVal);
        DrawRow(x, ref y, cw, lh, "D / →", "Move right",   sCardLbl, sCardVal);
        DrawRow(x, ref y, cw, lh, "SPACE",  "Jump",         sCardLbl, sCardVal);
        DrawRow(x, ref y, cw, lh, "SPACE×2","Double jump",  sCardLbl, sCardVal);
        DrawRow(x, ref y, cw, lh, "TAB",    "Auto jump on/off", sCardLbl, sCardVal);
        y += 5;

        GUI.color = new Color(0.18f,0.72f,0.32f,0.10f);
        GUI.DrawTexture(new Rect(x, y, cw, 1), Texture2D.whiteTexture);
        GUI.color = Color.white; y += 7;

        GUI.Label(new Rect(x, y, cw, 18), "OBJECTIVE", sCardHdr); y += 20;
        DrawBullet(x, ref y, cw, lh, "Reach the flag to win.",      sCardVal);
        DrawBullet(x, ref y, cw, lh, "Cross bridges for +10 pts.",  sCardVal);
        DrawBullet(x, ref y, cw, lh, "Avoid hazards & enemies.",    sCardVal);
        DrawBullet(x, ref y, cw, lh, "3 checkpoints save progress.",sCardVal);
        y += 5;

        GUI.color = new Color(0.18f,0.72f,0.32f,0.10f);
        GUI.DrawTexture(new Rect(x, y, cw, 1), Texture2D.whiteTexture);
        GUI.color = Color.white; y += 7;

        GUI.Label(new Rect(x, y, cw, 18), "LIVES", sCardHdr); y += 20;
        DrawRow(x, ref y, cw, lh, "Start",   "3 lives",           sCardLbl, sCardVal);
        DrawRow(x, ref y, cw, lh, "Enemy",   "-1 life",           sCardLbl, sCardVal);
        DrawRow(x, ref y, cw, lh, "Cliff / lake fall", "-½ life", sCardLbl, sCardVal);
        DrawRow(x, ref y, cw, lh, "Stomp",   "kill enemy",        sCardLbl, sCardVal);
        DrawRow(x, ref y, cw, lh, "3 bridges","+ 1 bonus life",   sCardLbl, sCardVal);

        // ── CARD 2 — SCORE & HAZARDS ───────────────────────────────────────────
        y = cardY + ip;
        x = cardX[1] + ip;

        GUI.Label(new Rect(x, y, cw, 18), "SCORE", sCardHdr); y += 20;
        DrawRow(x, ref y, cw, lh, "Bridge crossed",  "+10 pts",   sCardLbl, sCardVal);
        DrawRow(x, ref y, cw, lh, "3 bridges",       "+1 life",   sCardLbl, sCardVal);
        DrawRow(x, ref y, cw, lh, "Platform bump",   "-1 pt",     sCardLbl, sCardVal);
        DrawRow(x, ref y, cw, lh, "Hazard object",   "-10 pts",   sCardLbl, sCardVal);
        y += 5;

        GUI.color = new Color(0.18f,0.72f,0.32f,0.10f);
        GUI.DrawTexture(new Rect(x, y, cw, 1), Texture2D.whiteTexture);
        GUI.color = Color.white; y += 7;

        GUI.Label(new Rect(x, y, cw, 18), "HAZARDS", sCardHdr); y += 20;
        DrawBullet(x, ref y, cw, lh, "Cactus — -10 pts, -½ life, 3s stun",  sCardVal);
        DrawBullet(x, ref y, cw, lh, "Bush   — -10 pts, -½ life, 3s stun",  sCardVal);
        DrawBullet(x, ref y, cw, lh, "Campfire — -10 pts, -½ life, 3s stun",sCardVal);
        DrawBullet(x, ref y, cw, lh, "Lake — fall in, lose ½ life",          sCardVal);
        DrawBullet(x, ref y, cw, lh, "Cliff — fall off, respawn at checkpoint", sCardVal);
        y += 5;

        GUI.color = new Color(0.18f,0.72f,0.32f,0.10f);
        GUI.DrawTexture(new Rect(x, y, cw, 1), Texture2D.whiteTexture);
        GUI.color = Color.white; y += 7;

        GUI.Label(new Rect(x, y, cw, 18), "WORLD ZONES", sCardHdr); y += 20;
        DrawBullet(x, ref y, cw, lh, "Harbor → Settlement → Forest", sCardVal);
        DrawBullet(x, ref y, cw, lh, "Ruins → Desert → Mountain",    sCardVal);
        DrawBullet(x, ref y, cw, lh, "Castle (reach the flag!)",      sCardVal);

        // ── CARD 3 — FEATURES & TECH ───────────────────────────────────────────
        y = cardY + ip;
        x = cardX[2] + ip;

        GUI.Label(new Rect(x, y, cw, 18), "GAMEPLAY FEATURES", sCardHdr); y += 20;
        DrawCheck(x, ref y, cw, lh, "Player movement & double jump", sCardVal);
        DrawCheck(x, ref y, cw, lh, "Coyote time + jump buffering",  sCardVal);
        DrawCheck(x, ref y, cw, lh, "Enemy stomp kill",              sCardVal);
        DrawCheck(x, ref y, cw, lh, "3 checkpoints + cliff respawn", sCardVal);
        DrawCheck(x, ref y, cw, lh, "Score system with bridge bonus",sCardVal);
        DrawCheck(x, ref y, cw, lh, "Hazard objects with stun",      sCardVal);
        DrawCheck(x, ref y, cw, lh, "Smooth camera follow",          sCardVal);
        DrawCheck(x, ref y, cw, lh, "Auto Jump demo mode (TAB)",     sCardVal);
        y += 5;

        GUI.color = new Color(0.18f,0.72f,0.32f,0.10f);
        GUI.DrawTexture(new Rect(x, y, cw, 1), Texture2D.whiteTexture);
        GUI.color = Color.white; y += 7;

        GUI.Label(new Rect(x, y, cw, 18), "TECH", sCardHdr); y += 20;
        DrawRow(x, ref y, cw, lh, "Assets",   "Kenney Cartography Pack", sCardLbl, sCardVal);
        DrawRow(x, ref y, cw, lh, "Engine",   "Unity 6  |  C#",          sCardLbl, sCardVal);
        DrawRow(x, ref y, cw, lh, "UI",       "OnGUI — zero Canvas",     sCardLbl, sCardVal);
        DrawRow(x, ref y, cw, lh, "Pattern",  "Event-driven architecture",sCardLbl, sCardVal);
        DrawRow(x, ref y, cw, lh, "Course",   "COMP 6970  Summer 2026",  sCardLbl, sCardVal);

        // ── Bottom bar ─────────────────────────────────────────────────────────
        float barY = panelY + panelH - 44f;
        GUI.color = new Color(0.04f, 0.14f, 0.05f, 0.98f);
        GUI.DrawTexture(new Rect(0, barY, sw, 50), Texture2D.whiteTexture);
        GUI.color = new Color(0.18f, 0.72f, 0.32f, 0.5f);
        GUI.DrawTexture(new Rect(0, barY, sw, 1), Texture2D.whiteTexture);
        GUI.color = Color.white;

        var sPress  = Sty(22, FontStyle.Bold, new Color(0.7f,1f,0.72f), TextAnchor.MiddleCenter);
        var sTabHint= Sty(12, FontStyle.Normal, new Color(0.35f,0.55f,0.38f), TextAnchor.MiddleRight);
        GUI.Label(new Rect(0, barY, sw, 44), "Press SPACE to Start", sPress);
        GUI.Label(new Rect(0, barY, sw - 20, 44), "TAB = Auto Jump demo", sTabHint);
    }

    // ── Start screen helpers ───────────────────────────────────────────────────
    void DrawRow(float x, ref float y, float w, float lh,
                 string label, string val, GUIStyle lStyle, GUIStyle vStyle)
    {
        float lw = w * 0.48f;
        GUI.Label(new Rect(x,      y, lw,     lh), label, lStyle);
        GUI.Label(new Rect(x + lw, y, w - lw, lh), val,   vStyle);
        y += lh;
    }

    void DrawBullet(float x, ref float y, float w, float lh,
                    string text, GUIStyle style)
    {
        GUI.Label(new Rect(x, y, w, lh), "•  " + text, style);
        y += lh;
    }

    void DrawCheck(float x, ref float y, float w, float lh,
                   string text, GUIStyle style)
    {
        var chkStyle = Sty(style.fontSize, FontStyle.Normal,
                           new Color(0.22f, 0.85f, 0.38f), TextAnchor.UpperLeft);
        GUI.Label(new Rect(x,      y, 20,    lh), "✓", chkStyle);
        GUI.Label(new Rect(x + 20, y, w - 20, lh), text, style);
        y += lh;
    }


    void DrawHUD(float sw, float sh)
    {
        // Show half-heart for fractional lives (e.g. 2.5 shows [♥][♥][½])
        string livesStr = "Lives: ";
        for (int i = 0; i < 3; i++)
        {
            if (i < Mathf.FloorToInt(lives))      livesStr += "[♥] ";
            else if (i < lives)                    livesStr += "[½] ";
            else                                   livesStr += "[ ] ";
        }
        HudBox(10,       10, 210, 32, livesStr);
        HudBox(230,      10, 140, 32, "Score: " + score + " pts");
        HudBox(380,      10, 120, 32, "CP: " + checkpoint + "/3");
        HudBox(sw-130,   10, 120, 32, "Time: " + Mathf.FloorToInt(playTime) + "s");

        if (hint.Length > 0)
        {
            Box(sw/2f-200, sh-80, 400, 36, new Color(0,0,0,0.75f));
            GUI.Label(new Rect(sw/2f-200, sh-80, 400, 36), hint, sHint);
        }
    }

    // ── Win ───────────────────────────────────────────────────────────────────
    void DrawWin(float sw, float sh)
    {
        Box(0, 0, sw, sh, new Color(0, 0.15f, 0, 0.82f));
        var ws  = Sty(52, FontStyle.Bold,   new Color(0.3f,1f,0.4f),       TextAnchor.MiddleCenter);
        var sta = Sty(20, FontStyle.Normal, new Color(0.9f,0.9f,0.9f),      TextAnchor.MiddleCenter);
        var lbl = Sty(18, FontStyle.Bold,   new Color(0.4f,0.85f,1f),       TextAnchor.MiddleCenter);

        float cy = sh * 0.18f;

        GUI.Label(new Rect(0, cy, sw, 62), "YOU WIN!", ws); cy += 68;

        // Divider
        GUI.color = new Color(1,1,1,0.2f);
        GUI.DrawTexture(new Rect(sw*.2f, cy, sw*.6f, 1), Texture2D.whiteTexture);
        GUI.color = Color.white;
        cy += 14;

        // Summary table
        GUI.Label(new Rect(0, cy, sw, 28), "JOURNEY SUMMARY", lbl); cy += 34;

        float bw = 680f, bx = (sw-bw)/2f;

        // Row backgrounds
        DrawStatRow(bx, cy, bw, "Final Score",       score.ToString() + " pts",                        new Color(1f,0.9f,0.2f));  cy += 38;
        DrawStatRow(bx, cy, bw, "Checkpoints Hit",   checkpoint + " / 3",                              new Color(0.4f,1f,0.8f));  cy += 38;
        DrawStatRow(bx, cy, bw, "Bridges Crossed",   ScoreSystem.BridgesTouched.ToString(),            new Color(0.3f,0.9f,0.4f)); cy += 38;
        DrawStatRow(bx, cy, bw, "Time Survived",     Mathf.FloorToInt(playTime) + " seconds",           new Color(0.2f,0.6f,1f));  cy += 38;
        DrawStatRow(bx, cy, bw, "Steps Taken",       GameEvents.StatSteps.ToString(),                   new Color(0.9f,0.6f,1f));  cy += 38;
        DrawStatRow(bx, cy, bw, "Total Jumps",       GameEvents.StatJumps.ToString(),                   new Color(1f,0.8f,0.2f));  cy += 38;
        DrawStatRow(bx, cy, bw, "Water Falls",       GameEvents.StatWater.ToString(),                   new Color(0.2f,0.7f,1f));  cy += 38;
        DrawStatRow(bx, cy, bw, "Enemy Hits",        GameEvents.StatEnemies.ToString(),                 new Color(1f,0.4f,0.4f));  cy += 38;
        DrawStatRow(bx, cy, bw, "Distance Travelled",GameEvents.StatDistance.ToString("F0") + " units", new Color(0.8f,0.8f,0.8f));cy += 48;

        GUI.Label(new Rect(0, sh-58, sw, 44), "Press SPACE to play again", sPrompt);
    }

    void DrawStatRow(float x, float y, float w, string label, string value, Color accent)
    {
        var rowStyle = Sty(19, FontStyle.Normal, Color.white, TextAnchor.MiddleLeft);
        var valStyle = Sty(19, FontStyle.Bold,   accent,      TextAnchor.MiddleRight);

        // Row background
        GUI.color = new Color(1,1,1,0.06f);
        GUI.DrawTexture(new Rect(x, y, w, 32), Texture2D.whiteTexture);
        // Accent left bar
        GUI.color = accent;
        GUI.DrawTexture(new Rect(x, y, 4, 32), Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUI.Label(new Rect(x+12, y, w*0.6f, 32), label, rowStyle);
        GUI.Label(new Rect(x,    y, w-8,    32), value, valStyle);
    }

    // ── Lose ──────────────────────────────────────────────────────────────────
    void DrawLose(float sw, float sh)
    {
        Box(0, 0, sw, sh, new Color(0.18f, 0, 0, 0.78f));
        var ls = Sty(54, FontStyle.Bold, new Color(1f,0.3f,0.3f), TextAnchor.MiddleCenter);
        GUI.Label(new Rect(0, sh*.30f,    sw, 68), "GAME OVER",                               ls);
        GUI.Label(new Rect(0, sh*.30f+72, sw, 34), "Score: " + score + " pts",               sSub);
        GUI.Label(new Rect(0, sh-58,      sw, 44), "Press SPACE to try again",               sPrompt);
    }

    void Box(float x, float y, float w, float h, Color c)
    { GUI.color=c; GUI.DrawTexture(new Rect(x,y,w,h),Texture2D.whiteTexture); GUI.color=Color.white; }

    void HudBox(float x, float y, float w, float h, string t)
    { Box(x,y,w,h,new Color(0.05f,0.05f,0.25f,0.88f)); GUI.Label(new Rect(x,y,w,h),t,sHud); }

    void Div(float sw, float y)
    { GUI.color=new Color(1,1,1,0.18f); GUI.DrawTexture(new Rect(sw*.06f,y,sw*.88f,1),Texture2D.whiteTexture); GUI.color=Color.white; }
}
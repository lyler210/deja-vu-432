using UnityEngine;

/// <summary>
/// Minimal HUD using Unity's built-in IMGUI (OnGUI). No Canvas setup required —
/// just attach this to any GameObject in the scene and it draws a small overlay
/// in the corner showing controls + state.
///
/// Also handles the red-edge "you died" vignette flash. Subscribes to
/// PlayerHealth.OnPlayerDeath and fades in/out over a short time.
/// </summary>
public class HUD : MonoBehaviour
{
    [Tooltip("Show debug overlay (controls, checkpoint status, death count).")]
    public bool showOverlay = true;

    [Tooltip("Seconds the red death-vignette is visible.")]
    public float vignetteDuration = 1.5f;

    [Tooltip("Max alpha of the vignette frame (0–1).")]
    public float vignetteMaxAlpha = 0.85f;

    private GUIStyle labelStyle;
    private float vignetteTimer = 0f;
    private static Texture2D solidWhite;

    void OnEnable()
    {
        PlayerHealth.OnPlayerDeath += FlashVignette;
    }

    void OnDisable()
    {
        PlayerHealth.OnPlayerDeath -= FlashVignette;
    }

    void Update()
    {
        if (vignetteTimer > 0f) vignetteTimer -= Time.deltaTime;
    }

    void FlashVignette()
    {
        vignetteTimer = vignetteDuration;
    }

    void OnGUI()
    {
        // --- death vignette (drawn first so HUD labels sit on top) ---
        if (vignetteTimer > 0f) DrawDeathVignette();

        if (!showOverlay) return;

        if (labelStyle == null)
        {
            labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.fontSize = 16;
            labelStyle.normal.textColor = Color.white;
        }

        // Background panel for readability.
        GUI.Box(new Rect(10, 10, 290, 110), "");

        int deaths = GameManager.DeathCount;
        bool hasCheckpoint = RewindManager.Instance != null && RewindManager.Instance.HasCheckpoint;
        bool rewinding = RewindManager.Instance != null && RewindManager.Instance.IsRewinding;

        GUI.Label(new Rect(20, 14,  280, 20), "Move: ←/→ or A/D    Jump: Space", labelStyle);
        GUI.Label(new Rect(20, 34,  280, 20), "Save (S)  ·  Rewind (R, even after dying)", labelStyle);
        GUI.Label(new Rect(20, 58,  240, 20), $"Deaths: {deaths}",          labelStyle);
        GUI.Label(new Rect(20, 78,  240, 20),
            hasCheckpoint ? "Checkpoint: SAVED  (R to rewind)" : "Checkpoint: none",
            labelStyle);
        if (rewinding)
            GUI.Label(new Rect(20, 98, 240, 20), "Rewinding...", labelStyle);
    }

    void DrawDeathVignette()
    {
        float t = Mathf.Clamp01(vignetteTimer / vignetteDuration);
        // Ease the alpha: brightest right after death, fades out.
        float alpha = Mathf.SmoothStep(0f, vignetteMaxAlpha, t);

        if (solidWhite == null)
        {
            solidWhite = new Texture2D(1, 1);
            solidWhite.SetPixel(0, 0, Color.white);
            solidWhite.Apply();
        }

        // 4 red bars around the edge of the screen, forming a frame.
        int border = Mathf.RoundToInt(Mathf.Min(Screen.width, Screen.height) * 0.16f);
        Color red = new Color(1f, 0.05f, 0.05f, alpha);
        Color prev = GUI.color;
        GUI.color = red;

        // Top
        GUI.DrawTexture(new Rect(0, 0, Screen.width, border), solidWhite);
        // Bottom
        GUI.DrawTexture(new Rect(0, Screen.height - border, Screen.width, border), solidWhite);
        // Left
        GUI.DrawTexture(new Rect(0, 0, border, Screen.height), solidWhite);
        // Right
        GUI.DrawTexture(new Rect(Screen.width - border, 0, border, Screen.height), solidWhite);

        GUI.color = prev;
    }
}

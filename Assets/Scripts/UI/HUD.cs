using UnityEngine;

/// <summary>
/// Minimal HUD using Unity's built-in IMGUI (OnGUI). No Canvas setup required —
/// just attach this to any GameObject in the scene and it draws a small overlay
/// in the corner showing controls + state.
///
/// Replace with a proper Canvas/TextMeshPro UI when you're ready to polish.
/// </summary>
public class HUD : MonoBehaviour
{
    [Tooltip("Show debug overlay (controls, checkpoint status, death count).")]
    public bool showOverlay = true;

    private GUIStyle labelStyle;

    void OnGUI()
    {
        if (!showOverlay) return;

        if (labelStyle == null)
        {
            labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.fontSize = 16;
            labelStyle.normal.textColor = Color.white;
        }

        // Background panel for readability.
        GUI.Box(new Rect(10, 10, 260, 110), "");

        int deaths = GameManager.Instance != null ? GameManager.Instance.deathCount : 0;
        bool hasCheckpoint = RewindManager.Instance != null && RewindManager.Instance.HasCheckpoint;
        bool rewinding = RewindManager.Instance != null && RewindManager.Instance.IsRewinding;

        GUI.Label(new Rect(20, 14,  240, 20), "Move: A/D    Jump: Space",   labelStyle);
        GUI.Label(new Rect(20, 34,  240, 20), "Save (S)    Rewind (R)",     labelStyle);
        GUI.Label(new Rect(20, 58,  240, 20), $"Deaths: {deaths}",          labelStyle);
        GUI.Label(new Rect(20, 78,  240, 20),
            hasCheckpoint ? "Checkpoint: SAVED  (R to rewind)" : "Checkpoint: none",
            labelStyle);
        if (rewinding)
            GUI.Label(new Rect(20, 98, 240, 20), "Rewinding...", labelStyle);
    }
}

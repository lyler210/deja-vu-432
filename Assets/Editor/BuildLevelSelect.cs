using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Editor script — creates Assets/Scenes/LevelSelect.unity with a title and
/// 5 buttons (Level 1 unlocked, 2-5 locked). Adds the scene to Build Settings
/// as scene 0 so the game starts there.
///
/// Run from menu: Tools → Build Level Select.
/// </summary>
public static class BuildLevelSelect
{
    const string ScenePath = "Assets/Scenes/LevelSelect.unity";

    static readonly Color BgColor       = new Color(0.10f, 0.12f, 0.22f);
    static readonly Color UnlockedColor = new Color(0.30f, 0.78f, 0.32f);
    static readonly Color LockedColor   = new Color(0.22f, 0.22f, 0.26f);

    [MenuItem("Tools/Build Level Select")]
    public static void Build()
    {
        // Fresh scene.
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // Remove default directional light (UI scene doesn't need it).
        var dirLight = GameObject.Find("Directional Light");
        if (dirLight != null) Object.DestroyImmediate(dirLight);

        // Tint camera background.
        var cam = Camera.main;
        if (cam != null) cam.backgroundColor = BgColor;

        // EventSystem for UI input.
        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<StandaloneInputModule>();

        // Canvas.
        var canvasGO = new GameObject("Canvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        // Title.
        AddText(canvasGO, "Title", "DEJA VU 432",
                fontSize: 110, bold: true, color: Color.white,
                anchor: new Vector2(0.5f, 0.85f), size: new Vector2(1400, 200));

        // Subtitle.
        AddText(canvasGO, "Subtitle", "Select a level",
                fontSize: 40, bold: false, color: new Color(0.8f, 0.8f, 0.85f),
                anchor: new Vector2(0.5f, 0.74f), size: new Vector2(800, 60));

        // 5 level buttons in a horizontal row.
        for (int i = 0; i < 5; i++)
            CreateLevelButton(i + 1, unlocked: (i == 0), parent: canvasGO.transform);

        // Footer credit.
        AddText(canvasGO, "Credits", "Kieran Moynihan & Lyle Racho",
                fontSize: 28, bold: false, color: new Color(0.65f, 0.65f, 0.7f),
                anchor: new Vector2(0.5f, 0.08f), size: new Vector2(800, 50));

        // Save scene.
        EditorSceneManager.SaveScene(scene, ScenePath);

        // Make LevelSelect scene 0, Level1 scene 1, in Build Settings.
        EditorBuildSettings.scenes = new EditorBuildSettingsScene[]
        {
            new EditorBuildSettingsScene(ScenePath, true),
            new EditorBuildSettingsScene("Assets/Scenes/Level1.unity", true),
        };

        Debug.Log("[BuildLevelSelect] Done. Scene saved and added to Build Settings as scene 0.");
    }

    // -------------------------------------------------------------------

    static GameObject AddText(GameObject parent, string name, string content,
                              int fontSize, bool bold, Color color,
                              Vector2 anchor, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);

        var txt = go.AddComponent<Text>();
        txt.text = content;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = fontSize;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = color;
        txt.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = Vector2.zero;
        return go;
    }

    static void CreateLevelButton(int level, bool unlocked, Transform parent)
    {
        var btnGO = new GameObject($"Level{level}Button");
        btnGO.transform.SetParent(parent, false);

        var rt = btnGO.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(220, 220);

        // Center the 5 buttons in a horizontal row.
        const float spacing = 260f;
        rt.anchoredPosition = new Vector2((level - 3) * spacing, -50f);

        var img = btnGO.AddComponent<Image>();
        img.color = unlocked ? UnlockedColor : LockedColor;

        var btn = btnGO.AddComponent<Button>();
        btn.interactable = unlocked;
        var colors = btn.colors;
        colors.normalColor = unlocked ? UnlockedColor : LockedColor;
        colors.highlightedColor = unlocked
            ? new Color(0.45f, 0.92f, 0.45f)
            : LockedColor;
        colors.pressedColor = unlocked
            ? new Color(0.20f, 0.60f, 0.20f)
            : LockedColor;
        colors.disabledColor = LockedColor;
        btn.colors = colors;

        // Label.
        var label = new GameObject("Label");
        label.transform.SetParent(btnGO.transform, false);
        var t = label.AddComponent<Text>();
        t.text = unlocked ? level.ToString() : "LOCKED";
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = unlocked ? 110 : 40;
        t.fontStyle = FontStyle.Bold;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = unlocked ? Color.white : new Color(0.55f, 0.55f, 0.6f);

        var lrt = label.GetComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = Vector2.zero;
        lrt.offsetMax = Vector2.zero;

        // Hook our LevelSelectButton script that calls SceneManager.LoadScene.
        if (unlocked)
        {
            var lsb = btnGO.AddComponent<LevelSelectButton>();
            lsb.sceneName = $"Level{level}";
        }
    }
}

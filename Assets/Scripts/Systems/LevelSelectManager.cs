using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Drives the LevelSelect scene's button row. Looks up each "Level{N}Button"
/// GameObject by name and configures it based on PlayerPrefs["levelsUnlocked"]:
/// unlocked buttons load the matching scene; locked buttons show "LOCKED" and
/// are non-interactive.
///
/// Add a Button GameObject named "Level{N}Button" (with a Text child for the
/// label) to wire up a new level — no Inspector changes needed.
/// </summary>
public class LevelSelectManager : MonoBehaviour
{
    [Header("Optional back button")]
    [Tooltip("Drag the back-to-main-menu button here. Optional.")]
    public Button backButton;

    [Header("Colors")]
    public Color lockedColor   = new Color(0.22f, 0.22f, 0.26f);
    public Color unlockedColor = new Color(0.30f, 0.78f, 0.32f);

    const int TotalLevels = 5;

    void Start()
    {
        // Level 1 is always unlocked. PlayerPref tracks how far the player has reached.
        int levelsUnlocked = PlayerPrefs.GetInt("levelsUnlocked", 1);

        for (int i = 1; i <= TotalLevels; i++)
        {
            var go = GameObject.Find($"Level{i}Button");
            if (go == null)
            {
                Debug.LogWarning($"[LevelSelectManager] Level{i}Button not found in scene.");
                continue;
            }
            var btn = go.GetComponent<Button>();
            if (btn == null) continue;

            // Find the label — supports either UI Text or TMP_Text.
            var label = go.GetComponentInChildren<Text>(true);

            SetupButton(btn, label, levelsUnlocked >= i, i.ToString(), $"Level{i}");
        }

        // Auto-wire a back button if not assigned in Inspector — look for a
        // GameObject named "BackButton" (case-sensitive) under any Canvas.
        if (backButton == null)
        {
            var go = GameObject.Find("BackButton");
            if (go != null) backButton = go.GetComponent<Button>();
        }
        if (backButton != null)
            backButton.onClick.AddListener(() => SceneManager.LoadScene("MainMenu"));
    }

    void Update()
    {
        // Escape always backs out to the main menu, with or without a UI button.
        if (Input.GetKeyDown(KeyCode.Escape))
            SceneManager.LoadScene("MainMenu");
    }

    void SetupButton(Button btn, Text label, bool unlocked, string levelNumber, string sceneName)
    {
        // Clear any previously-registered listeners so re-entering the scene
        // doesn't stack duplicate handlers.
        btn.onClick.RemoveAllListeners();

        var colors = btn.colors;
        if (unlocked)
        {
            btn.interactable        = true;
            colors.normalColor      = unlockedColor;
            colors.highlightedColor = new Color(0.45f, 0.92f, 0.45f);
            colors.pressedColor     = new Color(0.20f, 0.60f, 0.20f);
            colors.disabledColor    = lockedColor;
            btn.colors              = colors;
            if (label != null)
            {
                label.text     = levelNumber;
                label.color    = Color.white;
                label.fontSize = 110;
            }
            btn.onClick.AddListener(() => SceneManager.LoadScene(sceneName));
        }
        else
        {
            btn.interactable        = false;
            colors.normalColor      = lockedColor;
            colors.highlightedColor = lockedColor;
            colors.pressedColor     = lockedColor;
            colors.disabledColor    = lockedColor;
            btn.colors              = colors;
            if (label != null)
            {
                label.text     = "LOCKED";
                label.color    = new Color(0.55f, 0.55f, 0.6f);
                label.fontSize = 40;
            }
        }

        // Also recolor the button's Image graphic so the visual reflects state
        // even when the button isn't being hovered.
        var img = btn.GetComponent<Image>();
        if (img != null) img.color = unlocked ? unlockedColor : lockedColor;
    }
}

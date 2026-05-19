using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Attached to a Button GameObject on the Level Select scene.
/// On click, loads the named scene.
/// </summary>
[RequireComponent(typeof(Button))]
public class LevelSelectButton : MonoBehaviour
{
    [Tooltip("Scene name to load when this button is clicked (must be in Build Settings).")]
    public string sceneName;

    void Start()
    {
        var btn = GetComponent<Button>();
        if (btn != null && !string.IsNullOrEmpty(sceneName))
            btn.onClick.AddListener(LoadScene);
    }

    public void LoadScene()
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning($"[LevelSelectButton] No scene name set on {name}.");
            return;
        }
        SceneManager.LoadScene(sceneName);
    }
}

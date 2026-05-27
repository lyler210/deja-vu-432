using UnityEngine;
using UnityEngine.UI;

public class LevelSelectManager : MonoBehaviour
{
    public Button level2Button;
    public Button level3Button;
    public Button backButton;

    public Text level2Text;
    public Text level3Text;

    public Color lockedColor = Color.black;
    public Color unlockedColor = Color.green;

    void Start()
    {
        int levelsUnlocked = PlayerPrefs.GetInt("levelsUnlocked", 1);

        SetupButton(level2Button, level2Text, levelsUnlocked >= 2, "2", "Level2");
        SetupButton(level3Button, level3Text, levelsUnlocked >= 3, "3", "Level3");

        backButton.onClick.AddListener(() => UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu"));
    }

    void SetupButton(Button btn, Text btnText, bool unlocked, string levelNumber, string sceneName)
    {
        ColorBlock colors = btn.colors;

        if (unlocked)
        {
            btn.interactable = true;
            colors.normalColor = unlockedColor;
            colors.highlightedColor = unlockedColor;
            colors.disabledColor = lockedColor;
            btn.colors = colors;
            btnText.text = levelNumber;
            btn.onClick.AddListener(() => UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName));
        }
        else
        {
            btn.interactable = false;
            colors.normalColor = lockedColor;
            colors.disabledColor = lockedColor;
            btn.colors = colors;
            btnText.text = "LOCKED";
        }
    }
}
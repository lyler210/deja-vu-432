using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Deja Vu 432's Main Menu screen.
/// Directs the 'Start Game' and 'Settings' buttons to their scenes.
/// 
/// Also resets the death counter so that each play through starts fresh.
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    public Button startGameButton;
    public Button settingsButton;

    void Start()
    {
        startGameButton.onClick.AddListener(StartGame);
        settingsButton.onClick.AddListener(OpenSettings);
    }

    void StartGame()
    {
        // Reset the run-wide death counter so each "Start" launches a fresh run.
        GameManager.ResetStats();
        SceneManager.LoadScene("LevelSelect");
    }

    void OpenSettings()
    {
        SceneManager.LoadScene("Settings");
    }
}
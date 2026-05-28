using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

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
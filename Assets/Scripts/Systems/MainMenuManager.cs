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
        SceneManager.LoadScene("LevelSelect");
    }

    void OpenSettings()
    {
        SceneManager.LoadScene("Settings");
    }
}
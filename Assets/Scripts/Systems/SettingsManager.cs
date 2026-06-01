using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// User configuration
/// For this game, we decided to give the user the choice to
/// change their "save" or "checkpoint" button and the "rewind" button.
/// 
/// Default keybinds:
/// Save Button: S
/// Rewind Button: R
/// 
/// </summary>
public class SettingsManager : MonoBehaviour
{
    public Button saveButton;
    public Button rewindButton;
    public Button backButton;

    private Text saveButtonText;
    private Text rewindButtonText;

    // Defaults
    private KeyCode save = KeyCode.S;
    private KeyCode rewind = KeyCode.R;

    // When player wants to change keybind
    private bool isListeningForSave = false;
    private bool isListeningForRewind = false;

    void Start()
    {
        // Setting defaults
        LoadKeys();

        saveButtonText = saveButton.GetComponentInChildren<Text>();
        rewindButtonText = rewindButton.GetComponentInChildren<Text>();

        saveButtonText.text = save.ToString();
        rewindButtonText.text = rewind.ToString();

        saveButton.onClick.AddListener(() => StartListening("save"));
        rewindButton.onClick.AddListener(() => StartListening("rewind"));
        backButton.onClick.AddListener(GoBack);
    }

    // When player clicks on button to change keybind
    void StartListening(string action)
    {
        if (action == "save")
        {
            isListeningForSave = true;
            isListeningForRewind = false;
            saveButtonText.text = "Press any key...";
        }
        else if (action == "rewind")
        {
            isListeningForRewind = true;
            isListeningForSave = false;
            rewindButtonText.text = "Press any key...";
        }
    }

    // Checking if keybinds needs to be changed
    void Update()
    {
        if (!isListeningForSave && !isListeningForRewind) return;

        foreach (KeyCode key in System.Enum.GetValues(typeof(KeyCode)))
        {
            if (Input.GetKeyDown(key))
            {
                if (isListeningForSave)
                {
                    save = key;
                    isListeningForSave = false;
                    saveButtonText.text = save.ToString();
                }
                else if (isListeningForRewind)
                {
                    rewind = key;
                    isListeningForRewind = false;
                    rewindButtonText.text = rewind.ToString();
                }
                SaveKeys();
                break;
            }
        }
    }

    // Saving user keybinds
    void SaveKeys()
    {
        PlayerPrefs.SetString("save", save.ToString());
        PlayerPrefs.SetString("rewind", rewind.ToString());
        PlayerPrefs.Save();
    }

    // Settings default keybinds
    void LoadKeys()
    {
        save = (KeyCode)System.Enum.Parse(typeof(KeyCode), PlayerPrefs.GetString("save", KeyCode.S.ToString()));
        rewind = (KeyCode)System.Enum.Parse(typeof(KeyCode), PlayerPrefs.GetString("rewind", KeyCode.R.ToString()));
    }

    // Back to main menu
    void GoBack()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }
}
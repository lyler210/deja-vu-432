using UnityEngine;
using UnityEngine.UI;

public class SettingsManager : MonoBehaviour
{
    public Button saveButton;
    public Button rewindButton;
    public Button backButton;

    private Text saveButtonText;
    private Text rewindButtonText;

    private KeyCode save = KeyCode.S;
    private KeyCode rewind = KeyCode.R;

    private bool isListeningForSave = false;
    private bool isListeningForRewind = false;

    void Start()
    {

        LoadKeys();

        saveButtonText = saveButton.GetComponentInChildren<Text>();
        rewindButtonText = rewindButton.GetComponentInChildren<Text>();

        UpdateButtonText();

        saveButton.onClick.AddListener(() => StartListening("save"));
        rewindButton.onClick.AddListener(() => StartListening("rewind"));
        backButton.onClick.AddListener(GoBack);
    }

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
                }
                else if (isListeningForRewind)
                {
                    rewind = key;
                    isListeningForRewind = false;
                }
                SaveKeys();
                UpdateButtonText();
                break;
            }
        }
    }

    void UpdateButtonText()
    {
        saveButtonText.text = save.ToString();
        rewindButtonText.text = rewind.ToString();
    }

    void SaveKeys()
    {
        PlayerPrefs.SetString("save", save.ToString());
        PlayerPrefs.SetString("rewind", rewind.ToString());
        PlayerPrefs.Save();
    }

    void LoadKeys()
    {
        save = (KeyCode)System.Enum.Parse(typeof(KeyCode), PlayerPrefs.GetString("save", KeyCode.S.ToString()));
        rewind = (KeyCode)System.Enum.Parse(typeof(KeyCode), PlayerPrefs.GetString("rewind", KeyCode.R.ToString()));
    }

    void GoBack()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }
}
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Persistent across all scenes. Holds counters and global game state that
/// shouldn't reset between levels (death count, level reached, etc.).
///
/// Place a GameManager GameObject in your FIRST scene only — it will survive
/// scene loads via DontDestroyOnLoad.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Run Stats (read-only at runtime)")]
    public int deathCount = 0;
    public int checkpointsSaved = 0;
    public int rewindsUsed = 0;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        PlayerHealth.OnPlayerDeath += HandleDeath;
    }

    void OnDestroy()
    {
        if (Instance == this) PlayerHealth.OnPlayerDeath -= HandleDeath;
    }

    private void HandleDeath() => deathCount++;

    public void LoadLevel(string sceneName) => SceneManager.LoadScene(sceneName);

    public void RestartCurrentLevel() => SceneManager.LoadScene(SceneManager.GetActiveScene().name);
}

using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Run-scoped game state (death count, checkpoints saved, rewinds used).
///
/// This manager is SCENE-SCOPED — it gets destroyed when the scene unloads.
/// However the stats themselves are STATIC, so they persist across scene
/// loads (Level1 → Level2 → ...) without needing DontDestroyOnLoad. They
/// reset to zero when <see cref="ResetStats"/> is called (e.g. from
/// MainMenuManager when starting a new run) or when the application restarts.
///
/// Why not DontDestroyOnLoad?  GameManager used to sit on the same "Managers"
/// GameObject as RewindManager, LevelManager, and HUD. Calling
/// DontDestroyOnLoad on its gameObject persisted ALL four components across
/// scene loads. The new scene's managers then got destroyed by the singleton
/// check, leaving the original ones registered to destroyed Player /
/// SpawnPoint Transforms. Result: broken respawn after revisiting a level,
/// saves that couldn't be placed, and double-counted deaths. Going static
/// for the data + scene-scoped for the MonoBehaviour avoids the whole mess.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    /// <summary>Total deaths in the current run. Persists across scene loads via static.</summary>
    public static int DeathCount { get; private set; }

    /// <summary>How many times the player pressed S this run. Reserved for future analytics.</summary>
    public static int CheckpointsSaved { get; private set; }

    /// <summary>How many times the player pressed R this run. Reserved for future analytics.</summary>
    public static int RewindsUsed { get; private set; }

    [Header("Run Stats (read-only mirror; real value is static)")]
    [Tooltip("Visible only so you can spot-check the death count in the Inspector while playing.")]
    [SerializeField] private int deathCountDebug;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // NO DontDestroyOnLoad — this manager rebuilds with each scene. See class header.
    }

    void OnEnable()
    {
        PlayerHealth.OnPlayerDeath += HandleDeath;
    }

    void OnDisable()
    {
        PlayerHealth.OnPlayerDeath -= HandleDeath;
    }

    void Update()
    {
        deathCountDebug = DeathCount;
    }

    private void HandleDeath() => DeathCount++;

    /// <summary>Reset run-wide stats. Call from MainMenu's Play button to start a fresh run.</summary>
    public static void ResetStats()
    {
        DeathCount = 0;
        CheckpointsSaved = 0;
        RewindsUsed = 0;
    }

    public void LoadLevel(string sceneName) => SceneManager.LoadScene(sceneName);

    public void RestartCurrentLevel() => SceneManager.LoadScene(SceneManager.GetActiveScene().name);
}

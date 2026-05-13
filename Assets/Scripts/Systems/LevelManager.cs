using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Per-scene manager. Responsible for:
///   - Knowing where the level starts (spawnPoint).
///   - Sending the player back to the start on death.
///   - Telling the RewindManager who the player is.
///
/// Drop one onto a "Managers" GameObject in each level scene.
/// </summary>
public class LevelManager : MonoBehaviour
{
    [Tooltip("Drag the Player GameObject here.")]
    public Transform player;

    [Tooltip("Empty GameObject placed where the player should spawn at level start / after death.")]
    public Transform spawnPoint;

    [Tooltip("Scene name to load when player presses R to fully restart. Leave blank for current scene.")]
    public string restartSceneName = "";

    [Tooltip("Optional small delay before respawn after death (lets a death animation play).")]
    public float respawnDelay = 0.4f;

    void OnEnable()
    {
        PlayerHealth.OnPlayerDeath += HandleDeath;
    }

    void OnDisable()
    {
        PlayerHealth.OnPlayerDeath -= HandleDeath;
    }

    void Start()
    {
        if (player == null)
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go != null) player = go.transform;
        }

        if (player != null && spawnPoint != null)
            player.position = spawnPoint.position;

        if (RewindManager.Instance != null && player != null)
            RewindManager.Instance.RegisterPlayer(player);
    }

    private void HandleDeath()
    {
        Invoke(nameof(RespawnAtStart), respawnDelay);
    }

    private void RespawnAtStart()
    {
        if (player == null || spawnPoint == null) return;

        var rb = player.GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;
        player.position = spawnPoint.position;

        // Make sure rewind history is fresh after respawn.
        if (RewindManager.Instance != null) RewindManager.Instance.ClearAll();
    }

    /// <summary>Hard-reset the whole scene (e.g., from a pause menu).</summary>
    public void ReloadScene()
    {
        string name = string.IsNullOrEmpty(restartSceneName)
            ? SceneManager.GetActiveScene().name
            : restartSceneName;
        SceneManager.LoadScene(name);
    }
}

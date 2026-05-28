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

    [Tooltip("Delay before respawn after death — gives the death animation time to play.")]
    public float respawnDelay = 1.4f;

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
        if (rb != null)
        {
            // Safety: a rewind could in theory leave the body Kinematic if it
            // got interrupted abnormally. Force it back to Dynamic on every
            // respawn so the player always returns to a clean, gravity-affected
            // state. (The InDeathSequence guard in RewindManager should prevent
            // the interruption in the first place, but cheap insurance.)
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.linearVelocity = Vector2.zero;
        }
        player.position = spawnPoint.position;

        // Re-show the sprite and unlock controls (PlayerHealth hid them on Die).
        var health = player.GetComponent<PlayerHealth>();
        if (health != null) health.ResetVisuals();

        // Snap the camera straight to the spawn position — no smooth-damp lerp.
        // Otherwise the camera would slide back from wherever the corpse was
        // when the death animation ended, which feels like a second "drop".
        if (Camera.main != null)
        {
            var follow = Camera.main.GetComponent<CameraFollow>();
            if (follow != null) follow.SnapToTarget();
        }

        // NOTE: We deliberately DO NOT clear the saved rewind point here. Saves
        // survive death — the player respawns at start but can still press R to
        // teleport back to wherever they last pressed S. RewindManager.HandleDeath
        // has already cleared the position history so the rewind animation starts
        // clean from this fresh respawn position.
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

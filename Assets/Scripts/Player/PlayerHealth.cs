using UnityEngine;

/// <summary>
/// Tracks player "alive" state. Other systems (traps, kill zones) call Die()
/// to kill the player. Subscribers (LevelManager, HUD, RewindManager) listen
/// to OnPlayerDeath to react.
///
/// Attach to the Player GameObject.
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    /// <summary>Fired the instant the player dies. Subscribers should reset state.</summary>
    public static event System.Action OnPlayerDeath;

    [Tooltip("Brief invulnerability window after death so multiple traps don't fire repeatedly.")]
    public float deathCooldown = 0.5f;

    [Tooltip("Prefab spawned at death position (particle effect). Optional.")]
    public GameObject deathEffectPrefab;

    private bool isDead = false;
    private float deathTimer = 0f;

    // True from the moment Die() fires until LevelManager calls ResetVisuals().
    // Used by RewindManager to know it shouldn't sample, save, or rewind while
    // the player is mid-death-animation (otherwise the "death corpse" position
    // gets baked into the rewind history and the rewind animation re-visits
    // the killing hazard).
    private bool inDeathSequence = false;
    public bool InDeathSequence => inDeathSequence;

    void Update()
    {
        if (isDead)
        {
            deathTimer -= Time.deltaTime;
            if (deathTimer <= 0f) isDead = false;
        }
    }

    /// <summary>Call from traps / kill zones to kill the player.</summary>
    public void Die()
    {
        if (isDead) return;

        // Don't process death during a rewind. The rewind teleports the player
        // along their recent trail, which may pass through the same hazard that
        // killed them. Letting Die() fire mid-rewind leaves the Rigidbody2D in
        // Kinematic and produces a "walks but can't jump" state once the rewind
        // coroutine cleans up. The rewind itself fully controls player position
        // during playback — hazards have no business killing you mid-rewind.
        if (RewindManager.Instance != null && RewindManager.Instance.IsRewinding) return;

        isDead = true;
        inDeathSequence = true;
        deathTimer = deathCooldown;

        // Hide the frog sprite while dead — gives a clear visual cue.
        var sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null) sr.enabled = false;

        // Lock movement so the player can't keep walking after dying.
        var ctrl = GetComponent<PlayerController>();
        if (ctrl != null) ctrl.inputLocked = true;

        // Freeze the corpse in place for the death animation. Without this, the
        // invisible corpse keeps falling under gravity for the full 1.4s respawn
        // delay (~29 units at gravity -30!) — the camera chases it down then has
        // to snap back to the spawn point, producing a visible "re-drop". Going
        // Kinematic stops it cleanly. LevelManager.RespawnAtStart restores
        // Dynamic before re-showing the player.
        var rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        // Spawn the death-particle effect at the current player position.
        if (deathEffectPrefab != null)
            Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);

        Debug.Log("Player died.");
        OnPlayerDeath?.Invoke();
    }

    /// <summary>Called by LevelManager when the player respawns — re-show sprite + unlock input.</summary>
    public void ResetVisuals()
    {
        inDeathSequence = false;
        var sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null) sr.enabled = true;
        var ctrl = GetComponent<PlayerController>();
        if (ctrl != null) ctrl.inputLocked = false;
    }

    public bool IsDead => isDead;
}

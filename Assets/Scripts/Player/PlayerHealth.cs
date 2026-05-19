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
        isDead = true;
        deathTimer = deathCooldown;

        // Hide the frog sprite while dead — gives a clear visual cue.
        var sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null) sr.enabled = false;

        // Lock movement so the player can't keep walking after dying.
        var ctrl = GetComponent<PlayerController>();
        if (ctrl != null) ctrl.inputLocked = true;

        // Stop any residual velocity so we don't keep flying when the sprite is hidden.
        var rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;

        // Spawn the death-particle effect at the current player position.
        if (deathEffectPrefab != null)
            Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);

        Debug.Log("Player died.");
        OnPlayerDeath?.Invoke();
    }

    /// <summary>Called by LevelManager when the player respawns — re-show sprite + unlock input.</summary>
    public void ResetVisuals()
    {
        var sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null) sr.enabled = true;
        var ctrl = GetComponent<PlayerController>();
        if (ctrl != null) ctrl.inputLocked = false;
    }

    public bool IsDead => isDead;
}

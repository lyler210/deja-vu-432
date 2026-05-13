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

        Debug.Log("Player died.");
        OnPlayerDeath?.Invoke();
    }

    public bool IsDead => isDead;
}

using UnityEngine;

/// <summary>
/// A more configurable trap. Unlike KillZone (always instant kill), a Trap can
/// be a moving / animated hazard with optional delay before activation. For
/// the MVP this is functionally similar to KillZone but extensible.
///
/// Examples of how to use:
///   - Saw blade: attach to a child of an empty parent that animates left/right.
///   - Falling spike: trigger killOnContact only after a timer.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class Trap : MonoBehaviour
{
    [Tooltip("If true, killing collisions are active. Toggle off for traps that warm up.")]
    public bool active = true;

    [Tooltip("Seconds after Start() before the trap becomes active. 0 = always-on.")]
    public float activationDelay = 0f;

    void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    void Start()
    {
        if (activationDelay > 0f)
        {
            active = false;
            Invoke(nameof(Activate), activationDelay);
        }
    }

    private void Activate() => active = true;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!active) return;
        var health = other.GetComponent<PlayerHealth>();
        if (health != null) health.Die();
    }

    void OnTriggerStay2D(Collider2D other)
    {
        // Belt-and-braces: if the player walks into an already-overlapping trap.
        if (!active) return;
        var health = other.GetComponent<PlayerHealth>();
        if (health != null) health.Die();
    }
}

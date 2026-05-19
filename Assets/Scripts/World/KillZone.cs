using UnityEngine;

/// <summary>
/// Anything tagged as instant-death. Attach to spike GameObjects, saws,
/// pit colliders at the bottom of the level, etc. Requires a Collider2D set
/// to "Is Trigger".
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class KillZone : MonoBehaviour
{
    // Reset is called by Unity when the component is first added — auto-set trigger.
    void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        var health = other.GetComponent<PlayerHealth>();
        if (health != null) health.Die();
    }
}

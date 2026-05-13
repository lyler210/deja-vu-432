using UnityEngine;

/// <summary>
/// STRETCH FEATURE — not required for MVP.
///
/// Optional level-placed checkpoint. When the player walks into it, it auto-saves
/// a rewind point at this location. Useful for guiding new players or for puzzle
/// rooms where the designer wants to constrain where rewinds can drop you.
///
/// If you DON'T want auto-save behavior, ignore this script entirely — the
/// player can always press S to save anywhere via RewindManager.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Checkpoint : MonoBehaviour
{
    [Tooltip("Only fire once per scene-load.")]
    public bool oneShot = true;

    [Tooltip("Optional visual swap when activated (e.g., flag raised sprite).")]
    public SpriteRenderer visual;
    public Sprite inactiveSprite;
    public Sprite activeSprite;

    private bool used = false;

    void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    void Start()
    {
        if (visual != null && inactiveSprite != null) visual.sprite = inactiveSprite;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (used && oneShot) return;
        if (!other.CompareTag("Player")) return;
        if (RewindManager.Instance == null) return;

        RewindManager.Instance.SaveCheckpoint(transform.position);
        used = true;

        if (visual != null && activeSprite != null) visual.sprite = activeSprite;
    }
}

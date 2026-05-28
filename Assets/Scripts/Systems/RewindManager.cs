using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The core "Deja Vu 432" mechanic.
///
/// Two player-facing actions, both pressed on the keyboard:
///   - SAVE  (default: S)   → records the player's CURRENT position as a checkpoint.
///   - REWIND (default: R)  → if a checkpoint exists, plays a brief reverse animation
///                            of the player's recent path, then snaps them to that checkpoint.
///
/// On death (PlayerHealth.OnPlayerDeath): the player respawns at the level start
/// (LevelManager handles that), but the SAVED CHECKPOINT survives. Pressing R after
/// respawning teleports the player back to wherever they last pressed S. Only the
/// position history is cleared on death so the rewind animation doesn't replay
/// pre-death frames. (Save was originally cleared on death, but playtesters found
/// that frustrating — this change came from the May 27 feedback round.)
///
/// The saved checkpoint clears naturally when the scene unloads (level complete,
/// level reload, going to LevelSelect), because the whole Managers GameObject is
/// destroyed and the new scene gets a fresh RewindManager.
///
/// Singleton-per-scene: drop one onto a "Managers" GameObject in each level scene.
/// </summary>
public class RewindManager : MonoBehaviour
{
    public static RewindManager Instance { get; private set; }

    [Header("Input")]
    public KeyCode saveKey = KeyCode.S;
    public KeyCode rewindKey = KeyCode.R;

    [Header("History Sampling")]
    [Tooltip("How often (seconds) we record the player's position for the rewind animation.")]
    public float sampleInterval = 0.05f;

    [Tooltip("Maximum seconds of history retained. Older samples are dropped.")]
    public float maxHistorySeconds = 30f;

    [Header("Rewind Playback")]
    [Tooltip("Speed multiplier when playing back history during a rewind.")]
    public float playbackSpeed = 4f;

    [Tooltip("Tint applied to the player sprite during rewind (optional cue).")]
    public Color rewindTint = new Color(0.6f, 0.8f, 1f, 0.85f);

    [Header("Save Visuals")]
    [Tooltip("Prefab spawned at the save spot. Should have a SaveMarker component. Wired up by 'Tools → Build Level 1'.")]
    public GameObject saveMarkerPrefab;

    // --- runtime state ---
    private Transform player;
    private Rigidbody2D playerRb;
    private PlayerController playerController;
    private PlayerHealth playerHealth;
    private SpriteRenderer playerSprite;

    private Vector3? savedCheckpoint = null;
    private readonly List<Sample> history = new List<Sample>();
    private bool isRewinding = false;
    private GameObject currentMarker;

    void Start()
    {
        saveKey = (KeyCode)System.Enum.Parse(typeof(KeyCode), PlayerPrefs.GetString("save", KeyCode.S.ToString()));
        rewindKey = (KeyCode)System.Enum.Parse(typeof(KeyCode), PlayerPrefs.GetString("rewind", KeyCode.R.ToString()));
    }

    private struct Sample
    {
        public Vector3 position;
        public float time;
    }

    /// <summary>True if the player has saved a checkpoint that hasn't been used or wiped yet.</summary>
    public bool HasCheckpoint => savedCheckpoint.HasValue;

    /// <summary>True while a rewind animation is playing.</summary>
    public bool IsRewinding => isRewinding;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void OnEnable()
    {
        PlayerHealth.OnPlayerDeath += HandleDeath;
    }

    void OnDisable()
    {
        PlayerHealth.OnPlayerDeath -= HandleDeath;
    }

    /// <summary>Called by LevelManager once the player exists in the scene.</summary>
    public void RegisterPlayer(Transform t)
    {
        player = t;
        playerRb = t.GetComponent<Rigidbody2D>();
        playerController = t.GetComponent<PlayerController>();
        playerHealth = t.GetComponent<PlayerHealth>();
        playerSprite = t.GetComponentInChildren<SpriteRenderer>();
        history.Clear();
        savedCheckpoint = null;
    }

    void Update()
    {
        if (player == null || isRewinding) return;

        // Don't do ANYTHING while the player is mid-death (between Die() and
        // LevelManager.RespawnAtStart finishing). If we kept sampling here, the
        // corpse position would get baked into history and the next rewind would
        // animate the player back through the hazard that just killed them —
        // which then re-triggers Die() and leaves the Rigidbody in Kinematic
        // (walks but can't jump). Also blocks save/rewind input during this
        // window so the player can't queue weird actions mid-respawn.
        if (playerHealth != null && playerHealth.InDeathSequence) return;

        // Save current position as a checkpoint.
        if (Input.GetKeyDown(saveKey))
            SaveCheckpoint(player.position);

        // Rewind to the saved checkpoint (only if one exists).
        if (Input.GetKeyDown(rewindKey) && savedCheckpoint.HasValue)
            StartCoroutine(RewindRoutine());

        // Sample position into the history buffer at regular intervals.
        if (history.Count == 0 || Time.time - history[history.Count - 1].time >= sampleInterval)
        {
            history.Add(new Sample { position = player.position, time = Time.time });

            // Drop expired samples from the front.
            float cutoff = Time.time - maxHistorySeconds;
            while (history.Count > 0 && history[0].time < cutoff)
                history.RemoveAt(0);
        }
    }

    public void SaveCheckpoint(Vector3 worldPos)
    {
        savedCheckpoint = worldPos;
        // We intentionally KEEP history so the rewind animation has frames to play.

        // Replace any existing visual marker at the new save spot.
        if (currentMarker != null) Destroy(currentMarker);
        if (saveMarkerPrefab != null)
            currentMarker = Instantiate(saveMarkerPrefab, worldPos, Quaternion.identity);

        Debug.Log($"[Rewind] Checkpoint saved at {worldPos}");
    }

    /// <summary>Wipes both the saved checkpoint and the recorded path. Called on death.</summary>
    public void ClearAll()
    {
        savedCheckpoint = null;
        history.Clear();
        if (currentMarker != null)
        {
            Destroy(currentMarker);
            currentMarker = null;
        }
    }

    private void HandleDeath()
    {
        // SAVE SURVIVES DEATH (Cluster B fix from May 27 feedback).
        // The player respawns at the level start (LevelManager handles that),
        // but the saved checkpoint stays put so pressing R after respawning
        // teleports the player back to it. We clear only the position history
        // here so the rewind animation doesn't try to replay pre-death frames.
        history.Clear();
    }

    private IEnumerator RewindRoutine()
    {
        if (!savedCheckpoint.HasValue) yield break;

        isRewinding = true;
        Vector3 target = savedCheckpoint.Value;

        // Lock input + physics so the rewind animation isn't fought by gravity/movement.
        if (playerController != null) playerController.inputLocked = true;
        RigidbodyType2D originalBodyType = RigidbodyType2D.Dynamic;
        if (playerRb != null)
        {
            originalBodyType = playerRb.bodyType;
            playerRb.linearVelocity = Vector2.zero;
            playerRb.bodyType = RigidbodyType2D.Kinematic;
        }

        // Visual cue: tint the sprite while rewinding.
        Color originalColor = Color.white;
        if (playerSprite != null)
        {
            originalColor = playerSprite.color;
            playerSprite.color = rewindTint;
        }

        // Find the sample in history closest to the saved checkpoint, then play backwards to it.
        int targetIndex = 0;
        float bestDist = float.MaxValue;
        for (int i = 0; i < history.Count; i++)
        {
            float d = Vector3.Distance(history[i].position, target);
            if (d < bestDist)
            {
                bestDist = d;
                targetIndex = i;
            }
        }

        // Step backwards through history from now → target sample.
        float wait = Mathf.Max(sampleInterval / Mathf.Max(playbackSpeed, 0.01f), 0.005f);
        for (int i = history.Count - 1; i >= targetIndex; i--)
        {
            player.position = history[i].position;
            yield return new WaitForSeconds(wait);
        }

        // Snap exactly to the saved checkpoint.
        player.position = target;

        // Restore state.
        if (playerSprite != null) playerSprite.color = originalColor;
        if (playerRb != null)
        {
            playerRb.bodyType = originalBodyType;
            playerRb.linearVelocity = Vector2.zero;
        }
        if (playerController != null) playerController.inputLocked = false;

        // Trim history so the next rewind doesn't replay stale frames, but keep the checkpoint
        // so the player can rewind to it again.
        history.Clear();
        isRewinding = false;
    }
}

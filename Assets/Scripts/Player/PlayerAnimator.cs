using UnityEngine;

/// <summary>
/// Drives the player SpriteRenderer through two short sprite animations —
/// "idle" (standing still) and "run" (any horizontal velocity). Plays back
/// the correct frame-cycle based on Rigidbody2D.linearVelocity.x.
///
/// The Editor script 'Tools → Build Level 1' is responsible for slicing the
/// Pixel Adventure Idle/Run PNGs and populating the two Sprite[] arrays.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerAnimator : MonoBehaviour
{
    [Tooltip("Sprite frames for the idle animation (standing).")]
    public Sprite[] idleFrames;

    [Tooltip("Sprite frames for the run animation (any horizontal movement).")]
    public Sprite[] runFrames;

    [Tooltip("Frames per second for the idle animation.")]
    public float idleFps = 14f;

    [Tooltip("Frames per second for the run animation.")]
    public float runFps = 18f;

    [Tooltip("Horizontal speed above this threshold counts as 'running'.")]
    public float runThreshold = 0.2f;

    private SpriteRenderer sr;
    private Rigidbody2D rb;
    private bool wasRunning;
    private int frameIndex;
    private float frameTimer;

    void Awake()
    {
        sr = GetComponentInChildren<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        if (sr == null || rb == null) return;

        bool isRunning = Mathf.Abs(rb.linearVelocity.x) > runThreshold;

        // Reset the cycle when switching animations so we always start at frame 0.
        if (isRunning != wasRunning)
        {
            frameIndex = 0;
            frameTimer = 0f;
            wasRunning = isRunning;
        }

        Sprite[] frames = isRunning ? runFrames : idleFrames;
        float fps        = isRunning ? runFps   : idleFps;

        if (frames == null || frames.Length == 0 || fps <= 0f) return;

        frameTimer += Time.deltaTime;
        float frameDuration = 1f / fps;
        if (frameTimer >= frameDuration)
        {
            frameTimer -= frameDuration;
            frameIndex = (frameIndex + 1) % frames.Length;
            sr.sprite = frames[frameIndex];
        }
    }
}

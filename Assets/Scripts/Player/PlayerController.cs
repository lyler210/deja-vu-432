using UnityEngine;

/// <summary>
/// Basic 2D platformer movement. Reads horizontal input (A/D or arrow keys)
/// and Space/W for jump. Requires a Rigidbody2D and a ground-check Transform.
/// Attach to the Player GameObject.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [Tooltip("Horizontal movement speed in units/sec.")]
    public float moveSpeed = 7f;

    [Tooltip("Initial vertical velocity applied on jump.")]
    public float jumpForce = 14f;

    [Tooltip("Extra gravity applied while falling for a snappier feel.")]
    public float fallGravityMultiplier = 2f;

    [Tooltip("Extra gravity applied when jump is released early (variable jump height).")]
    public float lowJumpGravityMultiplier = 1.5f;

    [Header("Ground Detection")]
    [Tooltip("Empty child Transform placed at the player's feet.")]
    public Transform groundCheck;

    [Tooltip("Radius of the ground-check overlap circle.")]
    public float groundCheckRadius = 0.15f;

    [Tooltip("Which layers count as ground (set to the 'Ground' layer in Inspector).")]
    public LayerMask groundLayer;

    [Header("Coyote Time & Jump Buffer")]
    [Tooltip("How long after leaving a ledge the player can still jump.")]
    public float coyoteTime = 0.1f;

    [Tooltip("How early before landing a jump press is remembered.")]
    public float jumpBufferTime = 0.1f;

    // Internal state
    private Rigidbody2D rb;
    private SpriteRenderer sprite;
    private float horizontalInput;
    private bool isGrounded;
    private float coyoteTimer;
    private float jumpBufferTimer;
    private bool jumpHeld;

    // Set by RewindManager during a rewind so we don't fight the kinematic teleport.
    [HideInInspector] public bool inputLocked = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sprite = GetComponentInChildren<SpriteRenderer>();
        rb.freezeRotation = true; // never let the player tip over
    }

    void Update()
    {
        if (inputLocked)
        {
            horizontalInput = 0f;
            return;
        }

        // --- Input ---
        horizontalInput = Input.GetAxisRaw("Horizontal");
        jumpHeld = Input.GetButton("Jump");

        if (Input.GetButtonDown("Jump"))
            jumpBufferTimer = jumpBufferTime;
        else
            jumpBufferTimer -= Time.deltaTime;

        // --- Ground check ---
        if (groundCheck != null)
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        if (isGrounded) coyoteTimer = coyoteTime;
        else coyoteTimer -= Time.deltaTime;

        // --- Jump ---
        if (jumpBufferTimer > 0f && coyoteTimer > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            jumpBufferTimer = 0f;
            coyoteTimer = 0f;
        }

        // --- Sprite flip ---
        if (sprite != null)
        {
            if (horizontalInput > 0.01f) sprite.flipX = false;
            else if (horizontalInput < -0.01f) sprite.flipX = true;
        }
    }

    void FixedUpdate()
    {
        if (inputLocked) return;

        // Horizontal movement (we set velocity directly so it's responsive)
        rb.linearVelocity = new Vector2(horizontalInput * moveSpeed, rb.linearVelocity.y);

        // Variable jump height / better gravity feel
        if (rb.linearVelocity.y < 0f)
        {
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (fallGravityMultiplier - 1f) * Time.fixedDeltaTime;
        }
        else if (rb.linearVelocity.y > 0f && !jumpHeld)
        {
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (lowJumpGravityMultiplier - 1f) * Time.fixedDeltaTime;
        }
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}

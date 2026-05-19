using UnityEngine;

/// <summary>
/// Spawned at the player's position on death. Creates a brief shower of
/// tinted dust particles that fall + fade, then self-destructs.
///
/// Pair with PlayerHealth.deathEffectPrefab — the Editor script
/// "Tools/Build Level 1" creates this prefab and wires it up automatically.
/// </summary>
public class DeathEffect : MonoBehaviour
{
    [Tooltip("Number of particles to spawn.")]
    public int particleCount = 36;

    [Tooltip("How long the particles live before being destroyed.")]
    public float duration = 1.1f;

    [Tooltip("Random horizontal speed range.")]
    public float horizontalSpread = 8f;

    [Tooltip("Vertical launch speed (positive = up).")]
    public float verticalSpeed = 9f;

    [Tooltip("Gravity scale for each particle's rigidbody.")]
    public float gravityScale = 2.5f;

    [Tooltip("Particle tint — orange/red feels appropriately violent.")]
    public Color tint = new Color(1f, 0.35f, 0.18f);

    [Tooltip("Particle size in world units.")]
    public float particleSize = 0.16f;

    private static Sprite cachedSprite;

    void Start()
    {
        if (cachedSprite == null) cachedSprite = MakeWhiteSquareSprite();
        SpawnBurst();
        // Destroy the spawner GameObject after a short delay (longer than longest particle).
        Destroy(gameObject, duration + 0.3f);
    }

    void SpawnBurst()
    {
        for (int i = 0; i < particleCount; i++)
        {
            var p = new GameObject("DustParticle");
            p.transform.position = transform.position;
            p.transform.localScale = new Vector3(particleSize, particleSize, 1f);

            var sr = p.AddComponent<SpriteRenderer>();
            sr.sprite = cachedSprite;
            sr.color = tint;
            sr.sortingOrder = 20;

            var rb = p.AddComponent<Rigidbody2D>();
            rb.gravityScale = gravityScale;
            rb.linearVelocity = new Vector2(
                Random.Range(-horizontalSpread, horizontalSpread),
                Random.Range(verticalSpeed * 0.4f, verticalSpeed));

            // Tiny rotation for flavor.
            rb.angularVelocity = Random.Range(-360f, 360f);

            var fader = p.AddComponent<FadeAndDie>();
            fader.lifetime = duration;
        }
    }

    /// <summary>Build a 2x2 white sprite at runtime — no asset dependency.</summary>
    private static Sprite MakeWhiteSquareSprite()
    {
        var tex = new Texture2D(2, 2);
        for (int x = 0; x < 2; x++)
            for (int y = 0; y < 2; y++)
                tex.SetPixel(x, y, Color.white);
        tex.filterMode = FilterMode.Point;
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 16);
    }
}

/// <summary>
/// Helper: fades a SpriteRenderer to transparent over its lifetime, then destroys.
/// </summary>
public class FadeAndDie : MonoBehaviour
{
    public float lifetime = 0.5f;

    private SpriteRenderer sr;
    private Color startColor;
    private float elapsed;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr != null) startColor = sr.color;
    }

    void Update()
    {
        elapsed += Time.deltaTime;
        if (sr != null)
        {
            float t = Mathf.Clamp01(elapsed / lifetime);
            sr.color = new Color(startColor.r, startColor.g, startColor.b, 1f - t);
        }
        if (elapsed >= lifetime) Destroy(gameObject);
    }
}

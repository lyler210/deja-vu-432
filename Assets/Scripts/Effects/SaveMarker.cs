using System.Collections;
using UnityEngine;

/// <summary>
/// Visual marker that appears where the player saved a rewind point.
///   * On spawn: plays a quick pulse-in animation (scale 0 → 1.4 → 1) plus a
///     ring of upward-floating sparkles, then sits in place bobbing gently.
///   * The marker persists until RewindManager destroys it (on next save,
///     rewind, or death).
///
/// Spawned by RewindManager.SaveCheckpoint() via a prefab created by the
/// "Tools → Build Level 1" Editor script.
/// </summary>
public class SaveMarker : MonoBehaviour
{
    [Tooltip("Color of the marker diamond.")]
    public Color markerColor = new Color(0.35f, 0.95f, 1f);

    [Tooltip("Final world-space size of the diamond (multiplies all scale values).")]
    public float markerSize = 0.125f;

    [Tooltip("How many sparkle particles fire on spawn.")]
    public int sparkleCount = 12;

    [Tooltip("Duration of the pulse-in animation, in seconds.")]
    public float pulseInDuration = 0.35f;

    [Tooltip("Bob amplitude (how far up/down the marker drifts).")]
    public float bobAmplitude = 0.04f;

    [Tooltip("Bob frequency (cycles per second).")]
    public float bobFrequency = 1.5f;

    private static Sprite cachedSquare;
    private SpriteRenderer markerSr;
    private Vector3 basePos;

    void Start()
    {
        basePos = transform.position;

        // Build the marker sprite (a tinted rotated square = diamond).
        markerSr = gameObject.AddComponent<SpriteRenderer>();
        markerSr.sprite = GetSquareSprite();
        markerSr.color = markerColor;
        markerSr.sortingOrder = 6;

        // Diamond orientation.
        transform.rotation = Quaternion.Euler(0, 0, 45f);
        transform.localScale = Vector3.zero; // pop in from zero

        StartCoroutine(SpawnPulse());
        SpawnSparkles();
    }

    void Update()
    {
        // Gentle bobbing once we're at full size.
        if (transform.localScale.x >= markerSize * 0.99f)
        {
            float y = basePos.y + Mathf.Sin(Time.time * bobFrequency * Mathf.PI * 2f) * bobAmplitude;
            transform.position = new Vector3(basePos.x, y, basePos.z);
        }
    }

    IEnumerator SpawnPulse()
    {
        // Scale 0 → 1.4*markerSize (overshoot) → markerSize
        float t = 0f;
        while (t < pulseInDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / pulseInDuration);
            // Overshoot curve.
            float factor = 1.4f * Mathf.Sin(p * Mathf.PI * 0.5f);
            if (p > 0.55f)
            {
                float k = (p - 0.55f) / 0.45f;
                factor = Mathf.Lerp(1.4f, 1.0f, k);
            }
            float s = factor * markerSize;
            transform.localScale = new Vector3(s, s, 1f);
            yield return null;
        }
        transform.localScale = new Vector3(markerSize, markerSize, 1f);
    }

    void SpawnSparkles()
    {
        // Scale sparkles relative to the marker size so a tiny marker doesn't
        // get drowned in huge sparkles.
        float sparkleScale = Mathf.Max(0.03f, markerSize * 0.4f);
        for (int i = 0; i < sparkleCount; i++)
        {
            var p = new GameObject("SaveSparkle");
            p.transform.position = transform.position;
            p.transform.localScale = new Vector3(sparkleScale, sparkleScale, 1f);

            var sr = p.AddComponent<SpriteRenderer>();
            sr.sprite = GetSquareSprite();
            sr.color = new Color(markerColor.r, markerColor.g, markerColor.b, 0.95f);
            sr.sortingOrder = 7;

            float angle = (i / (float)sparkleCount) * Mathf.PI * 2f;
            float speed = Random.Range(2.5f, 4.5f);
            var rb = p.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0.4f;
            rb.linearVelocity = new Vector2(Mathf.Cos(angle) * speed, Mathf.Sin(angle) * speed + 2f);

            var f = p.AddComponent<FadeAndDie>();
            f.lifetime = 0.55f;
        }
    }

    static Sprite GetSquareSprite()
    {
        if (cachedSquare != null) return cachedSquare;
        var tex = new Texture2D(2, 2);
        for (int x = 0; x < 2; x++)
            for (int y = 0; y < 2; y++)
                tex.SetPixel(x, y, Color.white);
        tex.filterMode = FilterMode.Point;
        tex.Apply();
        cachedSquare = Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 2);
        return cachedSquare;
    }
}

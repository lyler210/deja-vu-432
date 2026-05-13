using UnityEngine;

/// <summary>
/// Smooth 2D camera follow. Attach to the Main Camera. Drag the Player into 'target'.
/// Z is locked at offset.z so the camera stays behind the 2D plane.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Tooltip("Usually the Player Transform.")]
    public Transform target;

    [Tooltip("Offset from target. Z should stay negative (e.g. -10) for the camera.")]
    public Vector3 offset = new Vector3(0f, 1.5f, -10f);

    [Tooltip("Higher = snappier follow.")]
    public float smoothSpeed = 6f;

    [Header("Optional Clamp (set both to non-zero to enable)")]
    public Vector2 minBounds;
    public Vector2 maxBounds;

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desired = target.position + offset;

        if (minBounds != Vector2.zero || maxBounds != Vector2.zero)
        {
            desired.x = Mathf.Clamp(desired.x, minBounds.x, maxBounds.x);
            desired.y = Mathf.Clamp(desired.y, minBounds.y, maxBounds.y);
        }

        transform.position = Vector3.Lerp(transform.position, desired, smoothSpeed * Time.deltaTime);
    }
}

using UnityEngine;

/// <summary>
/// Smooth 2D camera follow. Attach to the Main Camera. Drag the Player into 'target'.
/// Z is locked at offset.z so the camera stays behind the 2D plane.
///
/// Uses Vector3.SmoothDamp internally — well-behaved critically-damped motion
/// that stays responsive to fast vertical drops without overshoot/jitter.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Tooltip("Usually the Player Transform.")]
    public Transform target;

    [Tooltip("Offset from target. Z should stay negative (e.g. -10) for the camera.")]
    public Vector3 offset = new Vector3(0f, 1.5f, -10f);

    [Tooltip("Approximate time (seconds) for the camera to reach the target. " +
             "Lower = snappier follow. ~0.05 is very tight, ~0.2 is loose.")]
    public float smoothTime = 0.05f;

    [Tooltip("Max units/sec the camera can move. Very high = no speed cap.")]
    public float maxSpeed = 200f;

    [Header("Optional Clamp (set both to non-zero to enable)")]
    public Vector2 minBounds;
    public Vector2 maxBounds;

    private Vector3 velocity;

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desired = target.position + offset;

        if (minBounds != Vector2.zero || maxBounds != Vector2.zero)
        {
            desired.x = Mathf.Clamp(desired.x, minBounds.x, maxBounds.x);
            desired.y = Mathf.Clamp(desired.y, minBounds.y, maxBounds.y);
        }

        transform.position = Vector3.SmoothDamp(transform.position, desired,
                                                ref velocity, smoothTime,
                                                maxSpeed, Time.deltaTime);
    }
}

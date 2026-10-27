using UnityEngine;

/// <summary>
/// Smooth follow camera with optional level bounds.
/// Owns the camera position: effects such as screen shake must not write to the
/// transform themselves (this script would overwrite them every frame). They push
/// an additive offset through <see cref="SetShakeOffset"/> instead.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Follow Settings")]
    [SerializeField] private float smoothTime = 0.2f;
    [SerializeField] private Vector3 offset = new Vector3(0f, 1f, -10f);

    [Header("Boundaries")]
    [SerializeField] private bool useBounds = false;
    [SerializeField] private Collider2D boundsCollider;

    private Vector3 velocity = Vector3.zero;
    private Camera cam;
    private Vector2 minBounds;
    private Vector2 maxBounds;
    private float camHalfHeight;
    private float camHalfWidth;

    // The clean, un-shaken position. Shake is added on top of it at render time so
    // that it never feeds back into the smoothing.
    private Vector3 basePosition;
    private Vector3 shakeOffset;

    private void Start()
    {
        cam = GetComponent<Camera>();
        basePosition = transform.position;
        CalculateCameraBounds();
    }

    /// <summary>Additive offset applied on top of the follow position, in world units.</summary>
    public void SetShakeOffset(Vector3 offset) => shakeOffset = offset;

    private void CalculateCameraBounds()
    {
        if (useBounds && boundsCollider != null)
        {
            minBounds = boundsCollider.bounds.min;
            maxBounds = boundsCollider.bounds.max;

            camHalfHeight = cam.orthographicSize;
            camHalfWidth = camHalfHeight * cam.aspect;
        }
    }

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 targetPosition = target.position + offset;

        if (useBounds && boundsCollider != null)
        {
            float clampedX;
            float clampedY;

            // Safe clamp X: if camera is wider than bounds, center it. Otherwise, clamp normally.
            if (camHalfWidth * 2f > maxBounds.x - minBounds.x)
                clampedX = (minBounds.x + maxBounds.x) / 2f;
            else
                clampedX = Mathf.Clamp(targetPosition.x, minBounds.x + camHalfWidth, maxBounds.x - camHalfWidth);

            // Safe clamp Y: if camera is taller than bounds, center it. Otherwise, clamp normally.
            if (camHalfHeight * 2f > maxBounds.y - minBounds.y)
                clampedY = (minBounds.y + maxBounds.y) / 2f;
            else
                clampedY = Mathf.Clamp(targetPosition.y, minBounds.y + camHalfHeight, maxBounds.y - camHalfHeight);

            targetPosition = new Vector3(clampedX, clampedY, targetPosition.z);
        }

        // Smooth on the clean position, then add shake - otherwise the shake would
        // pollute SmoothDamp's velocity and drag the camera around.
        basePosition = Vector3.SmoothDamp(basePosition, targetPosition, ref velocity, smoothTime);
        transform.position = basePosition + shakeOffset;
    }
}

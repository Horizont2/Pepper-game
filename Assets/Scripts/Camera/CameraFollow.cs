using UnityEngine;

/// <summary>
/// Smooth follow camera with optional level bounds.
/// Kept as a dependency-free fallback; the scene is driven by Cinemachine, which
/// also handles room bounds through CinemachineConfiner2D. Screen shake is applied
/// by <see cref="CameraShake"/> on top of whatever drives the camera, so this script
/// does not need to know about it.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera cam;
    [SerializeField] private Transform target;

    [Header("Follow Settings")]
    [SerializeField] private float smoothTime = 0.2f;
    [SerializeField] private Vector3 offset = new Vector3(0f, 1f, -10f);

    [Header("Boundaries")]
    [SerializeField] private bool useBounds = false;
    [SerializeField] private Collider2D boundsCollider;

    private Vector3 velocity = Vector3.zero;
    private Vector2 minBounds;
    private Vector2 maxBounds;
    private float camHalfHeight;
    private float camHalfWidth;

    // The clean, un-shaken position. Smoothing runs on this rather than on the live
    // transform, so an offset another script adds never feeds back into SmoothDamp.
    private Vector3 basePosition;

    private void Start()
    {
        basePosition = transform.position;
        CalculateCameraBounds();
    }

    private void CalculateCameraBounds()
    {
        if (useBounds && boundsCollider != null && cam != null)
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

        basePosition = Vector3.SmoothDamp(basePosition, targetPosition, ref velocity, smoothTime);
        transform.position = basePosition;
    }
}

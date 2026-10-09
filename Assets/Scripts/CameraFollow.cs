using UnityEngine;

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

    private void Start()
    {
        cam = GetComponent<Camera>();
        CalculateCameraBounds();
    }

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

        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);
    }
}
using UnityEngine;

/// <summary>
/// Moves a background layer by a fraction of the camera's travel, so distant art drifts
/// slower than the foreground and the scene gains depth.
///
/// Runs after the camera has been placed for the frame, otherwise the layer chases a
/// position the camera has already left and the background visibly lags behind.
/// </summary>
[DefaultExecutionOrder(900)]
public class ParallaxLayer : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The camera this layer parallaxes against. Usually the Main Camera.")]
    [SerializeField] private Transform cameraTransform;

    [Header("Parallax")]
    [Tooltip("0 = pinned to the camera (infinitely far away). 1 = moves with the world (no parallax). " +
             "Sky sits near 0.1, mid-ground near 0.5.")]
    [Range(0f, 1f)]
    [SerializeField] private float parallaxFactor = 0.3f;

    [Tooltip("Apply the effect vertically as well. Usually off for a side-scroller - vertical " +
             "parallax makes the horizon slide around when the player jumps.")]
    [SerializeField] private bool affectY;

    [Header("Infinite Scroll")]
    [Tooltip("Snap the layer back by one texture width once the camera has travelled that far, " +
             "so a single sprite covers any distance. The sprite must tile seamlessly.")]
    [SerializeField] private bool infiniteScroll;
    [Tooltip("Width of one tile in world units. Leave at 0 to read it from the SpriteRenderer.")]
    [SerializeField] private float tileWidth;
    [SerializeField] private SpriteRenderer spriteRenderer;

    // Anchors rather than a running total. Integrating a per-frame delta accumulates
    // rounding error over a long level and leaves the layer permanently offset if the
    // camera is ever teleported - a respawn, a room transition. Measuring against a
    // fixed origin each frame cannot drift and self-corrects after any jump.
    private Vector3 startPosition;
    private Vector3 startCameraPosition;

    private void Awake()
    {
        if (cameraTransform == null)
        {
            Debug.LogError($"[ParallaxLayer] cameraTransform is not assigned on '{name}'; this layer will not move.", this);
            enabled = false;
            return;
        }

        if (infiniteScroll && tileWidth <= 0f)
        {
            if (spriteRenderer != null)
            {
                tileWidth = spriteRenderer.bounds.size.x;
            }
            else
            {
                Debug.LogWarning($"[ParallaxLayer] '{name}' has infinite scroll on but no tile width " +
                                 "and no SpriteRenderer to measure. Infinite scroll is off.", this);
                infiniteScroll = false;
            }
        }
    }

    private void Start()
    {
        startPosition = transform.position;
        startCameraPosition = cameraTransform.position;
    }

    private void LateUpdate()
    {
        Vector3 travel = cameraTransform.position - startCameraPosition;

        Vector3 target = startPosition + new Vector3(
            travel.x * parallaxFactor,
            affectY ? travel.y * parallaxFactor : 0f,
            0f);

        if (infiniteScroll && tileWidth > 0f)
        {
            // The layer falls behind the camera at (1 - factor) of its travel. Shifting
            // by whole tiles cancels that out, and is seamless if the art tiles.
            float lag = travel.x * (1f - parallaxFactor);
            target.x += Mathf.Round(lag / tileWidth) * tileWidth;
        }

        transform.position = target;
    }
}

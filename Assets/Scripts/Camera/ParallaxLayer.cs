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

    private Vector3 previousCameraPosition;
    private float travelSinceSnap;

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
        previousCameraPosition = cameraTransform.position;
    }

    private void LateUpdate()
    {
        Vector3 cameraDelta = cameraTransform.position - previousCameraPosition;
        previousCameraPosition = cameraTransform.position;

        Vector3 move = new Vector3(
            cameraDelta.x * parallaxFactor,
            affectY ? cameraDelta.y * parallaxFactor : 0f,
            0f);

        transform.position += move;

        if (!infiniteScroll) return;

        // The layer falls behind the camera at (1 - factor) of the camera's travel.
        // Once that reaches one tile, snap forward by a tile - seamless if the art tiles.
        travelSinceSnap += cameraDelta.x * (1f - parallaxFactor);

        if (Mathf.Abs(travelSinceSnap) < tileWidth) return;

        float snap = Mathf.Sign(travelSinceSnap) * tileWidth;
        transform.position += new Vector3(snap, 0f, 0f);
        travelSinceSnap -= snap;
    }
}

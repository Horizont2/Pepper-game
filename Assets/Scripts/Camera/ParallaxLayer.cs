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
    [Tooltip("How much of the camera's travel this layer copies.\n\n" +
             "0 = sits still in the world, so it scrolls past at the FULL speed of the " +
             "foreground - that is the near plane, not the far one.\n" +
             "1 = travels with the camera, so it never scrolls - infinitely far away.\n\n" +
             "On-screen scroll speed is (1 - factor). Distant layers therefore want a HIGH " +
             "number: sky around 0.93, nearest background around 0.35.")]
    [Range(0f, 1f)]
    [SerializeField] private float parallaxFactor = 0.5f;

    [Tooltip("The same thing for vertical camera travel, kept separate because a side-scroller " +
             "moves up and down far more abruptly than it moves sideways. 1 pins the layer to " +
             "the camera vertically - no vertical drift at all, which is the safe choice - and " +
             "matching parallaxFactor gives the physically correct amount. Somewhere between " +
             "the two reads best: a little vertical give sells the depth without the horizon " +
             "swinging every time the Pepper jumps.")]
    [Range(0f, 1f)]
    [SerializeField] private float verticalFactor = 1f;

    [Header("Drift")]
    [Tooltip("World units per second this layer slides on its own, regardless of the camera. " +
             "A small negative value gives clouds a breeze; everything anchored to the ground " +
             "should leave this at zero.")]
    [SerializeField] private float autoScrollSpeed;

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
    private float drift;

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
            travel.y * verticalFactor,
            0f);

        // Drift is a running total on purpose: it is the one part of this that is meant to
        // accumulate, and it has no camera position to measure itself against.
        drift += autoScrollSpeed * Time.deltaTime;

        if (infiniteScroll && tileWidth > 0f)
        {
            // On screen the layer slides by (1 - factor) of the camera's travel, plus
            // whatever it has drifted. Shifting the world position by whole tiles cancels
            // that out, and is seamless as long as the art tiles.
            float slide = travel.x * (1f - parallaxFactor) - drift;
            target.x += Mathf.Round(slide / tileWidth) * tileWidth;
        }

        target.x += drift;

        transform.position = target;
    }
}

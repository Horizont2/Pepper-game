using UnityEngine;

/// <summary>
/// Hangs dust and a camera nudge on the movement events the controller already raises.
/// Jumping, landing and dashing all had an event with nothing listening to it, which is
/// why they felt weightless: the motion was right but nothing acknowledged the ground.
///
/// Landing is gated on how hard it was. Dust on every touchdown - including stepping off
/// a kerb or scuffing a ledge - reads as noise and stops meaning anything, so a gentle
/// landing stays silent and a real drop earns the puff.
/// </summary>
public class PlayerMovementFx : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PepperController controller;
    [SerializeField] private Rigidbody2D rb;
    [Tooltip("Optional. Lets a hard landing nudge the camera as well as raise dust.")]
    [SerializeField] private GameFeelManager gameFeel;

    [Header("Effects")]
    [Tooltip("Puff at the feet on touchdown. Jumping deliberately has none - the launch " +
             "reads from the motion, and a puff at both ends of every hop is too busy.")]
    [SerializeField] private SpriteSheetEffect landEffect;
    [Tooltip("Puff behind the dash. Mirrored to trail the direction travelled.")]
    [SerializeField] private SpriteSheetEffect dashEffect;
    [SerializeField] private SpriteSheetEffect wallJumpEffect;
    [Tooltip("Looping puff while sliding down a wall - it is the only thing selling the friction.")]
    [SerializeField] private SpriteSheetEffect wallSlideEffect;
    [Tooltip("Periodic puff while running. Gives the ground something to say back.")]
    [SerializeField] private SpriteSheetEffect runEffect;

    [Header("Running")]
    [Tooltip("Seconds between running puffs. Too short and it is a smoke trail.")]
    [SerializeField] private float runDustInterval = 0.32f;
    [Tooltip("Below this speed, walking leaves no dust.")]
    [SerializeField] private float runDustMinSpeed = 1.8f;

    [Header("Landing")]
    [Tooltip("Below this impact speed a landing raises no dust at all.")]
    [SerializeField] private float dustFallSpeed = 3.5f;
    [Tooltip("Above this one, the landing also nudges the camera.")]
    [SerializeField] private float heavyFallSpeed = 7f;
    [SerializeField] private float landShakeDuration = 0.12f;
    [SerializeField] private float landShakeMagnitude = 0.06f;

    private void Awake()
    {
        if (controller == null)
            Debug.LogWarning($"[PlayerMovementFx] controller is not assigned on '{name}'; no movement dust will play.", this);
    }

    private void OnEnable()
    {
        if (controller == null) return;

        controller.OnLanded += HandleLanded;
        controller.OnDashed += PlayDash;
        controller.OnWallJumped += PlayWallJump;
    }

    private void OnDisable()
    {
        if (controller == null) return;

        controller.OnLanded -= HandleLanded;
        controller.OnDashed -= PlayDash;
        controller.OnWallJumped -= PlayWallJump;
    }

    private float runDustTimer;
    private bool wallSlideShowing;

    private void Update()
    {
        if (controller == null) return;

        UpdateWallSlide();
        UpdateRunDust();
    }

    private void UpdateWallSlide()
    {
        if (wallSlideEffect == null) return;

        bool sliding = controller.IsWallSliding;
        if (sliding == wallSlideShowing) return;

        wallSlideShowing = sliding;

        if (sliding)
        {
            wallSlideEffect.gameObject.SetActive(true);
            wallSlideEffect.Play();
        }
        else
        {
            wallSlideEffect.Stop();
            wallSlideEffect.gameObject.SetActive(false);
        }
    }

    private void UpdateRunDust()
    {
        if (runEffect == null || rb == null) return;

        // Only on the ground, only above a walk, and not while a committed action has
        // frozen the Pepper in place
        bool running = controller.IsGrounded && Mathf.Abs(rb.linearVelocity.x) >= runDustMinSpeed;

        if (!running)
        {
            runDustTimer = 0f;
            return;
        }

        runDustTimer -= Time.deltaTime;
        if (runDustTimer > 0f) return;

        runDustTimer = runDustInterval;
        Fire(runEffect);
    }

    private void HandleLanded(float fallSpeed)
    {
        if (fallSpeed < dustFallSpeed) return;

        Fire(landEffect);

        // A heavy landing settles the camera downward, the way the body would
        if (fallSpeed < heavyFallSpeed || gameFeel == null) return;

        gameFeel.TriggerHitImpact(0f, landShakeDuration, landShakeMagnitude, Vector2.down);
    }

    private void PlayWallJump() => Fire(wallJumpEffect);

    // The effects are children of the Pepper, and the Pepper turns by mirroring its own
    // transform, so every child is already mirrored with the facing. Flipping the sprite
    // on top of that cancelled out going right and double-flipped going left, which
    // pointed the dash puff forwards instead of trailing behind. Placement alone is
    // enough: the object sits behind the feet and the mirror carries it to the other side.
    private void PlayDash() => Fire(dashEffect);

    private static void Fire(SpriteSheetEffect effect)
    {
        if (effect == null) return;

        // Switching it on is what starts it, and it hides itself when the run ends
        effect.gameObject.SetActive(true);
        effect.Play();
    }
}

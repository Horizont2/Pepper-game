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
    [Tooltip("Optional. Lets a hard landing nudge the camera as well as raise dust.")]
    [SerializeField] private GameFeelManager gameFeel;

    [Header("Effects")]
    [Tooltip("Puff at the feet on touchdown. Jumping deliberately has none - the launch " +
             "reads from the motion, and a puff at both ends of every hop is too busy.")]
    [SerializeField] private SpriteSheetEffect landEffect;
    [Tooltip("Puff behind the dash. Mirrored to trail the direction travelled.")]
    [SerializeField] private SpriteSheetEffect dashEffect;
    [SerializeField] private SpriteSheetEffect wallJumpEffect;

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

    private void HandleLanded(float fallSpeed)
    {
        if (fallSpeed < dustFallSpeed) return;

        Fire(landEffect);

        // A heavy landing settles the camera downward, the way the body would
        if (fallSpeed < heavyFallSpeed || gameFeel == null) return;

        gameFeel.TriggerHitImpact(0f, landShakeDuration, landShakeMagnitude, Vector2.down);
    }

    private void PlayWallJump() => Fire(wallJumpEffect);

    private void PlayDash()
    {
        // Trails behind the dash rather than leading it
        if (dashEffect == null) return;

        dashEffect.gameObject.SetActive(true);
        dashEffect.Play(controller.IsFacingRight ? -1f : 1f);
    }

    private static void Fire(SpriteSheetEffect effect)
    {
        if (effect == null) return;

        // Switching it on is what starts it, and it hides itself when the run ends
        effect.gameObject.SetActive(true);
        effect.Play();
    }
}

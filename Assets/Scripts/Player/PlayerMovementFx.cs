using UnityEngine;

/// <summary>
/// Hangs dust on the movement events the controller already raises. Jumping, landing and
/// dashing all had an event and nothing listening to it, which is why they felt weightless:
/// the motion was right but nothing acknowledged the ground.
/// </summary>
public class PlayerMovementFx : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PepperController controller;

    [Header("Effects")]
    [Tooltip("Puff at the feet on touchdown.")]
    [SerializeField] private SpriteSheetEffect landEffect;
    [Tooltip("Puff at the feet as the jump leaves the ground.")]
    [SerializeField] private SpriteSheetEffect jumpEffect;
    [Tooltip("Puff behind the dash. Mirrored to trail the direction travelled.")]
    [SerializeField] private SpriteSheetEffect dashEffect;
    [SerializeField] private SpriteSheetEffect wallJumpEffect;

    private void Awake()
    {
        if (controller == null)
            Debug.LogWarning($"[PlayerMovementFx] controller is not assigned on '{name}'; no movement dust will play.", this);
    }

    private void OnEnable()
    {
        if (controller == null) return;

        controller.OnLanded += PlayLand;
        controller.OnJumped += PlayJump;
        controller.OnDashed += PlayDash;
        controller.OnWallJumped += PlayWallJump;
    }

    private void OnDisable()
    {
        if (controller == null) return;

        controller.OnLanded -= PlayLand;
        controller.OnJumped -= PlayJump;
        controller.OnDashed -= PlayDash;
        controller.OnWallJumped -= PlayWallJump;
    }

    private void PlayLand() => Fire(landEffect);
    private void PlayJump() => Fire(jumpEffect);
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

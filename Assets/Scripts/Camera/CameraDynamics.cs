using UnityEngine;
using Unity.Cinemachine;

/// <summary>
/// Retunes the camera for what the Pepper is actually doing.
///
/// One damping value cannot serve both a walk and a dash. Slack enough to stay calm while
/// running leaves the camera trailing a dash that crosses a sixth of the screen in 0.15s;
/// tight enough to keep up with that dash and every footstep drags the view along. So the
/// damping moves with the Pepper's speed instead, and a dash takes it all the way in.
///
/// Written in Update, not LateUpdate: CinemachineBrain places the camera in LateUpdate, and
/// it has to read a damping value that is already settled for this frame.
///
/// This component only ever touches the composer's damping. The look offset belongs to
/// <see cref="CameraLook"/> and the impact offset to <see cref="CameraShake"/>, so none of
/// the three ever writes a value another one owns.
/// </summary>
public class CameraDynamics : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CinemachinePositionComposer composer;
    [SerializeField] private PepperController controller;
    [Tooltip("The Pepper's body. Speed is read from here rather than from the transform, " +
             "so a hit stop does not read as the player having come to a halt.")]
    [SerializeField] private Rigidbody2D playerBody;

    [Header("Damping")]
    [Tooltip("Walking and jumping. Loose, so small movements do not drag the view around.")]
    [SerializeField] private Vector2 restingDamping = new Vector2(0.3f, 0.6f);
    [Tooltip("Dashing, or running flat out. Tight, or the Pepper outruns the frame.")]
    [SerializeField] private Vector2 chasingDamping = new Vector2(0.13f, 0.4f);
    [Tooltip("Horizontal speed at which the camera is fully in chasing mode. Around the " +
             "dash speed, so a normal run only reaches part of the way.")]
    [SerializeField] private float chaseAtSpeed = 5f;
    [Tooltip("How fast the damping itself changes. Snapping between the two would be a " +
             "jolt of its own, which is the thing this exists to avoid.")]
    [SerializeField] private float blendSpeed = 4f;

    private float chase;

    private void Awake()
    {
        if (composer == null)
            Debug.LogWarning($"[CameraDynamics] composer is not assigned on '{name}'; the camera will keep its serialised damping.", this);

        if (playerBody == null && controller == null)
            Debug.LogWarning($"[CameraDynamics] neither playerBody nor controller is assigned on '{name}'; the damping will never change.", this);
    }

    private void Update()
    {
        if (composer == null) return;

        // A dash goes all the way in on its own. Otherwise it is purely a question of
        // speed, which means there is no state to get stuck in: the camera tightens as the
        // Pepper gets going and relaxes the moment it stops, including on a wall, on a
        // ledge or mid-stagger, where the body is barely moving.
        bool dashing = controller != null && controller.IsDashing;

        float wanted = dashing ? 1f
            : playerBody != null && chaseAtSpeed > 0f
                ? Mathf.Clamp01(Mathf.Abs(playerBody.linearVelocity.x) / chaseAtSpeed)
                : 0f;

        chase = Mathf.MoveTowards(chase, wanted, blendSpeed * Time.deltaTime);

        Vector3 damping = composer.Damping;
        damping.x = Mathf.Lerp(restingDamping.x, chasingDamping.x, chase);
        damping.y = Mathf.Lerp(restingDamping.y, chasingDamping.y, chase);
        composer.Damping = damping;
    }
}

using UnityEngine;
using Unity.Cinemachine;

/// <summary>
/// Shifts what the camera is aimed at when the player holds up or down, so they can see
/// where a drop leads before committing to it. In a game with pits, looking before you
/// jump is not a luxury.
///
/// It nudges the composer's target offset rather than the camera itself, so the confiner
/// and the damping still apply and the look never pushes the view outside the room.
/// </summary>
public class CameraLook : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CinemachinePositionComposer composer;
    [Tooltip("Looking is only allowed with both feet down - not mid-jump, where the camera is already busy.")]
    [SerializeField] private PepperController controller;

    [Header("Look")]
    [Tooltip("How far the view shifts, in world units.")]
    [SerializeField] private float lookDistance = 1.1f;
    [Tooltip("Hold the direction this long before the camera starts to move, so a tap does nothing.")]
    [SerializeField] private float holdBeforeLook = 0.35f;
    [SerializeField] private float lookSpeed = 2.5f;
    [SerializeField] private bool requireGrounded = true;
    [Tooltip("Looking is a deliberate stop-and-peer. Running with the stick on a diagonal " +
             "should not drag the view around, so any horizontal input cancels it.")]
    [SerializeField] private bool requireStandingStill = true;

    private float holdTimer;
    private float currentOffset;

    private void Awake()
    {
        if (composer == null)
            Debug.LogWarning($"[CameraLook] composer is not assigned on '{name}'; looking up and down will do nothing.", this);
    }

    private void Update()
    {
        if (composer == null) return;

        float input = Input.GetAxisRaw("Vertical");

        bool allowed = input != 0f
                    && (!requireGrounded || controller == null || controller.IsGrounded)
                    && (!requireStandingStill || Input.GetAxisRaw("Horizontal") == 0f);

        if (allowed) holdTimer += Time.deltaTime;
        else holdTimer = 0f;

        float wanted = holdTimer >= holdBeforeLook ? input * lookDistance : 0f;

        currentOffset = Mathf.MoveTowards(currentOffset, wanted, lookSpeed * Time.deltaTime);

        Vector3 offset = composer.TargetOffset;
        offset.y = currentOffset;
        composer.TargetOffset = offset;
    }
}

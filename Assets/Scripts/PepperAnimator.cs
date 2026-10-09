using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody2D))]
public class PepperAnimator : MonoBehaviour
{
    [Header("Animation Names")]
    [SerializeField] private string idleAnimName = "Pepper_Idle";
    [SerializeField] private string runAnimName = "Pepper_Run";
    [SerializeField] private string jumpAnimName = "Pepper_Jump";
    [SerializeField] private string fallAnimName = "Pepper_Fall";
    [SerializeField] private string attackAnimName = "Pepper_Attack";

    private Animator animator;
    private Rigidbody2D rb;

    private int idleHash, runHash, jumpHash, fallHash, attackHash;
    private int currentState;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();

        idleHash = Animator.StringToHash(idleAnimName);
        runHash = Animator.StringToHash(runAnimName);
        jumpHash = Animator.StringToHash(jumpAnimName);
        fallHash = Animator.StringToHash(fallAnimName);
        attackHash = Animator.StringToHash(attackAnimName);
    }

    private void Update()
    {
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

        // Wait for attack animation to finish
        if (stateInfo.shortNameHash == attackHash && stateInfo.normalizedTime < 1f) return;

        UpdateAnimationState();
    }

    private void UpdateAnimationState()
    {
        int newState;

        // Determine correct state based on physics
        if (rb.linearVelocity.y > 0.1f) newState = jumpHash;
        else if (rb.linearVelocity.y < -0.1f) newState = fallHash;
        else if (Mathf.Abs(rb.linearVelocity.x) > 0.1f) newState = runHash;
        else newState = idleHash;

        // Sync internal state if another script (like Combat) changed the animation
        int actualState = animator.GetCurrentAnimatorStateInfo(0).shortNameHash;
        if (actualState != currentState)
        {
            currentState = actualState;
        }

        // Play new animation if state changed
        if (newState != currentState)
        {
            animator.Play(newState, 0, 0f);
            currentState = newState;
        }
    }
}
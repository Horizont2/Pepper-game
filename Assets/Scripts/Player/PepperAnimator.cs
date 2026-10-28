using UnityEngine;

/// <summary>
/// Single owner of the Pepper's animation state.
/// No other script should call animator.Play() - when several scripts do, they
/// overwrite each other and animations either flicker or never show at all.
/// This class reads state from PepperController / PepperCombat / HealthComponent
/// and decides what to play, by priority.
/// All component references are assigned in the Inspector, never looked up at runtime.
/// </summary>
[RequireComponent(typeof(Animator))]
public class PepperAnimator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private PepperController controller;
    [SerializeField] private PepperCombat combat;
    [SerializeField] private PepperParry parry;
    [SerializeField] private HealthComponent health;

    [Header("Animation State Names")]
    [Tooltip("Must match the state names in the Animator Controller exactly. Case sensitive!")]
    [SerializeField] private string idleAnimName = "Idle";
    [SerializeField] private string runAnimName = "Run";
    [SerializeField] private string jumpAnimName = "jump";
    [SerializeField] private string fallAnimName = "Fall";
    [SerializeField] private string attackAnimName = "Attack";
    [SerializeField] private string dashAnimName = "Dash";
    [SerializeField] private string wallSlideAnimName = "Wall-Slide";
    [Tooltip("The catch. Plays once, then settles into the hang below.")]
    [SerializeField] private string ledgeGrabAnimName = "Edge-Grab";
    [SerializeField] private string ledgeIdleAnimName = "Edge-Idle";
    [Tooltip("The Warrior placeholder set has no block pose, so a crouch stands in for now.")]
    [SerializeField] private string parryAnimName = "Croush";
    [SerializeField] private string hurtAnimName = "Hurt";
    [SerializeField] private string deathAnimName = "Death";

    [Header("Tuning")]
    [SerializeField] private float verticalThreshold = 0.1f;
    [SerializeField] private float horizontalThreshold = 0.1f;
    [Tooltip("How long the hurt animation holds before movement states take over again.")]
    [SerializeField] private float hurtLockDuration = 0.25f;
    [Tooltip("How long the catch plays before it settles into the hang.")]
    [SerializeField] private float ledgeGrabLeadIn = 0.25f;

    private int idleHash, runHash, jumpHash, fallHash, attackHash, dashHash, hurtHash, deathHash, wallSlideHash, parryHash;
    private int ledgeGrabHash, ledgeIdleHash;

    private int currentState;
    private float hurtLockUntil;
    private bool isDead;

    // Previous-frame values, used to catch the moment an action STARTS so the
    // animation can restart even when the resolved state is unchanged
    // (for example two attacks in a row).
    private bool wasAttacking;
    private bool wasDashing;
    private bool wasParrying;
    private bool wasLedgeHanging;
    private float ledgeGrabUntil;

    private void Awake()
    {
        if (animator == null) Debug.LogError($"[PepperAnimator] Animator is not assigned on '{name}'.", this);
        if (rb == null) Debug.LogError($"[PepperAnimator] Rigidbody2D is not assigned on '{name}'.", this);

        // Validation: animator.Play() on a state that does not exist fails silently.
        // One loud warning at startup beats guessing later why an animation "disappeared".
        idleHash = ResolveState(idleAnimName);
        runHash = ResolveState(runAnimName);
        jumpHash = ResolveState(jumpAnimName);
        fallHash = ResolveState(fallAnimName);
        attackHash = ResolveState(attackAnimName);
        dashHash = ResolveState(dashAnimName);
        wallSlideHash = ResolveState(wallSlideAnimName);
        ledgeGrabHash = ResolveState(ledgeGrabAnimName);
        ledgeIdleHash = ResolveState(ledgeIdleAnimName);
        parryHash = ResolveState(parryAnimName);
        hurtHash = ResolveState(hurtAnimName);
        deathHash = ResolveState(deathAnimName);
    }

    private int ResolveState(string stateName)
    {
        if (string.IsNullOrEmpty(stateName) || animator == null) return 0;

        int hash = Animator.StringToHash(stateName);

        if (animator.runtimeAnimatorController == null)
        {
            Debug.LogWarning($"[PepperAnimator] No Animator Controller assigned on '{name}'.", this);
            return hash;
        }

        if (!animator.HasState(0, hash))
        {
            Debug.LogWarning(
                $"[PepperAnimator] State '{stateName}' does not exist in controller " +
                $"'{animator.runtimeAnimatorController.name}' (Base Layer). This animation will never play.",
                this);
        }

        return hash;
    }

    private void OnEnable()
    {
        if (health == null) return;
        health.OnDamaged += HandleDamaged;
        health.OnDied += HandleDied;
    }

    private void OnDisable()
    {
        if (health == null) return;
        health.OnDamaged -= HandleDamaged;
        health.OnDied -= HandleDied;
    }

    private void HandleDamaged(Vector2 sourcePosition) => hurtLockUntil = Time.time + hurtLockDuration;

    private void HandleDied() => isDead = true;

    /// <summary>
    /// Clears the death state so movement animations take over again. Death is the one
    /// state with no timer - it holds until something says the character is back.
    /// </summary>
    public void ResetDeath()
    {
        isDead = false;
        hurtLockUntil = 0f;
        currentState = 0;   // force the next resolved state to actually play
    }

    private void Update()
    {
        bool isAttacking = combat != null && combat.IsAttacking;
        bool isDashing = controller != null && controller.IsDashing;
        bool isParrying = parry != null && parry.IsBusy;
        bool isLedgeHanging = controller != null && controller.IsLedgeHanging;

        // The catch gets its own beat before the hang takes over, so the grab is something
        // that happens rather than a pose the Pepper is suddenly in.
        if (isLedgeHanging && !wasLedgeHanging) ledgeGrabUntil = Time.time + ledgeGrabLeadIn;

        // A new attack, dash, parry or ledge catch restarts its animation from frame zero,
        // even when the resolved state did not technically change.
        bool forceRestart = (isAttacking && !wasAttacking)
                         || (isDashing && !wasDashing)
                         || (isParrying && !wasParrying)
                         || (isLedgeHanging && !wasLedgeHanging);

        wasAttacking = isAttacking;
        wasDashing = isDashing;
        wasParrying = isParrying;
        wasLedgeHanging = isLedgeHanging;

        PlayState(ResolveCurrentState(isAttacking, isDashing, isParrying), forceRestart);
    }

    /// <summary>Priority: death -> hurt -> dash -> parry -> attack -> ledge -> wall slide -> airborne -> run -> idle.</summary>
    private int ResolveCurrentState(bool isAttacking, bool isDashing, bool isParrying)
    {
        if (isDead) return deathHash;
        if (Time.time < hurtLockUntil) return hurtHash;
        if (isDashing) return dashHash;

        // Above attack: the two cannot run together, but a parry is the more committed of
        // the pair and the one that must stay readable for its whole lock.
        if (isParrying) return parryHash;

        if (isAttacking) return attackHash;

        // Above the wall slide: a hang starts as one touch of the wall and would otherwise
        // flicker between the two on the frame it is caught
        if (controller != null && controller.IsLedgeHanging)
            return Time.time < ledgeGrabUntil ? ledgeGrabHash : ledgeIdleHash;

        // Above the airborne states: a slide is falling, and would otherwise read as Fall
        if (controller != null && controller.IsWallSliding) return wallSlideHash;

        if (rb.linearVelocity.y > verticalThreshold) return jumpHash;
        if (rb.linearVelocity.y < -verticalThreshold) return fallHash;
        if (Mathf.Abs(rb.linearVelocity.x) > horizontalThreshold) return runHash;

        return idleHash;
    }

    private void PlayState(int newState, bool forceRestart)
    {
        if (newState == 0) return;
        if (newState == currentState && !forceRestart) return;

        animator.Play(newState, 0, 0f);
        currentState = newState;
    }
}

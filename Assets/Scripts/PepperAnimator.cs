using UnityEngine;

/// <summary>
/// Single owner of the Pepper's animation state.
/// No other script should call animator.Play() - when several scripts do, they
/// overwrite each other and animations either flicker or never show at all.
/// This class reads state from PepperController / PepperCombat / HealthComponent
/// and decides what to play, by priority.
/// </summary>
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody2D))]
public class PepperAnimator : MonoBehaviour
{
    [Header("Animation State Names")]
    [Tooltip("Must match the state names in the Animator Controller exactly. Case sensitive!")]
    [SerializeField] private string idleAnimName = "Idle";
    [SerializeField] private string runAnimName = "Run";
    [SerializeField] private string jumpAnimName = "jump";
    [SerializeField] private string fallAnimName = "Fall";
    [SerializeField] private string attackAnimName = "Attack";
    [SerializeField] private string dashAnimName = "Dash";
    [SerializeField] private string hurtAnimName = "Hurt";
    [SerializeField] private string deathAnimName = "Death";

    [Header("Tuning")]
    [SerializeField] private float verticalThreshold = 0.1f;
    [SerializeField] private float horizontalThreshold = 0.1f;
    [Tooltip("How long the hurt animation holds before movement states take over again.")]
    [SerializeField] private float hurtLockDuration = 0.25f;

    private Animator animator;
    private Rigidbody2D rb;
    private PepperController controller;
    private PepperCombat combat;
    private HealthComponent health;

    private int idleHash, runHash, jumpHash, fallHash, attackHash, dashHash, hurtHash, deathHash;

    private int currentState;
    private float hurtLockUntil;
    private bool isDead;

    // Previous-frame values, used to catch the moment an action STARTS so the
    // animation can restart even when the resolved state is unchanged
    // (for example two attacks in a row).
    private bool wasAttacking;
    private bool wasDashing;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        controller = GetComponent<PepperController>();
        combat = GetComponent<PepperCombat>();
        health = GetComponent<HealthComponent>();

        // Validation: animator.Play() on a state that does not exist fails silently.
        // One loud warning at startup beats guessing later why an animation "disappeared".
        idleHash = ResolveState(idleAnimName);
        runHash = ResolveState(runAnimName);
        jumpHash = ResolveState(jumpAnimName);
        fallHash = ResolveState(fallAnimName);
        attackHash = ResolveState(attackAnimName);
        dashHash = ResolveState(dashAnimName);
        hurtHash = ResolveState(hurtAnimName);
        deathHash = ResolveState(deathAnimName);
    }

    private int ResolveState(string stateName)
    {
        if (string.IsNullOrEmpty(stateName)) return 0;

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

    private void HandleDamaged() => hurtLockUntil = Time.time + hurtLockDuration;

    private void HandleDied() => isDead = true;

    private void Update()
    {
        bool isAttacking = combat != null && combat.IsAttacking;
        bool isDashing = controller != null && controller.IsDashing;

        // A new attack or dash restarts its animation from frame zero,
        // even when the resolved state did not technically change.
        bool forceRestart = (isAttacking && !wasAttacking) || (isDashing && !wasDashing);

        wasAttacking = isAttacking;
        wasDashing = isDashing;

        PlayState(ResolveCurrentState(isAttacking, isDashing), forceRestart);
    }

    /// <summary>Priority: death -> hurt -> dash -> attack -> airborne -> run -> idle.</summary>
    private int ResolveCurrentState(bool isAttacking, bool isDashing)
    {
        if (isDead) return deathHash;
        if (Time.time < hurtLockUntil) return hurtHash;
        if (isDashing) return dashHash;
        if (isAttacking) return attackHash;

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

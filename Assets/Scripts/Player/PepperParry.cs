using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// The parry. A short window on a button press during which any incoming hit is cancelled
/// outright, and everything in front of the Pepper is staggered long enough to punish.
///
/// Three timings make or break how this feels, and they are separate on purpose:
/// the <b>window</b> is how forgiving the move is, the <b>recovery</b> is what a miss costs,
/// and the <b>cooldown</b> is what stops the player mashing it as a second dodge.
/// Tune them in that order - widen the window until it feels fair, then lengthen recovery
/// until spamming it feels bad.
/// </summary>
public class PepperParry : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private HealthComponent health;
    [Tooltip("Centre of the stagger burst. The AttackPoint transform works well.")]
    [SerializeField] private Transform parryPoint;
    [SerializeField] private LayerMask enemyLayers;
    [Tooltip("Checked so a parry cannot start out of a swing. Both are committed actions.")]
    [SerializeField] private PepperCombat combat;
    [Tooltip("Lives on the Main Camera, so this is assigned on the scene instance, not the prefab.")]
    [SerializeField] private GameFeelManager gameFeel;
    [Tooltip("Optional burst on a successful parry. A child of the Pepper, so it needs no spawning.")]
    [SerializeField] private ParticleSystem parryEffect;

    [Header("Timing")]
    [Tooltip("How long incoming hits are cancelled. The forgiveness of the move.")]
    [SerializeField] private float parryWindow = 0.18f;
    [Tooltip("Locked in place after the window closes. What a mistimed parry costs.")]
    [SerializeField] private float parryRecovery = 0.22f;
    [Tooltip("Before it can be used again. What stops it being a second dodge button.")]
    [SerializeField] private float parryCooldown = 0.5f;

    [Header("On Success")]
    [SerializeField] private float staggerRadius = 0.7f;
    [Tooltip("How long a parried enemy stands open. This is the reward - keep it generous.")]
    [SerializeField] private float staggerDuration = 1.2f;
    [SerializeField] private float hitStopDuration = 0.14f;
    [SerializeField] private float shakeDuration = 0.22f;
    [SerializeField] private float shakeMagnitude = 0.3f;

    /// <summary>True only while incoming damage is being cancelled.</summary>
    public bool IsWindowOpen { get; private set; }

    /// <summary>True for window plus recovery. PepperController reads this to lock movement.</summary>
    public bool IsBusy { get; private set; }

    /// <summary>0 right after a parry, 1 when it is available again. Drives a HUD slot.</summary>
    public float CooldownProgress
    {
        get
        {
            if (canParry) return 1f;

            float total = parryWindow + parryRecovery + parryCooldown;
            if (total <= 0f) return 1f;

            return Mathf.Clamp01(1f - (readyTime - Time.time) / total);
        }
    }

    /// <summary>Raised on a parry that connected. Hook VFX and audio here.</summary>
    public event Action OnParrySuccess;

    /// <summary>Raised when the window closed without catching anything.</summary>
    public event Action OnParryWhiffed;

    private bool canParry = true;
    private bool caughtSomething;
    private float readyTime;

    private void Awake()
    {
        if (health == null)
            Debug.LogError($"[PepperParry] health is not assigned on '{name}'; the parry can never absorb a hit.", this);

        if (parryPoint == null)
            Debug.LogWarning($"[PepperParry] parryPoint is not assigned on '{name}'; a successful parry will not stagger anything.", this);

        // Cartoon FX systems ship with Play On Awake set, so they would fire once on
        // load. Clear it here rather than unticking it on every sub-emitter by hand.
        if (parryEffect != null) parryEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    private void OnEnable()
    {
        if (health != null) health.DamageAbsorber += TryAbsorb;
    }

    private void OnDisable()
    {
        if (health != null) health.DamageAbsorber -= TryAbsorb;
    }

    private void Update()
    {
        if (IsBusy || !canParry) return;

        // No parrying out of a swing - the attack is a commitment, that is its cost
        if (combat != null && combat.IsAttacking) return;

        if (Input.GetButtonDown("Fire2"))
        {
            StartCoroutine(ParryRoutine());
        }
    }

    private IEnumerator ParryRoutine()
    {
        canParry = false;
        caughtSomething = false;
        readyTime = Time.time + parryWindow + parryRecovery + parryCooldown;

        IsBusy = true;
        IsWindowOpen = true;

        yield return new WaitForSeconds(parryWindow);

        IsWindowOpen = false;

        if (!caughtSomething) OnParryWhiffed?.Invoke();

        yield return new WaitForSeconds(parryRecovery);

        IsBusy = false;

        yield return new WaitForSeconds(parryCooldown);

        canParry = true;
    }

    /// <summary>
    /// Registered on the HealthComponent. Returning true swallows the hit completely.
    /// </summary>
    private bool TryAbsorb(Vector2 sourcePosition)
    {
        if (!IsWindowOpen) return false;

        caughtSomething = true;

        // A parry lands harder than a normal hit - that contrast is the whole point
        if (gameFeel != null) gameFeel.TriggerHitImpact(hitStopDuration, shakeDuration, shakeMagnitude);

        if (parryEffect != null)
        {
            parryEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            parryEffect.Play(true);
        }

        StaggerEnemiesInRange();
        OnParrySuccess?.Invoke();

        return true;
    }

    private void StaggerEnemiesInRange()
    {
        if (parryPoint == null) return;

        // Everything in front is staggered, not just whoever happened to land the hit.
        // Parrying one attacker out of two and then being hit by the other reads as broken.
        Collider2D[] caught = Physics2D.OverlapCircleAll(parryPoint.position, staggerRadius, enemyLayers);

        foreach (Collider2D enemy in caught)
        {
            EnemyHitFeedback feedback = enemy.GetComponent<EnemyHitFeedback>();
            if (feedback != null) feedback.Stagger(staggerDuration);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (parryPoint == null) return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(parryPoint.position, staggerRadius);
    }
}

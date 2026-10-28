using System.Collections;
using UnityEngine;

/// <summary>
/// Pepper melee attack. Owns the attack state; PepperController reads it to lock
/// movement and PepperAnimator reads it to play the swing.
/// Does NOT drive animations directly - PepperAnimator owns those.
/// All references are assigned in the Inspector, never looked up at runtime.
/// </summary>
public class PepperCombat : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Empty transform marking where the attack hitbox is centred.")]
    [SerializeField] private Transform attackPoint;
    [SerializeField] private LayerMask enemyLayers;
    [Tooltip("Checked so a swing cannot start out of a parry. Both are committed actions.")]
    [SerializeField] private PepperParry parry;
    [Tooltip("Lives on the Main Camera, so this is assigned on the scene instance, not the prefab.")]
    [SerializeField] private GameFeelManager gameFeel;
    [Tooltip("Optional burst played where the swing connects. A child of attackPoint, so it " +
             "already sits in the right place and needs no spawning.")]
    [SerializeField] private ParticleSystem hitEffect;
    [Tooltip("Arc drawn as the swing starts, whether or not it lands. Without it the attack " +
             "is invisible until it hits something.")]
    [SerializeField] private SpriteSheetEffect swingEffect;

    [Header("Combat Settings")]
    [SerializeField] private int attackDamage = 25;
    [SerializeField] private float attackRate = 2f;
    [SerializeField] private float attackRange = 0.6f;
    [Tooltip("How long the Pepper stays locked in place for one swing.")]
    [SerializeField] private float attackDuration = 0.4f;
    [Tooltip("Delay before the hitbox opens, so the swing lands mid-animation instead of on the button press.")]
    [SerializeField] private float attackWindup = 0.12f;

    [Tooltip("An attack press is remembered this long, so one made during a swing or a cooldown still lands.")]
    [SerializeField] private float attackBufferTime = 0.14f;

    [Header("Game Feel Settings")]
    [SerializeField] private float hitStopDuration = 0.05f;
    [SerializeField] private float shakeDuration = 0.1f;
    [SerializeField] private float shakeMagnitude = 0.15f;
    [Tooltip("A killing blow hits harder than a glancing one. Multiplies the freeze.")]
    [SerializeField] private float killHitStopMultiplier = 2.5f;
    [Tooltip("And the shake, so the last hit of a fight reads as the last hit.")]
    [SerializeField] private float killShakeMultiplier = 1.6f;

    /// <summary>True for the whole swing. Read by PepperController and PepperAnimator.</summary>
    public bool IsAttacking { get; private set; }

    /// <summary>A swing started, before anyone knows whether it lands.</summary>
    public event System.Action OnAttackStarted;

    /// <summary>The swing connected with at least one enemy.</summary>
    public event System.Action OnAttackHit;

    private float nextAttackTime;
    private float attackBufferCounter;
    private bool lastSwingKilled;

    private void Awake()
    {
        if (attackPoint == null)
            Debug.LogError($"[PepperCombat] attackPoint is not assigned on '{name}'; attacks cannot hit anything.", this);

        if (gameFeel == null)
            Debug.LogWarning($"[PepperCombat] gameFeel is not assigned on '{name}'; hits will land without hit stop or shake.", this);

        // Cartoon FX systems ship with Play On Awake set, so they would fire once on
        // load. Clear it here rather than unticking it on every sub-emitter by hand.
        if (hitEffect != null) hitEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    private void Update()
    {
        // Remember the press even when it arrives mid-swing or mid-cooldown. Silently
        // dropping input the player clearly gave is the most common source of a combat
        // system feeling unresponsive.
        if (Input.GetButtonDown("Fire1")) attackBufferCounter = attackBufferTime;
        else attackBufferCounter -= Time.deltaTime;

        // Already committed to a swing or a parry - both are commitments.
        // Without the parry check both run at once and the Pepper attacks while blocking.
        if (IsAttacking) return;
        if (parry != null && parry.IsBusy) return;

        if (attackBufferCounter <= 0f || Time.time < nextAttackTime) return;

        attackBufferCounter = 0f;
        StartCoroutine(AttackRoutine());
    }

    /// <summary>Abandon a swing in progress. Called on death so IsAttacking cannot stay stuck on.</summary>
    public void CancelAttack()
    {
        StopAllCoroutines();
        IsAttacking = false;
        nextAttackTime = 0f;
    }

    private IEnumerator AttackRoutine()
    {
        // 1. Open the attack (this locks movement via PepperController)
        IsAttacking = true;
        lastSwingKilled = false;
        OnAttackStarted?.Invoke();

        if (swingEffect != null)
        {
            swingEffect.gameObject.SetActive(true);
            swingEffect.Play();
        }

        // 2. Wind up, so the hit connects partway through the swing
        float windup = Mathf.Clamp(attackWindup, 0f, attackDuration);
        if (windup > 0f) yield return new WaitForSeconds(windup);

        // 3. Hitbox opens for one frame
        bool hitSuccessful = ApplyDamageInRange();

        // 4. Impact feedback (hit stop + camera shake + burst)
        if (hitSuccessful)
        {
            OnAttackHit?.Invoke();

            // The blow that finishes something lands harder than the three before it
            float stop = hitStopDuration * (lastSwingKilled ? killHitStopMultiplier : 1f);
            float shake = shakeMagnitude * (lastSwingKilled ? killShakeMultiplier : 1f);

            // Lean the shake along the swing rather than scattering it
            Vector2 swing = attackPoint != null
                ? (Vector2)(attackPoint.position - transform.position)
                : Vector2.zero;

            if (gameFeel != null) gameFeel.TriggerHitImpact(stop, shakeDuration, shake, swing);

            if (hitEffect != null)
            {
                // Restart rather than queue: two quick hits should each get their own burst
                hitEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                hitEffect.Play(true);
            }
        }

        // 5. Play out the rest of the swing
        float recovery = attackDuration - windup;
        if (recovery > 0f) yield return new WaitForSeconds(recovery);

        // 6. Close the attack, release movement and start the cooldown
        IsAttacking = false;
        nextAttackTime = Time.time + 1f / attackRate;
    }

    private bool ApplyDamageInRange()
    {
        if (attackPoint == null) return false;

        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayers);
        bool hitSuccessful = false;

        foreach (Collider2D enemy in hitEnemies)
        {
            HealthComponent health = enemy.GetComponent<HealthComponent>();
            if (health == null) continue;

            health.TakeDamage(attackDamage, transform.position);
            hitSuccessful = true;

            if (!health.IsAlive) lastSwingKilled = true;
        }

        return hitSuccessful;
    }

    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
}

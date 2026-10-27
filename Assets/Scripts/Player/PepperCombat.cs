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
    [Tooltip("Lives on the Main Camera, so this is assigned on the scene instance, not the prefab.")]
    [SerializeField] private GameFeelManager gameFeel;
    [Tooltip("Optional burst played where the swing connects. A child of attackPoint, so it " +
             "already sits in the right place and needs no spawning.")]
    [SerializeField] private ParticleSystem hitEffect;

    [Header("Combat Settings")]
    [SerializeField] private int attackDamage = 25;
    [SerializeField] private float attackRate = 2f;
    [SerializeField] private float attackRange = 0.6f;
    [Tooltip("How long the Pepper stays locked in place for one swing.")]
    [SerializeField] private float attackDuration = 0.4f;
    [Tooltip("Delay before the hitbox opens, so the swing lands mid-animation instead of on the button press.")]
    [SerializeField] private float attackWindup = 0.12f;

    [Header("Game Feel Settings")]
    [SerializeField] private float hitStopDuration = 0.05f;
    [SerializeField] private float shakeDuration = 0.1f;
    [SerializeField] private float shakeMagnitude = 0.15f;

    /// <summary>True for the whole swing. Read by PepperController and PepperAnimator.</summary>
    public bool IsAttacking { get; private set; }

    private float nextAttackTime;

    private void Awake()
    {
        if (attackPoint == null)
            Debug.LogError($"[PepperCombat] attackPoint is not assigned on '{name}'; attacks cannot hit anything.", this);

        if (gameFeel == null)
            Debug.LogWarning($"[PepperCombat] gameFeel is not assigned on '{name}'; hits will land without hit stop or shake.", this);
    }

    private void Update()
    {
        // Already swinging - ignore any new attack input
        if (IsAttacking) return;

        if (Time.time >= nextAttackTime && Input.GetButtonDown("Fire1"))
        {
            StartCoroutine(AttackRoutine());
        }
    }

    private IEnumerator AttackRoutine()
    {
        // 1. Open the attack (this locks movement via PepperController)
        IsAttacking = true;

        // 2. Wind up, so the hit connects partway through the swing
        float windup = Mathf.Clamp(attackWindup, 0f, attackDuration);
        if (windup > 0f) yield return new WaitForSeconds(windup);

        // 3. Hitbox opens for one frame
        bool hitSuccessful = ApplyDamageInRange();

        // 4. Impact feedback (hit stop + camera shake + burst)
        if (hitSuccessful)
        {
            if (gameFeel != null) gameFeel.TriggerHitImpact(hitStopDuration, shakeDuration, shakeMagnitude);

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

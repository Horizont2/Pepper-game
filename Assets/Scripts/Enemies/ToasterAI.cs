using System.Collections;
using UnityEngine;

/// <summary>
/// Patrol / chase / attack AI for the kitchen-appliance enemies.
/// All references are assigned in the Inspector. The player reference in particular is a
/// scene object, so enemies are placed in the scene and wired there rather than looked up
/// by tag at runtime.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(HealthComponent))]
public class ToasterAI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private HealthComponent health;
    [SerializeField] private Animator animator;
    [Tooltip("Optional. While it reports a reaction, this AI stops steering so the hit reads.")]
    [SerializeField] private HitFeedback hitFeedback;
    [Tooltip("The player in this scene. Assigned per instance, not on the prefab.")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform[] patrolPoints;

    [Header("Facing")]
    [Tooltip("Untick when the source art faces left by default, so flipping is not inverted.")]
    [SerializeField] private bool spriteFacesRight = true;

    [Header("Movement Settings")]
    [SerializeField] private float patrolSpeed = 0.7f;
    [SerializeField] private float chaseSpeed = 1.8f;
    [Tooltip("Compared on X only. Including Y would let a marker at a different height never count as reached.")]
    [SerializeField] private float patrolPointReachedDistance = 0.12f;

    [Header("Detection Settings")]
    [SerializeField] private float detectionRange = 2.5f;
    [Tooltip("How close it walks before stopping to swing. Keep it outside the player's body so the two do not overlap.")]
    [SerializeField] private float stopDistance = 0.5f;
    [Tooltip("Reach of the swing. A little more than stopDistance, or the hit never connects.")]
    [SerializeField] private float attackRange = 0.75f;
    [SerializeField] private LayerMask playerLayer;

    [Header("Combat Settings")]
    [SerializeField] private int attackDamage = 15;
    [SerializeField] private float attackCooldown = 1.5f;
    [Tooltip("Telegraph before the hit lands. Without it the swing is unreadable and unparryable.")]
    [SerializeField] private float attackWindup = 0.35f;
    [Tooltip("How long it stays committed after the swing - the opening to punish.")]
    [SerializeField] private float attackRecovery = 0.25f;
    [SerializeField] private string attackAnimName = "Attack";

    private int currentPatrolIndex;
    private float nextAttackTime;
    private bool isChasing;
    private bool isAttacking;
    private bool isFacingRight;
    private int attackAnimHash;

    private void Awake()
    {
        if (rb == null) Debug.LogError($"[ToasterAI] Rigidbody2D is not assigned on '{name}'.", this);
        if (health == null) Debug.LogError($"[ToasterAI] HealthComponent is not assigned on '{name}'.", this);
        if (player == null) Debug.LogWarning($"[ToasterAI] player is not assigned on '{name}'; this enemy will only patrol.", this);

        // The art starts unflipped, so logical facing starts at whatever the art does
        isFacingRight = spriteFacesRight;

        attackAnimHash = ResolveAttackState();
    }

    /// <summary>Animator.Play on a missing state fails silently, so check it up front.</summary>
    private int ResolveAttackState()
    {
        if (animator == null || string.IsNullOrEmpty(attackAnimName)) return 0;

        int hash = Animator.StringToHash(attackAnimName);

        if (animator.runtimeAnimatorController == null)
        {
            Debug.LogWarning($"[ToasterAI] No Animator Controller assigned on '{name}'.", this);
            return hash;
        }

        if (!animator.HasState(0, hash))
        {
            Debug.LogWarning(
                $"[ToasterAI] State '{attackAnimName}' does not exist in controller " +
                $"'{animator.runtimeAnimatorController.name}' (Base Layer). The attack animation will never play.",
                this);
        }

        return hash;
    }

    private void OnEnable()
    {
        if (health != null) health.OnDied += HandleDeath;
    }

    private void OnDisable()
    {
        if (health != null) health.OnDied -= HandleDeath;
    }

    private void Update()
    {
        // Let a knockback or a parry stagger play out before taking the wheel back
        if (hitFeedback != null && hitFeedback.IsReacting) return;

        // Committed to a swing: hold still and keep facing the target
        if (isAttacking)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            if (player != null) FaceTowards(player.position.x);
            return;
        }

        if (player == null)
        {
            Patrol();
            return;
        }

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        if (distanceToPlayer <= detectionRange)
        {
            isChasing = true;
        }
        else if (distanceToPlayer > detectionRange * 1.3f) // buffer so it doesn't flicker on the boundary
        {
            isChasing = false;
        }

        if (!isChasing)
        {
            Patrol();
            return;
        }

        // Face the player for the whole chase, not only while moving. Steering off
        // velocity alone left the enemy swinging at empty air whenever the player
        // stepped around it.
        FaceTowards(player.position.x);

        if (distanceToPlayer <= stopDistance)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            TryAttack();
        }
        else
        {
            ChasePlayer();
        }
    }

    private void Patrol()
    {
        if (patrolPoints == null || patrolPoints.Length == 0) return;

        Transform targetPoint = patrolPoints[currentPatrolIndex];
        if (targetPoint == null) return;

        float dx = targetPoint.position.x - transform.position.x;

        rb.linearVelocity = new Vector2(Mathf.Sign(dx) * patrolSpeed, rb.linearVelocity.y);
        FaceTowards(targetPoint.position.x);

        // X only: a marker placed slightly above or below the floor would otherwise never
        // count as reached and the enemy would grind against it forever.
        if (Mathf.Abs(dx) < patrolPointReachedDistance)
        {
            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
        }
    }

    private void ChasePlayer()
    {
        float dx = player.position.x - transform.position.x;
        rb.linearVelocity = new Vector2(Mathf.Sign(dx) * chaseSpeed, rb.linearVelocity.y);
    }

    private void TryAttack()
    {
        if (Time.time < nextAttackTime) return;

        nextAttackTime = Time.time + attackCooldown;
        StartCoroutine(AttackRoutine());
    }

    private IEnumerator AttackRoutine()
    {
        isAttacking = true;

        if (animator != null && attackAnimHash != 0)
        {
            animator.Play(attackAnimHash, 0, 0f);
        }

        // Wind up first. A hit landing on the frame the swing starts is unreadable, and
        // leaves the parry nothing to catch.
        if (attackWindup > 0f) yield return new WaitForSeconds(attackWindup);

        // Only connects if the player is still in reach when the swing lands
        Collider2D hit = Physics2D.OverlapCircle(transform.position, attackRange, playerLayer);
        if (hit != null)
        {
            HealthComponent playerHealth = hit.GetComponent<HealthComponent>();
            if (playerHealth != null) playerHealth.TakeDamage(attackDamage, transform.position);
        }

        if (attackRecovery > 0f) yield return new WaitForSeconds(attackRecovery);

        isAttacking = false;
    }

    /// <summary>Turn to face a world X position, honouring which way the art is drawn.</summary>
    private void FaceTowards(float targetX)
    {
        float delta = targetX - transform.position.x;
        if (Mathf.Abs(delta) < 0.02f) return;   // dead zone, or it jitters when level with the target

        SetFacing(delta > 0f);
    }

    private void SetFacing(bool faceRight)
    {
        if (faceRight == isFacingRight) return;

        isFacingRight = faceRight;

        Vector3 localScale = transform.localScale;
        localScale.x = Mathf.Abs(localScale.x) * (faceRight == spriteFacesRight ? 1f : -1f);
        transform.localScale = localScale;
    }

    private void HandleDeath()
    {
        // Only stand down. EnemyDeath owns the teardown - it plays the burst and clears the
        // object afterwards. Destroying here as well killed the GameObject on the same frame
        // and the death effect never got to show.
        enabled = false;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = new Color(1f, 0.6f, 0f);
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, stopDistance);
    }
}

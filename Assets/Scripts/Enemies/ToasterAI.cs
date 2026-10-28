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
    [Tooltip("Shared asset the player publishes itself into. Lets an enemy dropped anywhere " +
             "find the player with no per-instance wiring and no runtime search.")]
    [SerializeField] private PlayerLocator playerLocator;
    [Tooltip("Optional override for one specific instance. Takes priority over the locator.")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform[] patrolPoints;
    [Tooltip("The body collider. Used to work out where this enemy's feet and front edge are.")]
    [SerializeField] private Collider2D bodyCollider;

    [Header("Edge And Wall Detection")]
    [Tooltip("What counts as solid footing. Without it the enemy happily walks off every platform.")]
    [SerializeField] private LayerMask groundLayer;
    [Tooltip("How far below the feet still counts as floor. Any further down is a drop.")]
    [SerializeField] private float groundProbeDepth = 0.25f;
    [Tooltip("How far in front of the body to look for that floor, or for a wall.")]
    [SerializeField] private float probeAhead = 0.08f;

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

    [Header("Alert")]
    [Tooltip("Beat of hesitation the first time the player is spotted. Switching straight from " +
             "patrolling to charging gives the player nothing to read.")]
    [SerializeField] private float alertDuration = 0.4f;

    [Header("Debug")]
    [Tooltip("Logs every decision this AI makes. Leave off except when something looks wrong.")]
    [SerializeField] private bool logDecisions;

    /// <summary>Raised the moment this enemy notices the player. For audio and a future "!" mark.</summary>
    public event System.Action OnAlerted;

    private int currentPatrolIndex;
    private float nextAttackTime;
    private bool isChasing;
    private bool isAlerting;
    private bool isAttacking;
    private bool isFacingRight;
    private int attackAnimHash;
    private string lastReport;

    /// <summary>The instance override if there is one, otherwise whatever the locator holds.</summary>
    private Transform Target => player != null ? player
                              : playerLocator != null ? playerLocator.Current
                              : null;

    private void Awake()
    {
        if (rb == null) Debug.LogError($"[ToasterAI] Rigidbody2D is not assigned on '{name}'.", this);
        if (health == null) Debug.LogError($"[ToasterAI] HealthComponent is not assigned on '{name}'.", this);

        if (player == null && playerLocator == null)
            Debug.LogWarning($"[ToasterAI] '{name}' has neither a player override nor a PlayerLocator, so it will only patrol.", this);

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

        Transform target = Target;

        // Committed to a swing, or taking the beat after spotting the player: hold
        // still and keep facing the target
        if (isAttacking || isAlerting)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            if (target != null) FaceTowards(target.position.x);
            return;
        }

        if (target == null)
        {
            Report("no target, patrolling only");
            Patrol();
            return;
        }

        float distanceToPlayer = Vector2.Distance(transform.position, target.position);

        if (distanceToPlayer <= detectionRange)
        {
            // Only on the transition, not every frame it stays in range
            if (!isChasing) StartCoroutine(AlertRoutine());

            isChasing = true;
        }
        else if (distanceToPlayer > detectionRange * 1.3f) // buffer so it doesn't flicker on the boundary
        {
            isChasing = false;
        }

        if (!isChasing)
        {
            Report("patrolling, player out of range");
            Patrol();
            return;
        }

        // Face the player for the whole chase, not only while moving. Steering off
        // velocity alone left the enemy swinging at empty air whenever the player
        // stepped around it.
        FaceTowards(target.position.x);

        if (distanceToPlayer <= stopDistance)
        {
            Report("in range, swinging");
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            TryAttack();
        }
        else
        {
            Report("chasing");
            ChasePlayer();
        }
    }

    /// <summary>Logs a state only when it changes, so the console is readable.</summary>
    private void Report(string state)
    {
        if (!logDecisions || state == lastReport) return;

        lastReport = state;
        Debug.Log($"[ToasterAI] {name}: {state}", this);
    }

    private void Patrol()
    {
        if (patrolPoints == null || patrolPoints.Length == 0) return;

        Transform targetPoint = patrolPoints[currentPatrolIndex];
        if (targetPoint == null) return;

        float dx = targetPoint.position.x - transform.position.x;
        float direction = Mathf.Sign(dx);

        // Turn back at a drop or a wall rather than marching into it. Taking the next
        // patrol point reverses the walk, which is what a marker pair means anyway.
        if (!CanAdvance(direction))
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
            return;
        }

        rb.linearVelocity = new Vector2(direction * patrolSpeed, rb.linearVelocity.y);
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
        Transform target = Target;
        if (target == null) return;

        float direction = Mathf.Sign(target.position.x - transform.position.x);

        // Hold at the edge. It keeps facing the player and will still swing if they come
        // back into reach - better than a suicide run, and it reads as the enemy being wary.
        if (!CanAdvance(direction))
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        rb.linearVelocity = new Vector2(direction * chaseSpeed, rb.linearVelocity.y);
    }

    /// <summary>False when the next step would walk off a ledge or into a wall.</summary>
    private bool CanAdvance(float direction)
    {
        // With nothing configured to test against, never block movement
        if (bodyCollider == null || groundLayer.value == 0) return true;

        Bounds bounds = bodyCollider.bounds;
        float frontX = direction > 0f ? bounds.max.x : bounds.min.x;

        // Is there floor just ahead of the leading foot?
        Vector2 footProbe = new Vector2(frontX + direction * probeAhead, bounds.min.y + 0.02f);
        if (!Physics2D.Raycast(footProbe, Vector2.down, groundProbeDepth, groundLayer)) return false;

        // Is something solid in the way at body height?
        Vector2 sideProbe = new Vector2(frontX, bounds.center.y);
        if (Physics2D.Raycast(sideProbe, new Vector2(direction, 0f), probeAhead + 0.04f, groundLayer)) return false;

        return true;
    }

    private void TryAttack()
    {
        if (Time.time < nextAttackTime) return;

        nextAttackTime = Time.time + attackCooldown;
        StartCoroutine(AttackRoutine());
    }

    private IEnumerator AlertRoutine()
    {
        isAlerting = true;

        Report("spotted the player");
        if (hitFeedback != null) hitFeedback.ShowTell(alertDuration);
        OnAlerted?.Invoke();

        yield return new WaitForSeconds(alertDuration);

        isAlerting = false;
    }

    private IEnumerator AttackRoutine()
    {
        isAttacking = true;

        if (animator != null && attackAnimHash != 0)
        {
            animator.Play(attackAnimHash, 0, 0f);
        }

        // Colour the wind-up so the swing can be read, and parried, on sight
        if (hitFeedback != null) hitFeedback.ShowTell(attackWindup);

        // Wind up first. A hit landing on the frame the swing starts is unreadable, and
        // leaves the parry nothing to catch.
        if (attackWindup > 0f) yield return new WaitForSeconds(attackWindup);

        // Only connects if the player is still in reach when the swing lands
        Collider2D hit = Physics2D.OverlapCircle(transform.position, attackRange, playerLayer);

        if (hit == null)
        {
            if (logDecisions) Debug.Log($"[ToasterAI] {name}: swing found nothing on layer mask {playerLayer.value} within {attackRange}", this);
        }
        else
        {
            HealthComponent playerHealth = hit.GetComponent<HealthComponent>();

            if (playerHealth == null)
            {
                if (logDecisions) Debug.Log($"[ToasterAI] {name}: swing hit '{hit.name}' but it has no HealthComponent", this);
            }
            else
            {
                playerHealth.TakeDamage(attackDamage, transform.position);
                if (logDecisions) Debug.Log($"[ToasterAI] {name}: dealt {attackDamage} to '{playerHealth.name}', now {playerHealth.CurrentHealth}/{playerHealth.MaxHealth}", this);
            }
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

        if (bodyCollider == null) return;

        // The edge probes, both ways, so a patrol route can be placed by eye
        Bounds bounds = bodyCollider.bounds;
        Gizmos.color = Color.cyan;

        foreach (float direction in new[] { -1f, 1f })
        {
            float frontX = direction > 0f ? bounds.max.x : bounds.min.x;

            Vector3 foot = new Vector3(frontX + direction * probeAhead, bounds.min.y + 0.02f, 0f);
            Gizmos.DrawLine(foot, foot + Vector3.down * groundProbeDepth);

            Vector3 side = new Vector3(frontX, bounds.center.y, 0f);
            Gizmos.DrawLine(side, side + Vector3.right * direction * (probeAhead + 0.04f));
        }
    }
}

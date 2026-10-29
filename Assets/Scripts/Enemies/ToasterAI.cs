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
    [Tooltip("Fraction of the reach at which it commits to a swing. Below 1, so a swing " +
             "started at the very edge of reach is not a guaranteed whiff.")]
    [Range(0.3f, 1f)]
    [SerializeField] private float attackCommitFraction = 0.8f;
    [SerializeField] private LayerMask playerLayer;

    [Header("Combat Settings")]
    [SerializeField] private int attackDamage = 15;
    [SerializeField] private float attackCooldown = 1.5f;
    [Tooltip("Random slack either side of the cooldown. Without it two enemies that spot " +
             "the player together swing in lockstep for the rest of the fight, which lands " +
             "as one unbroken stream of damage rather than two attackers.")]
    [SerializeField] private float attackCooldownVariance = 0.35f;
    [Tooltip("Telegraph before the hit lands. Without it the swing is unreadable and unparryable.")]
    [SerializeField] private float attackWindup = 0.35f;
    [Tooltip("How long it stays committed after the swing - the opening to punish.")]
    [SerializeField] private float attackRecovery = 0.25f;
    [SerializeField] private string attackAnimName = "Attack";

    [Header("Attack Motion")]
    [Tooltip("The placeholder enemies have no attack clip, so the body has to sell the swing: " +
             "it gathers backwards on the wind-up and drives forward on the strike. Gather, " +
             "pause, strike reads as an attack even with a walk cycle on screen.")]
    [SerializeField] private float coilSpeed = 0.8f;
    [Tooltip("Speed of the forward drive on the strike.")]
    [SerializeField] private float lungeSpeed = 3.2f;
    [Tooltip("How long the lunge travels before the hit is resolved. It closes the last few " +
             "centimetres, so a swing committed at the edge of reach can still land.")]
    [SerializeField] private float lungeTravel = 0.08f;
    [Tooltip("Step back after the swing. Without it the enemy parks inside its own reach " +
             "and swings on every cooldown with no gap for the player to act in.")]
    [SerializeField] private float backOffSpeed = 1.3f;
    [SerializeField] private float backOffDuration = 0.22f;

    [Header("Alert")]
    [Tooltip("Beat of hesitation the first time the player is spotted. Switching straight from " +
             "patrolling to charging gives the player nothing to read.")]
    [SerializeField] private float alertDuration = 0.4f;

    [Header("Debug")]
    [Tooltip("Logs every decision this AI makes. Leave off except when something looks wrong.")]
    [SerializeField] private bool logDecisions;

    /// <summary>Raised the moment this enemy notices the player. For audio and a future "!" mark.</summary>
    public event System.Action OnAlerted;

    /// <summary>Raised as a swing starts winding up, before it can possibly connect.</summary>
    public event System.Action OnAttackStarted;

    /// <summary>Raised only when a swing actually reached the player.</summary>
    public event System.Action OnAttackLanded;

    private int currentPatrolIndex;
    private float nextAttackTime;
    private float shoveUntil;
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
            // A gather or a lunge owns the body for its beat. Zeroing the velocity every
            // frame would cancel the shove before it was ever drawn.
            if (Time.time >= shoveUntil)
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

        // Hold position once it is close enough, but swing on reach rather than on arrival.
        // These two used to be sequential - walk all the way in, and only then start a
        // 0.35s wind-up - which added up to well over a second before the first swing
        // could land. The player kills a weak enemy in half that, so in practice the
        // attack never happened: the enemy died mid-approach every time.
        if (distanceToPlayer <= stopDistance)
        {
            Report("holding at range");
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }
        else
        {
            Report("chasing");
            ChasePlayer();
        }

        if (distanceToPlayer <= attackRange * attackCommitFraction) TryAttack();
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

        nextAttackTime = Time.time + attackCooldown
                       + UnityEngine.Random.Range(-attackCooldownVariance, attackCooldownVariance);

        StartCoroutine(AttackRoutine());
    }

    private IEnumerator AlertRoutine()
    {
        isAlerting = true;

        Report("spotted the player");
        if (hitFeedback != null) hitFeedback.ShowTell(alertDuration);
        OnAlerted?.Invoke();

        // Push the first swing out by a random beat. Two enemies that notice the player on
        // the same frame would otherwise stay in phase and read as a single attacker
        // hitting twice as hard.
        nextAttackTime = Mathf.Max(
            nextAttackTime,
            Time.time + UnityEngine.Random.Range(0f, attackCooldownVariance * 2f));

        yield return new WaitForSeconds(alertDuration);

        isAlerting = false;
    }

    private IEnumerator AttackRoutine()
    {
        isAttacking = true;

        Transform target = Target;
        float toTarget = target != null
            ? Mathf.Sign(target.position.x - transform.position.x)
            : (isFacingRight ? 1f : -1f);

        if (animator != null && attackAnimHash != 0)
        {
            animator.Play(attackAnimHash, 0, 0f);
        }

        // Colour the wind-up so the swing can be read, and parried, on sight
        if (hitFeedback != null) hitFeedback.ShowTell(attackWindup);

        OnAttackStarted?.Invoke();

        // Gather backwards for the first half of the wind-up, then stand still for the
        // second. Only ever onto footing it already has - an enemy that lunges off its
        // own ledge is worse than one that cannot attack.
        if (coilSpeed > 0f && CanAdvance(-toTarget))
            Shove(-toTarget * coilSpeed, attackWindup * 0.5f);

        // Wind up first. A hit landing on the frame the swing starts is unreadable, and
        // leaves the parry nothing to catch.
        if (attackWindup > 0f) yield return new WaitForSeconds(attackWindup);

        // Drive into the strike. This is what makes a swing visible at all, the placeholder
        // enemies having no attack clip, and it closes the last stretch of the reach.
        if (lungeSpeed > 0f && CanAdvance(toTarget))
        {
            Shove(toTarget * lungeSpeed, lungeTravel);
            if (lungeTravel > 0f) yield return new WaitForSeconds(lungeTravel);
        }

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
                OnAttackLanded?.Invoke();
                if (logDecisions) Debug.Log($"[ToasterAI] {name}: dealt {attackDamage} to '{playerHealth.name}', now {playerHealth.CurrentHealth}/{playerHealth.MaxHealth}", this);
            }
        }

        // Give ground after swinging. The recovery is the window to punish, and it only
        // means anything if the enemy is out of its own reach while it lasts.
        if (backOffSpeed > 0f && CanAdvance(-toTarget))
            Shove(-toTarget * backOffSpeed, backOffDuration);

        if (attackRecovery > 0f) yield return new WaitForSeconds(attackRecovery);

        isAttacking = false;
    }

    /// <summary>
    /// Drive the body sideways for a beat and keep <see cref="Update"/> from zeroing it on
    /// the next frame. The gather and the lunge are the only animation the swing has.
    /// </summary>
    private void Shove(float speedX, float duration)
    {
        if (duration <= 0f) return;

        rb.linearVelocity = new Vector2(speedX, rb.linearVelocity.y);
        shoveUntil = Time.time + duration;
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
        // Where it actually commits, which is the one that decides whether it ever swings
        Gizmos.color = new Color(1f, 0.35f, 0f);
        Gizmos.DrawWireSphere(transform.position, attackRange * attackCommitFraction);
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

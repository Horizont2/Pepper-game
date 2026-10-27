using UnityEngine;

/// <summary>
/// Patrol / chase / attack AI for the kitchen-appliance enemies.
/// All references are assigned in the Inspector. The player reference in
/// particular is a scene object, so enemies are placed in the scene and wired
/// there rather than looked up by tag at runtime.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(HealthComponent))]
public class ToasterAI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private HealthComponent health;
    [SerializeField] private Animator animator;
    [Tooltip("The player in this scene. Assigned per instance, not on the prefab.")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform[] patrolPoints;

    [Header("Movement Settings")]
    [SerializeField] private float patrolSpeed = 3f;
    [SerializeField] private float chaseSpeed = 5.5f;
    [SerializeField] private float patrolPointReachedDistance = 0.5f;

    [Header("Detection Settings")]
    [SerializeField] private float detectionRange = 7f;
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private LayerMask playerLayer;

    [Header("Combat Settings")]
    [SerializeField] private int attackDamage = 15;
    [SerializeField] private float attackCooldown = 1.5f;
    [SerializeField] private string attackAnimName = "Attack";

    private int currentPatrolIndex;
    private float nextAttackTime;
    private bool isChasing;
    private bool isFacingRight = true;
    private int attackAnimHash;

    private void Awake()
    {
        if (rb == null) Debug.LogError($"[ToasterAI] Rigidbody2D is not assigned on '{name}'.", this);
        if (health == null) Debug.LogError($"[ToasterAI] HealthComponent is not assigned on '{name}'.", this);
        if (player == null) Debug.LogWarning($"[ToasterAI] player is not assigned on '{name}'; this enemy will only patrol.", this);

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
        else if (distanceToPlayer > detectionRange * 1.3f) // buffer so the enemy doesn't flicker on the boundary
        {
            isChasing = false;
        }

        if (!isChasing)
        {
            Patrol();
            return;
        }

        if (distanceToPlayer <= attackRange)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y); // stop moving to attack
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

        Vector2 direction = targetPoint.position - transform.position;

        rb.linearVelocity = new Vector2(Mathf.Sign(direction.x) * patrolSpeed, rb.linearVelocity.y);
        FlipTowards(rb.linearVelocity.x);

        if (Vector2.Distance(transform.position, targetPoint.position) < patrolPointReachedDistance)
        {
            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
        }
    }

    private void ChasePlayer()
    {
        Vector2 direction = player.position - transform.position;
        rb.linearVelocity = new Vector2(Mathf.Sign(direction.x) * chaseSpeed, rb.linearVelocity.y);
        FlipTowards(rb.linearVelocity.x);
    }

    private void TryAttack()
    {
        if (Time.time < nextAttackTime) return;

        nextAttackTime = Time.time + attackCooldown;

        if (animator != null && attackAnimHash != 0)
        {
            animator.Play(attackAnimHash, 0, 0f);
        }

        // Deal damage only if the player is still in range when the swing lands
        Collider2D playerCollider = Physics2D.OverlapCircle(transform.position, attackRange, playerLayer);
        if (playerCollider == null) return;

        HealthComponent playerHealth = playerCollider.GetComponent<HealthComponent>();
        if (playerHealth != null) playerHealth.TakeDamage(attackDamage);
    }

    private void FlipTowards(float velocityX)
    {
        if (velocityX > 0.1f && !isFacingRight) Flip();
        else if (velocityX < -0.1f && isFacingRight) Flip();
    }

    private void Flip()
    {
        isFacingRight = !isFacingRight;
        Vector3 localScale = transform.localScale;
        localScale.x *= -1f;
        transform.localScale = localScale;
    }

    private void HandleDeath()
    {
        // Death VFX and loot hook in here later
        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}

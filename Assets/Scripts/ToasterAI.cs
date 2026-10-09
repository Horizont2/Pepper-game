using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(HealthComponent))]
public class ToasterAI : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float patrolSpeed = 3f;
    [SerializeField] private float chaseSpeed = 5.5f;
    [SerializeField] private Transform[] patrolPoints;

    [Header("Detection Settings")]
    [SerializeField] private float detectionRange = 7f;
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private LayerMask playerLayer;

    [Header("Combat Settings")]
    [SerializeField] private int attackDamage = 15;
    [SerializeField] private float attackCooldown = 1.5f;
    [SerializeField] private string attackAnimName = "Toaster_Attack";

    [Header("References")]
    [SerializeField] private Animator animator;

    private Rigidbody2D rb;
    private Transform player;
    private int currentPatrolIndex = 0;
    private float nextAttackTime;
    private bool isChasing = false;
    private bool isFacingRight = true;
    private int attackAnimHash;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;

        // Find player by tag - Standard way for dynamic enemy spawning
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;

        if (animator != null)
        {
            attackAnimHash = Animator.StringToHash(attackAnimName);
        }

        // Subscribe to Death events
        GetComponent<HealthComponent>().OnDied += HandleDeath;
    }

    private void Update()
    {
        if (player == null) return;

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        if (distanceToPlayer <= detectionRange)
        {
            isChasing = true;
        }
        else if (distanceToPlayer > detectionRange * 1.3f) // Extra buffer so enemy doesn't instantly forget on boundary
        {
            isChasing = false;
        }

        if (isChasing)
        {
            if (distanceToPlayer <= attackRange)
            {
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y); // Stop moving to attack
                TryAttack();
            }
            else
            {
                ChasePlayer();
            }
        }
        else
        {
            Patrol();
        }
    }

    private void Patrol()
    {
        if (patrolPoints.Length == 0) return;

        Transform targetPoint = patrolPoints[currentPatrolIndex];
        Vector2 direction = (targetPoint.position - transform.position);

        rb.linearVelocity = new Vector2(Mathf.Sign(direction.x) * patrolSpeed, rb.linearVelocity.y);
        FlipTowards(rb.linearVelocity.x);

        // Check if reached destination
        if (Vector2.Distance(transform.position, targetPoint.position) < 0.5f)
        {
            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
        }
    }

    private void ChasePlayer()
    {
        Vector2 direction = (player.position - transform.position);
        rb.linearVelocity = new Vector2(Mathf.Sign(direction.x) * chaseSpeed, rb.linearVelocity.y);
        FlipTowards(rb.linearVelocity.x);
    }

    private void TryAttack()
    {
        if (Time.time < nextAttackTime) return;

        nextAttackTime = Time.time + attackCooldown;
        if (animator != null)
        {
            animator.Play(attackAnimHash, 0, 0f);
        }

        // Deal damage to player if they're still in range
        Collider2D playerCollider = Physics2D.OverlapCircle(transform.position, attackRange, playerLayer);
        if (playerCollider != null)
        {
            HealthComponent playerHealth = playerCollider.GetComponent<HealthComponent>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(attackDamage);
            }
        }
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
        // Add particle effects, spawn toast, destroy obj, etc.
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        GetComponent<HealthComponent>().OnDied -= HandleDeath;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
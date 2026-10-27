using System.Collections;
using UnityEngine;

/// <summary>
/// Pepper movement: acceleration/deceleration, jump with the usual game-feel helpers
/// (coyote time, jump buffering, variable jump height) and a dash.
/// Does NOT drive animations - PepperAnimator owns those.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class PepperController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float acceleration = 13f;
    [SerializeField] private float deceleration = 16f;
    [SerializeField] private float frictionAmount = 0.2f;

    [Header("Jump Settings")]
    [SerializeField] private float jumpForce = 16f;
    [SerializeField] private float fallMultiplier = 2.5f;
    [SerializeField] private float lowJumpMultiplier = 2f;
    [SerializeField] private float jumpCooldown = 0.2f;

    [Header("AAA Polish (Game Feel)")]
    [Tooltip("Grace period after walking off a ledge where a jump still counts.")]
    [SerializeField] private float coyoteTime = 0.15f;
    [Tooltip("How long an early jump press is remembered before landing.")]
    [SerializeField] private float jumpBufferTime = 0.1f;

    [Header("Ground Detection")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private Vector2 groundCheckSize = new Vector2(0.5f, 0.1f);
    [SerializeField] private LayerMask groundLayer;

    [Header("Dash Settings")]
    [SerializeField] private float dashForce = 24f;
    [SerializeField] private float dashDuration = 0.15f;
    [SerializeField] private float dashCooldown = 1f;
    [SerializeField] private TrailRenderer dashTrail;

    private Rigidbody2D rb;
    private Collider2D playerCollider;
    private PepperCombat combat; // used to know whether an attack is in progress

    private float horizontalInput;
    private float coyoteTimeCounter;
    private float jumpBufferCounter;
    private float jumpCooldownTimer;
    private bool isFacingRight = true;

    private bool canDash = true;
    private int enemyLayerIndex;

    // Read by PepperAnimator and other systems
    public bool IsDashing { get; private set; }
    public bool IsGrounded { get; private set; }
    public bool IsFacingRight => isFacingRight;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerCollider = GetComponent<Collider2D>();
        combat = GetComponent<PepperCombat>();

        rb.freezeRotation = true;
        enemyLayerIndex = LayerMask.NameToLayer("Enemy");

        if (dashTrail != null) dashTrail.emitting = false;
    }

    private void Update()
    {
        if (IsDashing) return;

        // The Pepper is rooted while attacking, so movement input is ignored
        if (combat != null && combat.IsAttacking)
        {
            horizontalInput = 0f;
            jumpBufferCounter = 0f; // and a jump press must not queue up during the swing
        }
        else
        {
            HandleInput();
        }

        UpdateTimers();
        CheckEnvironment();

        // No jumping out of an attack
        if (combat == null || !combat.IsAttacking)
        {
            HandleJump();
        }

        FlipController();
    }

    private void FixedUpdate()
    {
        if (IsDashing) return;

        // Grounded attack kills horizontal speed entirely, which gives the swing weight
        if (combat != null && combat.IsAttacking && IsGrounded)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }
        else
        {
            HandleMovement();
            ApplyFriction();
        }

        ApplyBetterGravity();
    }

    private void HandleInput()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");

        if (Input.GetButtonDown("Jump"))
        {
            jumpBufferCounter = jumpBufferTime;
        }

        if (Input.GetButtonDown("Fire3") && canDash)
        {
            StartCoroutine(PerformDash());
        }
    }

    private void UpdateTimers()
    {
        jumpBufferCounter -= Time.deltaTime;
        jumpCooldownTimer -= Time.deltaTime;

        if (IsGrounded && jumpCooldownTimer <= 0f)
        {
            coyoteTimeCounter = coyoteTime;
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }
    }

    private void CheckEnvironment()
    {
        IsGrounded = Physics2D.OverlapBox(groundCheck.position, groundCheckSize, 0f, groundLayer);
    }

    private void HandleMovement()
    {
        float targetSpeed = horizontalInput * moveSpeed;
        float speedDifference = targetSpeed - rb.linearVelocity.x;
        float accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? acceleration : deceleration;

        float movement = speedDifference * accelRate;
        rb.AddForce(movement * Vector2.right);
    }

    private void ApplyFriction()
    {
        if (IsGrounded && Mathf.Abs(horizontalInput) < 0.01f)
        {
            float amount = Mathf.Min(Mathf.Abs(rb.linearVelocity.x), Mathf.Abs(frictionAmount));
            amount *= Mathf.Sign(rb.linearVelocity.x);
            rb.AddForce(Vector2.right * -amount, ForceMode2D.Impulse);
        }
    }

    private void HandleJump()
    {
        if (jumpBufferCounter > 0f && coyoteTimeCounter > 0f && jumpCooldownTimer <= 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);

            jumpBufferCounter = 0f;
            coyoteTimeCounter = 0f;
            jumpCooldownTimer = jumpCooldown;
        }
    }

    /// <summary>Fall faster than we rise, and cut the jump short when the button is released.</summary>
    private void ApplyBetterGravity()
    {
        if (rb.linearVelocity.y < 0)
        {
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1) * Time.fixedDeltaTime;
        }
        else if (rb.linearVelocity.y > 0 && !Input.GetButton("Jump"))
        {
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (lowJumpMultiplier - 1) * Time.fixedDeltaTime;
        }
    }

    private void FlipController()
    {
        if (horizontalInput > 0f && !isFacingRight) Flip();
        else if (horizontalInput < 0f && isFacingRight) Flip();
    }

    private void Flip()
    {
        isFacingRight = !isFacingRight;
        Vector3 localScale = transform.localScale;
        localScale.x *= -1f;
        transform.localScale = localScale;
    }

    private IEnumerator PerformDash()
    {
        canDash = false;
        IsDashing = true;

        float originalGravity = rb.gravityScale;
        rb.gravityScale = 0f;

        float dashDirection = isFacingRight ? 1f : -1f;
        rb.linearVelocity = new Vector2(dashDirection * dashForce, 0f);

        if (dashTrail != null) dashTrail.emitting = true;

        // Pass through enemies for the duration of the dash (i-frames)
        if (playerCollider != null && enemyLayerIndex > -1)
        {
            playerCollider.excludeLayers |= (1 << enemyLayerIndex);
        }

        yield return new WaitForSeconds(dashDuration);

        if (dashTrail != null) dashTrail.emitting = false;

        if (playerCollider != null && enemyLayerIndex > -1)
        {
            playerCollider.excludeLayers &= ~(1 << enemyLayerIndex);
        }

        rb.gravityScale = originalGravity;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x * 0.5f, rb.linearVelocity.y);

        IsDashing = false;

        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(groundCheck.position, groundCheckSize);
        }
    }
}

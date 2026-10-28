using System.Collections;
using UnityEngine;

/// <summary>
/// Pepper movement: acceleration/deceleration, jump with the usual game-feel helpers
/// (coyote time, jump buffering, variable jump height) and a dash.
/// Does NOT drive animations - PepperAnimator owns those.
/// All component references are assigned in the Inspector, never looked up at runtime.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class PepperController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Collider2D playerCollider;
    [SerializeField] private PepperCombat combat;
    [SerializeField] private PepperParry parry;
    [Tooltip("Needed for the dash to actually grant invulnerability, not just pass through bodies.")]
    [SerializeField] private HealthComponent health;

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
    [Tooltip("Layers the Pepper passes through while dashing - the dash i-frames.")]
    [SerializeField] private LayerMask dashIgnoreLayers;

    private float horizontalInput;
    private float coyoteTimeCounter;
    private float jumpBufferCounter;
    private float jumpCooldownTimer;
    private bool isFacingRight = true;

    private bool canDash = true;
    private bool jumpHeld;
    private float dashReadyTime;
    private LayerMask excludeLayersBeforeDash;

    // Read by PepperAnimator and other systems
    public bool IsDashing { get; private set; }
    public bool IsGrounded { get; private set; }
    public bool IsFacingRight => isFacingRight;

    /// <summary>0 while the dash has just been spent, 1 when it is ready again. Drives the HUD slot.</summary>
    public float DashCooldownProgress
    {
        get
        {
            if (canDash) return 1f;

            float total = dashDuration + dashCooldown;
            if (total <= 0f) return 1f;

            return Mathf.Clamp01(1f - (dashReadyTime - Time.time) / total);
        }
    }

    private void Awake()
    {
        if (rb == null) Debug.LogError($"[PepperController] Rigidbody2D is not assigned on '{name}'.", this);
        if (playerCollider == null) Debug.LogError($"[PepperController] Collider2D is not assigned on '{name}'.", this);
        if (groundCheck == null) Debug.LogError($"[PepperController] groundCheck is not assigned on '{name}'.", this);
    }

    private void OnEnable()
    {
        // excludeLayers only stops physical contact. Enemy attacks land through an
        // overlap query, which ignores it entirely, so without this the dash looked
        // like i-frames and granted none.
        if (health != null) health.DamageAbsorber += AbsorbWhileDashing;
    }

    private void OnDisable()
    {
        if (health != null) health.DamageAbsorber -= AbsorbWhileDashing;
    }

    private bool AbsorbWhileDashing(Vector2 sourcePosition) => IsDashing;

    /// <summary>Attacking or parrying both root the Pepper in place.</summary>
    private bool IsActionLocked =>
        (combat != null && combat.IsAttacking) || (parry != null && parry.IsBusy);

    private void Update()
    {
        // Polled here rather than in FixedUpdate: a physics step can run zero or several
        // times per frame, so reading a button there drops or repeats presses and the
        // variable jump height becomes frame-rate dependent.
        jumpHeld = Input.GetButton("Jump");

        if (IsDashing) return;

        bool locked = IsActionLocked;

        // The Pepper is rooted during a committed action, so movement input is ignored
        if (locked)
        {
            horizontalInput = 0f;
            jumpBufferCounter = 0f; // and a jump press must not queue up during it
        }
        else
        {
            HandleInput();
        }

        UpdateTimers();
        CheckEnvironment();

        // No jumping out of an attack or a parry
        if (!locked) HandleJump();

        FlipController();
    }

    private void FixedUpdate()
    {
        if (IsDashing) return;

        // A grounded committed action kills horizontal speed entirely, which gives it weight
        if (IsActionLocked && IsGrounded)
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
        else if (rb.linearVelocity.y > 0 && !jumpHeld)
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
        dashReadyTime = Time.time + dashDuration + dashCooldown;
        IsDashing = true;

        float originalGravity = rb.gravityScale;
        rb.gravityScale = 0f;

        float dashDirection = isFacingRight ? 1f : -1f;
        rb.linearVelocity = new Vector2(dashDirection * dashForce, 0f);

        if (dashTrail != null) dashTrail.emitting = true;

        // Pass through the configured layers for the duration of the dash (i-frames)
        excludeLayersBeforeDash = playerCollider.excludeLayers;
        playerCollider.excludeLayers = excludeLayersBeforeDash.value | dashIgnoreLayers.value;

        yield return new WaitForSeconds(dashDuration);

        if (dashTrail != null) dashTrail.emitting = false;

        playerCollider.excludeLayers = excludeLayersBeforeDash;

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

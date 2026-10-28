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
    [Tooltip("Optional. While it reports a reaction, movement input is ignored so a hit reads.")]
    [SerializeField] private HitFeedback hitFeedback;

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
    [Tooltip("Vertical speed below which the jump counts as being at its peak.")]
    [SerializeField] private float apexThreshold = 2f;
    [Tooltip("Fraction of gravity that still applies at the peak. Lower means more hang time.")]
    [Range(0f, 1f)]
    [SerializeField] private float apexGravity = 0.55f;
    [Tooltip("Extra acceleration when reversing, so a turn is sharp instead of a slow slide.")]
    [SerializeField] private float turnAroundBoost = 2.2f;

    [Header("Ground Detection")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private Vector2 groundCheckSize = new Vector2(0.5f, 0.1f);
    [SerializeField] private LayerMask groundLayer;

    [Header("Wall Movement")]
    [Tooltip("Empty child out in front at body height. It mirrors with the sprite, so one marker serves both sides.")]
    [SerializeField] private Transform wallCheck;
    [SerializeField] private Vector2 wallCheckSize = new Vector2(0.06f, 0.22f);
    [SerializeField] private LayerMask wallLayer;
    [Tooltip("Capped fall speed while sliding down a wall.")]
    [SerializeField] private float wallSlideSpeed = 1.2f;
    [Tooltip("Push away from the wall. X is the shove off it, Y the lift.")]
    [SerializeField] private Vector2 wallJumpForce = new Vector2(4f, 8f);
    [Tooltip("Movement input ignored right after a wall jump, or holding into the wall cancels it instantly.")]
    [SerializeField] private float wallJumpLockTime = 0.18f;
    [Tooltip("Grace period after leaving a wall where a wall jump still counts.")]
    [SerializeField] private float wallCoyoteTime = 0.12f;

    [Header("Ledge Grab")]
    [Tooltip("Empty child just above the head, in the same column as wallCheck. A wall at " +
             "the wall check with nothing here is a ledge.")]
    [SerializeField] private Transform ledgeCheck;
    [SerializeField] private Vector2 ledgeCheckSize = new Vector2(0.06f, 0.08f);
    [Tooltip("The push that carries the Pepper over the lip. X is forward, Y is the lift.")]
    [SerializeField] private Vector2 ledgeClimbForce = new Vector2(3f, 5.5f);
    [Tooltip("After letting go, how long before the same ledge can be caught again.")]
    [SerializeField] private float ledgeReleaseLock = 0.3f;

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

    // Captured once so a dash interrupted by death can be undone exactly. The dash zeroes
    // gravity and widens the collider's exclusions; dying mid-dash without restoring them
    // leaves the Pepper floating and phasing through enemies for the rest of the session.
    private float defaultGravityScale;
    private LayerMask defaultExcludeLayers;

    private float wallCoyoteCounter;
    private float wallJumpLockUntil;
    private float ledgeLockUntil;

    // Read by PepperAnimator and other systems
    public bool IsDashing { get; private set; }
    public bool IsGrounded { get; private set; }
    public bool IsFacingRight => isFacingRight;
    public bool IsTouchingWall { get; private set; }
    public bool IsWallSliding { get; private set; }

    /// <summary>Hanging off a ledge by the hands. Movement, gravity and the jump all defer to it.</summary>
    public bool IsLedgeHanging { get; private set; }

    /// <summary>Raised the moment a wall jump fires, for audio and effects.</summary>
    public event System.Action OnWallJumped;

    /// <summary>A normal jump left the ground.</summary>
    public event System.Action OnJumped;

    /// <summary>A dash started.</summary>
    public event System.Action OnDashed;

    /// <summary>Caught a ledge.</summary>
    public event System.Action OnLedgeGrabbed;

    /// <summary>Pulled up over a ledge rather than dropping off it.</summary>
    public event System.Action OnLedgeClimbed;

    /// <summary>Touched down after being airborne. Carries the downward speed on impact,
    /// so a gentle step off a kerb and a long drop can be told apart.</summary>
    public event System.Action<float> OnLanded;

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

        if (rb != null) defaultGravityScale = rb.gravityScale;
        if (playerCollider != null) defaultExcludeLayers = playerCollider.excludeLayers;
    }

    /// <summary>
    /// Drop everything in progress and return to a clean standing state. Called on death so
    /// a half-finished dash cannot resume, or leave its gravity and collider changes behind.
    /// </summary>
    public void CancelActions()
    {
        StopAllCoroutines();

        IsDashing = false;
        IsWallSliding = false;
        IsTouchingWall = false;
        IsLedgeHanging = false;
        canDash = true;
        jumpBufferCounter = 0f;
        coyoteTimeCounter = 0f;
        wallCoyoteCounter = 0f;
        wallJumpLockUntil = 0f;
        ledgeLockUntil = 0f;
        jumpCooldownTimer = 0f;
        horizontalInput = 0f;

        if (rb != null) rb.gravityScale = defaultGravityScale;
        if (playerCollider != null) playerCollider.excludeLayers = defaultExcludeLayers;
        if (dashTrail != null) dashTrail.emitting = false;
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

        // The hang owns the jump button - pulling up over the lip is what it is for - and
        // the facing has to stay into the wall, so the flip is skipped as well.
        if (IsLedgeHanging)
        {
            HandleLedgeHang();
            return;
        }

        // No jumping out of an attack or a parry
        if (!locked) HandleJump();

        FlipController();
    }

    private void FixedUpdate()
    {
        if (IsDashing) return;

        // The hang holds the body still. Movement and gravity would both pull it off.
        if (IsLedgeHanging)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        // Let a knockback carry. Driving movement through it would cancel the impulse on
        // the next physics step and taking a hit would not read at all.
        if (hitFeedback != null && hitFeedback.IsReacting)
        {
            ApplyBetterGravity();
            return;
        }

        // The slide owns the fall: cap it and skip the extra gravity, or the clamp and
        // the fall multiplier fight each other and the slide stutters.
        if (IsWallSliding)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x,
                Mathf.Max(rb.linearVelocity.y, -wallSlideSpeed));
            return;
        }

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

        // The same grace idea as coyote time, for leaving a wall
        if (IsWallSliding) wallCoyoteCounter = wallCoyoteTime;
        else wallCoyoteCounter -= Time.deltaTime;
    }

    private void CheckEnvironment()
    {
        bool wasGrounded = IsGrounded;
        float fallSpeed = Mathf.Max(0f, -rb.linearVelocity.y);

        IsGrounded = Physics2D.OverlapBox(groundCheck.position, groundCheckSize, 0f, groundLayer);

        // Edge-triggered, so landing fires once rather than every frame on the floor
        if (IsGrounded && !wasGrounded) OnLanded?.Invoke(fallSpeed);

        IsTouchingWall = wallCheck != null &&
                         Physics2D.OverlapBox(wallCheck.position, wallCheckSize, 0f, wallLayer);

        // A slide needs the player to still be holding into the wall. Without that test
        // the Pepper sticks to every surface it so much as brushes past in mid-air.
        bool holdingIntoWall = Mathf.Abs(horizontalInput) > 0.01f && (horizontalInput > 0f) == isFacingRight;

        // A ledge is wall at hand height with nothing directly above it. The two probes are
        // stacked bands rather than the whole-body wallCheck, so the lip always ends up at
        // the hands - reusing wallCheck would catch a ledge level with the knees.
        // Only while falling and holding into it, so brushing a wall on the way up does not
        // stick, and only once the release lock is up, or letting go would re-catch it.
        if (!IsLedgeHanging
            && IsTouchingWall
            && !IsGrounded
            && holdingIntoWall
            && rb.linearVelocity.y <= 0f
            && Time.time >= ledgeLockUntil
            && ledgeCheck != null
            && !Physics2D.OverlapBox(ledgeCheck.position, ledgeCheckSize, 0f, wallLayer)
            && Physics2D.OverlapBox(HandProbePosition, ledgeCheckSize, 0f, wallLayer))
        {
            BeginLedgeHang();
        }

        IsWallSliding = IsTouchingWall && !IsGrounded && rb.linearVelocity.y < 0f
                        && holdingIntoWall && !IsLedgeHanging;
    }

    /// <summary>The band directly below the clear probe: where the lip has to be.</summary>
    private Vector2 HandProbePosition =>
        (Vector2)ledgeCheck.position + Vector2.down * ledgeCheckSize.y;

    private void BeginLedgeHang()
    {
        IsLedgeHanging = true;

        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 0f;

        // A ledge is footing. It gives back the dash the same way the ground does, or a
        // long climb would run the Pepper out of options halfway up.
        canDash = true;
        jumpBufferCounter = 0f;

        OnLedgeGrabbed?.Invoke();
    }

    private void ReleaseLedge()
    {
        IsLedgeHanging = false;
        rb.gravityScale = defaultGravityScale;
        ledgeLockUntil = Time.time + ledgeReleaseLock;
    }

    /// <summary>Hanging: the only inputs that mean anything are up and over, or let go.</summary>
    private void HandleLedgeHang()
    {
        rb.linearVelocity = Vector2.zero;

        bool pullingAway = Mathf.Abs(horizontalInput) > 0.01f && (horizontalInput > 0f) != isFacingRight;

        if (Input.GetAxisRaw("Vertical") < -0.5f || pullingAway)
        {
            ReleaseLedge();
            return;
        }

        if (!Input.GetButtonDown("Jump")) return;

        ReleaseLedge();

        rb.linearVelocity = new Vector2((isFacingRight ? 1f : -1f) * ledgeClimbForce.x, ledgeClimbForce.y);

        // The same press was buffered by HandleInput a few lines ago. Left alone it would
        // spend itself again as a ground jump the moment the Pepper lands.
        jumpBufferCounter = 0f;
        coyoteTimeCounter = 0f;

        OnLedgeClimbed?.Invoke();
        OnJumped?.Invoke();
    }

    private void HandleMovement()
    {
        // Right after a wall jump the input is ignored. Otherwise holding back toward the
        // wall cancels the push before it has carried the Pepper anywhere.
        if (Time.time < wallJumpLockUntil) return;

        float targetSpeed = horizontalInput * moveSpeed;
        float speedDifference = targetSpeed - rb.linearVelocity.x;
        float accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? acceleration : deceleration;

        // Changing direction has to fight the existing speed as well as build the new one,
        // so without a boost a turn feels like wading. This is the difference between a
        // character that pivots and one that slides.
        bool isTurning = Mathf.Abs(targetSpeed) > 0.01f &&
                         Mathf.Abs(rb.linearVelocity.x) > 0.01f &&
                         Mathf.Sign(targetSpeed) != Mathf.Sign(rb.linearVelocity.x);

        if (isTurning) accelRate *= turnAroundBoost;

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
        if (jumpBufferCounter <= 0f) return;

        // Off the ground and still within the wall's grace period: this is a wall jump.
        // Checked first, or the ground jump would swallow the press.
        if (!IsGrounded && wallCoyoteCounter > 0f)
        {
            PerformWallJump();
            return;
        }

        if (coyoteTimeCounter > 0f && jumpCooldownTimer <= 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);

            jumpBufferCounter = 0f;
            coyoteTimeCounter = 0f;
            jumpCooldownTimer = jumpCooldown;

            OnJumped?.Invoke();
        }
    }

    private void PerformWallJump()
    {
        float away = isFacingRight ? -1f : 1f;

        // Set the velocity outright rather than adding force: the push has to beat
        // whatever downward speed the slide had built up.
        rb.linearVelocity = new Vector2(away * wallJumpForce.x, wallJumpForce.y);

        // Turn to face the way the Pepper is now travelling
        if ((away > 0f) != isFacingRight) Flip();

        jumpBufferCounter = 0f;
        wallCoyoteCounter = 0f;
        coyoteTimeCounter = 0f;
        jumpCooldownTimer = jumpCooldown;
        wallJumpLockUntil = Time.time + wallJumpLockTime;
        IsWallSliding = false;

        OnWallJumped?.Invoke();
    }

    /// <summary>Fall faster than we rise, and cut the jump short when the button is released.</summary>
    private void ApplyBetterGravity()
    {
        // Hold back part of gravity near the top of the arc. That moment of hang is where
        // a player actually aims a jump, and it costs no height to give them.
        if (!IsGrounded && Mathf.Abs(rb.linearVelocity.y) < apexThreshold)
        {
            float heldBack = -Physics2D.gravity.y * rb.gravityScale * (1f - apexGravity);
            rb.linearVelocity += Vector2.up * heldBack * Time.fixedDeltaTime;
        }

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
        // The wall jump already set the facing; let it stand for the lock
        if (Time.time < wallJumpLockUntil) return;

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

        OnDashed?.Invoke();

        float originalGravity = rb.gravityScale;
        rb.gravityScale = 0f;

        // Dash where the stick is pointing, falling back to the facing when it is neutral.
        // Dashing backwards because the sprite had not turned yet feels like a bug.
        float dashDirection = Mathf.Abs(horizontalInput) > 0.01f
            ? Mathf.Sign(horizontalInput)
            : (isFacingRight ? 1f : -1f);

        if ((dashDirection > 0f) != isFacingRight) Flip();

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

        if (wallCheck != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(wallCheck.position, wallCheckSize);
        }

        if (ledgeCheck != null)
        {
            // Magenta must be clear and yellow must be solid for a ledge to be caught.
            // The pair has to straddle the lip, so place the marker just above the head.
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireCube(ledgeCheck.position, ledgeCheckSize);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(HandProbePosition, ledgeCheckSize);
        }
    }
}

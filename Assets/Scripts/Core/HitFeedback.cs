using System.Collections;
using UnityEngine;

/// <summary>
/// What a hit looks and feels like on the receiving end: a short colour flash, a knockback
/// impulse away from whoever landed it, and the long open stagger a parry earns.
/// Lives on the player as well as on enemies - taking a hit has to read on both sides, and
/// with no HUD yet this flash is the only sign the player gets that something landed.
///
/// While either reaction is running the AI must not steer, so it exposes
/// <see cref="IsReacting"/> for ToasterAI to check - the same pattern the player uses for
/// attacking, dashing and parrying. Without it the AI overwrites the velocity on the next
/// frame and the hit visually never happened.
/// </summary>
public class HitFeedback : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private HealthComponent health;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Rigidbody2D rb;
    [Tooltip("Optional. Set on the player so taking a hit shakes the camera, not just the sprite.")]
    [SerializeField] private GameFeelManager gameFeel;

    [Header("Camera (only when gameFeel is set)")]
    [SerializeField] private float hitStopDuration = 0.04f;
    [SerializeField] private float shakeDuration = 0.18f;
    [SerializeField] private float shakeMagnitude = 0.22f;

    [Header("Flash")]
    [SerializeField] private Color flashColor = Color.white;
    [SerializeField] private float flashDuration = 0.08f;

    [Header("Knockback")]
    [SerializeField] private float knockbackForce = 5f;
    [Tooltip("Upward component, so the hit pops the target rather than sliding it along the floor.")]
    [SerializeField] private float knockbackLift = 2.5f;
    [SerializeField] private float knockbackDuration = 0.18f;

    [Header("Tell")]
    [Tooltip("Flashed while this character is winding up, or the moment it spots the player. " +
             "A swing with no visible tell can only be parried from memory.")]
    [SerializeField] private Color tellColor = new Color(1f, 0.74f, 0.16f, 1f);
    [Tooltip("Blinks per second during the wind-up. A flat tint sits too close to the art " +
             "to be noticed in a fight; a pulse is what the eye actually catches.")]
    [SerializeField] private float tellPulsesPerSecond = 4.5f;

    [Header("Stagger (parry reward)")]
    [Tooltip("Tint held for the whole stagger, so an open enemy is readable at a glance.")]
    [SerializeField] private Color staggerColor = new Color(1f, 0.85f, 0.4f, 1f);

    /// <summary>True while knockback or stagger is playing out. The AI stands down.</summary>
    public bool IsReacting => isKnockedBack || isStaggered;

    /// <summary>True only during a parry stagger - the window to punish.</summary>
    public bool IsStaggered => isStaggered;

    private bool isKnockedBack;
    private bool isStaggered;

    private Color originalColor = Color.white;
    private Coroutine flashRoutine;
    private Coroutine knockbackRoutine;
    private Coroutine staggerRoutine;
    private Coroutine tellRoutine;

    private void Awake()
    {
        if (health == null) Debug.LogError($"[HitFeedback] health is not assigned on '{name}'.", this);
        if (spriteRenderer != null) originalColor = spriteRenderer.color;
    }

    private void OnEnable()
    {
        if (health != null) health.OnDamaged += HandleDamaged;
    }

    private void OnDisable()
    {
        if (health != null) health.OnDamaged -= HandleDamaged;
    }

    /// <summary>
    /// Hold this enemy open and defenceless. Called by PepperParry on a successful parry.
    /// </summary>
    public void Stagger(float duration)
    {
        if (duration <= 0f) return;

        if (staggerRoutine != null) StopCoroutine(staggerRoutine);
        staggerRoutine = StartCoroutine(StaggerRoutine(duration));
    }

    /// <summary>
    /// Hold the tell colour for a beat - a wind-up, or the moment the player is spotted.
    /// It is the difference between an attack the player can read and one they memorise.
    /// </summary>
    public void ShowTell(float duration)
    {
        if (duration <= 0f || spriteRenderer == null) return;

        if (tellRoutine != null) StopCoroutine(tellRoutine);
        tellRoutine = StartCoroutine(TellRoutine(duration));
    }

    private IEnumerator TellRoutine(float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            // A hit flash is the more urgent read and a stagger owns the sprite outright,
            // so the pulse stands aside for both instead of overwriting them every frame.
            // Lerping from originalColor rather than the live colour matters here: the tell
            // runs long enough that whatever happened to be on the sprite when it started
            // is not the colour to come back to.
            if (flashRoutine == null && !isStaggered)
            {
                float t = Mathf.PingPong(elapsed * tellPulsesPerSecond * 2f, 1f);
                spriteRenderer.color = Color.Lerp(originalColor, tellColor, t);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (flashRoutine == null && !isStaggered) spriteRenderer.color = originalColor;

        tellRoutine = null;
    }

    private void HandleDamaged(Vector2 sourcePosition)
    {
        // Taking a hit should be as loud as landing one, and shoved the way it threw us
        if (gameFeel != null)
        {
            Vector2 away = (Vector2)transform.position - sourcePosition;
            gameFeel.TriggerHitImpact(hitStopDuration, shakeDuration, shakeMagnitude, away);
        }

        if (spriteRenderer != null && !isStaggered)
        {
            if (flashRoutine != null) StopCoroutine(flashRoutine);
            flashRoutine = StartCoroutine(FlashRoutine());
        }

        if (rb != null)
        {
            if (knockbackRoutine != null) StopCoroutine(knockbackRoutine);
            knockbackRoutine = StartCoroutine(KnockbackRoutine(sourcePosition));
        }
    }

    private IEnumerator FlashRoutine()
    {
        spriteRenderer.color = flashColor;

        // Unscaled, so the flash is still visible through the hit stop freeze -
        // which is exactly the frame the player is looking at
        yield return new WaitForSecondsRealtime(flashDuration);

        // A stagger may have started during the flash; do not stomp its tint
        if (!isStaggered) spriteRenderer.color = originalColor;

        flashRoutine = null;
    }

    private IEnumerator KnockbackRoutine(Vector2 sourcePosition)
    {
        isKnockedBack = true;

        float direction = Mathf.Sign(transform.position.x - sourcePosition.x);

        // A hit landed from exactly the same column still pushes somewhere
        if (Mathf.Approximately(direction, 0f)) direction = 1f;

        rb.linearVelocity = new Vector2(direction * knockbackForce, knockbackLift);

        yield return new WaitForSeconds(knockbackDuration);

        isKnockedBack = false;
        knockbackRoutine = null;
    }

    private IEnumerator StaggerRoutine(float duration)
    {
        isStaggered = true;

        if (spriteRenderer != null) spriteRenderer.color = staggerColor;

        // Drop any momentum - a staggered enemy stands still and takes it
        if (rb != null) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        yield return new WaitForSeconds(duration);

        isStaggered = false;

        if (spriteRenderer != null) spriteRenderer.color = originalColor;

        staggerRoutine = null;
    }
}

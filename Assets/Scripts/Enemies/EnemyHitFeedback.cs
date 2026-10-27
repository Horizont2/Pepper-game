using System.Collections;
using UnityEngine;

/// <summary>
/// What a hit looks and feels like on the receiving end: a short colour flash and a
/// knockback impulse away from whoever landed it.
/// While knockback is running the AI must not steer, so it exposes
/// <see cref="IsKnockedBack"/> for ToasterAI to check - the same pattern the player
/// uses for attacking and dashing.
/// </summary>
public class EnemyHitFeedback : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private HealthComponent health;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Rigidbody2D rb;

    [Header("Flash")]
    [SerializeField] private Color flashColor = Color.white;
    [SerializeField] private float flashDuration = 0.08f;

    [Header("Knockback")]
    [SerializeField] private float knockbackForce = 5f;
    [Tooltip("Upward component, so the hit pops the target rather than sliding it along the floor.")]
    [SerializeField] private float knockbackLift = 2.5f;
    [SerializeField] private float knockbackDuration = 0.18f;

    /// <summary>True while the knockback impulse is still playing out.</summary>
    public bool IsKnockedBack { get; private set; }

    private Color originalColor = Color.white;
    private Coroutine flashRoutine;
    private Coroutine knockbackRoutine;

    private void Awake()
    {
        if (health == null) Debug.LogError($"[EnemyHitFeedback] health is not assigned on '{name}'.", this);
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

    private void HandleDamaged(Vector2 sourcePosition)
    {
        if (spriteRenderer != null)
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

        // Unscaled, so the flash is still visible through the hit stop freeze
        yield return new WaitForSecondsRealtime(flashDuration);

        spriteRenderer.color = originalColor;
        flashRoutine = null;
    }

    private IEnumerator KnockbackRoutine(Vector2 sourcePosition)
    {
        IsKnockedBack = true;

        float direction = Mathf.Sign(transform.position.x - sourcePosition.x);

        // A hit landed from exactly the same column still pushes somewhere
        if (Mathf.Approximately(direction, 0f)) direction = 1f;

        rb.linearVelocity = new Vector2(direction * knockbackForce, knockbackLift);

        yield return new WaitForSeconds(knockbackDuration);

        IsKnockedBack = false;
        knockbackRoutine = null;
    }
}

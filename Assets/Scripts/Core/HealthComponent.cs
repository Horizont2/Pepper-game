using UnityEngine;
using System;

/// <summary>
/// Shared health for the player and enemies. Raises events rather than acting
/// itself, so UI, audio and animation can react without coupling to each other.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class HealthComponent : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private float invulnerabilityDuration = 1f;

    private int currentHealth;

    // Negative infinity rather than 0: starting at 0 would leave the i-frame
    // window already open, making everything immune for the first second of play.
    private float lastDamageTime = float.NegativeInfinity;

    /// <summary>Current and max health, for UI to bind to.</summary>
    public event Action<int, int> OnHealthChanged;

    /// <summary>Raised on a hit that landed. Carries the world position the hit came
    /// from, so knockback and directional effects know which way to push.</summary>
    public event Action<Vector2> OnDamaged;

    public event Action OnDied;

    /// <summary>
    /// Lets a defensive move cancel a hit before any of it is applied - a parry today,
    /// a shield later. Each subscriber is asked in turn and the first to return true
    /// absorbs the hit entirely: no health lost, no i-frames spent, no events raised.
    /// Subscribers hold this component in a serialised field and register in OnEnable.
    /// </summary>
    public event Func<Vector2, bool> DamageAbsorber;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsAlive => currentHealth > 0;
    public float HealthFraction => maxHealth > 0 ? (float)currentHealth / maxHealth : 0f;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    /// <summary>Damage with no known source - knockback will have no direction.</summary>
    public void TakeDamage(int damageAmount) => TakeDamage(damageAmount, transform.position);

    public void TakeDamage(int damageAmount, Vector2 sourcePosition)
    {
        // Already dead - do not fire OnDied a second time
        if (!IsAlive) return;

        // Ask defensive moves first, before i-frames: a parry should pay out even if
        // the player happened to still be invulnerable from the previous hit.
        if (DamageAbsorber != null)
        {
            foreach (Delegate subscriber in DamageAbsorber.GetInvocationList())
            {
                if (((Func<Vector2, bool>)subscriber).Invoke(sourcePosition)) return;
            }
        }

        // Check for invulnerability frames (I-frames) to prevent instant multi-hits
        if (Time.time - lastDamageTime < invulnerabilityDuration) return;

        currentHealth = Mathf.Clamp(currentHealth - damageAmount, 0, maxHealth);
        lastDamageTime = Time.time;

        OnDamaged?.Invoke(sourcePosition);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// Kill outright, ignoring i-frames and any absorber. For things no defence applies
    /// to - a pit, a scripted death. A parry cannot save you from falling.
    /// </summary>
    public void Kill()
    {
        if (!IsAlive) return;

        currentHealth = 0;
        lastDamageTime = Time.time;

        OnDamaged?.Invoke(transform.position);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        Die();
    }

    /// <summary>Back to full health, with the i-frame window cleared. Used on respawn.</summary>
    public void Revive()
    {
        currentHealth = maxHealth;
        lastDamageTime = float.NegativeInfinity;

        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void Heal(int healAmount)
    {
        if (!IsAlive) return;

        currentHealth = Mathf.Clamp(currentHealth + healAmount, 0, maxHealth);

        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    private void Die()
    {
        OnDied?.Invoke();
        // The actual death logic (destroying object, playing animation)
        // should be handled by a separate script listening to OnDied.
    }
}

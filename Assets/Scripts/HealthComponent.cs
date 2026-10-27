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

    // Events for UI, audio, and animation systems to subscribe to
    public event Action<int, int> OnHealthChanged;
    public event Action OnDied;
    public event Action OnDamaged;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsAlive => currentHealth > 0;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damageAmount)
    {
        // Already dead - do not fire OnDied a second time
        if (!IsAlive) return;

        // Check for invulnerability frames (I-frames) to prevent instant multi-hits
        if (Time.time - lastDamageTime < invulnerabilityDuration) return;

        currentHealth = Mathf.Clamp(currentHealth - damageAmount, 0, maxHealth);
        lastDamageTime = Time.time;

        // Trigger events
        OnDamaged?.Invoke();
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
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

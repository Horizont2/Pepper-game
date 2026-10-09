using UnityEngine;
using System;

[RequireComponent(typeof(Collider2D))]
public class HealthComponent : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private float invulnerabilityDuration = 1f;

    private int currentHealth;
    private float lastDamageTime;

    // Events for UI, audio, and animation systems to subscribe to
    public event Action<int, int> OnHealthChanged;
    public event Action OnDied;
    public event Action OnDamaged;

    private void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damageAmount)
    {
        // Check for invulnerability frames (I-frames) to prevent instant multi-hits
        if (Time.time - lastDamageTime < invulnerabilityDuration) return;

        currentHealth -= damageAmount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
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
        currentHealth += healAmount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    private void Die()
    {
        OnDied?.Invoke();
        // The actual death logic (destroying object, playing animation) 
        // should be handled by a separate script listening to OnDied.
    }
}
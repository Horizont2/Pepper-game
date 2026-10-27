using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Drives one health bar from a <see cref="HealthComponent"/>. The same component serves
/// the player bar, a boss bar and a floating enemy bar - only the references differ.
///
/// Two fills rather than one: the front fill snaps toward the new value quickly, while the
/// delayed fill behind it waits a beat and then drains. That trailing red is what makes a
/// hit read as "that took a chunk" instead of the bar simply being shorter than before.
/// </summary>
public class HealthBarUI : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Left empty for a boss bar, which is given its target when the fight starts.")]
    [SerializeField] private HealthComponent target;
    [SerializeField] private Image fill;
    [Tooltip("Optional trailing fill behind the main one. Drives the chip-damage effect.")]
    [SerializeField] private Image delayedFill;
    [Tooltip("Optional. Shown and hidden with the target - used by the boss bar.")]
    [SerializeField] private GameObject root;

    [Header("Behaviour")]
    [SerializeField] private float fillSpeed = 10f;
    [SerializeField] private float delayedFillSpeed = 1.5f;
    [Tooltip("How long the trailing fill holds before it starts to drain.")]
    [SerializeField] private float trailDelay = 0.35f;
    [Tooltip("Hide the whole bar once the target is dead. Used by the boss bar, not the player's.")]
    [SerializeField] private bool hideOnDeath;

    private float targetFraction = 1f;
    private float trailHoldUntil;

    private void OnEnable()
    {
        Subscribe(target);
        SnapToTarget();
    }

    private void OnDisable()
    {
        Unsubscribe(target);
    }

    /// <summary>Point this bar at a different target - how a boss bar is handed its boss.</summary>
    public void SetTarget(HealthComponent newTarget)
    {
        Unsubscribe(target);
        target = newTarget;
        Subscribe(target);
        SnapToTarget();
    }

    private void Subscribe(HealthComponent h)
    {
        if (h == null) return;
        h.OnHealthChanged += HandleHealthChanged;
        h.OnDied += HandleDied;
    }

    private void Unsubscribe(HealthComponent h)
    {
        if (h == null) return;
        h.OnHealthChanged -= HandleHealthChanged;
        h.OnDied -= HandleDied;
    }

    /// <summary>Jump both fills to the current value, with no animation.</summary>
    private void SnapToTarget()
    {
        if (root != null) root.SetActive(target != null);

        targetFraction = target != null ? target.HealthFraction : 1f;

        if (fill != null) fill.fillAmount = targetFraction;
        if (delayedFill != null) delayedFill.fillAmount = targetFraction;
    }

    private void HandleHealthChanged(int current, int max)
    {
        targetFraction = max > 0 ? (float)current / max : 0f;
        trailHoldUntil = Time.unscaledTime + trailDelay;
    }

    private void HandleDied()
    {
        targetFraction = 0f;
        if (hideOnDeath && root != null) root.SetActive(false);
    }

    private void Update()
    {
        // Unscaled time throughout: the bar must keep animating during hit stop,
        // which is exactly when the player is looking at it.
        float dt = Time.unscaledDeltaTime;

        if (fill != null)
        {
            fill.fillAmount = Mathf.MoveTowards(fill.fillAmount, targetFraction, fillSpeed * dt);
        }

        if (delayedFill != null && Time.unscaledTime >= trailHoldUntil)
        {
            delayedFill.fillAmount = Mathf.MoveTowards(delayedFill.fillAmount, targetFraction, delayedFillSpeed * dt);
        }
    }
}

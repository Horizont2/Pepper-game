using UnityEngine;

/// <summary>
/// Publishes this player into a <see cref="PlayerLocator"/> asset so prefabs placed in the
/// scene can find it without searching for it. One component, one line of setup.
/// </summary>
[DefaultExecutionOrder(-100)]
public class PlayerRegistrar : MonoBehaviour
{
    [SerializeField] private PlayerLocator locator;
    [SerializeField] private HealthComponent health;

    private void Awake()
    {
        if (locator == null)
        {
            Debug.LogWarning($"[PlayerRegistrar] no locator assigned on '{name}'; enemies that rely on it will not find the player.", this);
            return;
        }

        // Runs ahead of everything else so an enemy's own Awake already sees a player
        locator.Register(transform, health);
    }

    private void OnDestroy()
    {
        if (locator != null) locator.Unregister(transform);
    }
}

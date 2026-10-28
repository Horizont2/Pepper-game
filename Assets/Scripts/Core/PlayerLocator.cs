using UnityEngine;

/// <summary>
/// A shared asset that holds whichever player is currently in the scene.
///
/// A prefab cannot hold a reference to a scene object, which meant every enemy had to be
/// wired by hand after being dropped in - and an enemy dragged in without that wiring just
/// stood there doing nothing. An asset solves it without a runtime search: the player
/// writes itself here on Awake and enemies read it, with both ends still pointing at
/// something assigned in the Inspector.
/// </summary>
[CreateAssetMenu(menuName = "Paprica/Player Locator", fileName = "PlayerLocator")]
public class PlayerLocator : ScriptableObject
{
    /// <summary>The live player, or null between scenes.</summary>
    public Transform Current { get; private set; }

    public HealthComponent Health { get; private set; }

    public void Register(Transform playerTransform, HealthComponent playerHealth)
    {
        Current = playerTransform;
        Health = playerHealth;
    }

    /// <summary>
    /// Clears only if this is still the registered player. An asset keeps its values between
    /// play sessions in the editor, so leaving a destroyed Transform here would hand every
    /// enemy a dead reference on the next run.
    /// </summary>
    public void Unregister(Transform playerTransform)
    {
        if (Current != playerTransform) return;

        Current = null;
        Health = null;
    }
}

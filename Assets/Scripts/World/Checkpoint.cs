using System;
using UnityEngine;

/// <summary>
/// Claims the spot the Pepper comes back to. Walk through it and it takes over from
/// whichever checkpoint held the role before.
///
/// Each one listens for the respawn point changing rather than keeping a list of its
/// siblings: a checkpoint only ever needs to know whether it is still the current one, and
/// asking that question of the player means no manager has to track them all.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Checkpoint : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The player's PlayerDeath. A scene object, so this is assigned per instance.")]
    [SerializeField] private PlayerDeath playerDeath;
    [Tooltip("Where the Pepper reappears. Leave empty to use this object's own position.")]
    [SerializeField] private Transform respawnAnchor;
    [SerializeField] private Collider2D trigger;
    [SerializeField] private LayerMask playerLayer;

    [Header("Visuals")]
    [Tooltip("Shown while this is the active checkpoint.")]
    [SerializeField] private GameObject litVisual;
    [Tooltip("Shown while it is not.")]
    [SerializeField] private GameObject unlitVisual;
    [Tooltip("Inactive child switched on for one burst at the moment it is claimed.")]
    [SerializeField] private GameObject activateFx;

    [Header("Behaviour")]
    [Tooltip("Top the Pepper up on arrival. Standard for a metroidvania rest point.")]
    [SerializeField] private bool healOnActivate = true;
    [SerializeField] private HealthComponent playerHealth;

    /// <summary>Raised the moment this one takes over. For audio.</summary>
    public event Action OnActivated;

    public bool IsActive { get; private set; }

    private Transform Anchor => respawnAnchor != null ? respawnAnchor : transform;

    private void Awake()
    {
        if (playerDeath == null)
            Debug.LogWarning($"[Checkpoint] playerDeath is not assigned on '{name}'; this checkpoint will do nothing.", this);

        if (trigger != null && !trigger.isTrigger)
            Debug.LogWarning($"[Checkpoint] the collider on '{name}' is not set as a trigger; the player will walk into it instead of through it.", this);

        ApplyVisualState();
    }

    private void OnEnable()
    {
        if (playerDeath != null) playerDeath.OnRespawnPointChanged += HandleRespawnPointChanged;
    }

    private void OnDisable()
    {
        if (playerDeath != null) playerDeath.OnRespawnPointChanged -= HandleRespawnPointChanged;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (IsActive || playerDeath == null) return;
        if ((playerLayer.value & (1 << other.gameObject.layer)) == 0) return;

        playerDeath.SetRespawnPoint(Anchor);

        if (healOnActivate && playerHealth != null)
        {
            playerHealth.Heal(playerHealth.MaxHealth);
        }

        if (activateFx != null) activateFx.SetActive(true);

        OnActivated?.Invoke();
    }

    private void HandleRespawnPointChanged(Transform newPoint)
    {
        IsActive = newPoint == Anchor;
        ApplyVisualState();
    }

    private void ApplyVisualState()
    {
        if (litVisual != null) litVisual.SetActive(IsActive);
        if (unlitVisual != null) unlitVisual.SetActive(!IsActive);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.44f, 0.66f, 0.36f);
        Gizmos.DrawWireSphere(Anchor.position, 0.12f);
        Gizmos.DrawLine(Anchor.position, Anchor.position + Vector3.up * 0.4f);
    }
}

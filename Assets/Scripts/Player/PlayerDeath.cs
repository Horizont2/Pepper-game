using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Death and respawn for the Pepper.
///
/// Health reaching zero is one way in; falling out of the level is the other, and a pit
/// kills outright - no parry, no i-frames, nothing to absorb it. Either way control is
/// handed back only after the death has been allowed to play, and every system that could
/// be mid-action is explicitly cancelled rather than left to resume into a fresh life.
/// </summary>
public class PlayerDeath : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private HealthComponent health;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private PepperController controller;
    [SerializeField] private PepperCombat combat;
    [SerializeField] private PepperParry parry;
    [SerializeField] private PepperAnimator animatorDriver;
    [Tooltip("Where the Pepper comes back. A scene object, so it is assigned on the instance.")]
    [SerializeField] private Transform respawnPoint;
    [Tooltip("Optional. Switched on as the Pepper dies and off again on respawn - it fires " +
             "itself when enabled. More of it, thrown further, than a hit throws off.")]
    [SerializeField] private GameObject deathDebris;
    [Tooltip("Optional. The Cinemachine camera, switched off and on again after a respawn.\n\n" +
             "A respawn teleports the Pepper, often several units up out of a pit. The camera " +
             "then eases after it and stops the moment it re-enters the composer's dead zone - " +
             "at its EDGE, not its middle - which on a dead zone half a screen tall leaves the " +
             "view sitting about a unit low for the rest of the life. Re-activating the camera " +
             "makes its Center On Activate setting put the Pepper back in the middle.\n\n" +
             "Typed as a plain Behaviour on purpose, so this file needs no Cinemachine reference.")]
    [SerializeField] private Behaviour cameraToRecentre;

    [Header("Timing")]
    [Tooltip("How long the death is held before the respawn. Death.anim runs about 1.1s.")]
    [SerializeField] private float deathHold = 1.2f;

    [Header("Falling")]
    [Tooltip("Below this world height the Pepper is gone, whatever its health says.")]
    [SerializeField] private float fallKillY = -6f;

    /// <summary>True from the moment of death until control comes back.</summary>
    public bool IsDead { get; private set; }

    /// <summary>For the HUD, audio and a future death screen to hang off.</summary>
    public event Action OnDied;
    public event Action OnRespawned;

    /// <summary>Raised when a checkpoint takes over, so the previous one can dim itself.</summary>
    public event Action<Transform> OnRespawnPointChanged;

    public Transform RespawnPoint => respawnPoint;

    /// <summary>Claim this as the place to come back to. Called by a checkpoint.</summary>
    public void SetRespawnPoint(Transform point)
    {
        if (point == null || point == respawnPoint) return;

        respawnPoint = point;
        OnRespawnPointChanged?.Invoke(point);
    }

    private void Awake()
    {
        if (health == null)
            Debug.LogError($"[PlayerDeath] health is not assigned on '{name}'; the Pepper can never die or respawn.", this);

        if (respawnPoint == null)
            Debug.LogWarning($"[PlayerDeath] respawnPoint is not assigned on '{name}'; the Pepper will revive where it fell.", this);
    }

    private void OnEnable()
    {
        if (health != null) health.OnDied += HandleDeath;
    }

    private void OnDisable()
    {
        if (health != null) health.OnDied -= HandleDeath;
    }

    private void Update()
    {
        if (IsDead || health == null || !health.IsAlive) return;

        // A pit is not damage - nothing defends against it
        if (transform.position.y < fallKillY) health.Kill();
    }

    private void HandleDeath()
    {
        if (IsDead) return;
        StartCoroutine(DeathRoutine());
    }

    private IEnumerator RecentreCamera()
    {
        cameraToRecentre.enabled = false;

        // A frame with no live camera, so switching it back on registers as an activation
        // rather than as nothing having changed
        yield return null;

        cameraToRecentre.enabled = true;
    }

    private IEnumerator DeathRoutine()
    {
        IsDead = true;
        OnDied?.Invoke();

        // The Pepper comes apart. Switched on rather than told to play, so the burst
        // decides for itself what that means - see DebrisBurst.playOnEnable.
        if (deathDebris != null) deathDebris.SetActive(true);

        // Cancel before disabling. A disabled component's coroutine is not guaranteed to
        // stop, and a dash left half-finished would restore its gravity change after the
        // respawn had already happened.
        if (controller != null) controller.CancelActions();
        if (combat != null) combat.CancelAttack();
        if (parry != null) parry.CancelParry();

        SetControlEnabled(false);

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            // Kinematic, or a death in mid-air keeps falling for the whole hold
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        // Real time: a kill often lands inside a hit stop, and the death should play
        // through the freeze rather than hang on one frame
        yield return new WaitForSecondsRealtime(deathHold);

        Respawn();
    }

    /// <summary>Put the Pepper back on its feet. Safe to call directly for a debug respawn.</summary>
    public void Respawn()
    {
        // Off again, or the next death switches on something that is already on and the
        // burst never replays
        if (deathDebris != null) deathDebris.SetActive(false);

        if (respawnPoint != null) transform.position = respawnPoint.position;

        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.linearVelocity = Vector2.zero;
        }

        if (health != null) health.Revive();
        if (animatorDriver != null) animatorDriver.ResetDeath();

        if (cameraToRecentre != null) StartCoroutine(RecentreCamera());

        SetControlEnabled(true);

        IsDead = false;
        OnRespawned?.Invoke();
    }

    private void SetControlEnabled(bool isEnabled)
    {
        if (controller != null) controller.enabled = isEnabled;
        if (combat != null) combat.enabled = isEnabled;
        if (parry != null) parry.enabled = isEnabled;
    }

    private void OnDrawGizmosSelected()
    {
        // The kill line, drawn across the level so it can be placed by eye
        Gizmos.color = new Color(1f, 0.3f, 0.2f, 0.8f);
        Vector3 at = new Vector3(transform.position.x, fallKillY, 0f);
        Gizmos.DrawLine(at + Vector3.left * 20f, at + Vector3.right * 20f);
    }
}

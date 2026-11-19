using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Something lying in the world to walk into: a resource, or healing.
///
/// Refusing to be collected is a feature, not an oversight. A health pickup taken at full
/// health is wasted, so by default it stays on the ground until it is worth something -
/// which is what lets a designer place one before a hard fight and trust it will be there.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Pickup : MonoBehaviour
{
    public enum PickupKind
    {
        Resource,
        Health,
    }

    [Header("What It Gives")]
    [SerializeField] private PickupKind kind = PickupKind.Resource;
    [SerializeField] private int amount = 1;
    [Tooltip("Collect it even at full health, wasting it. Off by default.")]
    [SerializeField] private bool consumeWhenFullHealth;

    [Header("References")]
    [SerializeField] private Collider2D trigger;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [Tooltip("The idle animation on the body. Switched off when taken: it drives the sprite " +
             "every frame, and leaving it running is the surest way for a pickup to come " +
             "back after it has been collected.")]
    [SerializeField] private Animator bodyAnimator;
    [Tooltip("Inactive child switched on when taken. The object clears itself afterwards.")]
    [SerializeField] private GameObject collectFx;
    [SerializeField] private LayerMask playerLayer;

    [Header("Idle Motion")]
    [Tooltip("Gentle bob so a pickup reads as collectable rather than as scenery.")]
    [SerializeField] private float bobHeight = 0.04f;
    [SerializeField] private float bobSpeed = 2f;

    [Header("Refusal")]
    [Tooltip("How high it hops when it will not be taken - a health pickup at full health.")]
    [SerializeField] private float refuseHeight = 0.09f;
    [SerializeField] private float refuseDuration = 0.22f;

    [Header("Timing")]
    [SerializeField] private float destroyDelay = 0.5f;

    /// <summary>Raised when taken, before the object clears itself. For audio and counters.</summary>
    public event Action<Pickup> OnCollected;

    public PickupKind Kind => kind;
    public int Amount => amount;

    private Vector3 restPosition;
    private float bobPhase;
    private bool isCollected;
    private bool isRefusing;

    private void Awake()
    {
        if (trigger == null) Debug.LogError($"[Pickup] trigger is not assigned on '{name}'.", this);
        else if (!trigger.isTrigger) Debug.LogWarning($"[Pickup] the collider on '{name}' is not set as a trigger; it will block the player instead of being picked up.", this);

        // Qualified: this file also uses System, where Random is a different type entirely
        // and the bare name will not compile.
        // Offset the bob per instance, or a row of pickups pulses in unison like a metronome.
        bobPhase = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
    }

    private void Start()
    {
        restPosition = transform.position;
    }

    private void Update()
    {
        if (isCollected || isRefusing || bobHeight <= 0f) return;

        float offset = Mathf.Sin(Time.time * bobSpeed + bobPhase) * bobHeight;
        transform.position = restPosition + new Vector3(0f, offset, 0f);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isCollected) return;
        if ((playerLayer.value & (1 << other.gameObject.layer)) == 0) return;

        // Only consume it if it actually did something
        if (!TryGive(other))
        {
            if (!isRefusing) StartCoroutine(RefuseRoutine());
            return;
        }

        Collect();
    }

    private bool TryGive(Collider2D collector)
    {
        if (kind == PickupKind.Health)
        {
            HealthComponent health = collector.GetComponent<HealthComponent>();
            if (health == null) return false;

            if (!consumeWhenFullHealth && health.CurrentHealth >= health.MaxHealth) return false;

            health.Heal(amount);
            return true;
        }

        ResourceCounter counter = collector.GetComponent<ResourceCounter>();
        if (counter == null) return false;

        counter.Add(amount);
        return true;
    }

    private void Collect()
    {
        isCollected = true;

        OnCollected?.Invoke(this);

        if (trigger != null) trigger.enabled = false;

        // Order matters: the animator goes first. It writes the sprite every frame, so
        // switching the renderer off underneath a running animator is asking for the body
        // to flicker back.
        if (bodyAnimator != null) bodyAnimator.enabled = false;
        if (spriteRenderer != null) spriteRenderer.enabled = false;

        if (collectFx != null) collectFx.SetActive(true);

        StartCoroutine(ClearAfterDelay());
    }

    /// <summary>
    /// A nudge for a pickup that refused. Health taken at full health is deliberately not
    /// consumed, but silence is indistinguishable from a broken trigger - the player walks
    /// into it, nothing happens, and they file a bug. A bounce says "not now" instead.
    /// </summary>
    private IEnumerator RefuseRoutine()
    {
        isRefusing = true;

        float elapsed = 0f;
        while (elapsed < refuseDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = elapsed / refuseDuration;
            float lift = Mathf.Sin(t * Mathf.PI) * refuseHeight;
            transform.position = restPosition + new Vector3(0f, lift, 0f);

            yield return null;
        }

        isRefusing = false;
    }

    private IEnumerator ClearAfterDelay()
    {
        // Real time, so a pickup taken during a hit stop still clears on schedule
        yield return new WaitForSecondsRealtime(destroyDelay);

        Destroy(gameObject);
    }
}

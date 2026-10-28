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
    [Tooltip("Inactive child switched on when taken. The object clears itself afterwards.")]
    [SerializeField] private GameObject collectFx;
    [SerializeField] private LayerMask playerLayer;

    [Header("Idle Motion")]
    [Tooltip("Gentle bob so a pickup reads as collectable rather than as scenery.")]
    [SerializeField] private float bobHeight = 0.04f;
    [SerializeField] private float bobSpeed = 2f;

    [Header("Timing")]
    [SerializeField] private float destroyDelay = 0.5f;

    /// <summary>Raised when taken, before the object clears itself. For audio and counters.</summary>
    public event Action<Pickup> OnCollected;

    public PickupKind Kind => kind;
    public int Amount => amount;

    private Vector3 restPosition;
    private float bobPhase;
    private bool isCollected;

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
        if (isCollected || bobHeight <= 0f) return;

        float offset = Mathf.Sin(Time.time * bobSpeed + bobPhase) * bobHeight;
        transform.position = restPosition + new Vector3(0f, offset, 0f);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isCollected) return;
        if ((playerLayer.value & (1 << other.gameObject.layer)) == 0) return;

        // Only consume it if it actually did something
        if (!TryGive(other)) return;

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
        if (spriteRenderer != null) spriteRenderer.enabled = false;
        if (collectFx != null) collectFx.SetActive(true);

        StartCoroutine(ClearAfterDelay());
    }

    private IEnumerator ClearAfterDelay()
    {
        // Real time, so a pickup taken during a hit stop still clears on schedule
        yield return new WaitForSecondsRealtime(destroyDelay);

        Destroy(gameObject);
    }
}

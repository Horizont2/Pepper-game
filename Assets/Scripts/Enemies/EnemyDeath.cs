using System.Collections;
using UnityEngine;

/// <summary>
/// Turns a dead enemy into a death: stops it behaving, stops it colliding, swaps the body
/// for a burst effect and clears it away.
///
/// The effect is a child object that ships inactive on the prefab and is switched on here,
/// rather than a prefab spawned at runtime. That keeps it visible and tunable in the
/// Inspector, costs no allocation at the exact moment the frame is busiest, and means a
/// designer can preview the burst by ticking one checkbox.
/// </summary>
public class EnemyDeath : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private HealthComponent health;
    [Tooltip("Switched off on death so the corpse stops chasing.")]
    [SerializeField] private ToasterAI ai;
    [SerializeField] private HitFeedback hitFeedback;
    [SerializeField] private Collider2D bodyCollider;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [Tooltip("Inactive child holding the death burst sprite and its animator. Enabled on death.")]
    [SerializeField] private GameObject deathFx;
    [Tooltip("Optional second inactive child holding a particle burst. Enabled on death too.")]
    [SerializeField] private GameObject deathParticles;

    [Header("Timing")]
    [Tooltip("How long the burst plays before the object is cleared away.")]
    [SerializeField] private float destroyDelay = 0.6f;
    [Tooltip("Hide the body as the burst starts. Off if the death animation is on the body itself.")]
    [SerializeField] private bool hideBodyOnDeath = true;

    private void OnEnable()
    {
        if (health != null) health.OnDied += HandleDeath;
    }

    private void OnDisable()
    {
        if (health != null) health.OnDied -= HandleDeath;
    }

    private void HandleDeath()
    {
        // Stop everything that could still act on a dead enemy. Order matters: the AI goes
        // first, because it writes velocity every frame and would otherwise keep walking
        // the corpse for one more frame.
        if (ai != null) ai.enabled = false;
        if (hitFeedback != null) hitFeedback.enabled = false;

        // Dead enemies neither block the player nor take further hits
        if (bodyCollider != null) bodyCollider.enabled = false;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        if (hideBodyOnDeath && spriteRenderer != null) spriteRenderer.enabled = false;

        // Both bursts ship inactive on the prefab and start themselves when switched on
        if (deathFx != null) deathFx.SetActive(true);
        if (deathParticles != null) deathParticles.SetActive(true);

        StartCoroutine(DestroyAfterDelay());
    }

    private IEnumerator DestroyAfterDelay()
    {
        // Real time: a kill often lands inside a hit stop, and the burst should play
        // through the freeze rather than hang on one frame until time resumes.
        yield return new WaitForSecondsRealtime(destroyDelay);

        Destroy(gameObject);
    }
}

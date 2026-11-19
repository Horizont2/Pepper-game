using System.Collections;
using UnityEngine;

/// <summary>
/// The "!" that pops over an enemy the instant it notices the player.
///
/// The alert beat already existed - the enemy stops, colours itself and hesitates before
/// charging - but all of that happens ON the enemy, and at a glance across a busy screen a
/// sprite changing tint is easy to miss. A mark above the head is read without looking at
/// it, which is the point: the player needs to know they have been seen before the thing
/// arrives, not as it arrives.
///
/// It is a child object on the prefab rather than something spawned when the alert fires.
/// Same reason as every other effect here: no allocation on the frame the fight starts,
/// the whole thing is visible and tunable in the Inspector, and an artist can preview it
/// by ticking one box.
///
/// It listens to <see cref="ToasterAI.OnAlerted"/>, which has been sitting there waiting
/// for exactly this since the alert beat was added.
/// </summary>
public class AlertMark : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ToasterAI ai;
    [SerializeField] private SpriteRenderer mark;

    [Header("Pop")]
    [Tooltip("How long the mark takes to snap in.")]
    [SerializeField] private float popDuration = 0.16f;
    [Tooltip("How far past full size it goes before settling. This overshoot is the whole " +
             "difference between a mark that appears and a mark that pops.")]
    [SerializeField] private float overshoot = 3.2f;

    [Header("Hold and go")]
    [SerializeField] private float holdDuration = 0.45f;
    [SerializeField] private float fadeDuration = 0.22f;
    [Tooltip("How far it drifts up while fading, in world units. A mark that fades in place " +
             "looks switched off; one that lifts away looks spent.")]
    [SerializeField] private float riseDistance = 0.09f;

    private Vector3 restPosition;
    private Coroutine routine;

    private void Awake()
    {
        // Captured before anything can bail out, because OnDisable restores it
        restPosition = transform.localPosition;

        if (mark == null)
        {
            Debug.LogWarning($"[AlertMark] mark is not assigned on '{name}'; nothing will show.", this);
            enabled = false;
            return;
        }

        if (ai == null)
            Debug.LogWarning($"[AlertMark] ai is not assigned on '{name}'; the mark will never be triggered.", this);

        mark.enabled = false;
    }

    private void OnEnable()
    {
        if (ai != null) ai.OnAlerted += Show;
    }

    private void OnDisable()
    {
        if (ai != null) ai.OnAlerted -= Show;

        // Dying mid-alert must not leave the mark hanging over the corpse. Stopped rather
        // than forgotten: a disabled component only pauses its coroutines, so dropping the
        // handle would leave the old one to pick up where it left off on re-enable.
        StopAllCoroutines();
        routine = null;

        if (mark != null) mark.enabled = false;
        transform.localPosition = restPosition;
    }

    /// <summary>Pop the mark. Re-alerting restarts it rather than queueing a second one.</summary>
    public void Show()
    {
        if (mark == null) return;

        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(ShowRoutine());
    }

    private IEnumerator ShowRoutine()
    {
        transform.localPosition = restPosition;
        SetAlpha(1f);
        mark.enabled = true;

        // Unscaled throughout: an alert can land inside a hit stop from a blow that was
        // already in the air, and the mark should not stall on its first frame.
        float elapsed = 0f;
        while (elapsed < popDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            transform.localScale = Vector3.one * EaseOutBack(Mathf.Clamp01(elapsed / popDuration));
            yield return null;
        }

        transform.localScale = Vector3.one;

        yield return new WaitForSecondsRealtime(holdDuration);

        elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(elapsed / fadeDuration);
            SetAlpha(1f - t);
            transform.localPosition = restPosition + Vector3.up * (riseDistance * t);

            yield return null;
        }

        mark.enabled = false;
        transform.localPosition = restPosition;
        routine = null;
    }

    /// <summary>Standard back ease: 0 to 1, passing above 1 on the way.</summary>
    private float EaseOutBack(float t)
    {
        float back = t - 1f;
        return 1f + back * back * ((overshoot + 1f) * back + overshoot);
    }

    private void SetAlpha(float alpha)
    {
        Color color = mark.color;
        color.a = alpha;
        mark.color = color;
    }
}

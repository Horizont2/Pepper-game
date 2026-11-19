using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Keeps a light from sitting at one dead value: the slow breathing of a room light, or the
/// unsteady flicker of a gas ring under a pan.
///
/// Two sines at unrelated speeds rather than one, plus noise. A single sine is a pulse and
/// the eye locks onto its period within a couple of seconds; layering rates that do not
/// divide into each other never visibly repeats, which is the whole job.
///
/// Drives a URP <see cref="Light2D"/> and a <see cref="SpriteRenderer"/>, either or both,
/// so the same component serves the lamp and the glowing sprite under it.
/// </summary>
public class AmbientLightPulse : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Light2D light2D;
    [Tooltip("Optional. Its alpha is driven alongside the light - the visible flame or " +
             "window pane that the light is supposed to be coming out of.")]
    [SerializeField] private SpriteRenderer glow;

    [Header("Range")]
    [Tooltip("Multiplied against whatever the light and the sprite were authored at, so the " +
             "brightness stays an art decision and this only decides how much it wavers.")]
    [SerializeField] private float minMultiplier = 0.88f;
    [SerializeField] private float maxMultiplier = 1.06f;

    [Header("Rates")]
    [Tooltip("The slow swell, in cycles per second.")]
    [SerializeField] private float slowSpeed = 0.23f;
    [Tooltip("The faster waver on top. Keep it an awkward ratio to the slow one - 3.7, not " +
             "4 - or the two line up and the pattern becomes obvious.")]
    [SerializeField] private float fastSpeed = 1.7f;
    [Range(0f, 1f)]
    [Tooltip("How much of the movement comes from the fast layer. Low for a room light, " +
             "high for a flame.")]
    [SerializeField] private float fastWeight = 0.35f;
    [Tooltip("Unrepeating jitter over the top. A gas ring wants some; a window does not.")]
    [Range(0f, 1f)]
    [SerializeField] private float noiseWeight = 0.2f;
    [SerializeField] private float noiseSpeed = 4f;

    private float baseIntensity = 1f;
    private float baseAlpha = 1f;
    private float seed;

    private void Awake()
    {
        if (light2D == null && glow == null)
        {
            Debug.LogWarning($"[AmbientLightPulse] neither light2D nor glow is assigned on '{name}'; it has nothing to drive.", this);
            enabled = false;
            return;
        }

        if (light2D != null) baseIntensity = light2D.intensity;
        if (glow != null) baseAlpha = glow.color.a;

        // So two lamps in the same room are never in step
        seed = Random.Range(0f, 100f);
    }

    private void Update()
    {
        float t = Time.time;

        float slow = Mathf.Sin((t * slowSpeed + seed) * Mathf.PI * 2f);
        float fast = Mathf.Sin((t * fastSpeed + seed) * Mathf.PI * 2f);
        float noise = Mathf.PerlinNoise(seed, t * noiseSpeed) * 2f - 1f;

        float blended = Mathf.Lerp(slow, fast, fastWeight);
        blended = Mathf.Lerp(blended, noise, noiseWeight);

        // -1..1 to 0..1, then across the authored range
        float multiplier = Mathf.Lerp(minMultiplier, maxMultiplier, (blended + 1f) * 0.5f);

        if (light2D != null) light2D.intensity = baseIntensity * multiplier;

        if (glow == null) return;

        Color color = glow.color;
        color.a = Mathf.Clamp01(baseAlpha * multiplier);
        glow.color = color;
    }
}

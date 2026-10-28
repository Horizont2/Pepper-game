using UnityEngine;

/// <summary>
/// Impact screen shake that survives having the camera driven by something else.
/// It runs after anything on the default execution order - CinemachineBrain included,
/// which rewrites the camera transform every LateUpdate - and lays its offset on top
/// of whatever wrote the position that frame. That keeps it working whether the camera
/// is driven by Cinemachine, by CameraFollow, or by nothing at all.
/// </summary>
[DefaultExecutionOrder(1000)]
public class CameraShake : MonoBehaviour
{
    [Tooltip("Shake fades out across its duration instead of cutting off abruptly.")]
    [SerializeField] private AnimationCurve falloff = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

    private float duration;
    private float magnitude;
    private float elapsed;
    private bool isShaking;

    private Vector2 bias;
    private Vector3 lastAppliedOffset;
    private Vector3 lastWrittenPosition;

    /// <summary>Start a shake. A weaker hit will not cut a stronger one short.</summary>
    public void Shake(float shakeDuration, float shakeMagnitude) =>
        Shake(shakeDuration, shakeMagnitude, Vector2.zero);

    /// <summary>
    /// As above, but biased along a direction. A shake that leans the way the blow went
    /// reads as an impact; an evenly random one reads as noise.
    /// Pass <see cref="Vector2.zero"/> for the undirected version.
    /// </summary>
    public void Shake(float shakeDuration, float shakeMagnitude, Vector2 direction)
    {
        if (shakeDuration <= 0f || shakeMagnitude <= 0f) return;

        if (isShaking && shakeMagnitude < CurrentStrength()) return;

        duration = shakeDuration;
        magnitude = shakeMagnitude;
        elapsed = 0f;
        isShaking = true;
        bias = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.zero;
    }

    private float CurrentStrength()
    {
        if (!isShaking || duration <= 0f) return 0f;
        return magnitude * falloff.Evaluate(elapsed / duration);
    }

    private void LateUpdate()
    {
        // If nothing else moved the camera since our last write, our previous offset is
        // still sitting on the transform. Take it back out before adding the next one,
        // otherwise the shake accumulates and slowly walks the camera off target.
        if (transform.position == lastWrittenPosition)
        {
            transform.position -= lastAppliedOffset;
        }

        lastAppliedOffset = Vector3.zero;

        if (!isShaking)
        {
            lastWrittenPosition = transform.position;
            return;
        }

        // Unscaled, so the shake keeps moving during hit stop
        elapsed += Time.unscaledDeltaTime;

        if (elapsed >= duration)
        {
            isShaking = false;
            lastWrittenPosition = transform.position;
            return;
        }

        float strength = CurrentStrength();
        Vector3 offset;

        if (bias == Vector2.zero)
        {
            offset = new Vector3(Random.Range(-1f, 1f) * strength, Random.Range(-1f, 1f) * strength, 0f);
        }
        else
        {
            // Mostly along the blow, with a little scatter across it so it is a shake
            // rather than a straight line
            Vector2 across = new Vector2(-bias.y, bias.x);
            Vector2 shaken = bias * (Random.Range(-1f, 1f) * strength)
                           + across * (Random.Range(-1f, 1f) * strength * 0.35f);

            offset = new Vector3(shaken.x, shaken.y, 0f);
        }

        transform.position += offset;

        lastAppliedOffset = offset;
        lastWrittenPosition = transform.position;
    }
}

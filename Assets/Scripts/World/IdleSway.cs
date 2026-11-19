using UnityEngine;

/// <summary>
/// A slow sine on a prop's position and rotation: a hanging pan swinging, a plant breathing,
/// a sign rocking. Nothing in a room should be perfectly still.
///
/// Measured against the local position recorded at startup rather than accumulated, for the
/// same reason <see cref="ParallaxLayer"/> does it: a per-frame offset added to the current
/// position drifts, and anything that moves the object once leaves it permanently displaced.
///
/// Give each copy a different phase, or a row of identical props sways in unison and reads
/// as one object. <see cref="randomisePhase"/> does that without anyone having to remember.
/// </summary>
public class IdleSway : MonoBehaviour
{
    [Header("Motion")]
    [Tooltip("How far it moves, in world units, on each axis.")]
    [SerializeField] private Vector2 positionAmplitude = new Vector2(0f, 0.035f);
    [Tooltip("How far it rocks, in degrees.")]
    [SerializeField] private float rotationAmplitude = 2.5f;
    [Tooltip("Cycles per second. Below about 0.5 reads as breathing; above 1.5 as nervous.")]
    [SerializeField] private float frequency = 0.45f;

    [Header("Phase")]
    [Tooltip("Start somewhere random in the cycle. Leave on unless several objects are " +
             "meant to move together.")]
    [SerializeField] private bool randomisePhase = true;
    [Tooltip("Used when the phase is not randomised, so a set can be hand-staggered.")]
    [Range(0f, 1f)]
    [SerializeField] private float phase;

    [Tooltip("Rotate a quarter-cycle behind the movement. A pan that tilts as it reaches the " +
             "end of its swing rather than at the middle of it looks like it has weight.")]
    [SerializeField] private bool rotationLagsPosition = true;

    private Vector3 startPosition;
    private Quaternion startRotation;
    private float offset;

    private void Awake()
    {
        startPosition = transform.localPosition;
        startRotation = transform.localRotation;
        offset = randomisePhase ? Random.Range(0f, Mathf.PI * 2f) : phase * Mathf.PI * 2f;
    }

    private void Update()
    {
        float t = Time.time * frequency * Mathf.PI * 2f + offset;

        transform.localPosition = startPosition + new Vector3(
            Mathf.Sin(t) * positionAmplitude.x,
            Mathf.Cos(t) * positionAmplitude.y,
            0f);

        if (Mathf.Approximately(rotationAmplitude, 0f)) return;

        float turn = Mathf.Sin(rotationLagsPosition ? t - Mathf.PI * 0.5f : t) * rotationAmplitude;
        transform.localRotation = startRotation * Quaternion.Euler(0f, 0f, turn);
    }
}

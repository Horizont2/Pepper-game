using UnityEngine;

/// <summary>
/// A field of slow-moving motes that always fills the view: spice dust and flour hanging in
/// the kitchen air.
///
/// It is the cheapest thing that stops a 2D scene reading as a flat backdrop with a
/// character pasted on it. Nothing here is a gameplay element - the point is that the air
/// between the player and the background is not empty.
///
/// Like <see cref="DebrisBurst"/>, the motes are child SpriteRenderers placed in the prefab
/// rather than anything spawned, and they are driven in world space. They WRAP around the
/// follow target instead of being respawned, so a fixed handful covers a level of any size:
/// a mote that leaves one edge of the box comes back in at the opposite one. Make the box
/// comfortably larger than the camera view and the wrap always happens off screen.
///
/// Scaled time, unlike the impact effects. When a hit freezes the game the dust should
/// freeze with it - that stillness is most of what sells the hit stop.
/// </summary>
public class AmbientDrifters : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Motes, one per child renderer. Twenty or so is plenty; this is texture, not a crowd.")]
    [SerializeField] private SpriteRenderer[] motes;
    [Tooltip("Picked per mote at startup. A soft one and a hard one is enough variety.")]
    [SerializeField] private Sprite[] variants;
    [Tooltip("What the field stays centred on. The camera, normally - the motes wrap around " +
             "it, so wherever it goes the air is already full.")]
    [SerializeField] private Transform followTarget;

    [Header("Field")]
    [Tooltip("The box motes wrap inside, in world units, centred on the follow target. Must " +
             "be bigger than the camera view or the wrap happens in plain sight. The view is " +
             "about 7.1 x 4.0.")]
    [SerializeField] private Vector2 area = new Vector2(9.5f, 6f);

    [Header("Look")]
    [Tooltip("Picked per mote. Warm, low-contrast colours: dust is lit by the room, it does " +
             "not glow on its own.")]
    [SerializeField] private Color[] tints = {
        new Color(1f, 0.92f, 0.78f, 1f),
        new Color(1f, 0.80f, 0.56f, 1f),
        new Color(0.95f, 0.62f, 0.42f, 1f),
    };
    [SerializeField] private Vector2 scaleRange = new Vector2(0.35f, 1f);
    [Tooltip("How visible a mote is at its brightest. Keep it low - dust you notice is dirt.")]
    [SerializeField] private Vector2 alphaRange = new Vector2(0.12f, 0.38f);
    [Tooltip("How fast a mote breathes in and out of visibility, in cycles per second.")]
    [SerializeField] private float twinkleSpeed = 0.35f;

    [Header("Drift")]
    [Tooltip("Sideways speed range. A gentle draught across the room.")]
    [SerializeField] private Vector2 driftX = new Vector2(-0.14f, 0.06f);
    [Tooltip("Vertical speed range. Mostly upward - warm air off a stove.")]
    [SerializeField] private Vector2 driftY = new Vector2(0.02f, 0.11f);
    [Tooltip("How far a mote wanders sideways off its drift, in world units.")]
    [SerializeField] private float swayAmplitude = 0.16f;
    [SerializeField] private float swaySpeed = 0.5f;
    [Tooltip("Tie speed to size, so the big near motes outrun the small far ones. This is " +
             "the whole illusion of depth in the field; at 0 it flattens into a sheet.")]
    [Range(0f, 1f)]
    [SerializeField] private float sizeDrivesSpeed = 0.7f;

    private Vector2[] positions;
    private Vector2[] drifts;
    private float[] phases;
    private float[] twinklePhases;
    private float[] alphas;
    private float[] swayBase;

    private void Awake()
    {
        if (motes == null || motes.Length == 0)
        {
            Debug.LogWarning($"[AmbientDrifters] '{name}' has no motes assigned; the air will stay empty.", this);
            enabled = false;
            return;
        }

        if (followTarget == null)
            Debug.LogWarning($"[AmbientDrifters] followTarget is not assigned on '{name}'; the field will not follow the camera and the player will drift out of it.", this);

        int n = motes.Length;
        positions = new Vector2[n];
        drifts = new Vector2[n];
        phases = new float[n];
        twinklePhases = new float[n];
        alphas = new float[n];
        swayBase = new float[n];
    }

    // Start, not Awake: the camera is placed by Cinemachine, and scattering the field around
    // wherever it happened to be serialised would put the first frame's dust in the wrong room.
    private void Start() => Scatter();

    private void Scatter()
    {
        Vector2 centre = Centre;

        for (int i = 0; i < motes.Length; i++)
        {
            SpriteRenderer mote = motes[i];
            if (mote == null) continue;

            if (variants != null && variants.Length > 0)
                mote.sprite = variants[Random.Range(0, variants.Length)];

            float scale = Random.Range(scaleRange.x, scaleRange.y);
            mote.transform.localScale = Vector3.one * scale;

            // Near motes move more than far ones. Lerping from 1 rather than multiplying by
            // the scale keeps the small ones from stopping dead at low sizes.
            float speed = Mathf.Lerp(1f, scale / Mathf.Max(0.0001f, scaleRange.y), sizeDrivesSpeed);

            drifts[i] = new Vector2(Random.Range(driftX.x, driftX.y),
                                    Random.Range(driftY.x, driftY.y)) * speed;

            positions[i] = centre + new Vector2(Random.Range(-area.x, area.x) * 0.5f,
                                                Random.Range(-area.y, area.y) * 0.5f);

            phases[i] = Random.Range(0f, Mathf.PI * 2f);
            twinklePhases[i] = Random.Range(0f, Mathf.PI * 2f);
            alphas[i] = Random.Range(alphaRange.x, alphaRange.y);
            swayBase[i] = positions[i].x;

            if (tints != null && tints.Length > 0)
            {
                Color tint = tints[Random.Range(0, tints.Length)];
                tint.a = alphas[i];
                mote.color = tint;
            }

            mote.transform.position = positions[i];
            mote.enabled = true;
        }
    }

    private Vector2 Centre => followTarget != null ? (Vector2)followTarget.position : (Vector2)transform.position;

    private void Update()
    {
        if (positions == null) return;

        float dt = Time.deltaTime;
        Vector2 centre = Centre;
        Vector2 half = area * 0.5f;

        for (int i = 0; i < motes.Length; i++)
        {
            SpriteRenderer mote = motes[i];
            if (mote == null) continue;

            positions[i] += drifts[i] * dt;
            phases[i] += swaySpeed * dt;

            // Wrapped against the target, not against world coordinates, so the field
            // travels with the camera and a mote that leaves one edge is already
            // reappearing at the other before anyone could look there.
            Vector2 local = positions[i] - centre;
            local.x = Mathf.Repeat(local.x + half.x, area.x) - half.x;
            local.y = Mathf.Repeat(local.y + half.y, area.y) - half.y;
            positions[i] = centre + local;
            swayBase[i] = positions[i].x;

            Vector2 drawn = positions[i];
            drawn.x += Mathf.Sin(phases[i]) * swayAmplitude;
            mote.transform.position = drawn;

            twinklePhases[i] += twinkleSpeed * Mathf.PI * 2f * dt;

            Color color = mote.color;
            // Never all the way out: a mote that blinks off entirely reads as a dead pixel
            color.a = alphas[i] * Mathf.Lerp(0.45f, 1f, (Mathf.Sin(twinklePhases[i]) + 1f) * 0.5f);
            mote.color = color;
        }
    }

    private void OnDrawGizmosSelected()
    {
        // The wrap box. Anything inside the camera view here will be seen popping.
        Gizmos.color = new Color(1f, 0.85f, 0.4f, 0.6f);
        Vector3 centre = followTarget != null ? followTarget.position : transform.position;
        Gizmos.DrawWireCube(centre, new Vector3(area.x, area.y, 0f));
    }
}

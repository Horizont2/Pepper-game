using UnityEngine;

/// <summary>
/// Throws a handful of small pieces out of a point: bits of pepper when the Pepper is hit,
/// crumbs when something breaks.
///
/// Deliberately not a ParticleSystem. The pieces are ordinary child SpriteRenderers placed
/// in the prefab, which keeps to the rule that nothing is created at runtime, makes every
/// piece visible and tweakable in the Inspector, and means a burst can be re-skinned by
/// dropping different sprites into one array rather than editing a particle module.
///
/// Pieces are simulated in WORLD space even though they are children. Debris that follows
/// the character it came off reads as being glued to them; once a bit is in the air it has
/// nothing more to do with whoever was hit. Writing transform.position every frame does
/// that without reparenting anything.
///
/// Unscaled time throughout, so the burst keeps moving through the hit stop - which is the
/// exact moment it is meant to be looked at.
/// </summary>
public class DebrisBurst : MonoBehaviour
{
    [Header("Pieces")]
    [Tooltip("Child renderers, one per piece. How many there are is how big the burst is.")]
    [SerializeField] private SpriteRenderer[] pieces;
    [Tooltip("Picked at random per piece, per burst. Two or three shapes is enough; one " +
             "shape repeated eight times reads as a pattern rather than debris.")]
    [SerializeField] private Sprite[] variants;

    [Header("Behaviour")]
    [Tooltip("Fire as soon as the object is switched on. How a burst that ships inactive on " +
             "a prefab - a death, a breakage - goes off without anything having to call it.")]
    [SerializeField] private bool playOnEnable;

    [Header("Throw")]
    [SerializeField] private float minSpeed = 1.6f;
    [SerializeField] private float maxSpeed = 3.4f;
    [Tooltip("Half-angle of the cone thrown away from the blow, in degrees. 180 is a full " +
             "circle, which is what an undirected burst uses.")]
    [SerializeField] private float spreadDegrees = 55f;
    [Tooltip("Lift added on top of the throw, so pieces arc instead of spraying flat.")]
    [SerializeField] private float upwardBias = 1.5f;
    [Tooltip("Pieces start scattered within this radius rather than all from one point. " +
             "Without it the first frame of a burst is a single dot that then opens up, " +
             "which is the frame the hit stop holds you on.")]
    [SerializeField] private float originRadius = 0.06f;

    [Header("Flight")]
    [SerializeField] private float gravity = 9f;
    [Tooltip("Air resistance per second. Takes the speed off the throw so pieces settle " +
             "rather than carrying on at the speed they left.")]
    [SerializeField] private float drag = 1.8f;
    [SerializeField] private float spinDegreesPerSecond = 420f;

    [Header("Life")]
    [SerializeField] private float lifetime = 0.55f;
    [Tooltip("Fraction either side of the lifetime. Pieces that all vanish on the same " +
             "frame read as a switch being thrown.")]
    [Range(0f, 0.9f)]
    [SerializeField] private float lifetimeVariance = 0.35f;
    [Tooltip("Fraction of a piece's life before it starts fading out.")]
    [Range(0f, 1f)]
    [SerializeField] private float fadeAfter = 0.55f;

    [Header("Size")]
    [SerializeField] private float minScale = 0.7f;
    [SerializeField] private float maxScale = 1.15f;
    [Tooltip("Scale multiplier at the end of a piece's life. Slightly under 1 makes the " +
             "burst collapse in on itself as it fades.")]
    [SerializeField] private float endScaleMultiplier = 0.75f;

    // Runtime only, one entry per piece. Parallel arrays rather than a struct array so
    // there is nothing to allocate when a burst fires.
    private Vector2[] positions;
    private Vector2[] velocities;
    private float[] spins;
    private float[] ages;
    private float[] lifetimes;
    private float[] scales;
    private int alive;

    private void Awake()
    {
        if (pieces == null || pieces.Length == 0)
        {
            Debug.LogWarning($"[DebrisBurst] '{name}' has no pieces assigned; it will never show anything.", this);
            return;
        }

        positions = new Vector2[pieces.Length];
        velocities = new Vector2[pieces.Length];
        spins = new float[pieces.Length];
        ages = new float[pieces.Length];
        lifetimes = new float[pieces.Length];
        scales = new float[pieces.Length];

        // Start hidden. The burst object itself stays active so there is no enable dance
        // to get wrong; Update costs nothing while no piece is alive.
        foreach (SpriteRenderer piece in pieces)
        {
            if (piece != null) piece.enabled = false;
        }
    }

    private void OnEnable()
    {
        if (playOnEnable) Play();
    }

    /// <summary>Throw everything outward in all directions.</summary>
    public void Play() => Play(Vector2.zero);

    /// <summary>
    /// Throw the pieces away from a blow. Pass the direction the hit pushed in - the same
    /// vector the knockback uses - or <see cref="Vector2.zero"/> for an even scatter.
    /// </summary>
    public void Play(Vector2 away)
    {
        if (pieces == null || pieces.Length == 0 || positions == null) return;

        bool directed = away.sqrMagnitude > 0.0001f;
        float baseAngle = directed ? Mathf.Atan2(away.y, away.x) * Mathf.Rad2Deg : 90f;
        float spread = directed ? spreadDegrees : 180f;

        Vector2 origin = transform.position;

        for (int i = 0; i < pieces.Length; i++)
        {
            SpriteRenderer piece = pieces[i];
            if (piece == null) continue;

            if (variants != null && variants.Length > 0)
            {
                piece.sprite = variants[Random.Range(0, variants.Length)];
            }

            float angle = (baseAngle + Random.Range(-spread, spread)) * Mathf.Deg2Rad;
            float speed = Random.Range(minSpeed, maxSpeed);

            positions[i] = origin + Random.insideUnitCircle * originRadius;
            velocities[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed
                          + Vector2.up * upwardBias;
            spins[i] = Random.Range(-spinDegreesPerSecond, spinDegreesPerSecond);
            ages[i] = 0f;
            lifetimes[i] = Mathf.Max(0.05f, lifetime * Random.Range(1f - lifetimeVariance, 1f + lifetimeVariance));
            scales[i] = Random.Range(minScale, maxScale);

            Transform t = piece.transform;
            t.position = positions[i];
            t.localScale = Vector3.one * scales[i];

            // Local, not world: the Pepper turns by mirroring its own transform, and
            // decomposing a world rotation out of a negatively scaled parent is where
            // Unity starts handing back rotations nobody asked for. Mirrored debris is
            // not something anyone can see anyway.
            t.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

            Color color = piece.color;
            color.a = 1f;
            piece.color = color;
            piece.enabled = true;
        }

        alive = pieces.Length;
    }

    private void Update()
    {
        if (alive <= 0 || positions == null) return;

        float dt = Time.unscaledDeltaTime;
        int stillAlive = 0;

        for (int i = 0; i < pieces.Length; i++)
        {
            SpriteRenderer piece = pieces[i];
            if (piece == null || !piece.enabled) continue;

            ages[i] += dt;

            if (ages[i] >= lifetimes[i])
            {
                piece.enabled = false;
                continue;
            }

            stillAlive++;

            velocities[i] += Vector2.down * gravity * dt;
            velocities[i] *= Mathf.Max(0f, 1f - drag * dt);
            positions[i] += velocities[i] * dt;

            Transform t = piece.transform;
            t.position = positions[i];
            t.Rotate(0f, 0f, spins[i] * dt);

            float life = ages[i] / lifetimes[i];

            t.localScale = Vector3.one * (scales[i] * Mathf.Lerp(1f, endScaleMultiplier, life));

            Color color = piece.color;
            color.a = fadeAfter >= 1f ? 1f : 1f - Mathf.Clamp01((life - fadeAfter) / (1f - fadeAfter));
            piece.color = color;
        }

        alive = stillAlive;
    }
}

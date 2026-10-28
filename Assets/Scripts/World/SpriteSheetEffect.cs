using UnityEngine;

/// <summary>
/// Plays a one-shot sprite animation from a list of frames.
///
/// Deliberately not an AnimationClip. These packs ship loose frames, and an effect that is
/// a plain array of sprites can be retimed, recoloured or swapped in the Inspector without
/// opening the animation window or regenerating an asset. The same component serves a dust
/// puff, a slash and a pillar of light.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class SpriteSheetEffect : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SpriteRenderer target;

    [Header("Frames")]
    [SerializeField] private Sprite[] frames;
    [SerializeField] private float framesPerSecond = 20f;

    [Header("Behaviour")]
    [SerializeField] private bool loop;
    [Tooltip("Start as soon as the object is switched on. How a child effect fires when it is enabled.")]
    [SerializeField] private bool playOnEnable = true;
    [Tooltip("Clear the sprite at the end rather than leaving the last frame sitting there.")]
    [SerializeField] private bool hideWhenFinished = true;
    [Tooltip("Mirror horizontally. Set from code so a puff can face the way the player moved.")]
    [SerializeField] private bool flipX;

    public bool IsPlaying { get; private set; }

    private float elapsed;

    private void Reset() => target = GetComponent<SpriteRenderer>();

    private void Awake()
    {
        if (target == null) target = GetComponent<SpriteRenderer>();

        if (frames == null || frames.Length == 0)
            Debug.LogWarning($"[SpriteSheetEffect] '{name}' has no frames assigned; it will never show anything.", this);
    }

    private void OnEnable()
    {
        if (playOnEnable) Play();
    }

    /// <summary>Restart from the first frame.</summary>
    public void Play()
    {
        if (frames == null || frames.Length == 0) return;

        elapsed = 0f;
        IsPlaying = true;

        if (target == null) return;

        target.enabled = true;
        target.flipX = flipX;
        target.sprite = frames[0];
    }

    /// <summary>Restart, mirrored to face the given direction.</summary>
    public void Play(float direction)
    {
        flipX = direction < 0f;
        Play();
    }

    public void Stop()
    {
        IsPlaying = false;
        if (hideWhenFinished && target != null) target.enabled = false;
    }

    private void Update()
    {
        if (!IsPlaying || frames.Length == 0) return;

        // Unscaled, so an effect triggered by a hit still plays through the hit stop -
        // which is exactly the moment it is meant to be seen.
        elapsed += Time.unscaledDeltaTime;

        int index = Mathf.FloorToInt(elapsed * framesPerSecond);

        if (index >= frames.Length)
        {
            if (!loop)
            {
                Stop();
                return;
            }

            index %= frames.Length;
            elapsed %= frames.Length / Mathf.Max(framesPerSecond, 0.0001f);
        }

        if (target != null) target.sprite = frames[index];
    }
}

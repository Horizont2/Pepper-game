using UnityEngine;
using System.Collections;

/// <summary>
/// Impact feedback: hit stop (a brief freeze on contact) and camera shake.
/// Shake is routed through <see cref="CameraFollow"/> rather than written to the
/// transform directly, because the follow script rewrites the camera position
/// every LateUpdate and would otherwise erase it.
/// </summary>
public class GameFeelManager : MonoBehaviour
{
    public static GameFeelManager Instance { get; private set; }

    [Header("Shake")]
    [Tooltip("Camera shake fades out over its duration instead of stopping abruptly.")]
    [SerializeField] private AnimationCurve shakeFalloff = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

    private CameraFollow cameraFollow;
    private Coroutine shakeCoroutine;

    // Hit stop can be triggered again while one is already running (two hits in a
    // row). Counting them means time only resumes once the last one has finished.
    private int activeHitStops;
    private float timeScaleBeforeHitStop = 1f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // Destroy only this component - this lives on the Main Camera, and
            // destroying the GameObject would take the camera with it.
            Destroy(this);
            return;
        }

        Instance = this;
        cameraFollow = GetComponent<CameraFollow>();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>Call this when a hit connects.</summary>
    public void TriggerHitImpact(float stopDuration = 0.05f, float shakeDuration = 0.1f, float shakeMagnitude = 0.15f)
    {
        if (stopDuration > 0f) StartCoroutine(HitStopRoutine(stopDuration));

        if (shakeDuration <= 0f || shakeMagnitude <= 0f) return;

        if (shakeCoroutine != null) StopCoroutine(shakeCoroutine);
        shakeCoroutine = StartCoroutine(CameraShakeRoutine(shakeDuration, shakeMagnitude));
    }

    private IEnumerator HitStopRoutine(float duration)
    {
        if (activeHitStops == 0) timeScaleBeforeHitStop = Time.timeScale;
        activeHitStops++;

        Time.timeScale = 0f;

        // Real time, otherwise the wait never ends while time is frozen
        yield return new WaitForSecondsRealtime(duration);

        activeHitStops--;

        // Restore whatever the time scale was before, so a pause menu that set it
        // to 0 is not silently un-paused by a hit landing.
        if (activeHitStops <= 0)
        {
            activeHitStops = 0;
            Time.timeScale = timeScaleBeforeHitStop;
        }
    }

    private IEnumerator CameraShakeRoutine(float duration, float magnitude)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float strength = magnitude * shakeFalloff.Evaluate(elapsed / duration);

            Vector3 offset = new Vector3(
                Random.Range(-1f, 1f) * strength,
                Random.Range(-1f, 1f) * strength,
                0f);

            ApplyShakeOffset(offset);

            // Unscaled, so the shake keeps moving during hit stop
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        ApplyShakeOffset(Vector3.zero);
        shakeCoroutine = null;
    }

    private void ApplyShakeOffset(Vector3 offset)
    {
        if (cameraFollow != null) cameraFollow.SetShakeOffset(offset);
        else transform.localPosition += offset;
    }
}

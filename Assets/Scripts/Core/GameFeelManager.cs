using UnityEngine;
using System.Collections;

/// <summary>
/// Impact feedback hub: hit stop (a brief freeze on contact) and camera shake.
/// The shake itself lives in <see cref="CameraShake"/>, which knows how to apply an
/// offset on top of a camera that Cinemachine is driving.
/// Whoever lands a hit holds a reference to this, assigned in the Inspector.
/// </summary>
public class GameFeelManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CameraShake cameraShake;

    // Hit stop can fire again while one is already running (two hits in a row).
    // Counting them means time only resumes once the last one has finished.
    private int activeHitStops;
    private float timeScaleBeforeHitStop = 1f;

    private void Awake()
    {
        if (cameraShake == null)
            Debug.LogWarning($"[GameFeelManager] cameraShake is not assigned on '{name}'; hits will have no screen shake.", this);
    }

    /// <summary>Call this when a hit connects.</summary>
    public void TriggerHitImpact(float stopDuration = 0.05f, float shakeDuration = 0.1f, float shakeMagnitude = 0.15f)
    {
        if (stopDuration > 0f) StartCoroutine(HitStopRoutine(stopDuration));

        if (cameraShake != null) cameraShake.Shake(shakeDuration, shakeMagnitude);
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
}

using UnityEngine;
using System.Collections;

public class GameFeelManager : MonoBehaviour
{
    // Singleton pattern to easily call effects from anywhere
    public static GameFeelManager Instance { get; private set; }

    private Coroutine shakeCoroutine;
    private Vector3 originalCameraPosition;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        originalCameraPosition = transform.localPosition;
    }

    // Call this when a hit connects
    public void TriggerHitImpact(float stopDuration = 0.05f, float shakeDuration = 0.1f, float shakeMagnitude = 0.15f)
    {
        StartCoroutine(HitStopRoutine(stopDuration));

        // Restart shake if already shaking
        if (shakeCoroutine != null) StopCoroutine(shakeCoroutine);
        shakeCoroutine = StartCoroutine(CameraShakeRoutine(shakeDuration, shakeMagnitude));
    }

    private IEnumerator HitStopRoutine(float duration)
    {
        // Freeze game time
        Time.timeScale = 0f;

        // Wait using REAL time, otherwise we freeze forever
        yield return new WaitForSecondsRealtime(duration);

        // Restore time
        Time.timeScale = 1f;
    }

    private IEnumerator CameraShakeRoutine(float duration, float magnitude)
    {
        float elapsed = 0.0f;

        while (elapsed < duration)
        {
            // Create random offset
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            transform.localPosition = new Vector3(originalCameraPosition.x + x, originalCameraPosition.y + y, originalCameraPosition.z);

            // Use unscaledDeltaTime so the shake works even during Hit Stop
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        transform.localPosition = originalCameraPosition;
    }
}
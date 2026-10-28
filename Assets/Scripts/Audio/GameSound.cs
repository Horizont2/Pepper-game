using UnityEngine;

/// <summary>
/// One sound, addressed two ways at once.
///
/// FMOD is not in the project yet, so <see cref="clip"/> is what actually plays today.
/// <see cref="fmodEvent"/> is the slot for the FMOD event path, and once the integration is
/// installed it takes priority - every field already laid out in the Inspector keeps its
/// place, and only <see cref="AudioManager"/>'s one playback method changes. Nothing that
/// calls a sound has to be touched.
/// </summary>
[System.Serializable]
public class GameSound
{
    [Tooltip("Plays today. Leave empty once the FMOD event below is filled in.")]
    public AudioClip clip;

    [Tooltip("FMOD event path, for example event:/Player/Jump. Wins over the clip once FMOD is installed.")]
    public string fmodEvent;

    [Range(0f, 1f)]
    public float volume = 1f;

    [Tooltip("Random pitch range. Keep a little spread on anything that repeats often, " +
             "or footsteps and hits turn into a machine gun.")]
    public Vector2 pitchRange = new Vector2(0.95f, 1.05f);

    /// <summary>Nothing to play - an empty slot is normal while the set is being filled in.</summary>
    public bool IsEmpty => clip == null && string.IsNullOrWhiteSpace(fmodEvent);

    public float RandomPitch => Random.Range(pitchRange.x, pitchRange.y);
}

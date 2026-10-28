using UnityEngine;

/// <summary>
/// Every sound in the game, in one place, with one slot per sound.
///
/// It wires itself to the player by subscribing to events the gameplay scripts already
/// raise, so adding a sound is filling in a field rather than editing code. Enemies and
/// world objects are prefabs and cannot point at a scene object, so those call the public
/// Play methods instead - see the note at the bottom of this file.
///
/// FMOD: each <see cref="GameSound"/> carries an event-path field alongside its clip. When
/// the integration is installed, fill those in and change <see cref="Play"/> only - the
/// Inspector layout and every call site stay as they are.
/// </summary>
public class AudioManager : MonoBehaviour
{
    [Header("Output")]
    [Tooltip("One source is enough: PlayOneShot mixes overlapping sounds on it.")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource musicSource;
    [Range(0f, 1f)]
    [SerializeField] private float masterVolume = 1f;

    [Header("Player - Movement")]
    [SerializeField] private GameSound jump = new GameSound();
    [SerializeField] private GameSound land = new GameSound();
    [SerializeField] private GameSound dash = new GameSound();
    [SerializeField] private GameSound wallJump = new GameSound();
    [Tooltip("Not called yet - there is no footstep trigger on the walk cycle so far.")]
    [SerializeField] private GameSound footstep = new GameSound();

    [Header("Player - Combat")]
    [SerializeField] private GameSound attackSwing = new GameSound();
    [SerializeField] private GameSound attackHit = new GameSound();
    [SerializeField] private GameSound parrySuccess = new GameSound();
    [SerializeField] private GameSound parryWhiff = new GameSound();

    [Header("Player - State")]
    [SerializeField] private GameSound playerHurt = new GameSound();
    [SerializeField] private GameSound playerDeathSound = new GameSound();
    [SerializeField] private GameSound respawn = new GameSound();
    [SerializeField] private GameSound heal = new GameSound();

    [Header("Enemy")]
    [SerializeField] private GameSound enemyAlert = new GameSound();
    [SerializeField] private GameSound enemyAttack = new GameSound();
    [SerializeField] private GameSound enemyHurt = new GameSound();
    [SerializeField] private GameSound enemyDeathSound = new GameSound();

    [Header("World")]
    [SerializeField] private GameSound pickupResource = new GameSound();
    [SerializeField] private GameSound pickupHealth = new GameSound();
    [SerializeField] private GameSound checkpointActivate = new GameSound();

    [Header("UI")]
    [SerializeField] private GameSound uiMove = new GameSound();
    [SerializeField] private GameSound uiConfirm = new GameSound();
    [SerializeField] private GameSound uiCancel = new GameSound();

    [Header("Music")]
    [SerializeField] private GameSound levelMusic = new GameSound();
    [SerializeField] private bool playMusicOnStart = true;

    [Header("Player Hookup")]
    [Tooltip("Assigned once on the scene's player. Everything under Player above wires itself from these.")]
    [SerializeField] private HealthComponent playerHealth;
    [SerializeField] private PepperController playerController;
    [SerializeField] private PepperCombat playerCombat;
    [SerializeField] private PepperParry playerParry;
    [SerializeField] private PlayerDeath playerDeath;
    [SerializeField] private ResourceCounter playerResources;

    private void OnEnable()
    {
        if (playerController != null)
        {
            playerController.OnJumped += PlayJump;
            playerController.OnLanded += PlayLand;
            playerController.OnDashed += PlayDash;
            playerController.OnWallJumped += PlayWallJump;
        }

        if (playerCombat != null)
        {
            playerCombat.OnAttackStarted += PlayAttackSwing;
            playerCombat.OnAttackHit += PlayAttackHit;
        }

        if (playerParry != null)
        {
            playerParry.OnParrySuccess += PlayParrySuccess;
            playerParry.OnParryWhiffed += PlayParryWhiff;
        }

        if (playerHealth != null)
        {
            playerHealth.OnDamaged += HandlePlayerDamaged;
            playerHealth.OnDied += PlayPlayerDeath;
        }

        if (playerDeath != null) playerDeath.OnRespawned += PlayRespawn;
        if (playerResources != null) playerResources.OnAmountChanged += HandleResourcesChanged;
    }

    private void OnDisable()
    {
        if (playerController != null)
        {
            playerController.OnJumped -= PlayJump;
            playerController.OnLanded -= PlayLand;
            playerController.OnDashed -= PlayDash;
            playerController.OnWallJumped -= PlayWallJump;
        }

        if (playerCombat != null)
        {
            playerCombat.OnAttackStarted -= PlayAttackSwing;
            playerCombat.OnAttackHit -= PlayAttackHit;
        }

        if (playerParry != null)
        {
            playerParry.OnParrySuccess -= PlayParrySuccess;
            playerParry.OnParryWhiffed -= PlayParryWhiff;
        }

        if (playerHealth != null)
        {
            playerHealth.OnDamaged -= HandlePlayerDamaged;
            playerHealth.OnDied -= PlayPlayerDeath;
        }

        if (playerDeath != null) playerDeath.OnRespawned -= PlayRespawn;
        if (playerResources != null) playerResources.OnAmountChanged -= HandleResourcesChanged;
    }

    private void Start()
    {
        if (playMusicOnStart) PlayMusic(levelMusic);
    }

    // --- player, wired above -------------------------------------------------

    public void PlayJump() => Play(jump);
    public void PlayLand() => Play(land);
    public void PlayDash() => Play(dash);
    public void PlayWallJump() => Play(wallJump);
    public void PlayFootstep() => Play(footstep);

    public void PlayAttackSwing() => Play(attackSwing);
    public void PlayAttackHit() => Play(attackHit);
    public void PlayParrySuccess() => Play(parrySuccess);
    public void PlayParryWhiff() => Play(parryWhiff);

    public void PlayRespawn() => Play(respawn);
    public void PlayHeal() => Play(heal);

    private void HandlePlayerDamaged(Vector2 sourcePosition) => Play(playerHurt);
    private void PlayPlayerDeath() => Play(playerDeathSound);
    private void HandleResourcesChanged(int amount) => Play(pickupResource);

    // --- called from elsewhere -----------------------------------------------

    public void PlayEnemyAlert() => Play(enemyAlert);
    public void PlayEnemyAttack() => Play(enemyAttack);
    public void PlayEnemyHurt() => Play(enemyHurt);
    public void PlayEnemyDeath() => Play(enemyDeathSound);

    public void PlayPickupHealth() => Play(pickupHealth);
    public void PlayCheckpoint() => Play(checkpointActivate);

    public void PlayUIMove() => Play(uiMove);
    public void PlayUIConfirm() => Play(uiConfirm);
    public void PlayUICancel() => Play(uiCancel);

    // --- playback -------------------------------------------------------------

    /// <summary>
    /// The single place sound actually leaves the game. Swapping to FMOD means adding one
    /// branch here, above the AudioSource path:
    ///
    ///     if (!string.IsNullOrWhiteSpace(sound.fmodEvent))
    ///     {
    ///         FMODUnity.RuntimeManager.PlayOneShot(sound.fmodEvent, transform.position);
    ///         return;
    ///     }
    /// </summary>
    public void Play(GameSound sound)
    {
        if (sound == null || sound.IsEmpty) return;          // an unfilled slot is not an error
        if (sfxSource == null || sound.clip == null) return;

        // PlayOneShot takes the source's current pitch, so this nudges anything already
        // ringing. Harmless for short effects and it avoids a pool of sources.
        sfxSource.pitch = sound.RandomPitch;
        sfxSource.PlayOneShot(sound.clip, sound.volume * masterVolume);
    }

    public void PlayMusic(GameSound music)
    {
        if (music == null || music.clip == null || musicSource == null) return;

        musicSource.clip = music.clip;
        musicSource.volume = music.volume * masterVolume;
        musicSource.loop = true;
        musicSource.Play();
    }

    public void StopMusic()
    {
        if (musicSource != null) musicSource.Stop();
    }
}

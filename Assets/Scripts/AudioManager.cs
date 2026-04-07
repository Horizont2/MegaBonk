using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Central audio system. Manages music tracks and SFX with pooled AudioSources.
/// Attach to a persistent GameObject. Assign AudioClips in Inspector.
///
/// SFX: AudioManager.Instance.PlaySFX("hit")
/// Music: AudioManager.Instance.PlayMusic("gameplay") with crossfade
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Music")]
    public AudioClip menuMusic;
    public AudioClip gameplayMusic;
    public AudioClip bossMusic;
    [Range(0f, 1f)] public float musicVolume = 0.4f;
    public float musicCrossfadeDuration = 1.5f;

    [Header("SFX Clips")]
    public AudioClip hitSFX;
    public AudioClip enemyDeathSFX;
    public AudioClip bossDeathSFX;
    public AudioClip bossSpawnSFX;
    public AudioClip bossAttackSFX;
    public AudioClip bossWarningSFX;
    public AudioClip crystalPickupSFX;
    public AudioClip levelUpSFX;
    public AudioClip dashSFX;
    public AudioClip playerHurtSFX;
    public AudioClip achievementSFX;
    public AudioClip buttonClickSFX;
    public AudioClip playerDeathSFX;

    [Header("SFX Settings")]
    [Range(0f, 1f)] public float sfxVolume = 0.7f;
    public int sfxPoolSize = 10;
    public float minPitchVariation = 0.9f;
    public float maxPitchVariation = 1.1f;

    // Internal
    private AudioSource musicSourceA;
    private AudioSource musicSourceB;
    private AudioSource activeMusicSource;
    private AudioSource[] sfxPool;
    private int sfxPoolIndex = 0;
    private Dictionary<string, AudioClip> sfxLookup;
    private Dictionary<string, AudioClip> musicLookup;

    // Cooldown to prevent SFX spam (same sound too rapidly)
    private Dictionary<string, float> sfxCooldowns = new Dictionary<string, float>();
    private const float SFX_MIN_INTERVAL = 0.05f;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        SetupAudioSources();
        BuildLookupTables();
    }

    private void SetupAudioSources()
    {
        // Two music sources for crossfading
        musicSourceA = gameObject.AddComponent<AudioSource>();
        musicSourceA.loop = true;
        musicSourceA.playOnAwake = false;
        musicSourceA.volume = 0f;

        musicSourceB = gameObject.AddComponent<AudioSource>();
        musicSourceB.loop = true;
        musicSourceB.playOnAwake = false;
        musicSourceB.volume = 0f;

        activeMusicSource = musicSourceA;

        // SFX pool
        sfxPool = new AudioSource[sfxPoolSize];
        for (int i = 0; i < sfxPoolSize; i++)
        {
            sfxPool[i] = gameObject.AddComponent<AudioSource>();
            sfxPool[i].playOnAwake = false;
            sfxPool[i].loop = false;
        }
    }

    private void BuildLookupTables()
    {
        sfxLookup = new Dictionary<string, AudioClip>
        {
            { "hit", hitSFX },
            { "enemyDeath", enemyDeathSFX },
            { "bossDeath", bossDeathSFX },
            { "bossSpawn", bossSpawnSFX },
            { "bossAttack", bossAttackSFX },
            { "bossWarning", bossWarningSFX },
            { "crystalPickup", crystalPickupSFX },
            { "levelUp", levelUpSFX },
            { "dash", dashSFX },
            { "playerHurt", playerHurtSFX },
            { "achievement", achievementSFX },
            { "buttonClick", buttonClickSFX },
            { "playerDeath", playerDeathSFX },
        };

        musicLookup = new Dictionary<string, AudioClip>
        {
            { "menu", menuMusic },
            { "gameplay", gameplayMusic },
            { "boss", bossMusic },
        };
    }

    // ─── PUBLIC API ───

    /// <summary>
    /// Play a one-shot SFX by name. Supports pitch variation for organic feel.
    /// </summary>
    public void PlaySFX(string name, float volumeScale = 1f)
    {
        if (!sfxLookup.TryGetValue(name, out AudioClip clip) || clip == null) return;

        // Prevent spam of the same sound
        if (sfxCooldowns.TryGetValue(name, out float lastTime))
        {
            if (Time.unscaledTime - lastTime < SFX_MIN_INTERVAL) return;
        }
        sfxCooldowns[name] = Time.unscaledTime;

        AudioSource source = sfxPool[sfxPoolIndex];
        sfxPoolIndex = (sfxPoolIndex + 1) % sfxPoolSize;

        source.clip = clip;
        source.volume = sfxVolume * volumeScale;
        source.pitch = Random.Range(minPitchVariation, maxPitchVariation);

        // Some sounds should not have pitch variation
        if (name == "bossWarning" || name == "achievement" || name == "levelUp")
            source.pitch = 1f;

        source.Play();
    }

    /// <summary>
    /// Play a one-shot SFX at a specific world position (3D spatialized).
    /// </summary>
    public void PlaySFXAt(string name, Vector3 position, float volumeScale = 1f)
    {
        if (!sfxLookup.TryGetValue(name, out AudioClip clip) || clip == null) return;

        AudioSource.PlayClipAtPoint(clip, position, sfxVolume * volumeScale);
    }

    /// <summary>
    /// Switch music with crossfade. Pass null name to stop music.
    /// </summary>
    public void PlayMusic(string name)
    {
        AudioClip targetClip = null;
        if (name != null && musicLookup.TryGetValue(name, out AudioClip clip))
            targetClip = clip;

        StartCoroutine(CrossfadeMusic(targetClip));
    }

    /// <summary>
    /// Immediately stop all music.
    /// </summary>
    public void StopMusic()
    {
        StopAllCoroutines();
        musicSourceA.Stop();
        musicSourceB.Stop();
        musicSourceA.volume = 0f;
        musicSourceB.volume = 0f;
    }

    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        if (activeMusicSource != null && activeMusicSource.isPlaying)
            activeMusicSource.volume = musicVolume;
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
    }

    // ─── CROSSFADE ───

    private IEnumerator CrossfadeMusic(AudioClip newClip)
    {
        AudioSource fadeOut = activeMusicSource;
        AudioSource fadeIn = (activeMusicSource == musicSourceA) ? musicSourceB : musicSourceA;
        activeMusicSource = fadeIn;

        if (newClip != null)
        {
            fadeIn.clip = newClip;
            fadeIn.volume = 0f;
            fadeIn.Play();
        }

        float t = 0f;
        float startVolOut = fadeOut.volume;

        while (t < musicCrossfadeDuration)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / musicCrossfadeDuration);

            fadeOut.volume = Mathf.Lerp(startVolOut, 0f, p);
            if (newClip != null)
                fadeIn.volume = Mathf.Lerp(0f, musicVolume, p);

            yield return null;
        }

        fadeOut.Stop();
        fadeOut.volume = 0f;

        if (newClip != null)
            fadeIn.volume = musicVolume;
    }
}

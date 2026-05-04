using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// --- НАШ СЛОВНИК ЗВУКІВ ---
public static class AudioID
{
    public const string UI_Click = "UI_Click";
    public const string UI_Hover = "UI_Hover";
    public const string UI_QuestAccept = "UI_QuestAccept";
    public const string UI_QuestComplete = "UI_QuestComplete";
    public const string UI_Error = "UI_Error";
    public const string UI_LevelUp = "UI_LevelUp";
    public const string Player_Dash = "Player_Dash";
    public const string Player_Swing = "Player_Swing";
    public const string Player_HitEnemy = "Player_HitEnemy";
    public const string Player_HitResource = "Player_HitRes";
    public const string Player_Hurt = "Player_Hurt";
    public const string Player_Throw = "Player_Throw";
    public const string Explosion = "Explosion";
    public const string Enemy_Agro = "Enemy_Agro";
    public const string Enemy_Telegraph = "Enemy_Telegraph";
    public const string Enemy_Hurt = "Enemy_Hurt";
    public const string Enemy_Die = "Enemy_Die";
    public const string Camp_CollectItem = "Camp_CollectItem";
    public const string Camp_CollectGem = "Camp_CollectGem";
    public const string Camp_BuildStart = "Camp_BuildStart";
    public const string Camp_BuildDone = "Camp_BuildDone";
    public const string NPC_Work = "NPC_Work";
    public const string Music_Camp = "Music_Camp";
    public const string Music_Battle = "Music_Battle";
}

[System.Serializable]
public class SoundGroup
{
    [Tooltip("Закинь сюди 1 або декілька звуків (гратиме випадковий)")]
    public AudioClip[] clips;

    [Range(0f, 1f)] public float volume = 1f;
    [Range(0.5f, 2f)] public float pitch = 1f;

    [Tooltip("Якщо УВІМКНЕНО - звук кожного разу буде трохи змінювати тональність")]
    public bool randomizePitch = false;
}

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("=== UI SOUNDS ===")]
    public SoundGroup uiClick;
    public SoundGroup uiHover;
    public SoundGroup uiQuestAccept;
    public SoundGroup uiQuestComplete;
    public SoundGroup uiError;
    public SoundGroup uiLevelUp;

    [Header("=== PLAYER SOUNDS ===")]
    public SoundGroup playerDash;
    public SoundGroup playerSwing;
    public SoundGroup playerHitEnemy;
    public SoundGroup playerHitResource;
    public SoundGroup playerHurt;
    public SoundGroup playerThrow;
    public SoundGroup explosion;

    [Header("=== ENEMY SOUNDS ===")]
    public SoundGroup enemyAgro;
    public SoundGroup enemyTelegraph;
    public SoundGroup enemyHurt;
    public SoundGroup enemyDie;

    [Header("=== CAMP SOUNDS ===")]
    public SoundGroup campCollectItem;
    public SoundGroup campCollectGem;
    public SoundGroup campBuildStart;
    public SoundGroup campBuildDone;
    public SoundGroup npcWork;

    [Header("=== MUSIC ===")]
    public SoundGroup musicCamp;
    public SoundGroup musicBattle;

    [Header("=== SETTINGS ===")]
    public int sfxPoolSize = 15;
    [Tooltip("Час плавного переходу музики (в секундах)")]
    public float musicFadeDuration = 1.5f;

    // Словники для внутрішньої роботи
    private Dictionary<string, SoundGroup> sfxDictionary;
    private Dictionary<string, SoundGroup> uiDictionary;
    private Dictionary<string, SoundGroup> musicDictionary;

    private AudioSource[] sfxSources;
    private AudioSource uiSource;
    private AudioSource musicSource;
    private Coroutine musicFadeCoroutine; // Корутина для фейдів

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        InitializeDictionaries();

        uiSource = gameObject.AddComponent<AudioSource>();
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true; // Робимо музику безкінечною (луп)

        sfxSources = new AudioSource[sfxPoolSize];
        for (int i = 0; i < sfxPoolSize; i++)
        {
            sfxSources[i] = gameObject.AddComponent<AudioSource>();
        }
    }

    private void InitializeDictionaries()
    {
        sfxDictionary = new Dictionary<string, SoundGroup>();
        uiDictionary = new Dictionary<string, SoundGroup>();
        musicDictionary = new Dictionary<string, SoundGroup>();

        uiDictionary.Add(AudioID.UI_Click, uiClick);
        uiDictionary.Add(AudioID.UI_Hover, uiHover);
        uiDictionary.Add(AudioID.UI_QuestAccept, uiQuestAccept);
        uiDictionary.Add(AudioID.UI_QuestComplete, uiQuestComplete);
        uiDictionary.Add(AudioID.UI_Error, uiError);
        uiDictionary.Add(AudioID.UI_LevelUp, uiLevelUp);

        sfxDictionary.Add(AudioID.Player_Dash, playerDash);
        sfxDictionary.Add(AudioID.Player_Swing, playerSwing);
        sfxDictionary.Add(AudioID.Player_HitEnemy, playerHitEnemy);
        sfxDictionary.Add(AudioID.Player_HitResource, playerHitResource);
        sfxDictionary.Add(AudioID.Player_Hurt, playerHurt);
        sfxDictionary.Add(AudioID.Player_Throw, playerThrow);
        sfxDictionary.Add(AudioID.Explosion, explosion);

        sfxDictionary.Add(AudioID.Enemy_Agro, enemyAgro);
        sfxDictionary.Add(AudioID.Enemy_Telegraph, enemyTelegraph);
        sfxDictionary.Add(AudioID.Enemy_Hurt, enemyHurt);
        sfxDictionary.Add(AudioID.Enemy_Die, enemyDie);

        sfxDictionary.Add(AudioID.Camp_CollectItem, campCollectItem);
        sfxDictionary.Add(AudioID.Camp_CollectGem, campCollectGem);
        sfxDictionary.Add(AudioID.Camp_BuildStart, campBuildStart);
        sfxDictionary.Add(AudioID.Camp_BuildDone, campBuildDone);
        sfxDictionary.Add(AudioID.NPC_Work, npcWork);

        musicDictionary.Add(AudioID.Music_Camp, musicCamp);
        musicDictionary.Add(AudioID.Music_Battle, musicBattle);
    }

    public void PlaySFX(string soundName)
    {
        if (sfxDictionary.TryGetValue(soundName, out SoundGroup group))
        {
            if (group == null || group.clips == null || group.clips.Length == 0) return;

            AudioClip randomClip = group.clips[Random.Range(0, group.clips.Length)];
            AudioSource source = GetAvailableSFXSource();

            if (source != null && randomClip != null)
            {
                source.clip = randomClip;
                source.volume = group.volume;
                source.pitch = group.randomizePitch ? group.pitch * Random.Range(0.85f, 1.15f) : group.pitch;
                source.Play();
            }
        }
    }

    public void PlayUI(string soundName)
    {
        if (uiDictionary.TryGetValue(soundName, out SoundGroup group))
        {
            if (group == null || group.clips == null || group.clips.Length == 0) return;

            AudioClip randomClip = group.clips[Random.Range(0, group.clips.Length)];
            if (randomClip != null) uiSource.PlayOneShot(randomClip, group.volume);
        }
    }

    // --- ОНОВЛЕНИЙ МЕТОД ДЛЯ МУЗИКИ З ФЕЙДАМИ ---
    public void PlayMusic(string soundName)
    {
        if (musicDictionary.TryGetValue(soundName, out SoundGroup group))
        {
            if (group == null || group.clips == null || group.clips.Length == 0) return;

            AudioClip clipToPlay = group.clips[0];
            if (musicSource.clip == clipToPlay && musicSource.isPlaying) return;

            // Зупиняємо попередній перехід, якщо він ще йде
            if (musicFadeCoroutine != null) StopCoroutine(musicFadeCoroutine);

            // Запускаємо плавний перехід
            musicFadeCoroutine = StartCoroutine(FadeMusicRoutine(clipToPlay, group.volume, group.pitch));
        }
    }

    private IEnumerator FadeMusicRoutine(AudioClip newClip, float targetVolume, float pitch)
    {
        // 1. Плавно затухаємо поточну музику (якщо вона грає)
        if (musicSource.isPlaying)
        {
            float startVol = musicSource.volume;
            float t = 0;
            while (t < musicFadeDuration)
            {
                t += Time.unscaledDeltaTime; // Використовуємо unscaled, щоб музика не залежала від паузи
                musicSource.volume = Mathf.Lerp(startVol, 0f, t / musicFadeDuration);
                yield return null;
            }
            musicSource.volume = 0f;
            musicSource.Stop();
        }

        // 2. Ставимо новий трек і плавно робимо його гучнішим
        musicSource.clip = newClip;
        musicSource.pitch = pitch;
        musicSource.Play();

        float fadeInTimer = 0;
        while (fadeInTimer < musicFadeDuration)
        {
            fadeInTimer += Time.unscaledDeltaTime;
            musicSource.volume = Mathf.Lerp(0f, targetVolume, fadeInTimer / musicFadeDuration);
            yield return null;
        }
        musicSource.volume = targetVolume;
    }

    private AudioSource GetAvailableSFXSource()
    {
        foreach (AudioSource source in sfxSources)
        {
            if (!source.isPlaying) return source;
        }
        return sfxSources[0];
    }
}
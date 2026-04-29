using UnityEngine;
using System.Collections.Generic;

// --- НАШ СЛОВНИК ЗВУКІВ (Захист від опечаток) ---
public static class AudioID
{
    // 1. UI (Інтерфейс)
    public const string UI_Click = "UI_Click";                 // Клік по кнопках
    public const string UI_QuestAccept = "UI_QuestAccept";     // Взяття місії
    public const string UI_QuestComplete = "UI_QuestComplete"; // Виконання місії
    public const string UI_Error = "UI_Error";                 // Недостатньо ресурсів
    public const string UI_LevelUp = "UI_LevelUp";             // Звук фанфар при підвищенні рівня

    // 2. Гравець (Player & Combat)
    public const string Player_Dash = "Player_Dash";           // Ривок (Dash)
    public const string Player_Swing = "Player_Swing";         // Замах зброєю (промах)
    public const string Player_HitEnemy = "Player_HitEnemy";   // Влучання по ворогу (м'ясо/метал)
    public const string Player_HitResource = "Player_HitRes";  // Влучання по дереву/каменю
    public const string Player_Hurt = "Player_Hurt";           // Гравець отримав шкоду
    public const string Player_Throw = "Player_Throw";         // Кидок гранати
    public const string Explosion = "Explosion";               // Вибух гранати / MegaBoom

    // 3. Вороги (Enemies)
    public const string Enemy_Agro = "Enemy_Agro";             // Ворог помітив гравця
    public const string Enemy_Telegraph = "Enemy_Telegraph";   // Дзенькіт перед ударом (блимання червоним)
    public const string Enemy_Hurt = "Enemy_Hurt";             // Ворог отримав шкоду
    public const string Enemy_Die = "Enemy_Die";               // Ворог розсипався

    // 4. Табір та Економіка (Camp & Economy)
    public const string Camp_CollectItem = "Camp_CollectItem"; // Зібрано ресурс (їжа/дерево)
    public const string Camp_CollectGem = "Camp_CollectGem";   // Зібрано кристал/діамант
    public const string Camp_BuildStart = "Camp_BuildStart";   // Початок будівництва (молотки)
    public const string Camp_BuildDone = "Camp_BuildDone";     // Завершення апгрейду (бум!)
    public const string NPC_Work = "NPC_Work";                 // Звук роботи (рубка дерева)

    // 5. Музика (Music)
    public const string Music_Camp = "Music_Camp";             // Спокійна музика табору
    public const string Music_Battle = "Music_Battle";         // Напружена музика світу
}

[System.Serializable]
public class Sound
{
    public string name; // СЮДИ В ІНСПЕКТОРІ ТРЕБА ВПИСАТИ НАЗВУ З AUDIO_ID (наприклад, Player_Dash)
    public AudioClip clip;
    [Range(0f, 1f)] public float volume = 1f;
    [Range(0.5f, 2f)] public float pitch = 1f;
    public bool randomizePitch = false;
}

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Sound Arrays (Fill in Inspector)")]
    public Sound[] sfxSounds;
    public Sound[] uiSounds;
    public Sound[] musicSounds;

    [Header("Settings")]
    public int sfxPoolSize = 10;

    private Dictionary<string, Sound> sfxDictionary = new Dictionary<string, Sound>();
    private Dictionary<string, Sound> uiDictionary = new Dictionary<string, Sound>();
    private Dictionary<string, Sound> musicDictionary = new Dictionary<string, Sound>();

    private AudioSource[] sfxSources;
    private AudioSource uiSource;
    private AudioSource musicSource;

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

        foreach (Sound s in sfxSounds) sfxDictionary[s.name] = s;
        foreach (Sound s in uiSounds) uiDictionary[s.name] = s;
        foreach (Sound s in musicSounds) musicDictionary[s.name] = s;

        uiSource = gameObject.AddComponent<AudioSource>();
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;

        sfxSources = new AudioSource[sfxPoolSize];
        for (int i = 0; i < sfxPoolSize; i++)
        {
            sfxSources[i] = gameObject.AddComponent<AudioSource>();
        }
    }

    public void PlaySFX(string soundName)
    {
        if (sfxDictionary.TryGetValue(soundName, out Sound sound))
        {
            AudioSource source = GetAvailableSFXSource();
            if (source != null)
            {
                source.clip = sound.clip;
                source.volume = sound.volume;
                source.pitch = sound.randomizePitch ? sound.pitch * Random.Range(0.85f, 1.15f) : sound.pitch;
                source.Play();
            }
        }
        else Debug.LogWarning($"[AudioManager] SFX '{soundName}' не знайдено!");
    }

    public void PlayUI(string soundName)
    {
        if (uiDictionary.TryGetValue(soundName, out Sound sound)) uiSource.PlayOneShot(sound.clip, sound.volume);
        else Debug.LogWarning($"[AudioManager] UI звук '{soundName}' не знайдено!");
    }

    public void PlayMusic(string soundName)
    {
        if (musicDictionary.TryGetValue(soundName, out Sound sound))
        {
            if (musicSource.clip == sound.clip && musicSource.isPlaying) return;
            musicSource.clip = sound.clip;
            musicSource.volume = sound.volume;
            musicSource.pitch = sound.pitch;
            musicSource.Play();
        }
        else Debug.LogWarning($"[AudioManager] Музику '{soundName}' не знайдено!");
    }

    private AudioSource GetAvailableSFXSource()
    {
        foreach (AudioSource source in sfxSources) if (!source.isPlaying) return source;
        return sfxSources[0];
    }
}
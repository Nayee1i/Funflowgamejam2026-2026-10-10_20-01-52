using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Все звуки игры. Создаётся сам при запуске и живёт между сценами.
// Эффекты: GameAudio.Play("shot"). Файл ищется в Resources/Audio/<имя>,
// громкость и разброс высоты берутся из Resources/Audio/SoundLibrary.
// Музыка включается сама по имени сцены, наведение и клик по кнопкам озвучиваются сами.
public class GameAudio : MonoBehaviour
{
    const string SoundKey = "Menu.SoundLevel", MusicKey = "Menu.MusicLevel";
    const int Voices = 12;

    static GameAudio instance;
    static readonly Dictionary<string, AudioClip> clips = new();
    static readonly Dictionary<string, float> lastPlayed = new();
    static readonly HashSet<string> missing = new();

    SoundLibrary library;
    readonly AudioSource[] voices = new AudioSource[Voices];
    int nextVoice;
    AudioSource music;
    SoundLibrary.Sound musicSettings;
    readonly List<RaycastResult> uiHits = new();
    Selectable hovered;

    public static float SoundVolume => Steps(PlayerPrefs.GetInt(SoundKey, 1), library => library.effectsVolume);
    public static float MusicVolume => Steps(PlayerPrefs.GetInt(MusicKey, 2), library => library.musicVolume);

    static float Steps(int level, System.Func<SoundLibrary, SoundLibrary.VolumeSteps> pick)
    {
        level = Mathf.Clamp(level, 0, 3);
        var library = instance != null ? instance.library : null;
        return library != null ? pick(library).For(level) : level / 3f;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Boot()
    {
        clips.Clear();
        lastPlayed.Clear();
        missing.Clear();
        var root = new GameObject("Game Audio");
        DontDestroyOnLoad(root);
        instance = root.AddComponent<GameAudio>();
    }

    void Awake()
    {
        library = Resources.Load<SoundLibrary>("Audio/SoundLibrary");
        AudioListener.volume = 1f; // громкость задаётся отдельно для эффектов и музыки
        for (int i = 0; i < Voices; i++)
        {
            voices[i] = gameObject.AddComponent<AudioSource>();
            voices[i].playOnAwake = false;
        }
        music = gameObject.AddComponent<AudioSource>();
        music.playOnAwake = false;
        music.loop = true;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy() => SceneManager.sceneLoaded -= OnSceneLoaded;

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        hovered = null;
        bool menu = scene.name == "MainMenu" || scene.name == "LevelSelection";
        Music(menu ? "music_menu" : "music_level");
    }

    public static void Play(string soundName)
    {
        if (instance == null || string.IsNullOrEmpty(soundName)) return;
        var settings = instance.library != null ? instance.library.Find(soundName) : null;
        var clip = Clip(soundName, settings);
        if (clip == null) return;

        float now = Time.unscaledTime;
        float cooldown = settings != null ? settings.cooldown : .03f;
        if (lastPlayed.TryGetValue(soundName, out float last) && now - last < cooldown) return;
        lastPlayed[soundName] = now;

        float volume = (settings != null ? settings.volume : 1f) * SoundVolume;
        if (volume <= 0f) return;
        float variation = settings != null ? settings.pitchVariation : 0f;

        var voice = instance.voices[instance.nextVoice];
        instance.nextVoice = (instance.nextVoice + 1) % Voices;
        voice.clip = clip;
        voice.volume = volume;
        voice.pitch = 1f + Random.Range(-variation, variation);
        voice.Play();
    }

    public static void Music(string trackName)
    {
        if (instance == null) return;
        var settings = instance.library != null ? instance.library.Find(trackName) : null;
        var clip = Clip(trackName, settings);
        instance.musicSettings = settings;
        if (clip == null) { instance.music.Stop(); instance.music.clip = null; return; }
        if (instance.music.clip == clip && instance.music.isPlaying) return;
        instance.music.clip = clip;
        instance.music.Play();
    }

    static AudioClip Clip(string soundName, SoundLibrary.Sound settings)
    {
        if (settings != null && settings.clip != null) return settings.clip;
        if (clips.TryGetValue(soundName, out var cached)) return cached;
        var clip = Resources.Load<AudioClip>("Audio/" + soundName);
        clips[soundName] = clip;
        if (clip == null && missing.Add(soundName))
            Debug.Log($"GameAudio: нет файла Resources/Audio/{soundName}, звук пропущен");
        return clip;
    }

    void Update()
    {
        music.volume = (musicSettings != null ? musicSettings.volume : 1f) * MusicVolume;
        UpdateInterfaceSounds();
    }

    // Наведение и клик по любой интерактивной кнопке UI, без правки самих кнопок.
    void UpdateInterfaceSounds()
    {
        var events = EventSystem.current;
        var mouse = Mouse.current;
        if (events == null || mouse == null) { hovered = null; return; }

        var pointer = new PointerEventData(events) { position = mouse.position.ReadValue() };
        uiHits.Clear();
        events.RaycastAll(pointer, uiHits);
        Selectable target = uiHits.Count > 0 ? uiHits[0].gameObject.GetComponentInParent<Selectable>() : null;
        if (target != null && !target.IsInteractable()) target = null;

        if (target != hovered)
        {
            hovered = target;
            if (target != null) Play("hover");
        }
        if (target != null && mouse.leftButton.wasPressedThisFrame) Play("click");
    }
}

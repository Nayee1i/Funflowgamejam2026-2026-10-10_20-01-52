using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Все звуки игры. Создаётся сам при запуске и живёт между сценами.
// Эффекты: GameAudio.Play("shot"). Файл ищется в Resources/Audio/<имя>,
// громкость и разброс высоты берутся из Resources/Audio/SoundLibrary.
// Музыка включается сама по уровню (в последней комнате level3 — музыка босса),
// мотор танка звучит сам, наведение и клик по кнопкам озвучиваются сами.
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
    AudioSource engineIdle, engineMove;
    float engineBlend, engineLevel;
    string sceneName;
    FirstLevelRooms rooms;
    Transform tank;
    Vector3 lastTankPosition;

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
        engineIdle = LoopSource();
        engineMove = LoopSource();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy() => SceneManager.sceneLoaded -= OnSceneLoaded;

    AudioSource LoopSource()
    {
        var source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = true;
        source.volume = 0f;
        return source;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        hovered = null;
        sceneName = scene.name;
        rooms = FindAnyObjectByType<FirstLevelRooms>();
        var movement = rooms == null ? FindAnyObjectByType<TankMovement>() : null;
        tank = rooms != null ? rooms.player : movement != null ? movement.transform : null;
        if (tank != null) lastTankPosition = tank.position;
        engineBlend = engineLevel = 0f;
        StartLoop(engineIdle, tank != null ? "tank_idle" : null);
        StartLoop(engineMove, tank != null ? "tank_move" : null);
        Music(LevelTrack());
    }

    string LevelTrack()
    {
        if (sceneName == "MainMenu" || sceneName == "LevelSelection") return "music_menu";
        if (sceneName == "level3" && rooms != null && rooms.rooms != null && rooms.rooms.Length > 0 &&
            rooms.CurrentRoom == rooms.rooms.Length - 1) return "music_boss";
        if (sceneName == "level2") return "music_level2";
        if (sceneName == "EndlessArena") return "music_level2";
        if (sceneName == "level3") return "music_level3";
        return "music_level1";
    }

    void StartLoop(AudioSource source, string soundName)
    {
        var clip = soundName != null ? Clip(soundName, Settings(soundName)) : null;
        if (clip == null) { source.Stop(); source.clip = null; return; }
        if (source.clip == clip && source.isPlaying) return;
        source.clip = clip;
        source.volume = 0f;
        source.Play();
        Debug.Log($"GameAudio: мотор {soundName} запущен в сцене {sceneName}");
    }

    public static void Play(string soundName)
    {
        if (instance == null || string.IsNullOrEmpty(soundName)) return;
        var settings = Settings(soundName);
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
        var settings = Settings(trackName);
        var clip = Clip(trackName, settings);
        instance.musicSettings = settings;
        if (clip == null) { instance.music.Stop(); instance.music.clip = null; return; }
        if (instance.music.clip == clip && instance.music.isPlaying) return;
        instance.music.clip = clip;
        instance.music.Play();
    }

    // Настройки звука из SoundLibrary. В редакторе новый файл из Resources/Audio
    // сам добавляется в список при первом проигрывании, чтобы его громкость можно было крутить.
    static SoundLibrary.Sound Settings(string soundName)
    {
        var library = instance != null ? instance.library : null;
        if (library == null) return null;
        var sound = library.Find(soundName);
#if UNITY_EDITOR
        if (sound == null && Clip(soundName, null) != null)
        {
            float volume = soundName.StartsWith("music") ? .5f : soundName.StartsWith("tank_") ? .6f : .8f;
            sound = new SoundLibrary.Sound { name = soundName, volume = volume };
            var list = new List<SoundLibrary.Sound>(library.sounds) { sound };
            library.sounds = list.ToArray();
            UnityEditor.EditorUtility.SetDirty(library);
        }
#endif
        return sound;
    }

    static float Volume(string soundName)
    {
        var settings = Settings(soundName);
        return settings != null ? settings.volume : 1f;
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
        Music(LevelTrack());
        music.volume = (musicSettings != null ? musicSettings.volume : 1f) * MusicVolume;
        UpdateEngine();
        UpdateInterfaceSounds();
    }

    // Мотор: на месте — tank_idle, в движении — tank_move, с плавным переходом.
    // Молчит в паузе, во время перехода между комнатами и после победы.
    void UpdateEngine()
    {
        bool active = tank != null && tank.gameObject.activeInHierarchy && Time.timeScale > 0f &&
                      (rooms == null || rooms.CanPlay);
        bool moving = false;
        if (tank != null)
        {
            moving = active && (tank.position - lastTankPosition).sqrMagnitude > 1e-6f;
            lastTankPosition = tank.position;
        }
        engineBlend = Mathf.MoveTowards(engineBlend, moving ? 1f : 0f, Time.unscaledDeltaTime * 5f);
        engineLevel = Mathf.MoveTowards(engineLevel, active ? 1f : 0f, Time.unscaledDeltaTime * 3f);
        float volume = engineLevel * SoundVolume;
        if (engineIdle.clip != null) engineIdle.volume = (1f - engineBlend) * Volume("tank_idle") * volume;
        if (engineMove.clip != null) engineMove.volume = engineBlend * Volume("tank_move") * volume;
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

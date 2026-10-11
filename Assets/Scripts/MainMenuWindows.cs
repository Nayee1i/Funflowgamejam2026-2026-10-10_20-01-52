using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainMenuWindows : MonoBehaviour
{
    public GameObject settingsWindow;
    public GameObject exitWindow;
    public GameObject modesWindow;
    public Image[] soundBars;
    public Image[] musicBars;
    public Sprite emptyBar;
    public Sprite filledBar;
    public Button[] levelButtons;
    public Image[] levelButtonArtwork;
    public Sprite unlockedLevelArtwork;
    private static bool openLevelSelection;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetMenuState() => openLevelSelection = false;

    public static void ReturnToLevelSelection()
    {
        openLevelSelection = true;
        SceneManager.LoadScene("MainMenu");
    }
    private int soundLevel = 1;
    private int musicLevel = 2;
    private void Awake()
    {
        soundLevel = Mathf.Clamp(PlayerPrefs.GetInt("Menu.SoundLevel", 1), 0, 3);
        musicLevel = Mathf.Clamp(PlayerPrefs.GetInt("Menu.MusicLevel", 2), 0, 3);
        BindLevelButtons();
        RefreshVolume();
        RefreshLevels();
    }
    private void BindLevelButtons()
    {
        // An already-open editor scene can retain its old, empty click events.
        // Bind from the actual window hierarchy each time the menu starts.
        levelButtons = new Button[LevelProgression.LevelCount];
        levelButtonArtwork = new Image[LevelProgression.LevelCount];
        string[] hitNames = { "image 50 Hit Area", "image 53 Hit Area", "image 57 Hit Area" };
        string[] artNames = { "image 50 48:2691", "image 53 48:2692", "image 57 48:2693" };
        foreach (Button button in modesWindow.GetComponentsInChildren<Button>(true))
        {
            // Transparent hit areas must still participate in UI raycasts.
            if (button.targetGraphic != null)
            {
                button.targetGraphic.raycastTarget = true;
                button.targetGraphic.canvasRenderer.cullTransparentMesh = false;
            }
            for (int i = 0; i < hitNames.Length; i++)
            {
                if (button.name != hitNames[i]) continue;
                int level = i + 1;
                levelButtons[i] = button;
                button.onClick = new Button.ButtonClickedEvent();
                button.onClick.AddListener(() => OpenLevel(level));
            }
            if (button.name == "image 54 Hit Area")
            {
                button.onClick = new Button.ButtonClickedEvent();
                button.onClick.AddListener(Arena);
            }
        }
        foreach (Image artwork in modesWindow.GetComponentsInChildren<Image>(true))
            for (int i = 0; i < artNames.Length; i++)
                if (artwork.name == artNames[i]) levelButtonArtwork[i] = artwork;
        if (unlockedLevelArtwork == null && levelButtonArtwork[0] != null)
            unlockedLevelArtwork = levelButtonArtwork[0].sprite;
    }
    private void Start()
    {
        if (!openLevelSelection) return;
        openLevelSelection = false;
        Play();
    }
    private void RefreshLevels()
    {
        if (levelButtons == null) return;
        for (int i = 0; i < levelButtons.Length; i++)
        {
            bool unlocked = LevelProgression.IsUnlocked(i + 1);
            if (levelButtons[i] != null) levelButtons[i].interactable = unlocked;
            if (unlocked && unlockedLevelArtwork != null && levelButtonArtwork != null &&
                i < levelButtonArtwork.Length && levelButtonArtwork[i] != null)
                levelButtonArtwork[i].sprite = unlockedLevelArtwork;
        }
    }
    public void Level1() => OpenLevel(1);
    public void Level2() => OpenLevel(2);
    public void Level3() => OpenLevel(3);
    public void Arena() => SceneManager.LoadScene("TankArena");
    private void OpenLevel(int level)
    {
        if (LevelProgression.IsUnlocked(level)) SceneManager.LoadScene("level" + level);
    }
    public void SoundUp() { soundLevel = Mathf.Min(3, soundLevel + 1); SaveVolume(); }
    public void SoundDown() { soundLevel = Mathf.Max(0, soundLevel - 1); SaveVolume(); }
    public void MusicUp() { musicLevel = Mathf.Min(3, musicLevel + 1); SaveVolume(); }
    public void MusicDown() { musicLevel = Mathf.Max(0, musicLevel - 1); SaveVolume(); }
    private void SaveVolume()
    {
        PlayerPrefs.SetInt("Menu.SoundLevel", soundLevel);
        PlayerPrefs.SetInt("Menu.MusicLevel", musicLevel);
        PlayerPrefs.Save();
        RefreshVolume();
    }
    private void RefreshVolume()
    {
        for (int i = 0; i < soundBars.Length; i++) soundBars[i].sprite = i < soundLevel ? filledBar : emptyBar;
        for (int i = 0; i < musicBars.Length; i++) musicBars[i].sprite = i < musicLevel ? filledBar : emptyBar;
        AudioListener.volume = soundLevel / 3f;
    }
    public void Settings() { Close(); settingsWindow.SetActive(true); }
    public void Exit() { Close(); exitWindow.SetActive(true); }
    public void Play() { Close(); RefreshLevels(); modesWindow.SetActive(true); }
    public void Close() { settingsWindow.SetActive(false); exitWindow.SetActive(false); modesWindow.SetActive(false); }
    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) Close();
    }
}

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class MainMenuWindows : MonoBehaviour
{
    public GameObject settingsWindow;
    public GameObject exitWindow;
    public GameObject modesWindow;
    public Image[] soundBars;
    public Image[] musicBars;
    public Sprite emptyBar;
    public Sprite filledBar;
    private int soundLevel = 1;
    private int musicLevel = 2;
    private void Awake()
    {
        soundLevel = Mathf.Clamp(PlayerPrefs.GetInt("Menu.SoundLevel", 1), 0, 3);
        musicLevel = Mathf.Clamp(PlayerPrefs.GetInt("Menu.MusicLevel", 2), 0, 3);
        RefreshVolume();
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
    public void Play() { Close(); modesWindow.SetActive(true); }
    public void StartLevel1() { UnityEngine.SceneManagement.SceneManager.LoadScene("Level1Rooms"); }
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

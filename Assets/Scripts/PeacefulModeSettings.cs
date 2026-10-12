using UnityEngine;

public static class PeacefulModeSettings
{
    public const string Key="Gameplay.PeacefulMode";
    public static bool Enabled => PlayerPrefs.GetInt(Key,0)!=0;
    public static void SetEnabled(bool value)
    {
        PlayerPrefs.SetInt(Key,value ? 1 : 0);
        PlayerPrefs.Save();
    }
}

using UnityEngine;

public static class PeacefulModeSettings
{
#if UNITY_EDITOR
    public const string Key="Campaign.Submission.v1.Editor.PeacefulMode";
#else
    public const string Key="Campaign.Submission.v1.Player.PeacefulMode";
#endif
    public static bool Enabled => PlayerPrefs.GetInt(Key,0)!=0;
    public static void SetEnabled(bool value)
    {
        PlayerPrefs.SetInt(Key,value ? 1 : 0);
        PlayerPrefs.Save();
    }
}

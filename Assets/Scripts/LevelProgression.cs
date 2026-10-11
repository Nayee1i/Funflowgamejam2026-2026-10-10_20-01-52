using UnityEngine;

public static class LevelProgression
{
    private const string UnlockedLevelKey = "Progress.UnlockedLevel";
    public const int LevelCount = 3;

    public static bool IsUnlocked(int level) => level >= 1 && level <= LevelCount &&
        level <= Mathf.Clamp(PlayerPrefs.GetInt(UnlockedLevelKey, 1), 1, LevelCount);

    public static void Complete(string sceneName)
    {
        for (int level = 1; level <= LevelCount; level++)
        {
            if (sceneName != "level" + level) continue;
            int unlocked = Mathf.Clamp(PlayerPrefs.GetInt(UnlockedLevelKey, 1), 1, LevelCount);
            PlayerPrefs.SetInt(UnlockedLevelKey, Mathf.Max(unlocked, Mathf.Min(level + 1, LevelCount)));
            PlayerPrefs.Save();
            return;
        }
    }
}

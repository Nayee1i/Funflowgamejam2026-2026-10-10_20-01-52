using UnityEngine;

public static class LevelProgression
{
    private const string UnlockedLevelKey = "Progress.UnlockedLevel";
    public const int LevelCount = 3;
    public static int LevelNumber(string sceneName) => sceneName == "Level1Rooms" ? 1 :
        sceneName == "level1" ? 1 : sceneName == "level2" ? 2 : sceneName == "level3" ? 3 : 0;
    public static float BestTime(int level) => PlayerPrefs.GetFloat("Progress.Time." + level, -1);
    public static int Stars(int level)
    {
        float time = BestTime(level);
        if (time < 0) return 0;
        return time <= PlayerPrefs.GetFloat("Progress.ThreeStars." + level, 60) ? 3 :
            time <= PlayerPrefs.GetFloat("Progress.TwoStars." + level, 120) ? 2 : 1;
    }
    public static void SetStarThresholds(int level, float threeStars, float twoStars)
    {
        if (level < 1 || level > LevelCount || float.IsNaN(threeStars) || float.IsNaN(twoStars) ||
            float.IsInfinity(threeStars) || float.IsInfinity(twoStars) || threeStars <= 0 || twoStars < threeStars) return;
        PlayerPrefs.SetFloat("Progress.ThreeStars." + level, threeStars);
        PlayerPrefs.SetFloat("Progress.TwoStars." + level, twoStars);
        PlayerPrefs.Save();
    }
    public static void RecordResult(string sceneName, float seconds)
    {
        int level = LevelNumber(sceneName);
        if (level == 0 || seconds < 0 || float.IsNaN(seconds) || float.IsInfinity(seconds)) return;
        float best = BestTime(level);
        if (best < 0 || seconds < best) PlayerPrefs.SetFloat("Progress.Time." + level, seconds);
        Complete(sceneName);
    }

    public static bool IsUnlocked(int level) => level >= 1 && level <= LevelCount &&
        level <= Mathf.Clamp(PlayerPrefs.GetInt(UnlockedLevelKey, 1), 1, LevelCount);

    public static void Complete(string sceneName)
    {
        for (int level = 1; level <= LevelCount; level++)
        {
            if (LevelNumber(sceneName) != level) continue;
            int unlocked = Mathf.Clamp(PlayerPrefs.GetInt(UnlockedLevelKey, 1), 1, LevelCount);
            PlayerPrefs.SetInt(UnlockedLevelKey, Mathf.Max(unlocked, Mathf.Min(level + 1, LevelCount)));
            PlayerPrefs.Save();
            return;
        }
    }
}

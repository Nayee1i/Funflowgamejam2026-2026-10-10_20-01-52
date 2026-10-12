using UnityEngine;

public static class LevelProgression
{
    // Release progress is separate from editor and older test builds.
    // Keep this prefix stable across updates so earned unlocks are retained.
#if UNITY_EDITOR
    private const string UnlockedLevelKey = "Campaign.Submission.v1.Editor.UnlockedLevel";
    private const string BestTimePrefix = "Campaign.Submission.v1.Editor.Time.";
#else
    private const string UnlockedLevelKey = "Campaign.Submission.v1.Player.UnlockedLevel";
    private const string BestTimePrefix = "Campaign.Submission.v1.Player.Time.";
#endif
    public const int LevelCount = 3;
    public static int LevelNumber(string sceneName) => sceneName == "Level1Rooms" ? 1 :
        sceneName == "level1" ? 1 : sceneName == "level2" ? 2 : sceneName == "level3" ? 3 : 0;
    public static float BestTime(int level) => PlayerPrefs.GetFloat(BestTimePrefix + level, -1);
    public static int Stars(int level)
    {
        float time = BestTime(level);
        return StarsForTime(level,time);
    }
    public static int StarsForTime(int level,float time)
    {
        if (time < 0) return 0;
        float threeStarTime = level == 3 ? 180f : PlayerPrefs.GetFloat("Progress.ThreeStars." + level, 60);
        float twoStarTime = level == 3 ? 240f : PlayerPrefs.GetFloat("Progress.TwoStars." + level, 120);
        return time <= threeStarTime ? 3 :
            time <= twoStarTime ? 2 : 1;
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
        if (best < 0 || seconds < best) PlayerPrefs.SetFloat(BestTimePrefix + level, seconds);
        Complete(sceneName);
    }

    public static bool IsUnlocked(int level) => level >= 1 && level <= LevelCount &&
        level <= Mathf.Clamp(PlayerPrefs.GetInt(UnlockedLevelKey, 1), 1, LevelCount);

    public static void ResetCampaign()
    {
        PlayerPrefs.DeleteKey(UnlockedLevelKey);
        for (int level = 1; level <= LevelCount; level++) PlayerPrefs.DeleteKey(BestTimePrefix + level);
        PlayerPrefs.Save();
    }

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

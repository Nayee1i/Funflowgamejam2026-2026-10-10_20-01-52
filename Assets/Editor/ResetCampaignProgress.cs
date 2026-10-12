using UnityEditor;

public static class ResetCampaignProgress
{
    [MenuItem("Bounce Club/Reset Campaign Progress")]
    public static void Reset() => LevelProgression.ResetCampaign();
}

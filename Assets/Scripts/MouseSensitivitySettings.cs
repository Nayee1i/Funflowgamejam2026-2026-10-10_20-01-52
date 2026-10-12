using UnityEngine;

/// <summary>Persistent mouse gain shared by all tank scenes. No UI dependencies.</summary>
public static class MouseSensitivitySettings
{
    public const string PreferenceKey = "Controls.MouseSensitivity";
    public const float Minimum = .1f;
    public const float Maximum = 3f;
    public const float Default = 1f;

    public static float Value => Validate(PlayerPrefs.GetFloat(PreferenceKey, Default));
    public static float NormalizedValue => Mathf.InverseLerp(Minimum, Maximum, Value);

    public static void Set(float value)
    {
        value = Validate(value);
        if (PlayerPrefs.HasKey(PreferenceKey) && PlayerPrefs.GetFloat(PreferenceKey) == value) return;
        PlayerPrefs.SetFloat(PreferenceKey, value);
        PlayerPrefs.Save();
    }

    // For a future slider whose range is 0..1.
    public static void SetNormalized(float value)
    {
        if (float.IsNaN(value) || float.IsInfinity(value)) { Reset(); return; }
        Set(Mathf.Lerp(Minimum, Maximum, Mathf.Clamp01(value)));
    }

    public static void Reset() => Set(Default);

    static float Validate(float value) => float.IsNaN(value) || float.IsInfinity(value)
        ? Default : Mathf.Clamp(value, Minimum, Maximum);
}

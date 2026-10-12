using UnityEditor;
using UnityEngine;

public static class VerifyMouseSensitivity
{
    [InitializeOnLoadMethod]
    static void Schedule()
    {
        EditorApplication.update += CheckRequest;
    }

    static void CheckRequest()
    {
        const string request = "Tools/verify-mouse-sensitivity.request";
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating || !System.IO.File.Exists(request)) return;
        System.IO.File.Delete(request);
        Verify();
        System.IO.File.WriteAllText("Tools/mouse-sensitivity-result.txt", "MOUSE_SENSITIVITY_VERIFIED");
    }

    public static void Verify()
    {
        bool existed = PlayerPrefs.HasKey(MouseSensitivitySettings.PreferenceKey);
        float previous = PlayerPrefs.GetFloat(MouseSensitivitySettings.PreferenceKey);
        var tank = new GameObject("Sensitivity verification");
        try
        {
            MouseSensitivitySettings.Set(-100);
            Require(MouseSensitivitySettings.Value == .1f, "Minimum clamp");
            MouseSensitivitySettings.Set(100);
            Require(MouseSensitivitySettings.Value == 3f, "Maximum clamp");
            MouseSensitivitySettings.Set(float.NaN);
            Require(MouseSensitivitySettings.Value == 1f, "Invalid value fallback");
            MouseSensitivitySettings.SetNormalized(.5f);
            Require(Mathf.Abs(MouseSensitivitySettings.Value - 1.55f) < .0001f, "Slider mapping");
            Require(PlayerPrefs.GetFloat(MouseSensitivitySettings.PreferenceKey) == MouseSensitivitySettings.Value, "Persistence");
            var gun = tank.AddComponent<FirstLevelTankAim>();
            MouseSensitivitySettings.Set(1);
            gun.ApplyMouseAim(new Vector2(80, 0));
            float first = gun.AimDirection.x;
            Object.DestroyImmediate(gun);
            gun = tank.AddComponent<FirstLevelTankAim>();
            MouseSensitivitySettings.Set(2);
            gun.ApplyMouseAim(new Vector2(80, 0));
            Require(gun.AimDirection.x > first + .2f, "Sensitivity applies to aiming");
            Vector2 direction = gun.AimDirection;
            MouseSensitivitySettings.Reset();
            gun.ApplyMouseAim(Vector2.zero);
            Require(Vector2.Distance(direction, gun.AimDirection) < .0001f, "Changing setting does not turn stationary aim");
            Debug.Log("MOUSE_SENSITIVITY_VERIFIED: clamp, invalid values, slider mapping, persistence, immediate aim gain, stationary aim.");
        }
        finally
        {
            Object.DestroyImmediate(tank);
            if (existed) PlayerPrefs.SetFloat(MouseSensitivitySettings.PreferenceKey, previous);
            else PlayerPrefs.DeleteKey(MouseSensitivitySettings.PreferenceKey);
            PlayerPrefs.Save();
        }
    }

    static void Require(bool condition, string check)
    {
        if (!condition) throw new System.Exception("Mouse sensitivity: " + check);
    }
}

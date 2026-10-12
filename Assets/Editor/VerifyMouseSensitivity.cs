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
        var cameraObject = new GameObject("Cursor aiming verification camera");
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
            gun.turret = tank.transform;
            var camera = cameraObject.AddComponent<Camera>();
            camera.enabled = false;
            camera.orthographic = true;
            camera.transform.position = new Vector3(0, 0, -10);
            Vector2 target = new Vector2(2, 3);
            Vector2 screen = camera.WorldToScreenPoint(target);
            MouseSensitivitySettings.Set(1);
            Require(gun.AimAtScreen(screen, camera), "Cursor projection");
            Require(Vector2.Distance(gun.AimDirection, target.normalized) < .0001f, "Aim follows cursor placement");
            Vector2 direction = gun.AimDirection;
            MouseSensitivitySettings.Set(2);
            gun.AimAtScreen(screen, camera);
            Require(Vector2.Distance(direction, gun.AimDirection) < .0001f, "Sensitivity must not offset cursor aim");
            tank.transform.position = Vector3.right;
            gun.AimAtScreen(screen, camera);
            Require(Vector2.Distance(gun.AimDirection, (target - Vector2.right).normalized) < .0001f, "Aim tracks stationary cursor while driving");
            direction = gun.AimDirection;
            MouseSensitivitySettings.Reset();
            gun.AimAtScreen(screen, camera);
            Require(Vector2.Distance(direction, gun.AimDirection) < .0001f, "Changing setting does not turn stationary aim");
            Debug.Log("MOUSE_SENSITIVITY_VERIFIED: settings persistence, cursor projection, cursor alignment independent of sensitivity, stationary cursor while driving.");
        }
        finally
        {
            Object.DestroyImmediate(tank);
            Object.DestroyImmediate(cameraObject);
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

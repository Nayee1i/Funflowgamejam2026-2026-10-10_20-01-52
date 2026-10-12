using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;

// Normal Play always starts the complete game. A separate command preserves level testing.
[InitializeOnLoad]
public static class EditorGameLaunch
{
    const string Menu = "Assets/Scenes/MainMenu.unity";
    static EditorGameLaunch()
    {
        EditorApplication.delayCall += SetMenuStart;
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredEditMode) SetMenuStart();
        };
        EditorApplication.update += Check;
    }
    static void SetMenuStart()
    {
        EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(Menu);
    }
    [MenuItem("Bounce Club/Play Current Scene")]
    public static void PlayCurrentScene()
    {
        EditorSceneManager.playModeStartScene = null;
        EditorApplication.isPlaying = true;
    }
    [MenuItem("Bounce Club/Open Main Menu")]
    public static void OpenMenu()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EditorSceneManager.OpenScene(Menu);
        SetMenuStart();
    }
    static void Check()
    {
        if (EditorApplication.isPlaying && SessionState.GetBool("MenuLaunch.Verify", false) && !EditorApplication.isCompiling)
        {
            var menu = UnityEngine.Object.FindAnyObjectByType<MainMenuWindows>();
            if (menu == null || UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "MainMenu") return;
            SessionState.SetBool("MenuLaunch.Verify", false);
            menu.Settings(); bool settings = menu.settingsWindow.activeInHierarchy;
            menu.Play(); bool levels = menu.modesWindow.activeInHierarchy;
            menu.Close();
            File.WriteAllText("Tools/menu-launch-result.txt", settings && levels ? "PASSED: normal Play starts MainMenu, settings and level selection open.\n" : "FAILED: menu controls\n");
            return;
        }
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists("Tools/open-main-menu.request")) return;
        File.Delete("Tools/open-main-menu.request");
        OpenMenu();
        if (EditorSceneManager.GetActiveScene().path != Menu) return;
        SessionState.SetBool("MenuLaunch.Verify", true);
        EditorApplication.isPlaying = true;
    }
}

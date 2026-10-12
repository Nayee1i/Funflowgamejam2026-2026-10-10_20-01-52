using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;

// Normal Play always starts the complete game. A separate command preserves level testing.
[InitializeOnLoad]
public static class EditorGameLaunch
{
    const string Menu = "Assets/Scenes/MainMenu.unity";
    const string Presented = "BounceClub.MainMenuPresented";
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
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
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
        if(!UnityEngine.Application.isBatchMode && !EditorApplication.isCompiling && !EditorApplication.isUpdating && !EditorApplication.isPlayingOrWillChangePlaymode)
        {
            bool requested=File.Exists("Tools/present-main-menu.request");
            if(requested || !SessionState.GetBool(Presented,false))
            {
                if(AssetDatabase.LoadAssetAtPath<SceneAsset>(Menu)==null)return;
                SessionState.SetBool(Presented,true);
                if(requested){File.Delete("Tools/present-main-menu.request");OpenMenu();}
                else
                {
                    // Never replace unsaved scene edits. Run once per editor session,
                    // so recompiling scripts does not interrupt level editing.
                    bool dirty=false;
                    for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)
                        dirty|=UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty;
                    if(!dirty){EditorSceneManager.OpenScene(Menu);SetMenuStart();}
                }
                if(EditorSceneManager.GetActiveScene().path==Menu)
                {
                    var gameView=typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
                    if(gameView!=null)EditorWindow.GetWindow(gameView).Focus();
                    if(requested)File.WriteAllText("Tools/main-menu-presentation-result.txt","MainMenu opened in Edit Mode; Game view focused; Play starts MainMenu.\n");
                }
            }
        }
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

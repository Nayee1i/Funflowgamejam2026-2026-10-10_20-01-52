using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

public static class VerifyProportionNavigation
{
    [InitializeOnLoadMethod] static void Initialize() => EditorApplication.update += Check;
    static void Check()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isUpdating || EditorApplication.isCompiling || !File.Exists("Tools/verify-proportions.request")) return;
        File.Delete("Tools/verify-proportions.request");
        try { Run(); } catch (Exception e) { File.AppendAllText("Tools/proportions-navigation.txt", "FAILED: " + e); Debug.LogException(e); }
        EditorSceneManager.OpenScene("Assets/Scenes/level3.unity");
    }
    static void Run()
    {
        File.WriteAllText("Tools/proportions-navigation.txt", "Checking scaled tank passages...\n");
        bool allClear = true;
        foreach (string name in new[] { "level2", "level3" })
        {
            EditorSceneManager.OpenScene("Assets/Scenes/" + name + ".unity");
            var level = Object.FindAnyObjectByType<FirstLevelRooms>();
            for (int i = 0; i < level.rooms.Length; i++)
            {
                for (int j = 0; j < level.rooms.Length; j++) level.rooms[j].SetActive(i == j);
                var room = level.rooms[i];
                foreach (var target in room.GetComponentsInChildren<TutorialTarget>()) target.Complete();
                foreach (var cover in room.GetComponentsInChildren<DestructibleCover>()) cover.Hit(5);
                foreach (var door in room.GetComponentsInChildren<WorkshopDoor>()) door.Refresh();
                level.player.position = new Vector3(-level.spawnX, level.doorwayY, 0);
                Physics2D.SyncTransforms();
                var reachable = BuildFullSecondLevel.Reachable(level, .06f);
                bool exit = reachable.Any(p => p.x > level.exitX && Mathf.Abs(p.y - level.doorwayY) < level.doorwayHalfHeight);
                File.AppendAllText("Tools/proportions-navigation.txt", name + " room " + (i + 1) + ": cells=" + reachable.Count + " maxX=" + reachable.Max(p => p.x) + " exit=" + exit + "\n");
                if (!exit) { allClear = false; File.AppendAllText("Tools/proportions-navigation.txt", "BLOCKED\n"); }
            }
        }
        if (!allClear) throw new Exception("Some room exits are blocked");
        File.AppendAllText("Tools/proportions-navigation.txt", "PASSED all nine room exits with full enlarged tank radius.\n");
    }
}

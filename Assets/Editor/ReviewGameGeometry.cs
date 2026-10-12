using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

public static class ReviewGameGeometry
{
    [InitializeOnLoadMethod] static void Initialize() => EditorApplication.update += Check;
    static void Check()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating || !File.Exists("Tools/review-geometry.request")) return;
        File.Delete("Tools/review-geometry.request");
        try { Run(); } catch (Exception e) { File.AppendAllText("Tools/game-review-geometry.txt", "FAILED: " + e); Debug.LogException(e); }
        finally { EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity"); }
    }
    static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
    public static void Run()
    {
        File.WriteAllText("Tools/game-review-geometry.txt", "Real movement through authored rooms, not visual-only checks.\n");
        var move = typeof(FirstLevelRooms).GetMethod("MoveAxis", BindingFlags.Instance | BindingFlags.NonPublic);
        int paths = 0, movements = 0, contacts = 0, mirrors = 0;
        foreach (string sceneName in new[] { "Level1Rooms", "level2", "level3" })
        {
            EditorSceneManager.OpenScene("Assets/Scenes/" + sceneName + ".unity");
            var level = Object.FindAnyObjectByType<FirstLevelRooms>();
            for (int roomIndex = 0; roomIndex < level.rooms.Length; roomIndex++)
            {
                for (int j = 0; j < level.rooms.Length; j++) level.rooms[j].SetActive(j == roomIndex);
                typeof(FirstLevelRooms).GetField("<CurrentRoom>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(level, roomIndex);
                var room = level.rooms[roomIndex];
                foreach (var mirror in room.GetComponentsInChildren<MirrorRicochetSurface>())
                {
                    var glow = mirror.GetComponentsInChildren<SpriteRenderer>().Where(s => s.name.StartsWith("Native Mirror Glow")).ToArray();
                    Require(glow.Length > 0 && glow.All(g => g.sprite != null && g.sortingOrder < mirror.GetComponent<SpriteRenderer>().sortingOrder), "Missing/invalid glow: " + mirror.name); mirrors++;
                    foreach(var visual in glow)
                    {
                        var pulse=visual.GetComponent<MirrorGlowPulse>();
                        Require(pulse!=null && Mathf.Approximately(pulse.opacity,.75f),"Missing subdued animated glow");
                        var apply=typeof(MirrorGlowPulse).GetMethod("Apply",BindingFlags.NonPublic|BindingFlags.Instance);
                        typeof(MirrorGlowPulse).GetMethod("OnEnable",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(pulse,null);
                        apply.Invoke(pulse,new object[]{pulse.period*.25f});float bright=visual.color.a;
                        apply.Invoke(pulse,new object[]{pulse.period*.75f});float dim=visual.color.a;
                        Require(bright>dim && bright<=pulse.originalAlpha*.826f && dim>=pulse.originalAlpha*.674f,"Glow pulse exceeds gentle brightness range");
                        apply.Invoke(pulse,new object[]{0f});
                    }
                }
                foreach (var target in room.GetComponentsInChildren<TutorialTarget>()) target.Complete();
                foreach (var cover in room.GetComponentsInChildren<DestructibleCover>()) cover.Hit(5);
                foreach (var door in room.GetComponentsInChildren<WorkshopDoor>()) door.Refresh();
                Physics2D.SyncTransforms();
                Vector2 spawn = new Vector2(-level.spawnX, level.doorwayY);
                float grid = sceneName=="level2" ? .04f : .12f;
                var parent = new Dictionary<Vector2Int, Vector2Int>();
                var queue = new Queue<Vector2Int>(); queue.Enqueue(Vector2Int.zero); parent[Vector2Int.zero] = Vector2Int.zero;
                var directions = new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
                while (queue.Count > 0)
                {
                    var id = queue.Dequeue(); Vector2 p = spawn + (Vector2)id * grid;
                    foreach (var d in directions)
                    {
                        var next = id + d; if (parent.ContainsKey(next)) continue; var q = spawn + (Vector2)next * grid;
                        if (Mathf.Abs(q.x) > level.exitX + .6f || Mathf.Abs(q.y) > 5) continue;
                        if (Physics2D.CircleCast(p, level.playerRadius + .006f, (q - p).normalized, grid).collider != null) continue;
                        parent[next] = id; queue.Enqueue(next);
                    }
                }
                var ids = parent.Keys.ToArray();
                if (!ids.Any(id => (spawn + (Vector2)id * grid).x > level.exitX))
                {
                    File.AppendAllText("Tools/game-review-geometry.txt", "maxX=" + ids.Max(id => (spawn + (Vector2)id * grid).x) + "\n");
                    foreach (var collider in room.GetComponentsInChildren<Collider2D>()) File.AppendAllText("Tools/game-review-geometry.txt", collider.name + " " + collider.bounds + "\n");
                    throw new Exception("Blocked exit " + sceneName + " room " + roomIndex);
                }
                int sampleStride=Mathf.Max(300,ids.Length/12);
                var goals = ids.Where((id, n) => n % sampleStride == 0).Concat(new[] { ids.OrderBy(id => id.x).Last(), ids.OrderBy(id => id.y).First(), ids.OrderBy(id => id.y).Last() }).Distinct();
                foreach (var goal in goals)
                {
                    var path = new List<Vector2Int>(); var id = goal;
                    while (id != Vector2Int.zero) { path.Add(id); id = parent[id]; }
                    path.Reverse(); level.player.position = spawn;
                    foreach (var point in path)
                    {
                        Vector2 destination = spawn + (Vector2)point * grid;
                        for (int step = 0; step < 6 && Vector2.Distance(level.player.position, destination) > .001f; step++)
                        {
                            Vector2 delta = Vector2.ClampMagnitude(destination - (Vector2)level.player.position, .04f);
                            move.Invoke(level, new object[] { new Vector2(delta.x, 0) }); move.Invoke(level, new object[] { new Vector2(0, delta.y) }); movements++;
                        }
                        Require(Vector2.Distance(level.player.position, destination) < .012f, "Real movement stuck: " + sceneName + " room " + roomIndex + " at " + destination);
                    }
                    paths++;
                }
                for (int pass = 0; pass < 5; pass++)
                foreach (Vector2 direction in new[] { Vector2.up, Vector2.down, Vector2.left, Vector2.right })
                {
                    level.player.position = spawn;
                    for (int step = 0; step < 180; step++) move.Invoke(level, new object[] { direction * .06f });
                    Vector2 stopped = level.player.position;
                    for (int step = 0; step < 12; step++) move.Invoke(level, new object[] { -direction * .04f });
                    Require(Vector2.Distance(stopped, level.player.position) > .4f, "Cannot reverse after contact: " + sceneName + " room " + roomIndex); contacts++;
                }
                File.AppendAllText("Tools/game-review-geometry.txt", sceneName + " room " + (roomIndex + 1) + ": " + ids.Length + " reachable cells; actual routes and retreat passed.\n");
            }
        }
        VerifyMouseSensitivity.Verify(); VerifyFirstLevelAimPreview.Verify();
        EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
        var camera = Camera.main;
        var targetBefore = camera.targetTexture;
        var activeBefore = RenderTexture.active;
        var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        var modes = canvases.Select(c => c.renderMode).ToArray();
        var cameras = canvases.Select(c => c.worldCamera).ToArray();
        var planes = canvases.Select(c => c.planeDistance).ToArray();
        BuildFullSecondLevel.Capture("Tools/menu-review-capture.png");
        bool writeFailed = false;
        try { BuildFullSecondLevel.Capture("Tools/__nonexistent_review_directory__/capture.png"); }
        catch (DirectoryNotFoundException) { writeFailed = true; }
        Require(writeFailed, "Capture failure regression did not exercise failed write");
        Require(camera.targetTexture == targetBefore && RenderTexture.active == activeBefore, "Capture left a render target assigned");
        for (int i = 0; i < canvases.Length; i++)
            Require(canvases[i].renderMode == modes[i] && canvases[i].worldCamera == cameras[i] && canvases[i].planeDistance == planes[i], "Capture did not restore canvas");
        File.AppendAllText("Tools/game-review-geometry.txt", "Menu capture and failed-write camera/canvas restoration passed.\n");
        File.AppendAllText("Tools/game-review-geometry.txt", "PASSED: " + paths + " real driving routes, " + movements + " movement steps, " + contacts + " contact/retreat checks, " + mirrors + " mirror glows; mouse sensitivity and real ricochet preview/projectile tests.\n");
    }
}

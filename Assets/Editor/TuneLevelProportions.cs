using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

public static class TuneLevelProportions
{
    [InitializeOnLoadMethod] static void Initialize() => EditorApplication.update += Check;
    static void Check()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating ||
            !File.Exists("Tools/tune-proportions.request")) return;
        string mode = File.ReadAllText("Tools/tune-proportions.request").Trim(); File.Delete("Tools/tune-proportions.request");
        try { Run(mode == "apply"); }
        catch (Exception e) { File.AppendAllText("Tools/proportions-result.txt", "FAILED: " + e); Debug.LogException(e); }
    }
    public static void Run(bool apply)
    {
        var current = EditorSceneManager.GetActiveScene();
        if (current.isDirty) { Directory.CreateDirectory("Tools/Backups"); EditorSceneManager.SaveScene(current, "Tools/Backups/before-proportions-open.unity", true); }
        File.WriteAllText("Tools/proportions-result.txt", "Proportions relative to Level1Rooms.\n");
        float referenceHeight = 0, referenceRadius = 0, referenceEnemySize = 0;
        foreach (string name in new[] { "Level1Rooms", "level2", "level3" })
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/" + name + ".unity");
            var level = Object.FindAnyObjectByType<FirstLevelRooms>();
            var fit = Camera.main.GetComponent<FirstLevelCameraFit>();
            if (name == "Level1Rooms")
            {
                referenceHeight = fit.minimumHalfHeight; referenceRadius = level.playerRadius;
                referenceEnemySize = BodySize(level.rooms[0].GetComponentInChildren<TutorialRobot>(true));
            }
            File.AppendAllText("Tools/proportions-result.txt", name + " hero scale=" + level.player.localScale +
                " radius=" + level.playerRadius + " camera halfHeight=" + fit.minimumHalfHeight + "\n");
            foreach (var robot in level.rooms.SelectMany(r => r.GetComponentsInChildren<TutorialRobot>(true)))
            {
                var circle = robot.GetComponent<CircleCollider2D>();
                float radius = circle.radius * Mathf.Abs(robot.transform.lossyScale.x);
                File.AppendAllText("Tools/proportions-result.txt", robot.name + " radius=" + radius + " ratio=" + radius / level.playerRadius +
                    " bodySize=" + BodySize(robot) + "\n");
            }
            if (!apply || name == "Level1Rooms") continue;
            float multiplier = fit.minimumHalfHeight / referenceHeight;
            float desiredPlayerScale = .7f * multiplier;
            level.player.localScale = Vector3.one * desiredPlayerScale;
            level.playerRadius = referenceRadius * multiplier;
            // FirstLevelTankAim already multiplies its local barrel distance by the tank scale.
            foreach (var robot in level.rooms.SelectMany(r => r.GetComponentsInChildren<TutorialRobot>(true)))
            {
                var circle = robot.GetComponent<CircleCollider2D>();
                bool boss = robot.GetComponent<TutorialTarget>().MaxHealth >= 10;
                float desired = referenceEnemySize * multiplier * (boss ? 1.65f : robot.name.StartsWith("Promoted") ? 1.086f : 1f);
                robot.transform.localScale *= desired / BodySize(robot);
            }
            foreach (var room in level.rooms)
            foreach (var polygon in room.GetComponentsInChildren<PolygonCollider2D>(true))
            {
                if (polygon.GetComponent<MirrorRicochetSurface>() != null) continue;
                if (!polygon.name.Contains("Obstacle") && polygon.GetComponent<DestructibleCover>() == null) continue;
                string marker = "Proportion Scale Applied";
                if (polygon.transform.Find(marker) != null)
                {
                    if (name == "level2" && room == level.rooms[3] && polygon.transform.Find("Wider Tank Passage Applied") == null)
                    {
                        polygon.transform.localScale *= .70f / .86f;
                        new GameObject("Wider Tank Passage Applied").transform.SetParent(polygon.transform, false);
                    }
                    if (name == "level3" && room == level.rooms[3] && polygon.transform.Find("Wider Tank Passage Applied") == null)
                    {
                        polygon.transform.localScale *= .60f / .72f;
                        new GameObject("Wider Tank Passage Applied").transform.SetParent(polygon.transform, false);
                    }
                    continue;
                }
                // Scale visual and collider together, preserving the obstacle's centre and rotation.
                polygon.transform.localScale *= name == "level3" ? .72f : .86f;
                new GameObject(marker).transform.SetParent(polygon.transform, false);
            }
            for (int r = 0; r < level.rooms.Length; r++)
            {
                for (int j = 0; j < level.rooms.Length; j++) level.rooms[j].SetActive(j == r);
                Physics2D.SyncTransforms();
                foreach (var robot in level.rooms[r].GetComponentsInChildren<TutorialRobot>())
                {
                    var body = robot.GetComponent<CircleCollider2D>();
                    Vector3 original = robot.transform.position;
                    bool Fits(Vector3 position)
                    {
                        Vector2 centre = position + robot.transform.TransformVector(body.offset);
                        return !Physics2D.OverlapCircleAll(centre, body.radius * Mathf.Abs(robot.transform.lossyScale.x) + .045f)
                            .Any(c => !c.isTrigger && c != body && !c.transform.IsChildOf(robot.transform));
                    }
                    if (Fits(original)) continue;
                    bool found = false;
                    for (int ring = 1; ring <= 20 && !found; ring++)
                    for (int angle = 0; angle < 32 && !found; angle++)
                    {
                        float a = angle * Mathf.PI / 16;
                        var candidate = original + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0) * ring * .08f;
                        if (!Fits(candidate)) continue;
                        robot.transform.position = candidate; Physics2D.SyncTransforms(); found = true;
                    }
                    if (!found) throw new Exception("No free spawn for " + robot.name);
                }
            }
            for (int j = 0; j < level.rooms.Length; j++) level.rooms[j].SetActive(j == 0);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            BuildFullSecondLevel.Capture("Tools/" + name + "-proportions-preview.png");
            File.AppendAllText("Tools/proportions-result.txt", "APPLIED hero scale=" + level.player.localScale +
                " radius=" + level.playerRadius + "\n");
        }
        EditorSceneManager.OpenScene("Assets/Scenes/level3.unity");
    }
    static float BodySize(TutorialRobot robot)
    {
        var body = robot.GetComponent<SpriteRenderer>();
        if (body == null) body = robot.transform.Find("Body").GetComponent<SpriteRenderer>();
        Vector3 size = Vector3.Scale(body.sprite.bounds.size, body.transform.lossyScale);
        return Mathf.Max(Mathf.Abs(size.x), Mathf.Abs(size.y));
    }
}

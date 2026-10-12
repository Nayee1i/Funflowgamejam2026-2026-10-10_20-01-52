using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

public static class VerifyRobotCoverPatrol
{
    [InitializeOnLoadMethod] static void Initialize() => EditorApplication.update += Check;
    static void Check()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating ||
            !File.Exists("Tools/verify-robot-cover.request")) return;
        File.Delete("Tools/verify-robot-cover.request");
        try { Run(); }
        catch (Exception e) { File.AppendAllText("Tools/robot-cover-result.txt", "FAILED: " + e); Debug.LogException(e); }
        finally { EditorSceneManager.OpenScene("Assets/Scenes/level3.unity"); }
    }
    static void Require(bool value, string message) { if (!value) throw new Exception(message); }
    public static void Run()
    {
        File.WriteAllText("Tools/robot-cover-result.txt", "Checking fixed cover routes in levels 2 and 3.\n");
        int moving = 0, covered = 0, samples = 0;
        foreach (string scene in new[] { "level2", "level3" })
        {
            EditorSceneManager.OpenScene("Assets/Scenes/" + scene + ".unity");
            var level = Object.FindAnyObjectByType<FirstLevelRooms>();
            for (int room = 0; room < level.rooms.Length; room++)
            {
                for (int r = 0; r < level.rooms.Length; r++) level.rooms[r].SetActive(r == room);
                level.player.position = new Vector3(-20, 0, 0);
                Physics2D.SyncTransforms();
                var robots = level.rooms[room].GetComponentsInChildren<TutorialRobot>();
                int ordinal = 0;
                foreach (var robot in robots)
                {
                    var target = robot.GetComponent<TutorialTarget>();
                    if (target.MaxHealth >= 10 || (scene == "level2" && room == 0 && target.IsShielded)) continue;
                    Vector3 initial = robot.transform.position;
                    var patrol = robot.gameObject.AddComponent<RobotCoverPatrol>();
                    patrol.Configure(level, room, ordinal++); patrol.Plan();
                    float distance = Vector2.Distance(patrol.HidePoint, patrol.PeekPoint);
                    File.AppendAllText("Tools/robot-cover-result.txt", scene + " room " + (room + 1) + " " + robot.name +
                        " cover=" + patrol.HasCover + " hide=" + patrol.HidePoint + " peek=" + patrol.PeekPoint + "\n");
                    Require(distance > .6f, "No usable movement route for " + scene + " room " + room + " " + robot.name);
                    Require(!patrol.CanAttack, "Robot attacks while approaching cover");
                    bool attacked = false, retreated = false; float travelled = 0;
                    Vector2 previous = initial;
                    for (int step = 0; step < 1600; step++)
                    {
                        patrol.Tick(.025f); samples++;
                        Vector2 position = robot.transform.position;
                        travelled += Vector2.Distance(previous, position); previous = position;
                        var body = robot.GetComponent<Collider2D>();
                        foreach (var collider in Physics2D.OverlapCircleAll(position, Mathf.Max(body.bounds.extents.x, body.bounds.extents.y) + .01f))
                        {
                            if (collider.isTrigger || collider == body || collider.GetComponentInParent<TutorialTarget>() != null) continue;
                            var separation = body.Distance(collider);
                            Require(!separation.isOverlapped || separation.distance > -.005f, "Robot body intersects geometry: " + collider.name);
                        }
                        if (patrol.CanAttack)
                        {
                            Require(Vector2.Distance(position, patrol.PeekPoint) < .03f, "Attack outside peek point");
                            attacked = true; patrol.NotifyVolley();
                        }
                        if (attacked && !patrol.CanAttack && Vector2.Distance(position, patrol.HidePoint) < .03f)
                        { retreated = true; break; }
                    }
                    Require(travelled > 1f && attacked && retreated, "Patrol never completed peek/volley/retreat cycle");
                    moving++; if (patrol.HasCover) covered++;
                    robot.transform.position = initial; Object.DestroyImmediate(patrol); Physics2D.SyncTransforms();
                }
            }
        }
        File.AppendAllText("Tools/robot-cover-result.txt", "PASSED: " + moving + " moving robots, " + covered +
            " cover routes, " + samples + " movement samples; peek/volley/retreat cycles and stationary exceptions.\n");
    }
}

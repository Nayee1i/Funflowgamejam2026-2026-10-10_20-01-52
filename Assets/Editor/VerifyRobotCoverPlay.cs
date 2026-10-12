using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class VerifyRobotCoverPlay
{
    const string Flag = "RobotCover.RuntimeTest";
    const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
    [InitializeOnLoadMethod] static void Initialize() => EditorApplication.update += Check;
    static void Require(bool ok, string message) { if (!ok) throw new Exception(message); }
    static void Check()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        if (!SessionState.GetBool(Flag, false))
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists("Tools/verify-robot-cover-play.request")) return;
            File.Delete("Tools/verify-robot-cover-play.request");
            File.WriteAllText("Tools/robot-cover-play-result.txt", "Checking runtime patrol integration...\n");
            EditorSceneManager.OpenScene("Assets/Scenes/level2.unity");
            SessionState.SetInt(Flag + ".Scene", 2); SessionState.SetBool(Flag, true);
            EditorGameLaunch.PlayCurrentScene(); return;
        }
        if (!EditorApplication.isPlaying || EditorApplication.isPaused) return;
        var level = Object.FindAnyObjectByType<FirstLevelRooms>();
        int scene = SessionState.GetInt(Flag + ".Scene", 2);
        if (level == null || SceneManager.GetActiveScene().name != "level" + scene ||
            (bool)typeof(FirstLevelRooms).GetField("transitioning", Private).GetValue(level)) return;
        try
        {
            level.GetComponent<FirstLevelPresentation>().CloseModal();
            int moving = 0, volleys = 0;
            foreach (var robot in Object.FindObjectsByType<TutorialRobot>(FindObjectsInactive.Include, FindObjectsSortMode.None)) robot.enabled = false;
            for (int room = 0; room < level.rooms.Length; room++)
            {
                typeof(FirstLevelRooms).GetMethod("ShowRoom", Private).Invoke(level, new object[] { room });
                level.player.position = new Vector3(-20, 0, 0); Physics2D.SyncTransforms();
                foreach (var robot in level.rooms[room].GetComponentsInChildren<TutorialRobot>())
                {
                    var patrol = robot.GetComponent<RobotCoverPatrol>();
                    var target = robot.GetComponent<TutorialTarget>();
                    bool stationary = target.MaxHealth >= 10 || (scene == 2 && room == 0 && target.IsShielded);
                    Require(stationary == (patrol == null), "Stationary exception/runtime attachment mismatch");
                    if (patrol == null) continue;
                    patrol.enabled = false; patrol.Plan(); moving++;
                    var initial = robot.transform.position;
                    level.SetModal(true);
                    typeof(RobotCoverPatrol).GetMethod("Update", Private).Invoke(patrol, null);
                    Require(robot.transform.position == initial, "Patrol moved during modal pause");
                    level.SetModal(false);
                    bool exposed = false;
                    for (int i = 0; i < 1600; i++)
                    {
                        patrol.Tick(.025f);
                        if (patrol.CanAttack) { exposed = true; break; }
                    }
                    Require(exposed, "Runtime patrol cannot reach firing point");
                    if (patrol.HasCover)
                    {
                        level.player.position = new Vector3(-level.spawnX, level.doorwayY, 0); Physics2D.SyncTransforms();
                        typeof(TutorialRobot).GetField("firstShotDelay", Private).SetValue(robot, 0f);
                        typeof(TutorialRobot).GetField("reactionOffset", Private).SetValue(robot, 0f);
                        typeof(TutorialRobot).GetField("nextShot", Private).SetValue(robot, 0f);
                        FirstLevelShot.ClearAll();
                        int beforeFire = Object.FindObjectsByType<FirstLevelShot>(FindObjectsSortMode.None).Length;
                        typeof(TutorialRobot).GetMethod("Update", Private).Invoke(robot, null);
                        Require(Object.FindObjectsByType<FirstLevelShot>(FindObjectsSortMode.None).Length > beforeFire,
                            "Robot does not fire from cover peek point");
                        for (int burst = 0; burst < 8 && (int)typeof(TutorialRobot).GetField("burstShotsRemaining", Private).GetValue(robot) > 0; burst++)
                        {
                            typeof(TutorialRobot).GetField("nextBurstShot", Private).SetValue(robot, 0f);
                            typeof(TutorialRobot).GetMethod("Update", Private).Invoke(robot, null);
                        }
                        volleys++; FirstLevelShot.ClearAll();
                        level.player.position = new Vector3(-20, 0, 0);
                        for (int i = 0; i < 40; i++) patrol.Tick(.025f);
                        Require(!patrol.CanAttack, "Robot did not retreat after volley");
                        typeof(TutorialRobot).GetField("nextShot", Private).SetValue(robot, 0f);
                        int beforeRetreat = Object.FindObjectsByType<FirstLevelShot>(FindObjectsSortMode.None).Length;
                        typeof(TutorialRobot).GetMethod("Update", Private).Invoke(robot, null);
                        Require(Object.FindObjectsByType<FirstLevelShot>(FindObjectsSortMode.None).Length == beforeRetreat,
                            "Robot fired while retreating");
                    }
                    robot.transform.position = initial; Physics2D.SyncTransforms();
                }
            }
            File.AppendAllText("Tools/robot-cover-play-result.txt", "level" + scene + " PASSED: " + moving +
                " runtime patrols, " + volleys + " actual peek volleys; pause and no firing during retreat.\n");
            if (scene == 2)
            { SessionState.SetInt(Flag + ".Scene", 3); SceneManager.LoadScene("level3"); }
            else { SessionState.SetBool(Flag, false); EditorApplication.isPlaying = false; }
        }
        catch (Exception e)
        {
            File.AppendAllText("Tools/robot-cover-play-result.txt", "FAILED: " + e);
            SessionState.SetBool(Flag, false); EditorApplication.isPlaying = false; Debug.LogException(e);
        }
    }
}

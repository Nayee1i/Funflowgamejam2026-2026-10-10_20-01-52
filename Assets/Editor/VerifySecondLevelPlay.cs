using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

// Exercises the real shared systems in Play Mode, including coroutine transitions and impact lifetime.
public static class VerifySecondLevelPlay
{
    const string Key="VerifySecondLevelPlay.",Request="Tools/verify-second-level-play.request";
    static double readyAfter;
    [InitializeOnLoadMethod] static void Hook()=>EditorApplication.update+=Tick;
    static void Require(bool condition,string message){if(!condition)throw new Exception(message);}
    static void Tick()
    {
        if(EditorApplication.isCompiling || EditorApplication.isUpdating){readyAfter=EditorApplication.timeSinceStartup+2;return;}
        if(EditorApplication.timeSinceStartup<readyAfter)return;
        if(!SessionState.GetBool(Key+"active",false))
        {
            if(!File.Exists(Request) || EditorApplication.isPlayingOrWillChangePlaymode)return;
            File.Delete(Request);
            SavePref("Progress.UnlockedLevel",true);SavePref("Progress.Time.1",false);SavePref("Progress.Time.2",false);
            SessionState.SetBool(Key+"active",true);SessionState.SetInt(Key+"stage",0);
            SessionState.SetFloat(Key+"deadline",(float)EditorApplication.timeSinceStartup+40);
            EditorSceneManager.OpenScene("Assets/Scenes/level2.unity");EditorApplication.isPlaying=true;return;
        }
        try
        {
            int stage=SessionState.GetInt(Key+"stage",0);
            if(!EditorApplication.isPlaying)
            {
                if(EditorApplication.isPlayingOrWillChangePlaymode)return;
                if(stage==9){Finish();return;}
                return;
            }
            Require(EditorApplication.timeSinceStartup<SessionState.GetFloat(Key+"deadline",0),"Play verification timed out at stage "+stage);
            var level=Object.FindAnyObjectByType<FirstLevelRooms>();if(level==null)return;
            var hud=level.GetComponent<FirstLevelPresentation>();
            if(stage==0)
            {
                if(!level.CanPlay)return;
                Require(level.rooms.Length==4 && level.GetComponent<SecondLevelWorkshop>()!=null,"Second level initialization.");
                Require(level.player.GetComponent<TutorialPlayer>().Health==3,"Player health initialization.");
                var gun=level.player.GetComponent<FirstLevelTankAim>();
                gun.AimAt((Vector2)level.player.position+Vector2.up);
                for(int i=0;i<4;i++)
                {
                    Require(gun.Fire()!=null,"Shot "+i+" failed.");gun.AdvanceReload(.41f);
                    FirstLevelShot.ClearAll();
                }
                Require(gun.BulletsRemaining==0 && gun.IsReloading && gun.Fire()==null,"Magazine must stop at four shots.");
                level.TogglePause();Require(!level.CanPlay && gun.Fire()==null,"Pause must stop combat.");
                float progress=gun.ReloadProgress;gun.AdvanceReload(5);Require(Mathf.Approximately(progress,gun.ReloadProgress),"Reload advances during pause.");
                level.TogglePause();gun.AdvanceReload(2);Require(gun.BulletsRemaining==4 && !gun.IsReloading,"Reload must restore four shots.");
                VerifyBlue(level);
                BuildFullSecondLevel.Capture("Tools/Level2-play-preview.png");
                FirstLevelShot.ClearAll();FirstLevelEffects.Clear();SessionState.SetInt(Key+"stage",1);return;
            }
            if(stage>=1 && stage<=4)
            {
                int room=stage-1;
                if(level.CurrentRoom!=room)return;
                if(level.ModalOpen)
                {
                    Require(room==1,"Unexpected workshop tutorial panel.");
                    var panel=GameObject.Find("Figma NewRobotHint");
                    if(panel!=null && panel.GetComponent<CanvasGroup>().alpha<.95f)return;
                    BuildFullSecondLevel.Capture("Tools/Level2-new-robot-hint-preview.png");hud.CloseModal();return;
                }
                if(!level.CanPlay)return;
                foreach(var robot in level.rooms[room].GetComponentsInChildren<TutorialTarget>())robot.Complete();
                Require(!level.Finished,"Defeating a room's last robot must not finish the level.");
                if(room<3)
                {
                    level.player.position=new Vector3(level.exitX+.04f,level.doorwayY,0);level.TryRoomExit();
                    Require(!level.CanPlay,"Exit must start fade transition.");SessionState.SetInt(Key+"stage",stage+1);
                }
                else
                {
                    FirstLevelShot.ClearAll();FirstLevelEffects.Clear();SessionState.SetInt(Key+"stage",5);
                }
                return;
            }
            if(stage==5)
            {
                if(FirstLevelEffects.HasPendingImpacts(level))return;
                FirstLevelEffects.Impact(level.player.position,.7f,level,true);
                Require(FirstLevelEffects.HasPendingImpacts(level),"Missing native impact animation.");
                level.player.position=new Vector3(level.exitX+.04f,level.doorwayY,0);level.CompleteLevel();
                Require(!level.Finished,"Victory must wait for the impact animation.");SessionState.SetInt(Key+"stage",6);return;
            }
            if(stage==6)
            {
                if(!level.Finished)return;
                Require(level.CurrentRoom==3 && level.ModalOpen,"Victory panel after actual final exit.");
                Require(LevelProgression.IsUnlocked(3) && LevelProgression.BestTime(2)>=0,"Second-level result/unlock was not recorded.");
                SessionState.SetFloat(Key+"captureAfter",(float)EditorApplication.timeSinceStartup+.6f);SessionState.SetInt(Key+"stage",7);return;
            }
            if(stage==7)
            {
                if(EditorApplication.timeSinceStartup<SessionState.GetFloat(Key+"captureAfter",0))return;
                BuildFullSecondLevel.Capture("Tools/Level2-victory-preview.png");
                File.WriteAllText("Tools/second-level-play-result.txt","PLAY_MODE_PASSED: four-shot magazine, reload, pause, actual blue reflection, 3 fade transitions, Figma robot hint, no premature victory, waiting for impacts, final exit, second-level time and third-level unlock. Test progress restored afterwards.");
                SessionState.SetInt(Key+"stage",9);EditorApplication.isPlaying=false;
            }
        }
        catch(Exception e)
        {
            File.WriteAllText("Tools/second-level-play-result.txt","FAILED: "+e);Debug.LogException(e);
            SessionState.SetInt(Key+"stage",9);EditorApplication.isPlaying=false;
        }
    }
    static void VerifyBlue(FirstLevelRooms level)
    {
        foreach(var mirror in level.rooms[0].GetComponentsInChildren<MirrorRicochetSurface>())
        {
            if(mirror.chargesShot)continue;var polygon=mirror.GetComponent<PolygonCollider2D>();
            for(int i=0;i<polygon.points.Length;i++)
            {
                Vector2 a=polygon.transform.TransformPoint(polygon.points[i]),b=polygon.transform.TransformPoint(polygon.points[(i+1)%polygon.points.Length]);
                var tangent=(b-a).normalized;var normal=new Vector2(-tangent.y,tangent.x);
                foreach(float side in new[]{-1f,1f})
                {
                    var p=(a+b)*.5f+normal*side;var direction=-normal*side;
                    var hit=FirstLevelShotPath.Cast(p,direction,.035f,2);
                    if(hit.mirror!=mirror)continue;
                    var shot=FirstLevelShot.Create(p,direction,level.player.GetComponent<FirstLevelTankAim>().projectileSprite,level);
                    shot.Step(hit.distance+.1f);Require(shot.Reflections==1 && !shot.Charged && Vector2.Dot(shot.Direction,hit.Reflect(direction))>.99f,"Blue mirror must really reflect along its actual surface normal without charging.");
                    return;
                }
            }
        }
        throw new Exception("No physical blue mirror contact found.");
    }
    static void SavePref(string pref,bool integer)
    {
        SessionState.SetBool(Key+pref+"exists",PlayerPrefs.HasKey(pref));
        if(integer)SessionState.SetInt(Key+pref,PlayerPrefs.GetInt(pref));else SessionState.SetFloat(Key+pref,PlayerPrefs.GetFloat(pref));
    }
    static void Finish()
    {
        foreach(string pref in new[]{"Progress.UnlockedLevel","Progress.Time.1","Progress.Time.2"})
        {
            if(!SessionState.GetBool(Key+pref+"exists",false))PlayerPrefs.DeleteKey(pref);
            else if(pref=="Progress.UnlockedLevel")PlayerPrefs.SetInt(pref,SessionState.GetInt(Key+pref,1));
            else PlayerPrefs.SetFloat(pref,SessionState.GetFloat(Key+pref,0));
        }
        PlayerPrefs.Save();SessionState.SetBool(Key+"active",false);
        EditorSceneManager.OpenScene("Assets/Scenes/level2.unity");Debug.Log("SECOND_LEVEL_PLAY_VERIFICATION_COMPLETE");
    }
}

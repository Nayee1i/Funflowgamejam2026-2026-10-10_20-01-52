using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

// Exercise runtime initialization, UI, ammo and real transitions, then discard changes.
public static class VerifyThirdLevelPlay
{
    const string Flag="Castle.PlayVerification";
    static int phase;
    static double next;
    static FirstLevelRooms level;
    [InitializeOnLoadMethod] static void Schedule()=>EditorApplication.update+=Check;
    static void Require(bool ok,string message){if(!ok)throw new Exception(message);}
    static void Check()
    {
        if(EditorApplication.isCompiling || EditorApplication.isUpdating)return;
        if(!SessionState.GetBool(Flag,false))
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists("Tools/verify-third-level-play.request"))return;
            File.Delete("Tools/verify-third-level-play.request");File.WriteAllText("Tools/third-level-play-result.txt","Starting runtime check...\n");
            // Previous automated runs cannot be legitimate sub-ten-second five-room results.
            if(File.Exists("Tools/third-level-play-result.txt") && LevelProgression.BestTime(3)>=0 && LevelProgression.BestTime(3)<10)PlayerPrefs.DeleteKey("Progress.Time.3");
            SessionState.SetFloat(Flag+".Best",LevelProgression.BestTime(3));SessionState.SetInt(Flag+".Unlocked",PlayerPrefs.GetInt("Progress.UnlockedLevel",1));
            EditorSceneManager.OpenScene("Assets/Scenes/level3.unity");SessionState.SetBool(Flag,true);EditorApplication.isPlaying=true;return;
        }
        if(!EditorApplication.isPlaying || EditorApplication.isPaused || EditorApplication.timeSinceStartup<next)return;
        try{Advance();}catch(Exception e){File.AppendAllText("Tools/third-level-play-result.txt","FAILED: "+e);Stop();Debug.LogException(e);}
    }
    static void Stop(){float best=SessionState.GetFloat(Flag+".Best",-1);if(best<0)PlayerPrefs.DeleteKey("Progress.Time.3");else PlayerPrefs.SetFloat("Progress.Time.3",best);PlayerPrefs.SetInt("Progress.UnlockedLevel",SessionState.GetInt(Flag+".Unlocked",1));PlayerPrefs.Save();SessionState.SetBool(Flag,false);phase=0;EditorApplication.isPlaying=false;}
    static void Advance()
    {
        if(phase==0)
        {level=Object.FindAnyObjectByType<FirstLevelRooms>();if(level==null)return;phase=1;next=EditorApplication.timeSinceStartup+1.5;return;}
        var ui=level.GetComponent<FirstLevelPresentation>();
        bool transitioning=(bool)typeof(FirstLevelRooms).GetField("transitioning",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(level);
        if(transitioning)return;
        if(phase==1)
        {
            Require(Object.FindAnyObjectByType<ThirdLevelCastle>()!=null,"Third-level runtime wiring");
            Require(GameObject.Find("Figma DestructibleHint")!=null,"First-room destructible hint");
            BuildFullSecondLevel.Capture("Tools/Level3-hint-runtime.png");ui.CloseModal();
            foreach(var robot in Object.FindObjectsByType<TutorialRobot>(FindObjectsInactive.Include))robot.enabled=false;
            phase=2;next=EditorApplication.timeSinceStartup+.4;return;
        }
        if(phase==2)
        {
            Require(level.CanPlay,"Closing hint resumes gameplay");
            var show=typeof(FirstLevelRooms).GetMethod("ShowRoom",BindingFlags.NonPublic|BindingFlags.Instance);show.Invoke(level,new object[]{1});
            var cover=level.rooms[1].GetComponentInChildren<DestructibleCover>();var center=cover.GetComponent<Collider2D>().bounds.center;
            level.player.position=new Vector3(center.x-3,center.y,0);
            var aim=level.player.GetComponent<FirstLevelTankAim>();aim.AimAt(center);
            var cooldown=typeof(FirstLevelTankAim).GetField("shotCooldown",BindingFlags.Instance|BindingFlags.NonPublic);
            for(int i=0;i<4;i++)
            {
                cooldown.SetValue(aim,0f);var shot=aim.Fire();Require(shot!=null,"Ammo shot "+i);
                if(i<3)
                {
                    for(int step=0;step<200 && !shot.Finished;step++)shot.Step(.05f);
                    Require(cover.Health==2-i,"Real gameplay cover hit "+i);
                    if(i<2){Require(cover.visual.sprite==cover.damageSprites[i+1],"Native damage texture stage "+i);BuildFullSecondLevel.Capture("Tools/Level3-block-hit"+(i+1)+"-runtime.png");}
                    else {Require(!cover.gameObject.activeSelf,"Third hit removes cover");BuildFullSecondLevel.Capture("Tools/Level3-block-debris-runtime.png");}
                }
                FirstLevelShot.ClearAll();
            }
            Require(aim.BulletsRemaining==0 && aim.IsReloading && aim.Fire()==null,"Four-round magazine and empty trigger");
            aim.AdvanceReload(aim.reloadDuration*.5f);Require(aim.IsReloading && aim.BulletsRemaining==0,"Reload remains incomplete halfway");
            aim.AdvanceReload(aim.reloadDuration);Require(!aim.IsReloading && aim.BulletsRemaining==4,"Reload refills four rounds");
            show.Invoke(level,new object[]{0});phase=3;next=EditorApplication.timeSinceStartup+.1;return;
        }
        if(phase>=3 && phase<=6)
        {
            int expected=phase-3;Require(level.CurrentRoom==expected,"Transition into room "+expected);
            foreach(var target in level.rooms[expected].GetComponentsInChildren<TutorialTarget>())target.Complete();
            level.player.position=new Vector3(level.exitX+.1f,level.doorwayY,0);level.TryRoomExit();phase++;next=EditorApplication.timeSinceStartup+1.3;return;
        }
        if(phase==7)
        {
            Require(level.CurrentRoom==4,"Fifth room reached");
            TutorialRobot boss=null;foreach(var robot in level.rooms[4].GetComponentsInChildren<TutorialRobot>())if(robot.name.Contains("Castle Boss"))boss=robot;
            Require(boss!=null && boss.GetComponent<TutorialTarget>().Health==10,"Boss runtime HP initialization");
            var extras=(Transform[])typeof(TutorialRobot).GetField("additionalMuzzles",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(boss);Require(extras.Length==2,"Three boss barrels");
            FirstLevelShot.ClearAll();var fire=typeof(TutorialRobot).GetMethod("Fire",BindingFlags.NonPublic|BindingFlags.Instance);
            fire.Invoke(boss,new object[]{boss.transform.Find("Turret/Muzzle")});foreach(var extra in extras)fire.Invoke(boss,new object[]{extra});
            Require(Object.FindObjectsByType<FirstLevelShot>().Length==3,"Three actual boss projectiles");FirstLevelShot.ClearAll();
            foreach(var target in level.rooms[4].GetComponentsInChildren<TutorialTarget>())target.Complete();
            level.player.position=new Vector3(0,level.doorwayY,0);level.CompleteLevel();Require(!level.Finished,"Killing boss cannot finish level before exit");
            FirstLevelEffects.Impact(new Vector2(2,0),1,level,true);level.player.position=new Vector3(level.exitX+.1f,level.doorwayY,0);level.CompleteLevel();Require(!level.Finished,"Finish waits for explosion");
            phase=8;next=EditorApplication.timeSinceStartup+1.5;return;
        }
        if(phase==8)
        {
            if(FirstLevelEffects.HasPendingImpacts(level))return;
            level.CompleteLevel();Require(level.Finished && GameObject.Find("Figma Victory")!=null,"Victory after final exit and effects");
            phase=9;next=EditorApplication.timeSinceStartup+.6;return;
        }
        if(phase==9)
        {
            if(GameObject.Find("Figma Victory").GetComponent<CanvasGroup>().alpha<=.95f)return;
            BuildFullSecondLevel.Capture("Tools/Level3-victory-runtime.png");
            File.AppendAllText("Tools/third-level-play-result.txt","PASSED: native block damage textures and debris from three real shots, startup fade/hint, four shots/reload, four real room transitions, boss 10 HP and three projectiles, finish only at final exit after explosion, victory UI.\n");Stop();
        }
    }
}

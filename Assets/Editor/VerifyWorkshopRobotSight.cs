using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;

public static class VerifyWorkshopRobotSight
{
    const string Request="Tools/verify-workshop-robot-sight.request";
    static double ready;
    [InitializeOnLoadMethod] static void Hook()=>EditorApplication.update+=Tick;
    static void Tick()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating){ready=EditorApplication.timeSinceStartup+2;return;}
        if(EditorApplication.timeSinceStartup<ready || !File.Exists(Request))return;
        File.Delete(Request);
        try{Run();}catch(Exception e){File.WriteAllText("Tools/workshop-robot-sight-result.txt","FAILED: "+e);Debug.LogException(e);EditorSceneManager.OpenScene("Assets/Scenes/level2.unity");}
    }
    static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/level2.unity");
        var level=Object.FindAnyObjectByType<FirstLevelRooms>();level.GetComponent<SecondLevelWorkshop>().ConfigureCombat(level);
        for(int i=0;i<4;i++)level.rooms[i].SetActive(i==2);
        var robot=level.rooms[2].GetComponentsInChildren<TutorialRobot>().First(r=>r.name.Contains("71:3589"));
        var canSee=typeof(TutorialRobot).GetMethod("CanSeePlayer",BindingFlags.Instance|BindingFlags.NonPublic);
        var clear=typeof(TutorialRobot).GetMethod("HasClearShot",BindingFlags.Instance|BindingFlags.NonPublic);
        var aimField=typeof(TutorialRobot).GetField("visibleAim",BindingFlags.Instance|BindingFlags.NonPublic);
        Vector2 origin=robot.GetComponent<Collider2D>().bounds.center;
        Vector2 visible=default,blocked=default,point=default;bool found=false,covered=false;
        for(float y=2.85f;y>-2.8f && (!found || !covered);y-=.05f)
        for(float x=-5.5f;x<-.5f && (!found || !covered);x+=.05f)
        {
            var p=new Vector2(x,y);if(Physics2D.OverlapCircle(p,level.playerRadius)!=null)continue;
            level.player.position=p;Physics2D.SyncTransforms();
            bool centre=(bool)clear.Invoke(robot,new object[]{origin,p});bool sees=(bool)canSee.Invoke(robot,null);
            if(!centre && sees && !found){visible=p;point=(Vector2)aimField.GetValue(robot);found=true;}
            if(!sees && !covered){blocked=p;covered=true;}
        }
        if(!found || !covered)throw new Exception("Need both partially visible and fully covered tank samples.");
        level.player.position=visible;Physics2D.SyncTransforms();
        var player=level.player.GetComponent<TutorialPlayer>();int hp=player.Health;
        var shot=FirstLevelShot.Create(origin,(point-origin).normalized,level.player.GetComponent<FirstLevelTankAim>().projectileSprite,level);
        shot.SetEnemyShooter(robot.GetComponent<TutorialTarget>());
        for(int i=0;i<1600 && !shot.Finished;i++)shot.Step(.05f);
        if(player.Health!=hp-1)throw new Exception("The visible-edge aim did not actually reach the player.");
        File.WriteAllText("Tools/workshop-robot-sight-result.txt","PASSED: sees exposed side when centre is behind conveyor; real enemy shot hits player; fully hidden tank remains unseen.\nVisible "+visible+" aim "+point+" covered "+blocked);
        InstallFirstLevelEnemies.Verify();
        File.AppendAllText("Tools/workshop-robot-sight-result.txt","\nFIRST_LEVEL_COMBAT_REGRESSION_PASSED.");
        EditorSceneManager.OpenScene("Assets/Scenes/level2.unity");
    }
}

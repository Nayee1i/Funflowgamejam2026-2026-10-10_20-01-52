using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class VerifyRobotBalance
{
    static double ready;
    [InitializeOnLoadMethod] static void Hook()=>EditorApplication.update+=Tick;
    static void Tick()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating){ready=EditorApplication.timeSinceStartup+3;return;}
        const string request="Tools/verify-robot-balance.request";
        if(!File.Exists(request) || EditorApplication.timeSinceStartup<ready)return;
        File.Delete(request);
        var original=SceneManager.GetActiveScene();
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
        try
        {
            var target=new GameObject("Balance test").AddComponent<TutorialTarget>();
            target.transform.position=new Vector3(1000,1000,0);
            target.gameObject.AddComponent<SpriteRenderer>();
            foreach(int hp in new[]{2,4})foreach(bool reflected in new[]{false,true})
            {
                target.gameObject.SetActive(true);target.Configure(false,null,hp,false,hp==4);
                int shots=hp/(reflected?2:1);
                for(int i=1;i<=shots;i++)
                {
                    target.Hit(reflected,false);
                    if(target.Health!=hp-i*(reflected?2:1) || target.IsDestroyed!=(i==shots))throw new Exception("Wrong kill count: "+hp+" HP, reflected="+reflected);
                }
            }
            target.gameObject.SetActive(true);target.Configure(true,null,4,false,true);
            target.Hit(true,false);if(!target.IsShielded || target.Health!=4)throw new Exception("Blue bypassed shield");
            target.Hit(true,true);if(target.IsShielded || target.Health!=4)throw new Exception("Shield break changed body HP");
            foreach(bool reflected in new[]{false,true})
            {
                target.gameObject.SetActive(true);target.Configure(true,null,2);
                target.Hit(true,true);
                if(target.Health!=2 || target.IsShielded || target.IsBoss)throw new Exception("First level shield robot incorrectly treated as boss");
                target.Hit(reflected,false);
                if(reflected ? !target.IsDestroyed : target.Health!=1)throw new Exception("First level shield robot damage");
                if(!reflected){target.Hit(false,false);if(!target.IsDestroyed)throw new Exception("First level shield robot needs more than two direct hits");}
            }
            target.gameObject.SetActive(true);
            target.Configure(false,null,10,true);target.Hit(true,false);
            if(target.Health!=5)throw new Exception("Boss balance changed");
            var robot=target.gameObject.AddComponent<TutorialRobot>();
            target.Configure(false,null,2);if(robot.DamageHalves!=1)throw new Exception("Black outgoing damage changed");
            target.Configure(false,null,4,false,true);if(robot.DamageHalves!=2)throw new Exception("Red outgoing damage changed");
            var mirror=new GameObject("Balance mirror").AddComponent<MirrorRicochetSurface>();mirror.transform.position=new Vector3(1010,1010,0);
            var edge=mirror.gameObject.AddComponent<EdgeCollider2D>();edge.points=new[]{Vector2.left,Vector2.right};Physics2D.SyncTransforms();
            var shot=FirstLevelShot.Create(new Vector2(1010,1009),Vector2.up,null,null);
            if(shot.Damage!=1)throw new Exception("Direct damage");shot.Step(1.1f);
            if(shot.Reflections!=1 || shot.Damage!=2)throw new Exception("Blue ricochet damage");
            mirror.transform.position=new Vector3(1010,1008,0);Physics2D.SyncTransforms();shot.Step(2);
            if(shot.Reflections!=2 || shot.Damage!=2)throw new Exception("Multiple ricochets stacked damage");
            UnityEngine.Object.DestroyImmediate(shot.gameObject);
            File.WriteAllText("Tools/robot-balance-result.txt","PASSED: black 2 direct / 1 ricochet; red 4 direct / 2 ricochets; repeated bounces stay at 2 damage; shield rules, outgoing half/full hearts and boss balance preserved.");
        }
        catch(Exception e){File.WriteAllText("Tools/robot-balance-result.txt","FAILED: "+e);Debug.LogException(e);}
        finally{EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(original);}
    }
}

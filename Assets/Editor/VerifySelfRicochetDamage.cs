using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

public static class VerifySelfRicochetDamage
{
    static double ready;
    [InitializeOnLoadMethod] static void Hook()=>EditorApplication.update+=Tick;
    static void Tick()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating){ready=EditorApplication.timeSinceStartup+2;return;}
        const string request="Tools/verify-self-ricochet.request";
        if(EditorApplication.timeSinceStartup<ready || !File.Exists(request))return;
        File.Delete(request);var original=SceneManager.GetActiveScene();
        var test=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
        try
        {
            // Keep test contacts far from authored geometry without modifying the open scene.
            Vector2 centre=new Vector2(1000,1000);
            var level=new GameObject("Self-hit verification").AddComponent<FirstLevelRooms>();
            level.player=new GameObject("Tank").transform;level.player.position=centre+Vector2.up*2;level.playerRadius=.54f;
            var player=level.player.gameObject.AddComponent<TutorialPlayer>();
            var mirror=new GameObject("Mirror").AddComponent<MirrorRicochetSurface>();mirror.reflectFromCollider=true;
            mirror.transform.position=centre;
            mirror.gameObject.AddComponent<BoxCollider2D>().size=new Vector2(3,.1f);Physics2D.SyncTransforms();
            foreach(bool red in new[]{false,true})
            {
                mirror.chargesShot=red;int hp=player.Health;
                var shot=FirstLevelShot.Create(level.player.position,Vector2.down,null,level);
                shot.Step(1.4f);Require(player.Health==hp,"Unreflected shot hurts its owner.");
                shot.Step(4);Require(player.Health==hp-1 && shot.Finished,"Returning ricochet must hurt once and stop.");
            }
            int remaining=player.Health;
            var stopped=FirstLevelShot.Create(level.player.position,Vector2.down,null,level);stopped.Step(2.2f);
            var wall=new GameObject("Cover").AddComponent<BoxCollider2D>();wall.transform.position=centre+Vector2.up;wall.size=new Vector2(3,.1f);Physics2D.SyncTransforms();
            stopped.Step(4);Require(player.Health==remaining && stopped.Finished,"Wall must stop returning shot before tank.");
            File.WriteAllText("Tools/self-ricochet-result.txt","PASSED: own direct shot is safe; blue/red returning ricochets each take one life and disappear; cover stops returning shots.");
        }
        catch(Exception e){File.WriteAllText("Tools/self-ricochet-result.txt","FAILED: "+e);Debug.LogException(e);}
        finally
        {
            EditorSceneManager.CloseScene(test,true);SceneManager.SetActiveScene(original);
        }
    }
    static void Require(bool condition,string message){if(!condition)throw new Exception(message);}
}

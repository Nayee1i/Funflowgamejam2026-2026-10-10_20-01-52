using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class VerifyUpperToyMirror
{
    static double ready;
    [InitializeOnLoadMethod] static void Hook() => EditorApplication.update += Tick;
    static void Tick()
    {
        const string request="Tools/verify-upper-toy-mirror.request";
        if(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating){ready=EditorApplication.timeSinceStartup+3;return;}
        if(!File.Exists(request) || EditorApplication.timeSinceStartup<ready)return;
        File.Delete(request);
        var original=SceneManager.GetActiveScene();
        var scene=EditorSceneManager.OpenScene(BuildFirstLevelRooms.ScenePath,OpenSceneMode.Additive);
        try
        {
            foreach(var root in scene.GetRootGameObjects())root.transform.position+=new Vector3(1000,1000,0);
            FirstLevelRooms level=null;
            foreach(var root in scene.GetRootGameObjects())if(root.TryGetComponent<FirstLevelRooms>(out var found))level=found;
            for(int i=0;i<level.rooms.Length;i++)level.rooms[i].SetActive(i==1);
            MirrorRicochetSurface mirror=null;
            foreach(var candidate in level.rooms[1].GetComponentsInChildren<MirrorRicochetSurface>())if(candidate.name.Contains("3067"))mirror=candidate;
            if(mirror==null)throw new Exception("Upper mirror missing");
            var path=new List<FirstLevelAimPreview.Segment>();
            int tested=0;
            for(int a=-60;a<=60;a+=10)
            {
                Vector2 direction=new Vector2(Mathf.Sin(a*Mathf.Deg2Rad),Mathf.Cos(a*Mathf.Deg2Rad));
                Vector2 point=mirror.ReflectionPoint;
                Vector2 origin=point-direction*.8f;
                Physics2D.SyncTransforms();
                var contact=FirstLevelShotPath.Cast(origin,direction,.035f,1);
                if(contact.mirror!=mirror)throw new Exception("Incoming blocked at angle "+a+": "+contact.solid);
                var reflected=FirstLevelShotPath.Cast(point+mirror.Reflect(direction)*FirstLevelShotPath.Separation,mirror.Reflect(direction),.035f,.2f);
                if(reflected.Hit)throw new Exception("Reflected shot immediately blocked at angle "+a);
                FirstLevelAimPreview.Trace(origin,direction,.035f,1.05f,path);
                if(path.Count!=2 || path[1].end.y>=path[1].start.y)throw new Exception("Preview failed "+a);
                var shot=FirstLevelShot.Create(origin,direction,null,null);
                shot.Step(1.05f);
                if(shot==null || shot.Finished || shot.Reflections!=1 || shot.Direction.y>=0)throw new Exception("Actual projectile failed "+a);
                UnityEngine.Object.DestroyImmediate(shot.gameObject);tested++;
            }
            File.WriteAllText("Tools/upper-toy-mirror-result.txt","PASSED: "+tested+" incoming angles (-60 to +60), actual projectiles and aim preview reflect inside glass before the top wall. Mirror artwork transform unchanged.");
        }
        catch(Exception e){File.WriteAllText("Tools/upper-toy-mirror-result.txt","FAILED: "+e);Debug.LogException(e);}
        finally{EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(original);}
    }
}

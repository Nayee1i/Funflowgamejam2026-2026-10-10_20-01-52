using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

public static class VerifyAdjustedCastleRicochet
{
    static double ready;
    [InitializeOnLoadMethod] static void Hook()=>EditorApplication.update+=Tick;
    static void Tick()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating){ready=EditorApplication.timeSinceStartup+2;return;}
        const string request="Tools/verify-adjusted-castle-ricochet.request";
        if(EditorApplication.timeSinceStartup<ready || !File.Exists(request))return;
        File.Delete(request);string original=SceneManager.GetActiveScene().path;
        try
        {
            EditorSceneManager.OpenScene("Assets/Scenes/level3.unity");var level=Object.FindAnyObjectByType<FirstLevelRooms>();
            for(int i=0;i<level.rooms.Length;i++)level.rooms[i].SetActive(i==3);
            int checkedTargets=0;
            foreach(var target in level.rooms[3].GetComponentsInChildren<TutorialTarget>())
            {
                if(!target.IsShielded)continue;
                target.Configure(true,target.transform.Find("Shield").gameObject,target.MaxHealth);
                var reachable=BuildFullSecondLevel.Reachable(level,.1f);
                if(!BuildFullSecondLevel.FindChargedRoute(level,target,reachable,out var origin,out var direction))throw new Exception("No charged route to "+target.name);
                level.player.position=origin;var shot=FirstLevelShot.Create(origin,direction,null,level);
                for(int step=0;step<600 && !shot.Finished;step++)shot.Step(.04f);
                if(target.IsShielded)throw new Exception("Actual charged shot missed "+target.name);
                checkedTargets++;
            }
            if(checkedTargets!=2)throw new Exception("Expected two shielded targets");
            File.WriteAllText("Tools/adjusted-castle-ricochet-result.txt","PASSED: actual charged ricochets break both shields in room 3.4 after the approved mirror move; no mirror tuning performed.");
        }
        catch(Exception e){File.WriteAllText("Tools/adjusted-castle-ricochet-result.txt","FAILED: "+e);Debug.LogException(e);}
        finally{EditorSceneManager.OpenScene(original);}
    }
}

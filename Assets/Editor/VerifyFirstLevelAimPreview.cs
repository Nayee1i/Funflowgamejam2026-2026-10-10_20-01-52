using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class VerifyFirstLevelAimPreview
{
    const string Request="Tools/verify-aim-preview.request";
    static double readyAt=-1;
    [InitializeOnLoadMethod]
    static void Schedule(){if(File.Exists(Request))EditorApplication.update+=Pending;}
    static void Pending()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating){readyAt=-1;return;}
        if(readyAt<0){readyAt=EditorApplication.timeSinceStartup+1;return;}
        if(EditorApplication.timeSinceStartup<readyAt)return;
        EditorApplication.update-=Pending;File.Delete(Request);Verify();
    }
    [MenuItem("Bounce Club/Verify Ricochet Aim Preview")]
    public static void Verify()
    {
        EditorSceneManager.OpenScene(BuildFirstLevelRooms.ScenePath);
        var level=UnityEngine.Object.FindAnyObjectByType<FirstLevelRooms>();
        for(int i=0;i<level.rooms.Length;i++)level.rooms[i].SetActive(i==2);
        var gun=level.player.GetComponent<FirstLevelTankAim>();
        var path=new List<FirstLevelAimPreview.Segment>();
        FirstLevelAimPreview.Trace(new Vector2(-.54f,-.55f),Vector2.down,gun.projectileRadius,80,path);
        if(path.Count!=3)throw new Exception("Expected red, blue, then target; got "+path.Count+" segments.");
        if(Vector2.Distance(path[0].end,new Vector2(-.54f,-2.98f))>.05f ||
            Vector2.Distance(path[1].end,new Vector2(.05f,1.2f))>.05f)
            throw new Exception("Prediction did not follow the two-mirror route.");
        var robot=level.rooms[2].GetComponentInChildren<TutorialTarget>();
        if(!robot.IsShielded || robot.IsDestroyed)throw new Exception("Preview modified the target.");
        FirstLevelAimPreview.Trace(new Vector2(-4,-.55f),Vector2.left,gun.projectileRadius,80,path);
        if(path.Count!=1 || Vector2.Distance(path[0].start,path[0].end)>4)
            throw new Exception("Preview reflected from a wall or passed through it.");
        level.player.position=new Vector3(-.54f,-.55f,0);
        gun.AimAt((Vector2)gun.turret.position+Vector2.down);
        var preview=new GameObject("Aim Preview Verification").AddComponent<FirstLevelAimPreview>();preview.Show(gun);
        var camera=Camera.main;var target=new RenderTexture(1920,1080,24);camera.targetTexture=target;
        camera.GetComponent<FirstLevelCameraFit>().Apply();camera.Render();
        var previous=RenderTexture.active;RenderTexture.active=target;
        var image=new Texture2D(1920,1080,TextureFormat.RGB24,false);
        image.ReadPixels(new Rect(0,0,1920,1080),0,0);image.Apply();
        File.WriteAllBytes("Tools/Level1-aim-preview.png",image.EncodeToPNG());
        camera.targetTexture=null;RenderTexture.active=previous;
        UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(image);
        // Existing integration tests fire actual shots through both types of mirrors.
        InstallFirstLevelEnemies.Verify();
        EditorSceneManager.OpenScene(BuildFirstLevelRooms.ScenePath);
        File.WriteAllText("Tools/aim-preview-result.txt","AIM_PREVIEW_VERIFIED: red-to-blue-to-robot prediction, wall stop, no target damage, actual projectile integration tests passed.");
        Debug.Log("AIM_PREVIEW_VERIFIED");
    }
}

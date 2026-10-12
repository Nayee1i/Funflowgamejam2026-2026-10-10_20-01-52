using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class VerifyShieldPerimeter
{
    static double ready;
    [InitializeOnLoadMethod] static void Hook()=>EditorApplication.update+=Tick;
    static void Tick()
    {
        const string request="Tools/verify-shield-perimeter.request";
        if(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating){ready=EditorApplication.timeSinceStartup+2;return;}
        if(EditorApplication.timeSinceStartup<ready || !File.Exists(request))return;
        File.Delete(request);
        var original=SceneManager.GetActiveScene();
        var test=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
        SceneManager.SetActiveScene(test);
        try
        {
            var sprite=Resources.Load<Sprite>("SecondLevel/Art_61_3461");
            if(sprite==null)throw new Exception("Native shield sprite missing");
            var target=new GameObject("Shield perimeter test").AddComponent<TutorialTarget>();
            target.transform.position=new Vector2(1000,1000);
            target.gameObject.AddComponent<CircleCollider2D>().radius=.25f;
            var shield=new GameObject("Native shield");shield.transform.SetParent(target.transform,false);
            shield.AddComponent<SpriteRenderer>().sprite=sprite;shield.transform.localScale=Vector3.one*.2f;
            var mirror=new GameObject("Test red mirror").AddComponent<MirrorRicochetSurface>();mirror.chargesShot=true;
            for(int i=0;i<24;i++)
            {
                target.Configure(true,shield,5);Physics2D.SyncTransforms();
                var bounds=target.ShieldCollider.bounds;
                float angle=i*Mathf.PI*2/24;
                Vector2 edge=(Vector2)bounds.center+new Vector2(Mathf.Cos(angle)*bounds.extents.x,Mathf.Sin(angle)*bounds.extents.y)*.98f;
                Vector2 outward=(edge-(Vector2)bounds.center).normalized;
                mirror.transform.position=edge+outward;
                mirror.transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(outward.y,outward.x)*Mathf.Rad2Deg-90);
                Physics2D.SyncTransforms();
                var contact=FirstLevelShotPath.Cast(edge+outward*.3f,-outward,.035f,.5f);
                if(contact.solid!=target.ShieldCollider)throw new Exception("Preview missed shield edge "+i);
                var shot=FirstLevelShot.Create(edge+outward*.3f,outward,null,null);
                shot.Step(2);
                if(target.IsShielded || target.Health!=5 || shield.activeSelf)throw new Exception("Charged shot missed edge "+i);
            }
            target.Configure(true,shield,5);Physics2D.SyncTransforms();
            var ordinary=FirstLevelShot.Create((Vector2)target.ShieldCollider.bounds.center+Vector2.left*2,Vector2.right,null,null);
            ordinary.Step(2);
            if(!target.IsShielded || target.Health!=5)throw new Exception("Ordinary shot bypassed shield");
            File.WriteAllText("Tools/shield-perimeter-result.txt","PASSED: 24 native shield edge directions register in preview and actual red ricochets; shield disappears, body HP preserved; ordinary shot cannot break shield. Authored scenes untouched.");
        }
        catch(Exception e){File.WriteAllText("Tools/shield-perimeter-result.txt","FAILED: "+e);Debug.LogException(e);}
        finally{EditorSceneManager.CloseScene(test,true);SceneManager.SetActiveScene(original);}
    }
}

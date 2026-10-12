using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class VerifyMirrorFaceAndHint
{
    [InitializeOnLoadMethod] static void Init() => EditorApplication.update += Check;
    static void Check()
    {
        if(EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists("Tools/verify-mirror-face.request"))return;
        File.Delete("Tools/verify-mirror-face.request");
        try
        {
            int count=0;
            foreach(var name in new[]{"Level1Rooms","level2","level3"})
            {
                EditorSceneManager.OpenScene("Assets/Scenes/"+name+".unity");
                foreach(var mirror in UnityEngine.Object.FindObjectsByType<MirrorRicochetSurface>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                {
                    var colliders=mirror.GetComponentsInChildren<Collider2D>();
                    if(!Array.Exists(colliders,c=>c.enabled && !c.isTrigger))throw new Exception("Mirror has no solid collider: "+mirror.name);
                    count++;
                }
            }
            EditorSceneManager.OpenScene("Assets/Scenes/Level1Rooms.unity");
            var ui=UnityEngine.Object.FindAnyObjectByType<FirstLevelPresentation>();ui.EnsureUI();ui.ShowPanel("StartHint");
            foreach(var group in UnityEngine.Object.FindObjectsByType<CanvasGroup>(FindObjectsSortMode.None))group.alpha=1;
            var images=UnityEngine.Object.FindObjectsByType<Image>(FindObjectsSortMode.None);
            Image aim=Array.Find(images,i=>i.name=="UI_119_78"),shot=Array.Find(images,i=>i.name=="UI_119_76");
            Image aimText=Array.Find(images,i=>i.name=="UI_119_79"),shotText=Array.Find(images,i=>i.name=="UI_119_77");
            if(aim.transform.localScale.x>=0 || shot.transform.localScale.x<0)throw new Exception("Incorrect mouse button icons");
            foreach(var pair in new[]{new[]{aim,aimText},new[]{shot,shotText}})
            {
                float a=pair[0].rectTransform.anchoredPosition.x;
                float b=pair[1].rectTransform.anchoredPosition.x+pair[1].rectTransform.sizeDelta.x*.5f;
                if(pair[0]==aim)a-=2; // User-requested optical alignment.
                if(pair[0]==shot)a+=shot.rectTransform.sizeDelta.x*.5f;
                if(Mathf.Abs(a-b)>.01f)throw new Exception("Off-centre label");
            }
            BuildFullSecondLevel.Capture("Tools/controls-hint-review.png");
            File.WriteAllText("Tools/mirror-face-result.txt","PASSED: "+count+" mirrors with physical colliders; left shot/right aim icons and centred labels.\n");
        }
        catch(Exception e){File.WriteAllText("Tools/mirror-face-result.txt","FAILED: "+e);Debug.LogException(e);}
        finally{EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");}
    }
}

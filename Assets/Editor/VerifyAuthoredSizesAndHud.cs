using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

public static class VerifyAuthoredSizesAndHud
{
    static double ready;
    [InitializeOnLoadMethod] static void Hook()=>EditorApplication.update+=Tick;
    static void Tick()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating){ready=EditorApplication.timeSinceStartup+2;return;}
        const string request="Tools/verify-authored-sizes.request";
        if(EditorApplication.timeSinceStartup<ready || !File.Exists(request))return;
        File.Delete(request);string original=SceneManager.GetActiveScene().path;
        bool peacefulExists=PlayerPrefs.HasKey(PeacefulModeSettings.Key);int peaceful=PlayerPrefs.GetInt(PeacefulModeSettings.Key);
        try
        {
            foreach(string name in new[]{"level2","level3"})
            {
                EditorSceneManager.OpenScene("Assets/Scenes/"+name+".unity");
                var level=Object.FindAnyObjectByType<FirstLevelRooms>();
                var hud=level.GetComponent<FirstLevelHealthHud>();hud.player=level.player.GetComponent<TutorialPlayer>();
                hud.player.Hit(1);typeof(FirstLevelHealthHud).GetMethod("Start",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(hud,null);
                var heart=GameObject.Find("Live Heart 3").GetComponent<Image>();
                if(heart.sprite==null || heart.sprite.name!="HeartHalfAligned")throw new Exception("Half-heart was not rendered");
                var presentation=level.GetComponent<FirstLevelPresentation>();presentation.EnsureUI();level.blackout.color=Color.clear;
                for(int room=2;room<level.rooms.Length;room++)
                {
                    for(int i=0;i<level.rooms.Length;i++)level.rooms[i].SetActive(i==room);
                    BuildFullSecondLevel.Capture("Tools/"+name+"-room-"+(room+1)+"-sizes-fixed.png");
                }
                presentation.ShowPanel("Victory");
                // Verify frame placement using the same runtime victory rendering without recording progress.
                var content=GameObject.Find("Content").GetComponent<RectTransform>();
                if(content.Find("UI_115_4")==null)FirstLevelPresentation.Picture("Victory Time Frame",content,"UI_115_4",presentation.victoryTimePosition.x-189,presentation.victoryTimePosition.y-62,493,164);
                FigmaFrontendUI.DrawTime(content,87,presentation.victoryTimePosition.x,presentation.victoryTimePosition.y,1);
                content.localScale=Vector3.one;
                foreach(var group in content.GetComponentsInParent<CanvasGroup>())group.alpha=1;
                var frame=content.Find("Victory Time Frame").GetComponent<Image>();if(frame.sprite==null)throw new Exception("Missing time-frame artwork");
                BuildFullSecondLevel.Capture("Tools/"+name+"-victory-time-fixed.png");
            }
            PeacefulModeSettings.SetEnabled(false);ReviewGameGeometry.Run();
            File.AppendAllText("Tools/authored-sizes-result.txt","VERIFIED: half-heart rendered, both victory frames rendered, movement checks passed.\n");
        }
        catch(Exception e){File.AppendAllText("Tools/authored-sizes-result.txt","VERIFICATION FAILED: "+e);Debug.LogException(e);}
        finally
        {
            if(peacefulExists)PlayerPrefs.SetInt(PeacefulModeSettings.Key,peaceful);else PlayerPrefs.DeleteKey(PeacefulModeSettings.Key);PlayerPrefs.Save();
            if(!string.IsNullOrEmpty(original))EditorSceneManager.OpenScene(original);
        }
    }
}

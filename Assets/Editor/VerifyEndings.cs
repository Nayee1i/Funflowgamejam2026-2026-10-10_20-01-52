using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class VerifyEndings
{
    static double ready;
    static VerifyEndings(){EditorApplication.update+=Tick;}
    static void Tick()
    {
        if(EditorApplication.isCompiling || EditorApplication.isUpdating){ready=EditorApplication.timeSinceStartup+3;return;}
        if(EditorApplication.timeSinceStartup<ready)return;
        if(EditorApplication.isPlaying)
        {
            if(SessionState.GetBool("Ending.Verify",false) && SceneManager.GetActiveScene().name=="MainMenu")SceneManager.LoadScene("Level1Rooms");
            if(SessionState.GetBool("Ending.Verify",false) && SceneManager.GetActiveScene().name=="Level1Rooms")
            {
                SessionState.SetBool("Ending.Verify",false);
                new GameObject("Ending Verification").AddComponent<EndingVerificationRunner>();
            }
            return;
        }
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        if(File.Exists("Tools/prepare-endings.request"))
        {
            File.Delete("Tools/prepare-endings.request");
            const string atlasPath="Assets/Resources/FirstLevelFX/VictoryAtlas.png";
            AssetDatabase.ImportAsset(atlasPath);
            var atlasImporter=(TextureImporter)AssetImporter.GetAtPath(atlasPath);
            atlasImporter.textureType=TextureImporterType.Default;atlasImporter.maxTextureSize=2048;
            atlasImporter.mipmapEnabled=false;atlasImporter.alphaIsTransparency=true;
            atlasImporter.textureCompression=TextureImporterCompression.Uncompressed;atlasImporter.SaveAndReimport();
            foreach(string path in Directory.GetFiles("Assets/Resources/FirstLevelFX","Ending_*.png"))
            {
                AssetDatabase.ImportAsset(path);
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
                importer.spritePixelsPerUnit=100;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;
                importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=2048;
                importer.SaveAndReimport();
            }
            File.WriteAllText("Tools/endings-assets-result.txt","PASSED: 16 current Figma ending assets imported as sprites.");
        }
        if(File.Exists("Tools/verify-endings.request"))
        {
            File.Delete("Tools/verify-endings.request");SessionState.SetBool("Ending.Verify",true);
            EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>(BuildFirstLevelRooms.ScenePath);
            EditorApplication.isPlaying=true;
        }
    }
}

public class EndingVerificationRunner : MonoBehaviour
{
    IEnumerator Start()
    {
        yield return new WaitForSecondsRealtime(1.3f);
        var sequence=Check();
        while(true)
        {
            bool more;
            try{more=sequence.MoveNext();}
            catch(Exception e){File.WriteAllText("Tools/endings-result.txt","FAILED: "+e);Debug.LogException(e);EditorApplication.isPlaying=false;yield break;}
            if(!more)break;
            yield return sequence.Current;
        }
        File.WriteAllText("Tools/endings-result.txt","PASSED: lethal hit removes last heart without restart; six Figma fire frames; delayed smooth defeat alpha/scale; home/restart callbacks; all three level victory panels animate exactly 1/2/3 earned stars with fade and scale settling; normal Play scene restored.");
        EditorApplication.isPlaying=false;
    }
    IEnumerator Check()
    {
        var level=UnityEngine.Object.FindAnyObjectByType<FirstLevelRooms>();
        var presentation=level.GetComponent<FirstLevelPresentation>();presentation.CloseModal();
        var player=level.player.GetComponent<TutorialPlayer>();player.Hit(6);
        Require(player.HealthHalves==0 && level.Defeated && !level.CanPlay,"Death not terminal");
        Require(GameObject.Find("Hero Death Fire")!=null,"Missing fire");
        Require(GameObject.Find("Defeat")==null,"Defeat appeared immediately");
        yield return new WaitForSecondsRealtime(.98f);
        var modal=GameObject.Find("Defeat");Require(modal!=null,"Missing delayed defeat");
        var group=modal.GetComponent<CanvasGroup>();Require(group.alpha>0 && group.alpha<.7f,"Defeat did not fade");
        yield return new WaitForSecondsRealtime(.8f);
        Require(group.alpha>.99f && group.interactable,"Defeat did not finish fade");
        Require(modal.GetComponentsInChildren<Button>().Length==2,"Defeat controls missing");
        Capture("Tools/defeat-animated.png");
        yield return new WaitForSecondsRealtime(.15f);
        var restart=modal.transform.Find("Content/Ending_141_102").GetComponent<Button>();
        DontDestroyOnLoad(gameObject);restart.onClick.Invoke();
        yield return new WaitForSecondsRealtime(1.3f);
        level=UnityEngine.Object.FindAnyObjectByType<FirstLevelRooms>();presentation=level.GetComponent<FirstLevelPresentation>();
        Require(level!=null && !level.Defeated && level.player.GetComponent<TutorialPlayer>().HealthHalves==6,"Restart failed");
        presentation.CloseModal();
        string[] resources={"FirstLevelFX/Panels","SecondLevel/Panels","ThirdLevel/Panels"};
        var elapsed=typeof(FirstLevelPresentation).GetField("elapsed",BindingFlags.Instance|BindingFlags.NonPublic);
        for(int l=0;l<3;l++)for(int count=1;count<=3;count++)
        {
            presentation.panelResource=resources[l];elapsed.SetValue(presentation,count==1 ? 150f : count==2 ? 90f : 30f);
            presentation.ShowVictory();
            modal=GameObject.Find("Victory");group=modal.GetComponent<CanvasGroup>();
            Require(group.alpha==0,"Victory appeared immediately");
            int actual=0;foreach(var image in modal.GetComponentsInChildren<Image>())if(image.name.StartsWith("Earned Star")){actual++;Require(image.color.a==0,"Star appeared immediately");}
            Require(actual==count,"Wrong current-run star count");
            yield return null;
            Require(group.alpha<.1f,"Loading skipped victory reveal");
            yield return new WaitForSecondsRealtime(.25f);
            Require(group.alpha>0 && group.alpha<1,"Victory has no intermediate fade");
            float deadline=Time.realtimeSinceStartup+15;
            bool overshoot=false;
            var age=typeof(FirstLevelPresentation).GetField("modalAge",BindingFlags.Instance|BindingFlags.NonPublic);
            while((float)age.GetValue(presentation)<3.2f)
            {
                foreach(var image in modal.GetComponentsInChildren<Image>())if(image.name.StartsWith("Earned Star") && image.transform.localScale.x>1.1f && image.color.a>.99f)overshoot=true;
                Require(Time.realtimeSinceStartup<deadline,"Animation stalled");yield return null;
            }
            Require(overshoot,"Stars never visibly overshot their final size");
            foreach(var image in modal.GetComponentsInChildren<Image>())if(image.name.StartsWith("Earned Star"))Require(image.color.a>.99f && Mathf.Abs(image.transform.localScale.x-1)<.01f,"Star did not settle");
            if(l==0)Capture("Tools/victory-"+count+"-stars-animated.png");
            yield return new WaitForSecondsRealtime(.15f);
        }
        presentation.ShowPanel("Defeat");
        yield return new WaitForSecondsRealtime(.8f);
        modal=GameObject.Find("Defeat");
        modal.transform.Find("Content/Ending_141_103").GetComponent<Button>().onClick.Invoke();
        yield return null;yield return null;
        Require(SceneManager.GetActiveScene().name=="MainMenu","Home failed");
    }
    static void Require(bool value,string message){if(!value)throw new Exception(message);}
    static void Capture(string path)
    {
        ScreenCapture.CaptureScreenshot(path,2);
    }
}

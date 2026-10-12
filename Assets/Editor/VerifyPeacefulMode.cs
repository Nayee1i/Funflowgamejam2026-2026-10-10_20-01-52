using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class VerifyPeacefulMode
{
    static double ready;
    [InitializeOnLoadMethod] static void Hook()=>EditorApplication.update+=Tick;
    static void Require(bool value,string message){if(!value)throw new Exception(message);}
    static void Tick()
    {
        const string request="Tools/verify-peaceful-mode.request";
        if(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating){ready=EditorApplication.timeSinceStartup+2;return;}
        if(EditorApplication.timeSinceStartup<ready || !File.Exists(request))return;
        File.Delete(request);
        bool exists=PlayerPrefs.HasKey(PeacefulModeSettings.Key);int old=PlayerPrefs.GetInt(PeacefulModeSettings.Key);
        var original=SceneManager.GetActiveScene();
        var test=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(test);
        try
        {
            foreach(string path in Directory.GetFiles("Assets/Resources/FirstLevelFX","UI_139_*.png"))
            {
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
                importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.filterMode=FilterMode.Bilinear;
                importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=4096;importer.SaveAndReimport();
            }
            PeacefulModeSettings.SetEnabled(false);
            var owner=new GameObject("Test level").AddComponent<FirstLevelRooms>();
            owner.player=new GameObject("Test hero").transform;owner.player.position=new Vector2(1000,1000);owner.playerRadius=.54f;
            var player=owner.player.gameObject.AddComponent<TutorialPlayer>();
            var black=new GameObject("Black robot").AddComponent<TutorialTarget>();var blackBrain=black.gameObject.AddComponent<TutorialRobot>();
            var red=new GameObject("Promoted Robot test").AddComponent<TutorialTarget>();var redBrain=red.gameObject.AddComponent<TutorialRobot>();
            Require(blackBrain.DamageHalves==1 && redBrain.DamageHalves==2,"Robot type damage");
            foreach(var target in new[]{black,red})
            {
                var shot=FirstLevelShot.Create((Vector2)owner.player.position+Vector2.left*2,Vector2.right,null,owner);shot.SetEnemyShooter(target);shot.Step(3);
            }
            Require(player.HealthHalves==3 && player.Hearts==1.5f,"Actual black/red shots must leave 1.5 hearts");
            var half=Resources.Load<Sprite>("FirstLevelFX/UI_139_18");Require(half!=null,"Missing Figma half-heart");
            Require(FirstLevelHealthHud.HeartSprite(3,1,null,half,null)==half,"Half-heart rendering");
            var turret=new GameObject("Turret").transform;var muzzle=new GameObject("Muzzle").transform;
            blackBrain.ConfigureFirstLevel(owner,turret,muzzle,null,0);
            var fire=typeof(TutorialRobot).GetMethod("Fire",BindingFlags.NonPublic|BindingFlags.Instance);
            PeacefulModeSettings.SetEnabled(true);int count=UnityEngine.Object.FindObjectsByType<FirstLevelShot>().Length;
            typeof(FirstLevelRooms).GetField("transitioning",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(owner,false);
            black.transform.position=(Vector2)owner.player.position+Vector2.left*4;turret.position=black.transform.position;
            var update=typeof(TutorialRobot).GetMethod("Update",BindingFlags.NonPublic|BindingFlags.Instance);
            update.Invoke(blackBrain,null);
            Require(Vector2.Angle(turret.up,Vector2.right)<.1f,"Peaceful robot does not aim at hero");
            owner.player.position+=(Vector3)Vector2.up*4;update.Invoke(blackBrain,null);
            Require(Vector2.Angle(turret.up,((Vector2)owner.player.position-(Vector2)turret.position).normalized)<.1f,"Peaceful turret does not follow moving hero");
            Require(UnityEngine.Object.FindObjectsByType<FirstLevelShot>().Length==count,"Peaceful tracking fired a shot");
            fire.Invoke(blackBrain,new object[]{muzzle});Require(UnityEngine.Object.FindObjectsByType<FirstLevelShot>().Length==count,"Peaceful robot fired");
            var cancelled=FirstLevelShot.Create((Vector2)owner.player.position+Vector2.left*2,Vector2.right,null,owner);cancelled.SetEnemyShooter(red);cancelled.Step(3);
            Require(player.HealthHalves==3,"Peaceful enemy projectile damaged hero");
            PeacefulModeSettings.SetEnabled(false);fire.Invoke(blackBrain,new object[]{muzzle});
            Require(UnityEngine.Object.FindObjectsByType<FirstLevelShot>().Length==count+1,"Normal fire did not resume");
            var menu=new GameObject("Test menu");menu.SetActive(false);var windows=menu.AddComponent<MainMenuWindows>();
            windows.settingsWindow=new GameObject("Settings",typeof(RectTransform));windows.modesWindow=new GameObject("Modes",typeof(RectTransform));
            Require(FigmaFrontendUI.Configure(windows),"Menu setup");
            var button=windows.modesWindow.transform.Find("peaceful Hit Area").GetComponent<Button>();
            button.onClick.Invoke();Require(PeacefulModeSettings.Enabled,"Toggle on");
            Require(windows.modesWindow.transform.Find("Peaceful Mode Artwork").GetComponent<Image>().sprite.name=="UI_139_8","Enabled artwork");
            button.onClick.Invoke();Require(!PeacefulModeSettings.Enabled,"Toggle off");
            Require(windows.modesWindow.transform.Find("Peaceful Mode Artwork").GetComponent<Image>().sprite.name=="UI_139_7","Disabled artwork");
            CaptureMenu(windows);
            File.WriteAllText("Tools/peaceful-mode-result.txt","PASSED: peaceful turret tracks moving hero without firing; Figma toggle on/off, saved setting, suppressed/resumed robot fire, cancelled enemy shots, actual black/red projectile damage 0.5/1 hearts, native half-heart HUD. User scenes untouched.");
        }
        catch(Exception e){File.WriteAllText("Tools/peaceful-mode-result.txt","FAILED: "+e);Debug.LogException(e);}
        finally
        {
            if(exists)PlayerPrefs.SetInt(PeacefulModeSettings.Key,old);else PlayerPrefs.DeleteKey(PeacefulModeSettings.Key);PlayerPrefs.Save();
            EditorSceneManager.CloseScene(test,true);SceneManager.SetActiveScene(original);
        }
    }
    static void CaptureMenu(MainMenuWindows windows)
    {
        var camera=new GameObject("Preview camera").AddComponent<Camera>();camera.transform.position=new Vector3(1000,1000,-10);
        camera.orthographic=true;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;camera.cullingMask=1<<30;
        var canvas=new GameObject("Preview canvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));
        var c=canvas.GetComponent<Canvas>();c.renderMode=RenderMode.ScreenSpaceCamera;c.worldCamera=camera;c.planeDistance=1;
        var scaler=canvas.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);
        var modes=windows.modesWindow.GetComponent<RectTransform>();modes.SetParent(canvas.transform,false);
        modes.anchorMin=Vector2.zero;modes.anchorMax=Vector2.one;modes.offsetMin=modes.offsetMax=Vector2.zero;
        foreach(var part in canvas.GetComponentsInChildren<Transform>(true))part.gameObject.layer=30;
        var rt=new RenderTexture(1920,1080,24);var previous=RenderTexture.active;Texture2D image=null;
        try
        {
            camera.targetTexture=rt;Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=rt;
            image=new Texture2D(1920,1080,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1920,1080),0,0);image.Apply();File.WriteAllBytes("Tools/peaceful-menu-preview.png",image.EncodeToPNG());
        }
        finally{camera.targetTexture=null;RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(rt);if(image!=null)UnityEngine.Object.DestroyImmediate(image);}
    }
}

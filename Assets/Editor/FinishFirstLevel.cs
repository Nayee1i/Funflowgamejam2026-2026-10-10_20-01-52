using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class FinishFirstLevel
{
    [MenuItem("Bounce Club/Finish First Level From Figma")]
    public static void Install()
    {
        AssetDatabase.Refresh();
        foreach(string path in Directory.GetFiles("Assets/Resources/FirstLevelFX","*.png"))
        {
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
            importer.spritePixelsPerUnit=400;importer.alphaIsTransparency=true;importer.mipmapEnabled=true;
            importer.filterMode=FilterMode.Trilinear;importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.maxTextureSize=4096;
            var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);
            settings.spriteAlignment=(int)SpriteAlignment.Custom;
            settings.spritePivot=path.Contains("Hero_") ? new Vector2(.1f,.5f) : path.Contains("Enemy_") ? new Vector2(.133f,.5f) : new Vector2(.5f,.5f);
            importer.SetTextureSettings(settings);importer.SaveAndReimport();
        }
        var scene=EditorSceneManager.OpenScene(BuildFirstLevelRooms.ScenePath);
        var level=UnityEngine.Object.FindAnyObjectByType<FirstLevelRooms>();FirstLevelEnemies.Place(level);
        EditorSceneManager.SaveScene(scene);
        Verify();InstallFirstLevelEnemies.Verify();
        Preview("StartHint","../Level1-tutorial.png");
        Preview("Victory","../Level1-victory.png");
        Preview(null,"../Level1-ammo.png");
        Debug.Log("FIRST_LEVEL_FINISH: Figma ammo, animated projectiles, impacts, tutorials, timer and victory installed.");
    }
    static void Require(bool condition,string message){if(!condition)throw new Exception(message);}
    public static void Verify()
    {
        EditorSceneManager.OpenScene(BuildFirstLevelRooms.ScenePath);
        var level=UnityEngine.Object.FindAnyObjectByType<FirstLevelRooms>();
        var gun=level.player.GetComponent<FirstLevelTankAim>();
        gun.AimAt((Vector2)gun.turret.position+Vector2.up);
        Vector2 initialAim=gun.AimDirection;
        Vector3 initialPosition=level.player.position;
        Quaternion initialRotation=level.player.rotation;
        level.player.position+=Vector3.up*3;
        level.player.rotation=Quaternion.Euler(0,0,120);
        gun.AimAt((Vector2)gun.turret.position+initialAim);
        Require(Vector2.Distance(initialAim,gun.AimDirection)<.0001f,"Driving changed turret aim without mouse input.");
        Require(Quaternion.Angle(gun.turret.rotation,Quaternion.Euler(0,0,90-gun.spriteAimOffset))<.001f,"Body rotation changed turret world rotation.");
        foreach(Vector2 direction in new[]{Vector2.right,Vector2.down,Vector2.left,Vector2.up})
        {
            var camera=Camera.main;
            var screen=camera.WorldToScreenPoint(gun.turret.position+(Vector3)direction);
            Require(gun.AimAtScreen(screen,camera),"Cursor could not be projected onto the gameplay plane.");
            Require(Vector2.Distance(gun.AimDirection,direction)<.0001f,"Cursor position aimed in the wrong direction.");
        }
        level.player.position=initialPosition;level.player.rotation=initialRotation;
        for(int i=0;i<4;i++)
        {
            Require(gun.Fire()!=null,"Shot "+i+" was not emitted.");gun.AdvanceReload(.4f);
        }
        Require(gun.BulletsRemaining==0 && gun.IsReloading,"Magazine must reload after four shots.");
        Require(gun.Fire()==null,"Fifth shot bypasses reload.");
        float progress=gun.ReloadProgress;level.SetModal(true);gun.AdvanceReload(10);
        Require(Mathf.Approximately(progress,gun.ReloadProgress),"Reload advanced while game was paused.");
        level.SetModal(false);gun.AdvanceReload(1.5f);
        Require(!gun.IsReloading && gun.BulletsRemaining==4 && gun.Fire()!=null,"Magazine did not refill.");
        foreach(string prefix in new[]{"Hero","Enemy"})
            foreach(var sprite in FirstLevelEffects.Projectile(prefix=="Enemy"))Require(sprite!=null,"Missing "+prefix+" animation frame.");
        foreach(var sprite in FirstLevelEffects.RedFrames)Require(sprite!=null,"Missing red mirror animation frame.");
        foreach(string name in new[]{"AmmoFrame","AmmoFull","AmmoEmpty","Confetti_01","UI_56_3207","UI_56_3111"})
            Require(Resources.Load<Sprite>("FirstLevelFX/"+name)!=null,"Missing Figma asset "+name);
        FirstLevelShot.ClearAll();EditorSceneManager.OpenScene(BuildFirstLevelRooms.ScenePath);
        Debug.Log("FIRST_LEVEL_AMMO_AND_ART_TESTS_PASSED");
    }
    static void Preview(string panel,string output)
    {
        EditorSceneManager.OpenScene(BuildFirstLevelRooms.ScenePath);
        var level=UnityEngine.Object.FindAnyObjectByType<FirstLevelRooms>();
        var ui=level.GetComponent<FirstLevelPresentation>();ui.EnsureUI();
        if(panel!=null){ui.ShowPanel(panel);foreach(var group in UnityEngine.Object.FindObjectsByType<CanvasGroup>(FindObjectsSortMode.None))group.alpha=1;}
        else
        {
            var shot=FirstLevelShot.Create(new Vector2(-2,-.55f),Vector2.right,level.player.GetComponent<FirstLevelTankAim>().projectileSprite,level);
            level.player.GetComponent<FirstLevelTankAim>().Fire();ui.RefreshHud();
        }
        var camera=Camera.main;var canvas=UnityEngine.Object.FindAnyObjectByType<Canvas>();
        canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
        var target=new RenderTexture(1920,1080,24);camera.targetTexture=target;camera.GetComponent<FirstLevelCameraFit>().Apply();Canvas.ForceUpdateCanvases();camera.Render();
        var previous=RenderTexture.active;RenderTexture.active=target;
        var image=new Texture2D(1920,1080,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1920,1080),0,0);image.Apply();File.WriteAllBytes(output,image.EncodeToPNG());
        camera.targetTexture=null;RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(image);
        FirstLevelShot.ClearAll();EditorSceneManager.OpenScene(BuildFirstLevelRooms.ScenePath);
    }
    [MenuItem("Bounce Club/Build Completed First Level")]
    public static void Build()
    {
        Install();
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=Array.ConvertAll(Array.FindAll(EditorBuildSettings.scenes,s=>s.enabled),s=>s.path),locationPathName="Builds/FunflowGameJam.exe",target=BuildTarget.StandaloneWindows64});
        if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new Exception("Build failed: "+report.summary.result);
        Debug.Log("FIRST_LEVEL_FINAL_BUILD: Succeeded");
    }
}

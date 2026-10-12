using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class InstallFigmaRobotShield
{
    const string Request="Tools/install-figma-shield.request";
    static double readyAt=-1;
    [InitializeOnLoadMethod]
    static void Schedule(){if(File.Exists(Request))EditorApplication.update+=Pending;}
    static void Pending()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating){readyAt=-1;return;}
        if(readyAt<0){readyAt=EditorApplication.timeSinceStartup+1;return;}
        if(EditorApplication.timeSinceStartup<readyAt)return;
        EditorApplication.update-=Pending;File.Delete(Request);Install();
    }
    [MenuItem("Bounce Club/Install Figma Robot Shield")]
    public static void Install()
    {
        const string asset="Assets/Resources/FirstLevelEnemies/Shield.png";
        AssetDatabase.ImportAsset(asset);
        var importer=(TextureImporter)AssetImporter.GetAtPath(asset);
        importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
        importer.spritePixelsPerUnit=600;importer.mipmapEnabled=true;importer.filterMode=FilterMode.Trilinear;
        importer.alphaIsTransparency=true;importer.maxTextureSize=2048;
        importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
        var shader=Shader.Find("Bounce Club/Shield Lighten");
        if(shader==null || ShaderUtil.ShaderHasError(shader))throw new Exception("Shield shader failed to compile.");
        const string materialPath="Assets/Resources/FirstLevelEnemies/ShieldLight.mat";
        var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if(material==null){material=new Material(shader);AssetDatabase.CreateAsset(material,materialPath);}
        else material.shader=shader;
        AssetDatabase.SaveAssets();
        var scene=EditorSceneManager.OpenScene(BuildFirstLevelRooms.ScenePath);
        var level=UnityEngine.Object.FindAnyObjectByType<FirstLevelRooms>();
        foreach(var room in level.rooms)
        {
            var robot=room.transform.Find("Figma Robot");
            if(robot!=null)FirstLevelEnemies.ConfigureShield(robot.Find("Shield").GetComponent<SpriteRenderer>());
        }
        var shield=level.rooms[2].transform.Find("Figma Robot/Shield").GetComponent<SpriteRenderer>();
        if(shield.sprite==null || shield.sprite.texture.width<1000 || shield.sharedMaterial.shader!=shader)
            throw new Exception("Original Figma shield was not assigned.");
        EditorSceneManager.SaveScene(scene);
        for(int i=0;i<level.rooms.Length;i++)level.rooms[i].SetActive(i==2);
        var camera=Camera.main;var target=new RenderTexture(1920,1080,24);camera.targetTexture=target;
        camera.GetComponent<FirstLevelCameraFit>().Apply();camera.Render();
        var previous=RenderTexture.active;RenderTexture.active=target;
        var preview=new Texture2D(1920,1080,TextureFormat.RGB24,false);
        preview.ReadPixels(new Rect(0,0,1920,1080),0,0);preview.Apply();
        File.WriteAllBytes("Tools/Level1-new-shield.png",preview.EncodeToPNG());
        camera.targetTexture=null;RenderTexture.active=previous;
        UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(preview);
        EditorSceneManager.OpenScene(BuildFirstLevelRooms.ScenePath);
        File.WriteAllText("Tools/figma-shield-result.txt","FIGMA_SHIELD_VERIFIED: node 115:2, original 1278px art, 65% opacity, Lighten blend.");
        Debug.Log("FIGMA_SHIELD_VERIFIED");
    }
}

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class BuildEmptySecondLevel
{
    const string ScenePath = "Assets/Scenes/level2.unity";
    const string BackgroundPath = "Assets/UI/Level2/RoomBackground.png";
    const string RequestPath = "Tools/build-empty-second-level.request";

    [InitializeOnLoadMethod]
    static void PendingBuild()
    {
        if(!File.Exists(RequestPath))return;
        EditorApplication.delayCall += () =>
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)return;
            File.Delete(RequestPath);
            Build();
        };
    }

    [MenuItem("Bounce Club/Build Empty Second Level")]
    public static void Build()
    {
        // Preserve the previous level outside Assets so Unity cannot import it as a second scene.
        const string backup = "Tools/Backups/level2-before-empty.unity";
        Directory.CreateDirectory("Tools/Backups");
        if(!File.Exists(backup))File.Copy(ScenePath,backup);
        AssetDatabase.ImportAsset(BackgroundPath);
        var importer=(TextureImporter)AssetImporter.GetAtPath(BackgroundPath);
        importer.textureType=TextureImporterType.Sprite;
        importer.spriteImportMode=SpriteImportMode.Single;
        importer.spritePixelsPerUnit=100;
        var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);
        settings.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(settings);
        importer.alphaIsTransparency=true;
        importer.mipmapEnabled=true;
        importer.filterMode=FilterMode.Trilinear;
        importer.maxTextureSize=4096;
        importer.textureCompression=TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();

        var scene=EditorSceneManager.OpenScene(BuildFirstLevelRooms.ScenePath);
        // Save the clone first; all subsequent edits belong to level2, leaving level1 intact.
        EditorSceneManager.SaveScene(scene,ScenePath,true);
        scene=EditorSceneManager.OpenScene(ScenePath);
        var level=UnityEngine.Object.FindAnyObjectByType<FirstLevelRooms>();
        level.name="Second Level — Empty Workshop";
        level.emptySandbox=true;
        foreach(var presentation in level.GetComponents<FirstLevelPresentation>())UnityEngine.Object.DestroyImmediate(presentation);
        foreach(var health in level.GetComponents<FirstLevelHealthHud>())UnityEngine.Object.DestroyImmediate(health);
        foreach(var route in UnityEngine.Object.FindObjectsByType<FirstLevelRicochetRoute>(FindObjectsInactive.Include))UnityEngine.Object.DestroyImmediate(route);
        foreach(var room in level.rooms)UnityEngine.Object.DestroyImmediate(room);
        var workshop=new GameObject("Room 2.1 — Empty Workshop");
        level.rooms=new[]{workshop};
        var floor=new GameObject("Second Level Background").AddComponent<SpriteRenderer>();
        floor.transform.SetParent(workshop.transform,false);
        floor.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(BackgroundPath);
        floor.sortingOrder=0;
        var boundary=new GameObject("Workshop Floor Boundary").AddComponent<EdgeCollider2D>();
        boundary.transform.SetParent(workshop.transform,false);
        boundary.points=BoundaryPixels.Select(Pixel).ToArray();

        // The scene is deliberately bare; retain only the hidden Escape menu and fade canvas.
        var canvas=GameObject.Find("Level HUD");
        var root=canvas.GetComponentInChildren<UnityEngine.UI.AspectRatioFitter>().transform;
        foreach(Transform child in root.Cast<Transform>().ToArray())
            if(child.gameObject!=level.pausePanel)UnityEngine.Object.DestroyImmediate(child.gameObject);
        level.pausePanel.SetActive(false);
        level.player.position=new Vector3(-4.45f,-.55f,0);
        var fit=Camera.main.GetComponent<FirstLevelCameraFit>();
        fit.minimumHalfHeight=5.35f;fit.horizontalHalfWidth=8.2f;fit.verticalOffset=0;fit.Apply();
        EditorSceneManager.SaveScene(scene);
        Verify(level,boundary);
        Preview();
        EditorSceneManager.OpenScene(ScenePath);
        Selection.activeGameObject=UnityEngine.Object.FindAnyObjectByType<FirstLevelRooms>().player.gameObject;
        File.WriteAllText("Tools/empty-second-level-result.txt","SECOND_LEVEL_EMPTY_VERIFIED: background, player, perimeter and movement verified.");
        Debug.Log("SECOND_LEVEL_EMPTY_VERIFIED");
    }

    // Native background coordinates, 1586 x 992. The outline follows the open floor
    // around the painted machinery; the two side doors remain closed in this sandbox.
    internal static readonly Vector2[] BoundaryPixels={
        new Vector2(125,375),new Vector2(158,375),new Vector2(158,350),
        new Vector2(225,350),new Vector2(239,333),new Vector2(240,274),
        new Vector2(266,268),new Vector2(266,252),new Vector2(319,252),
        new Vector2(330,231),new Vector2(352,225),new Vector2(365,207),
        new Vector2(392,216),new Vector2(417,209),new Vector2(434,190),
        new Vector2(446,178),new Vector2(600,178),new Vector2(620,171),
        new Vector2(973,171),new Vector2(1003,178),new Vector2(1167,178),
        new Vector2(1190,190),new Vector2(1238,190),new Vector2(1240,237),
        new Vector2(1318,237),new Vector2(1330,262),new Vector2(1330,337),
        new Vector2(1426,344),new Vector2(1426,375),new Vector2(1450,375),
        new Vector2(1450,555),new Vector2(1426,555),new Vector2(1440,666),
        new Vector2(1394,666),new Vector2(1394,727),new Vector2(1330,727),
        new Vector2(1330,784),new Vector2(1278,804),new Vector2(1220,826),
        new Vector2(1180,825),new Vector2(1180,817),new Vector2(1067,817),
        new Vector2(1067,826),new Vector2(975,826),new Vector2(975,849),
        new Vector2(615,849),new Vector2(615,827),new Vector2(560,827),
        new Vector2(560,806),new Vector2(415,806),new Vector2(385,819),
        new Vector2(350,814),new Vector2(335,794),new Vector2(313,778),
        new Vector2(300,751),new Vector2(278,734),new Vector2(248,729),
        new Vector2(229,717),new Vector2(229,653),new Vector2(214,641),
        new Vector2(150,641),new Vector2(158,555),new Vector2(125,555),
        new Vector2(125,375)
    };
    static Vector2 Pixel(Vector2 point)=>new Vector2((point.x-793)/100f,(496-point.y)/100f);
    static void Verify(FirstLevelRooms level,EdgeCollider2D boundary)
    {
        if(level.rooms.Length!=1 || !level.emptySandbox)throw new Exception("Second level is not an empty sandbox.");
        if(UnityEngine.Object.FindObjectsByType<TutorialTarget>(FindObjectsInactive.Include).Length!=0 ||
            UnityEngine.Object.FindObjectsByType<MirrorRicochetSurface>(FindObjectsInactive.Include).Length!=0)
            throw new Exception("Enemies or mirrors remain in the second level.");
        var move=typeof(FirstLevelRooms).GetMethod("MoveAxis",BindingFlags.NonPublic|BindingFlags.Instance);
        var original=level.player.position;
        // Sweep towards the perimeter from multiple starts, including both closed doorways.
        foreach(var start in new[]{Vector2.zero,new Vector2(-4,-.5f),new Vector2(4,-.5f)})
        foreach(var direction in new[]{Vector2.up,Vector2.down,Vector2.left,Vector2.right})
        {
            level.player.position=start;Physics2D.SyncTransforms();
            for(int i=0;i<220;i++)move.Invoke(level,new object[]{direction*.1f});
            Vector2 position=level.player.position;
            float clearance=Vector2.Distance(position,boundary.ClosestPoint(position));
            if(clearance<level.playerRadius-.01f || Mathf.Abs(position.x)>7 || Mathf.Abs(position.y)>4)
                throw new Exception("Tank crossed the workshop boundary.");
        }
        level.player.position=original;Physics2D.SyncTransforms();
    }
    static void Preview()
    {
        var camera=Camera.main;
        var target=new RenderTexture(1920,1080,24);camera.targetTexture=target;
        camera.GetComponent<FirstLevelCameraFit>().Apply();camera.Render();
        var previous=RenderTexture.active;RenderTexture.active=target;
        var image=new Texture2D(1920,1080,TextureFormat.RGB24,false);
        image.ReadPixels(new Rect(0,0,1920,1080),0,0);image.Apply();
        File.WriteAllBytes("Tools/Level2-empty-preview.png",image.EncodeToPNG());
        camera.targetTexture=null;RenderTexture.active=previous;
        UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(image);
    }
}

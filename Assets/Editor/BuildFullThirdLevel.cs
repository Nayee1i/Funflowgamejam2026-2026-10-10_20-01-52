using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

public static class BuildFullThirdLevel
{
    [Serializable] class Mirror { public BuildFullSecondLevel.Art art; public bool red; }
    [Serializable] class Enemy : BuildFullSecondLevel.Enemy { public bool boss; }
    [Serializable] class Room { public string frame; public BuildFullSecondLevel.Art background; public BuildFullSecondLevel.Art[] obstacles,blocks,glows; public Mirror[] mirrors; public Enemy[] enemies; }
    [Serializable] class Layout { public Room[] rooms; public BuildFullSecondLevel.Art[] damageStages; }
    const string Path="Assets/Scenes/level3.unity", Request="Tools/build-full-third-level.request";
    static double readyAfter;
    [InitializeOnLoadMethod] static void Schedule()=>EditorApplication.update+=Check;
    static void Check()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating){readyAfter=EditorApplication.timeSinceStartup+2;return;}
        if(EditorApplication.timeSinceStartup<readyAfter || !File.Exists(Request))return;
        File.Delete(Request);
        try{Run();}catch(Exception e){File.AppendAllText("Tools/full-third-level-result.txt","\nFAILED: "+e);Debug.LogException(e);}
    }
    static Vector2 Point(float x,float y)=>new Vector2((x-960)*.012488189f,(592-y)*.012488189f);
    [MenuItem("Bounce Club/Build Full Third Level From Figma")]
    public static void Run()
    {
        File.WriteAllText("Tools/full-third-level-result.txt","Building castle from Figma...\n");
        Directory.CreateDirectory("Tools/Backups");
        if(File.Exists(Path) && !File.Exists("Tools/Backups/level3-before-full.unity"))File.Copy(Path,"Tools/Backups/level3-before-full.unity");
        AssetDatabase.Refresh();
        foreach(string path in Directory.GetFiles("Assets/Resources/ThirdLevel","*.png"))
        {
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
            importer.spritePixelsPerUnit=100;importer.alphaIsTransparency=true;importer.maxTextureSize=4096;
            importer.textureCompression=TextureImporterCompression.Uncompressed;importer.mipmapEnabled=true;importer.filterMode=FilterMode.Trilinear;
            var importSettings=new TextureImporterSettings();importer.ReadTextureSettings(importSettings);importSettings.spriteMeshType=SpriteMeshType.FullRect;
            importer.SetTextureSettings(importSettings);importer.SaveAndReimport();
        }
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/level2.unity");
        EditorSceneManager.SaveScene(scene,Path,true);scene=EditorSceneManager.OpenScene(Path);
        var level=Object.FindAnyObjectByType<FirstLevelRooms>();
        foreach(var room in level.rooms)Object.DestroyImmediate(room);
        foreach(var wiring in level.GetComponents<SecondLevelWorkshop>())Object.DestroyImmediate(wiring);
        level.gameObject.AddComponent<ThirdLevelCastle>();level.name="Third Level РІР‚вЂќ Figma Castle";level.emptySandbox=false;
        level.doorwayY=Point(960,567).y;level.doorwayHalfHeight=.48f;level.spawnX=6.25f;level.exitX=8.05f;
        level.playerRadius=.54f;level.player.localScale=Vector3.one*.7f;
        var fit=Camera.main.GetComponent<FirstLevelCameraFit>();fit.verticalOffset=.65f;fit.minimumHalfHeight=6.746f;fit.horizontalHalfWidth=12;fit.Apply();
        var presentation=level.GetComponent<FirstLevelPresentation>();presentation.panelResource="ThirdLevel/Panels";
        presentation.roomHints=new[]{"DestructibleHint","","","",""};presentation.victoryTimePosition=new Vector2(926,311);
        var layout=JsonUtility.FromJson<Layout>(Resources.Load<TextAsset>("ThirdLevel/Layout").text);
        level.rooms=new GameObject[layout.rooms.Length];
        // Wall fronts follow the castle artwork. Door recesses extend beyond the transition.
        Vector2[] perimeter={Point(365,493),Point(365,397),Point(425,397),Point(425,324),Point(804,324),Point(804,350),Point(865,350),Point(865,324),Point(1054,324),Point(1054,350),Point(1115,350),Point(1115,324),Point(1495,324),Point(1495,397),Point(1555,397),Point(1555,493),Point(1680,493),Point(1680,633),Point(1555,633),Point(1555,751),Point(1495,751),Point(1495,836),Point(1115,836),Point(1115,815),Point(1054,815),Point(1054,836),Point(865,836),Point(865,815),Point(804,815),Point(804,836),Point(425,836),Point(425,751),Point(365,751),Point(365,633),Point(240,633),Point(240,493),Point(365,493)};
        for(int i=0;i<layout.rooms.Length;i++)
        {
            var data=layout.rooms[i];var room=new GameObject("Room 3."+(i+1)+" РІР‚вЂќ Figma "+data.frame);level.rooms[i]=room;
            BuildFullSecondLevel.Draw(data.background,room.transform,0,"Castle Background","ThirdLevel");
            var boundary=new GameObject("Castle Floor Boundary").AddComponent<EdgeCollider2D>();boundary.transform.SetParent(room.transform,false);boundary.points=perimeter;
            foreach(var art in data.obstacles)BuildFullSecondLevel.Draw(art,room.transform,4,"Castle Obstacle РІР‚вЂќ "+art.node,"ThirdLevel").gameObject.AddComponent<PolygonCollider2D>().points=art.outline;
            foreach(var art in data.blocks)
            {
                var block=BuildFullSecondLevel.Draw(art,room.transform,5,"Destructible Block РІР‚вЂќ "+art.node,"ThirdLevel");
                var holder=new GameObject(block.name);holder.transform.SetParent(room.transform,false);
                holder.transform.SetPositionAndRotation(block.transform.position,block.transform.rotation);holder.transform.localScale=block.transform.localScale;
                block.transform.SetParent(holder.transform,false);block.transform.localPosition=Vector3.zero;block.transform.localRotation=Quaternion.identity;block.transform.localScale=Vector3.one;
                holder.AddComponent<PolygonCollider2D>().points=art.outline;
                var cover=holder.AddComponent<DestructibleCover>();cover.level=level;cover.visual=block;
                cover.damageSprites=new Sprite[4];cover.damageScales=new Vector2[4];cover.damageOffsets=new Vector2[4];
                Vector2 initialMin=new Vector2(art.outline.Min(p=>p.x),art.outline.Min(p=>p.y)),initialMax=new Vector2(art.outline.Max(p=>p.x),art.outline.Max(p=>p.y));
                for(int stage=0;stage<4;stage++)
                {
                    var image=layout.damageStages[stage];cover.damageSprites[stage]=Resources.Load<Sprite>("ThirdLevel/"+image.asset);
                    Vector2 min=new Vector2(image.outline.Min(p=>p.x),image.outline.Min(p=>p.y)),max=new Vector2(image.outline.Max(p=>p.x),image.outline.Max(p=>p.y));
                    cover.damageScales[stage]=new Vector2((initialMax.x-initialMin.x)/(max.x-min.x),(initialMax.y-initialMin.y)/(max.y-min.y));
                    cover.damageOffsets[stage]=(initialMin+initialMax)*.5f-Vector2.Scale((min+max)*.5f,cover.damageScales[stage]);
                }
                cover.ShowDamage(0);
            }
            foreach(var glow in data.glows)BuildFullSecondLevel.Draw(glow,room.transform,6,"Figma Mirror Glow вЂ” "+glow.node,"ThirdLevel");
            foreach(var mirror in data.mirrors)
            {
                var image=BuildFullSecondLevel.Draw(mirror.art,room.transform,7,(mirror.red?"Red":"Blue")+" Mirror РІР‚вЂќ "+mirror.art.node,"ThirdLevel");
                image.gameObject.AddComponent<PolygonCollider2D>().points=mirror.art.outline;
                var surface=image.gameObject.AddComponent<MirrorRicochetSurface>();surface.chargesShot=mirror.red;surface.reflectFromCollider=true;
            }
            foreach(var enemy in data.enemies)
            {
                var brain=BuildFullSecondLevel.BuildEnemy(enemy,room.transform,i,level,"ThirdLevel",enemy.boss?10:0);
                if(!enemy.boss)continue;
                brain.name="Castle Boss РІР‚вЂќ Three Cannons";
                var turret=brain.transform.Find("Turret");var muzzle=turret.Find("Muzzle");
                // Native boss artwork has one forward barrel and two barrels at 120 degrees.
                turret.rotation=Quaternion.Euler(0,0,enemy.head.angle);turret.Find("Figma Turret").localRotation=Quaternion.identity;
                var extras=new Transform[2];
                for(int k=0;k<2;k++)
                {
                    extras[k]=new GameObject(k==0?"Left Cannon":"Right Cannon").transform;extras[k].SetParent(turret,false);
                    extras[k].localRotation=Quaternion.Euler(0,0,k==0?120:-120);extras[k].localPosition=extras[k].localRotation*muzzle.localPosition;
                }
                var config=new SerializedObject(brain);config.FindProperty("additionalMuzzles").arraySize=2;
                for(int k=0;k<2;k++)config.FindProperty("additionalMuzzles").GetArrayElementAtIndex(k).objectReferenceValue=extras[k];
                config.FindProperty("shotInterval").floatValue=2.4f;config.FindProperty("firstShotDelay").floatValue=1.2f;
                config.FindProperty("reactionOffset").floatValue=0;config.ApplyModifiedPropertiesWithoutUndo();
            }
            for(int side=0;side<2;side++)
            {
                var gate=new GameObject(side==0?"Entrance":"Exit");gate.transform.SetParent(room.transform,false);gate.transform.position=Point(side==0?365:1555,567);
                var barrier=gate.AddComponent<BoxCollider2D>();barrier.size=new Vector2(.04f,1.75f);
                var door=gate.AddComponent<WorkshopDoor>();door.level=level;door.room=room;door.barrier=barrier;door.entrance=side==0;door.Refresh();
            }
            room.SetActive(i==0);
        }
        level.player.position=new Vector3(-level.spawnX,level.doorwayY,0);level.pausePanel.SetActive(false);
        EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        var settings=EditorBuildSettings.scenes.ToList();if(!settings.Any(s=>s.path==Path))settings.Add(new EditorBuildSettingsScene(Path,true));else settings.Find(s=>s.path==Path).enabled=true;EditorBuildSettings.scenes=settings.ToArray();
        level.GetComponent<ThirdLevelCastle>().ConfigureCombat(level);presentation.EnsureUI();level.blackout.color=Color.clear;
        for(int i=0;i<5;i++){for(int j=0;j<5;j++)level.rooms[j].SetActive(i==j);BuildFullSecondLevel.Capture("Tools/Level3-room"+(i+1)+"-preview.png");}
        EditorSceneManager.OpenScene(Path);
        File.AppendAllText("Tools/full-third-level-result.txt","CASTLE_CREATED: 5 rooms, 17 enemies, 14 destructible blocks, 8 obstacles; boss 10 HP and three cannons.\n");
    }
}


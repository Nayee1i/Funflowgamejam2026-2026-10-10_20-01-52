using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

// The Figma export is editor-only input; all geometry is saved in an editable scene.
public static class BuildEndlessArena
{
    [Serializable] class Mirror { public BuildFullSecondLevel.Art art; public bool red; }
    [Serializable] class Wave
    {
        public string frame;
        public BuildFullSecondLevel.Art background,hero;
        public BuildFullSecondLevel.Art[] blocks,obstacles;
        public Mirror[] mirrors;
        public BuildFullSecondLevel.Enemy[] enemies;
    }
    [Serializable] class Layout { public Wave[] rooms; }
    const string ScenePath="Assets/Scenes/EndlessArena.unity";
    static Vector2 Point(float x,float y)=>new Vector2((x-960)*.012488189f,(592-y)*.012488189f);
    [MenuItem("Bounce Club/Build Endless Arena From Layout")] 
    public static void Run()
    {
        AssetDatabase.Refresh();
        foreach(var path in Directory.GetFiles("Assets/Resources/Endless","*.png"))
        {
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
            importer.spritePixelsPerUnit=100;importer.alphaIsTransparency=true;importer.maxTextureSize=4096;
            importer.textureCompression=TextureImporterCompression.Uncompressed;importer.mipmapEnabled=true;importer.filterMode=FilterMode.Trilinear;
            var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);settings.spriteMeshType=SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);importer.SaveAndReimport();
        }
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/level2.unity");
        EditorSceneManager.SaveScene(scene,ScenePath,true);scene=EditorSceneManager.OpenScene(ScenePath);
        var level=Object.FindAnyObjectByType<FirstLevelRooms>();
        foreach(var room in level.rooms)Object.DestroyImmediate(room);
        foreach(var wiring in level.GetComponents<SecondLevelWorkshop>())Object.DestroyImmediate(wiring);
        var arena=level.gameObject.AddComponent<EndlessArena>();
        level.gameObject.name="Endless Arena";level.emptySandbox=false;
        level.exitX=100;level.spawnX=5.8f;level.doorwayY=-1;
        var layout=JsonUtility.FromJson<Layout>(Resources.Load<TextAsset>("Endless/Layout").text);
        level.rooms=new GameObject[layout.rooms.Length];arena.waveSpawns=new Vector2[layout.rooms.Length];
        var presentation=level.GetComponent<FirstLevelPresentation>();presentation.panelResource="Endless/Panels";presentation.roomHints=new string[layout.rooms.Length];
        var fit=Camera.main.GetComponent<FirstLevelCameraFit>();fit.minimumHalfHeight=6.746f;fit.horizontalHalfWidth=12;fit.verticalOffset=.65f;
        fit.transform.position=new Vector3(0,.65f,-10);fit.Apply();
        // The visible top of the rails bounds the floor; no phantom door triggers.
        Vector2[] perimeter={Point(396,296),Point(1500,296),Point(1500,844),Point(396,844),Point(396,296)};
        for(int i=0;i<layout.rooms.Length;i++)
        {
            var wave=layout.rooms[i];var room=new GameObject("Wave "+(i+1));level.rooms[i]=room;
            var background=BuildFullSecondLevel.Draw(wave.background,room.transform,0,"Arena Background","Endless");
            var boundary=new GameObject("Arena Floor Boundary").AddComponent<EdgeCollider2D>();boundary.transform.SetParent(room.transform,false);boundary.points=perimeter;
            arena.waveSpawns[i]=new Vector2(wave.hero.x,wave.hero.y);
            foreach(var art in wave.obstacles)
                BuildFullSecondLevel.Draw(art,room.transform,4,"Arena Cover — "+art.node,"Endless").gameObject.AddComponent<PolygonCollider2D>().points=art.outline;
            foreach(var art in wave.blocks)AddBlock(art,room.transform,level);
            foreach(var mirror in wave.mirrors)AddMirror(mirror,room.transform);
            foreach(var enemy in wave.enemies)BuildFullSecondLevel.BuildEnemy(enemy,room.transform,i,level,"Endless");
            room.SetActive(i==0);
        }
        // Match the Figma hero's opaque body, retaining the established aiming rig.
        var hero=layout.rooms[0].hero;var playerBody=level.player.GetComponent<SpriteRenderer>();
        var targetBounds=BoundsOf(hero.outline);var sourceBounds=OpaqueBounds(playerBody.sprite);
        float scale=Mathf.Abs(hero.sx)*targetBounds.size.x/sourceBounds.size.x;
        level.player.localScale=Vector3.one*scale;
        level.player.rotation=Quaternion.Euler(0,0,hero.angle);
        level.playerRadius=Mathf.Min(Mathf.Abs(hero.sx)*targetBounds.size.x,Mathf.Abs(hero.sy)*targetBounds.size.y)*.44f;
        level.player.position=arena.SpawnFor(0);level.pausePanel.SetActive(false);
        EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        var scenes=EditorBuildSettings.scenes.ToList();
        if(!scenes.Any(s=>s.path==ScenePath))scenes.Add(new EditorBuildSettingsScene(ScenePath,true));
        else scenes.Find(s=>s.path==ScenePath).enabled=true;
        EditorBuildSettings.scenes=scenes.ToArray();
        File.WriteAllText("Tools/endless-build-result.txt","CREATED: 3 waves / 12 enemies / 10 Vlad prefab mirrors / 10 destructible covers.\n");
        // Preview wiring is discarded, so the saved scene has no transient HUD copies.
        arena.ConfigureCombat(level);presentation.EnsureUI();level.blackout.color=Color.clear;
        for(int i=0;i<level.rooms.Length;i++)
        {
            for(int j=0;j<level.rooms.Length;j++)level.rooms[j].SetActive(i==j);
            level.player.position=arena.SpawnFor(i);
            BuildFullSecondLevel.Capture("Tools/Endless-wave"+(i+1)+"-preview.png");
        }
        EditorSceneManager.OpenScene(ScenePath);
    }
    static Bounds BoundsOf(IEnumerable<Vector2> points)
    {
        bool first=true;var bounds=new Bounds();
        foreach(var point in points){if(first){bounds=new Bounds(point,Vector3.zero);first=false;}else bounds.Encapsulate(point);}
        return bounds;
    }
    static Bounds SpriteBounds(Sprite sprite)
    {
        var points=new List<Vector2>();var all=new List<Vector2>();
        for(int i=0;i<sprite.GetPhysicsShapeCount();i++){sprite.GetPhysicsShape(i,points);all.AddRange(points);}
        return all.Count>0 ? BoundsOf(all) : sprite.bounds;
    }
    static Bounds OpaqueBounds(Sprite sprite)
    {
        var texture=new Texture2D(2,2);ImageConversion.LoadImage(texture,File.ReadAllBytes(AssetDatabase.GetAssetPath(sprite)));
        var pixels=texture.GetPixels32();int minX=texture.width,minY=texture.height,maxX=-1,maxY=-1;
        for(int y=0;y<texture.height;y++)for(int x=0;x<texture.width;x++)
        {
            if(pixels[y*texture.width+x].a<=180)continue;
            minX=Mathf.Min(minX,x);minY=Mathf.Min(minY,y);maxX=Mathf.Max(maxX,x);maxY=Mathf.Max(maxY,y);
        }
        Object.DestroyImmediate(texture);
        return new Bounds(new Vector3(((minX+maxX)*.5f-sprite.pivot.x)/sprite.pixelsPerUnit,((minY+maxY)*.5f-sprite.pivot.y)/sprite.pixelsPerUnit),
            new Vector3((maxX-minX+1)/sprite.pixelsPerUnit,(maxY-minY+1)/sprite.pixelsPerUnit));
    }
    static void AddMirror(Mirror mirror,Transform parent)
    {
        var art=mirror.art;
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/"+(mirror.red?"RedMirror":"BlueMirror")+".prefab");
        var obj=(GameObject)PrefabUtility.InstantiatePrefab(prefab,parent);
        obj.name=ProjectHierarchyNames.Clean((mirror.red?"Red":"Blue")+" Mirror — "+art.node);
        var renderer=obj.GetComponent<SpriteRenderer>();
        var oldBounds=SpriteBounds(renderer.sprite);var newBounds=BoundsOf(art.outline);
        Vector2 ratio=new Vector2(newBounds.size.x/oldBounds.size.x,newBounds.size.y/oldBounds.size.y);
        var edge=obj.GetComponent<EdgeCollider2D>();
        edge.points=edge.points.Select(p=>(Vector2)newBounds.center+Vector2.Scale(p-(Vector2)oldBounds.center,ratio)).ToArray();
        // Same prefab and physical face; only adapt local coordinates to the native atlas crop.
        renderer.sprite=Resources.Load<Sprite>("Endless/"+art.asset);renderer.sortingOrder=7;
        foreach(var child in obj.GetComponentsInChildren<SpriteRenderer>().Where(r=>r!=renderer))
        {
            child.transform.localPosition=newBounds.center+Vector3.Scale(child.transform.localPosition-oldBounds.center,new Vector3(ratio.x,ratio.y,1));
            child.transform.localScale=Vector3.Scale(child.transform.localScale,new Vector3(ratio.x,ratio.y,1));
        }
        obj.transform.position=new Vector3(art.x,art.y,0);obj.transform.rotation=Quaternion.Euler(0,0,art.angle);obj.transform.localScale=new Vector3(art.sx,art.sy,1);
    }
    static void AddBlock(BuildFullSecondLevel.Art art,Transform parent,FirstLevelRooms level)
    {
        var renderer=BuildFullSecondLevel.Draw(art,parent,5,"Destructible Cover — "+art.node,"Endless");
        var holder=new GameObject(renderer.name);holder.transform.SetParent(parent,false);
        holder.transform.SetPositionAndRotation(renderer.transform.position,renderer.transform.rotation);holder.transform.localScale=renderer.transform.localScale;
        renderer.transform.SetParent(holder.transform,false);renderer.transform.localPosition=Vector3.zero;renderer.transform.localRotation=Quaternion.identity;renderer.transform.localScale=Vector3.one;
        holder.AddComponent<PolygonCollider2D>().points=art.outline;
        var cover=holder.AddComponent<DestructibleCover>();cover.level=level;cover.visual=renderer;
        cover.damageSprites=new Sprite[4];cover.damageScales=new Vector2[4];cover.damageOffsets=new Vector2[4];
        var initial=BoundsOf(art.outline);
        for(int stage=0;stage<4;stage++)
        {
            cover.damageSprites[stage]=stage==0 ? renderer.sprite : Resources.Load<Sprite>("ThirdLevel/BlockDamage_"+stage);
            var bounds=stage==0 ? initial : SpriteBounds(cover.damageSprites[stage]);
            cover.damageScales[stage]=new Vector2(initial.size.x/bounds.size.x,initial.size.y/bounds.size.y);
            cover.damageOffsets[stage]=(Vector2)initial.center-Vector2.Scale(bounds.center,cover.damageScales[stage]);
        }
        cover.ShowDamage(0);
    }
}

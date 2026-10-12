using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class FigmaLevelObstacles
{
    [MenuItem("Bounce Club/Place Figma Level Obstacles")]
    public static void Install()
    {
        AssetDatabase.ImportAsset("Assets/UI/Level1/RedMirrorGame.png");
        foreach(string id in new[]{"56_3042","56_3045","56_3064","56_3067","56_3068","56_3085","56_3086","RedMirrorGame"})
        {
            var importer=(TextureImporter)AssetImporter.GetAtPath("Assets/UI/Level1/"+id+".png");
            importer.textureType=TextureImporterType.Sprite; importer.spriteImportMode=SpriteImportMode.Single;
            Level1TextureQuality.Configure(importer,"Assets/UI/Level1/"+id+".png"); importer.alphaIsTransparency=true; importer.mipmapEnabled=false;
            importer.maxTextureSize=4096; importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
        var scene=EditorSceneManager.OpenScene(BuildFirstLevelRooms.ScenePath);
        var level=Object.FindAnyObjectByType<FirstLevelRooms>();
        for(int i=0;i<3;i++) Add(level.rooms[i].transform,i);
        ApplyBackgroundBlend(level);
        EditorSceneManager.SaveScene(scene);
        VerifyFirstLevelBoundaries.Run();
        VerifyRicochet();
        for(int i=0;i<3;i++) PreviewFirstLevel.RenderRoom(i);
        PreviewFirstLevel.RenderRicochet();
        Debug.Log("FIGMA_OBSTACLES_INSTALLED: 2 / 3 / 3 objects across three rooms.");
    }
    public static void Add(Transform room,int index)
    {
        var previous=room.Find("Figma Obstacles");
        if(previous!=null) Object.DestroyImmediate(previous.gameObject);
        var root=new GameObject("Figma Obstacles").transform; root.SetParent(room,false);
        if(index==0)
        {
            Create(root,"Pyramid — 56:3042","56_3042",614.822f,720.190f,
                new[]{new Vector2(118,5),new Vector2(234,182),new Vector2(5,182)});
            Blocks(root,"56_3045",843.505f,342.469f);
        }
        if(index==1)
        {
            // Center the chest and scale its graphic and collider together for two routes.
            var chest=Create(root,"Toy Chest — 56:3064","56_3064",852.5f,421,
                new[]{new Vector2(5,16),new Vector2(192,16),new Vector2(192,42),new Vector2(226,42),new Vector2(226,278),new Vector2(191,278),new Vector2(191,286),new Vector2(7,286)});
            chest.localPosition=new Vector3(0,-.76f,0);
            chest.localScale=Vector3.one*.7f;
            Mirror(root,"56_3067",806,171,false);
            Mirror(root,"56_3068",806,734,true);
        }
        if(index==2)
        {
            // Figma exports this rotated image without the node's rotation.
            var mirror=Mirror(root,"56_3085",782.834f,231.257f,false);
            var red=Create(root,"Red Mirror — 56:3404","RedMirrorGame",0,0,
                new[]{new Vector2(400,350),new Vector2(990,350),new Vector2(1070,465),new Vector2(1135,570),new Vector2(1100,880),new Vector2(790,970),new Vector2(590,970),new Vector2(300,880),new Vector2(265,570),new Vector2(330,465)});
            red.localScale=Vector3.one*.214f;
            Vector2 redPoint=new Vector2(-.54f,-2.98f);
            Vector2 bluePoint=new Vector2(.05f,1.2f);
            Vector2 bossPoint=new Vector2(3.848f,-1.948f);
            Vector2 between=(bluePoint-redPoint).normalized;
            var redSurface=OrientMirror(red,redPoint,Vector2.down,between,new Vector2(.22f,0),true);
            var blueSurface=OrientMirror(mirror,bluePoint,between,(bossPoint-bluePoint).normalized,new Vector2(0,.07f),false);
            var route=room.GetComponent<FirstLevelRicochetRoute>();
            if(route==null)route=room.gameObject.AddComponent<FirstLevelRicochetRoute>();
            route.redMirror=redSurface; route.blueMirror=blueSurface;
            route.shotOrigin=new Vector2(redPoint.x,-.55f); route.futureBossPosition=bossPoint;
            Blocks(root,"56_3086",996.505f,592.469f);
        }
    }
    private static MirrorRicochetSurface OrientMirror(Transform mirror,Vector2 point,Vector2 incoming,Vector2 outgoing,Vector2 localPoint,bool charges)
    {
        Vector2 normal=(incoming-outgoing).normalized;
        if(normal.y<0)normal=-normal;
        float angle=Mathf.Atan2(-normal.x,normal.y)*Mathf.Rad2Deg;
        mirror.localRotation=Quaternion.Euler(0,0,angle);
        var surface=mirror.GetComponent<MirrorRicochetSurface>();
        if(surface==null)surface=mirror.gameObject.AddComponent<MirrorRicochetSurface>();
        surface.localReflectionPoint=localPoint; surface.chargesShot=charges;
        surface.reflectFromCollider=false;
        mirror.position+=(Vector3)(point-surface.ReflectionPoint);
        return surface;
    }
    public static void ApplyBackgroundBlend(FirstLevelRooms level)
    {
        const string path="Assets/UI/Level1/RoomEdgeBlend.mat";
        var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(material==null){material=new Material(Shader.Find("Bounce Club/Room Edge Blend"));AssetDatabase.CreateAsset(material,path);}
        material.SetColor("_BackgroundColor",Object.FindAnyObjectByType<Camera>().backgroundColor);
        EditorUtility.SetDirty(material);
        foreach(var room in level.rooms) room.transform.Find("Figma — Empty Room Background").GetComponent<SpriteRenderer>().sharedMaterial=material;
        AssetDatabase.SaveAssets();
    }
    public static void VerifyRicochet()
    {
        var level=Object.FindAnyObjectByType<FirstLevelRooms>();
        for(int i=0;i<3;i++)level.rooms[i].SetActive(i==2);
        Physics2D.SyncTransforms();
        try
        {
            var route=level.rooms[2].GetComponent<FirstLevelRicochetRoute>();
            Vector2 red=route.redMirror.ReflectionPoint,blue=route.blueMirror.ReflectionPoint;
            if(Vector2.Angle(route.redMirror.Reflect(Vector2.down),blue-red)>.01f ||
               Vector2.Angle(route.blueMirror.Reflect(blue-red),route.futureBossPosition-blue)>.01f)
                throw new System.Exception("Ricochet angles do not reach the Figma target.");
            CheckSegment(route.shotOrigin,red,route.redMirror);
            CheckSegment(red,blue,route.blueMirror);
            CheckSegment(blue,route.futureBossPosition,null);
            Debug.Log("RICOCHET_ROUTE_VERIFIED: down -> red -> blue -> Figma robot position; unobstructed. Mirror angles: "+route.redMirror.transform.eulerAngles.z+", "+route.blueMirror.transform.eulerAngles.z);
        }
        finally {for(int i=0;i<3;i++)level.rooms[i].SetActive(i==0);EditorSceneManager.SaveScene(level.gameObject.scene);}
    }
    private static void CheckSegment(Vector2 start,Vector2 end,MirrorRicochetSurface destination)
    {
        Vector2 direction=(end-start).normalized;
        foreach(var hit in Physics2D.CircleCastAll(start,.035f,direction,Vector2.Distance(start,end)))
        {
            var mirror=hit.collider.GetComponent<MirrorRicochetSurface>();
            if(hit.distance<.01f || mirror==destination)continue;
            throw new System.Exception("Ricochet path obstructed by "+hit.collider.name+" at "+hit.point);
        }
    }
    private static void Blocks(Transform root,string id,float x,float y)
    {
        Create(root,"Blocks — "+id,id,x,y,new[]{new Vector2(7,6),new Vector2(110,6),new Vector2(120,47),new Vector2(224,47),new Vector2(227,152),new Vector2(269,152),new Vector2(269,261),new Vector2(30,261),new Vector2(30,154),new Vector2(7,154)});
    }
    private static Transform Mirror(Transform root,string id,float x,float y,bool upsideDown)
    {
        float cx=id=="56_3085"?177.5f:175;
        float cy=upsideDown?174:157;
        var points=new Vector2[24];
        for(int i=0;i<points.Length;i++)
        {
            float angle=i*Mathf.PI*2/points.Length;
            points[i]=new Vector2(cx+87*Mathf.Cos(angle),cy+67*Mathf.Sin(angle));
        }
        var mirror=Create(root,"Blue Mirror — "+id,id,x,y,points);
        var surface=mirror.gameObject.AddComponent<MirrorRicochetSurface>();
        surface.localReflectionPoint=new Vector2(0,(id=="56_3085"?164:161.5f)-cy)/100f;
        surface.chargesShot=false;
        surface.reflectFromCollider=false;
        return mirror;
    }
    private static Transform Create(Transform root,string name,string id,float x,float y,Vector2[] pixels)
    {
        var sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Level1/"+id+".png");
        if(sprite==null) sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Level1/"+id+".asset");
        var graphic=new GameObject(name,typeof(SpriteRenderer),typeof(PolygonCollider2D));
        graphic.transform.SetParent(root,false);
        float width=sprite.rect.width/sprite.pixelsPerUnit*100,height=sprite.rect.height/sprite.pixelsPerUnit*100;
        graphic.transform.localPosition=new Vector3((x+width*.5f-960)/100,(540-y-height*.5f)/100,0);
        var renderer=graphic.GetComponent<SpriteRenderer>(); renderer.sprite=sprite; renderer.sortingOrder=5;
        var points=new Vector2[pixels.Length];
        for(int i=0;i<points.Length;i++) points[i]=new Vector2((pixels[i].x-width*.5f)/100,(height*.5f-pixels[i].y)/100);
        graphic.GetComponent<PolygonCollider2D>().points=points;
        return graphic.transform;
    }
}

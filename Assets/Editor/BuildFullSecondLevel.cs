using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;

// Repeatable scene assembly from native Figma layers, with no runtime layout duplication.
public static class BuildFullSecondLevel
{
    [Serializable] public class Art { public string node,asset; public float x,y,angle,sx,sy,radius,w,h; public float[] opaque; public Vector2[] outline; }
    [Serializable] class Mirror { public Art art; public bool red; }
    [Serializable] public class Enemy { public Art body,head,shield; public bool promoted; }
    [Serializable] class Room { public string frame; public Art[] obstacles; public Mirror[] mirrors; public Enemy[] enemies; }
    [Serializable] class Layout { public Room[] rooms; }
    [Serializable] class Perimeter { public Vector2[] points; }
    const string ScenePath="Assets/Scenes/level2.unity",Request="Tools/build-full-second-level.request";
    static double readyAfter;
    [InitializeOnLoadMethod] static void Schedule()=>EditorApplication.update+=Check;
    static void Check()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating){readyAfter=EditorApplication.timeSinceStartup+2;return;}
        if(EditorApplication.timeSinceStartup<readyAfter || !File.Exists(Request))return;
        File.Delete(Request);
        try{Run();}catch(Exception e){File.AppendAllText("Tools/full-second-level-result.txt","\nFAILED: "+e);Debug.LogException(e);}
    }
    static string Hash(string path)=>Convert.ToBase64String(SHA256.Create().ComputeHash(File.ReadAllBytes(path)));
    [MenuItem("Bounce Club/Build Full Second Level From Figma")]
    public static void Run()
    {
        File.WriteAllText("Tools/full-second-level-result.txt","Building second level...\n");
        string firstHash=Hash(BuildFirstLevelRooms.ScenePath);
        Directory.CreateDirectory("Tools/Backups");
        const string backup="Tools/Backups/level2-before-full.unity";
        if(!File.Exists(backup))File.Copy(ScenePath,backup);
        EditorSceneManager.OpenScene(ScenePath);
        var previous=Object.FindAnyObjectByType<FirstLevelRooms>();
        Vector2[] perimeter=previous.rooms[0].GetComponentInChildren<EdgeCollider2D>().points;
        const string accepted="Tools/FigmaLevel2/AcceptedPerimeter.json";
        if(File.Exists(accepted))perimeter=JsonUtility.FromJson<Perimeter>(File.ReadAllText(accepted)).points;
        else File.WriteAllText(accepted,JsonUtility.ToJson(new Perimeter{points=perimeter},true));
        var previousFit=Camera.main.GetComponent<FirstLevelCameraFit>();
        float halfHeight=previousFit.minimumHalfHeight,width=previousFit.horizontalHalfWidth,offset=previousFit.verticalOffset;
        // Repeat builds must restore the accepted tank, not inherit a previous experimental preview.
        Vector3 tankScale=Vector3.one*.7f;
        float speed=previous.speed,radius=.54f;
        AssetDatabase.Refresh();
        foreach(string path in Directory.GetFiles("Assets/Resources/SecondLevel","*.png"))
        {
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
            importer.spritePixelsPerUnit=100;importer.alphaIsTransparency=true;importer.maxTextureSize=4096;
            importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled=true;importer.filterMode=FilterMode.Trilinear;
            var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);settings.spriteMeshType=SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);importer.SaveAndReimport();
        }
        var scene=EditorSceneManager.OpenScene(BuildFirstLevelRooms.ScenePath);
        EditorSceneManager.SaveScene(scene,ScenePath,true);scene=EditorSceneManager.OpenScene(ScenePath);
        var level=Object.FindAnyObjectByType<FirstLevelRooms>();
        level.name="Second Level — Figma Workshop";level.emptySandbox=false;
        level.doorwayY=.31f;level.doorwayHalfHeight=.35f;level.exitX=7.25f;level.spawnX=5.25f;
        level.speed=speed;
        // The empty sandbox had no HUD. Restore the gameplay composition from the 1920x1080 frames.
        level.playerRadius=radius;level.player.localScale=tankScale;
        var fit=Camera.main.GetComponent<FirstLevelCameraFit>();fit.minimumHalfHeight=6.746f;fit.horizontalHalfWidth=12;
        fit.verticalOffset=1.0615f;fit.Apply();
        foreach(var old in level.rooms)Object.DestroyImmediate(old);
        foreach(var route in Object.FindObjectsByType<FirstLevelRicochetRoute>(FindObjectsInactive.Include,FindObjectsSortMode.None))Object.DestroyImmediate(route);
        level.gameObject.AddComponent<SecondLevelWorkshop>();
        var presentation=level.GetComponent<FirstLevelPresentation>();if(presentation==null)presentation=level.gameObject.AddComponent<FirstLevelPresentation>();
        presentation.panelResource="SecondLevel/Panels";presentation.roomHints=new[]{"","NewRobotHint","",""};
        presentation.victoryTimePosition=new Vector2(926,311);
        // An unsaved runtime pause controller must not be carried over with missing event bindings.
        var pause=level.pausePanel.GetComponent<FigmaPauseMenu>();if(pause!=null)Object.DestroyImmediate(pause);
        var layout=JsonUtility.FromJson<Layout>(Resources.Load<TextAsset>("SecondLevel/Layout").text);
        level.rooms=new GameObject[layout.rooms.Length];
        for(int i=0;i<level.rooms.Length;i++)
        {
            var data=layout.rooms[i];var room=new GameObject("Room 2."+(i+1)+" — Figma "+data.frame);level.rooms[i]=room;
            var floor=new GameObject("Figma Workshop Background").AddComponent<SpriteRenderer>();floor.transform.SetParent(room.transform,false);
            floor.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Level2/RoomBackground.png");floor.sortingOrder=0;
            floor.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/UI/Level1/RoomEdgeBlend.mat");
            var edge=new GameObject("Workshop Floor Boundary").AddComponent<EdgeCollider2D>();edge.transform.SetParent(room.transform,false);
            edge.points=OpenDoorways(perimeter);
            foreach(var art in data.obstacles)
            {
                var picture=Draw(art,room.transform,4,"Figma Obstacle "+art.node);
                // Approved adjustment: the upper conveyor in 2.2 is 5% smaller so the hero fits.
                if(art.node=="71:3602" || art.node=="71:3652")picture.transform.localScale*=.95f;
                if(art.node=="71:3652")picture.transform.position+=Vector3.up*(44f*1586f/1270f/100f);
                if(art.node=="71:3575")
                {
                    // Shorten the marked upper conveyor while keeping its lower edge in place.
                    var bottom= new Vector3(0,art.outline.Min(p=>p.y),0);
                    var anchor=picture.transform.TransformPoint(bottom);
                    var shorter=picture.transform.localScale;shorter.y*=.65f;picture.transform.localScale=shorter;
                    picture.transform.position+=anchor-picture.transform.TransformPoint(bottom);
                }
                picture.gameObject.AddComponent<PolygonCollider2D>().points=art.outline;
            }
            foreach(var mirror in data.mirrors)
            {
                var picture=Draw(mirror.art,room.transform,7,(mirror.red ? "Red" : "Blue")+" Mirror — "+mirror.art.node);
                // Move the upper red mirror a little right, as requested, to make aiming clearer.
                if(mirror.art.node=="71:3577")picture.transform.position+=Vector3.right*(30f*1586f/1270f/100f);
                picture.gameObject.AddComponent<PolygonCollider2D>().points=mirror.art.outline;
                var surface=picture.gameObject.AddComponent<MirrorRicochetSurface>();surface.chargesShot=mirror.red;surface.reflectFromCollider=false;
            }
            foreach(var enemy in data.enemies)BuildEnemy(enemy,room.transform,i,level);
            if(i==0)AddWorkshopChainMirror.Restore(room);
            AddDoor(level,room,true);AddDoor(level,room,false);room.SetActive(i==0);
        }
        level.player.position=new Vector3(-level.spawnX,level.doorwayY,0);level.pausePanel.SetActive(false);
        EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        var scenes=EditorBuildSettings.scenes.ToList();
        if(!scenes.Any(s=>s.path==ScenePath))scenes.Add(new EditorBuildSettingsScene(ScenePath,true));
        else scenes.Find(s=>s.path==ScenePath).enabled=true;
        EditorBuildSettings.scenes=scenes.ToArray();
        for(int r=0;r<4;r++)
        {
            for(int j=0;j<4;j++)level.rooms[j].SetActive(j==r);
            Capture("Tools/Level2-room"+(r+1)+"-preview.png");
        }
        Verify(perimeter);
        InstallFirstLevelEnemies.Verify();
        if(Hash(BuildFirstLevelRooms.ScenePath)!=firstHash)throw new Exception("First level scene was modified.");
        EditorSceneManager.OpenScene(ScenePath);
        level=Object.FindAnyObjectByType<FirstLevelRooms>();
        level.GetComponent<SecondLevelWorkshop>().ConfigureCombat(level);
        level.GetComponent<FirstLevelPresentation>().EnsureUI();level.blackout.color=Color.clear;
        for(int i=0;i<4;i++)
        {
            for(int r=0;r<4;r++)level.rooms[r].SetActive(r==i);
            Capture("Tools/Level2-room"+(i+1)+"-preview.png");
        }
        // Preview changes are intentionally discarded; the authored scene remains the source of truth.
        EditorSceneManager.OpenScene(ScenePath);
        Selection.activeGameObject=Object.FindAnyObjectByType<FirstLevelRooms>().gameObject;
        File.AppendAllText("Tools/full-second-level-result.txt","\nFIRST_LEVEL_REGRESSION_PASSED; first scene SHA256 unchanged.\nSECOND_LEVEL_READY: four rooms, native Figma artwork, HUD, combat and exits.");
        Debug.Log("SECOND_LEVEL_FULL_VERIFIED");
    }
    public static SpriteRenderer Draw(Art art,Transform parent,int order,string name,string resource="SecondLevel")
    {
        var renderer=new GameObject(name).AddComponent<SpriteRenderer>();renderer.transform.SetParent(parent,false);
        renderer.sprite=Resources.Load<Sprite>(resource+"/"+art.asset);renderer.sortingOrder=order;
        renderer.transform.position=new Vector3(art.x,art.y,0);renderer.transform.rotation=Quaternion.Euler(0,0,art.angle);
        renderer.transform.localScale=new Vector3(art.sx,art.sy,1);return renderer;
    }
    public static TutorialRobot BuildEnemy(Enemy data,Transform room,int index,FirstLevelRooms level,string resource="SecondLevel",int health=0)
    {
        bool hasShield=data.shield!=null && !string.IsNullOrEmpty(data.shield.asset);
        var robot=new GameObject((data.promoted ? "Promoted Robot" : hasShield ? "Shielded Robot" : "Robot")+" — "+data.body.node);
        robot.transform.SetParent(room,false);
        robot.transform.position=new Vector3(data.body.x,data.body.y,0);
        var body=Draw(data.body,robot.transform,10,"Body",resource);
        var collider=robot.AddComponent<CircleCollider2D>();
        collider.offset=robot.transform.InverseTransformPoint(body.transform.TransformPoint(new Vector2(data.body.opaque[0],data.body.opaque[1])));
        collider.radius=data.body.radius*Mathf.Min(Mathf.Abs(data.body.sx),Mathf.Abs(data.body.sy))*.88f;
        var turret=new GameObject("Turret").transform;turret.SetParent(robot.transform,false);
        turret.position=robot.transform.TransformPoint(collider.offset);turret.rotation=Quaternion.Euler(0,0,data.head.angle+180);
        var head=Draw(data.head,turret,11,"Figma Turret",resource);
        // Preserve world transform from Figma despite the aiming rig's rotated parent.
        head.transform.localScale=new Vector3(data.head.sx,data.head.sy,1);
        var muzzle=new GameObject("Muzzle").transform;muzzle.SetParent(turret,false);muzzle.localPosition=new Vector3(0,collider.radius+.1f,0);
        GameObject shield=null;
        if(hasShield)
        {
            var glow=Draw(data.shield,robot.transform,12,"Shield",resource);shield=glow.gameObject;glow.color=new Color(1,1,1,.65f);
            // Ordinary alpha blending respects the native glow's transparency and the Figma opacity.
            // The first level's lighten material is left untouched.
        }
        var target=robot.AddComponent<TutorialTarget>();target.Configure(shield!=null,shield,health>0 ? health : data.promoted ? 2 : 5);
        var brain=robot.AddComponent<TutorialRobot>();brain.ConfigureFirstLevel(level,turret,muzzle,level.player.GetComponent<FirstLevelTankAim>().projectileSprite,index);
        var config=new SerializedObject(brain);config.FindProperty("shotsPerBurst").intValue=data.promoted ? 2 : 1;
        config.FindProperty("reactionOffset").floatValue=index*.08f;config.ApplyModifiedPropertiesWithoutUndo();
        return brain;
    }
    static Vector2[] OpenDoorways(Vector2[] source)
    {
        return source.Select(p=>
        {
            // Only the two vertical door segments move outward. Accepted top/bottom vertices stay exact.
            if(Mathf.Abs(p.x+6.68f)<.02f && (Mathf.Abs(p.y-1.21f)<.02f || Mathf.Abs(p.y+.59f)<.02f))p.x=-8.4f;
            if(Mathf.Abs(p.x-6.57f)<.02f && (Mathf.Abs(p.y-1.21f)<.02f || Mathf.Abs(p.y+.59f)<.02f))p.x=8.4f;
            return p;
        }).ToArray();
    }
    static void AddDoor(FirstLevelRooms level,GameObject room,bool entrance)
    {
        var gate=new GameObject(entrance ? "Entrance" : "Exit");gate.transform.SetParent(room.transform,false);
        gate.transform.position=new Vector3(entrance ? -6.68f : 6.57f,.31f,0);
        var barrier=gate.AddComponent<BoxCollider2D>();barrier.size=new Vector2(.04f,1.8f);
        var door=gate.AddComponent<WorkshopDoor>();door.level=level;door.room=room;door.entrance=entrance;door.barrier=barrier;door.Refresh();
    }
    static void Require(bool condition,string message){if(!condition)throw new Exception(message);}
    static void Verify(Vector2[] previousBoundary)
    {
        var level=Object.FindAnyObjectByType<FirstLevelRooms>();level.GetComponent<SecondLevelWorkshop>().ConfigureCombat(level);
        var move=typeof(FirstLevelRooms).GetMethod("MoveAxis",BindingFlags.Instance|BindingFlags.NonPublic);
        int contacts=0,targets=0,paths=0;
        var report=new List<string>();
        for(int roomIndex=0;roomIndex<4;roomIndex++)
        {
            foreach(var room in level.rooms)room.SetActive(false);var roomObject=level.rooms[roomIndex];roomObject.SetActive(true);
            Physics2D.SyncTransforms();var edge=roomObject.GetComponentInChildren<EdgeCollider2D>();
            Require(edge.points.SequenceEqual(OpenDoorways(previousBoundary)),"Accepted perimeter changed in room "+roomIndex);
            for(int pass=0;pass<5;pass++)
            for(int i=0;i<edge.points.Length-1;i++)
            {
                Vector2 a=edge.points[i],b=edge.points[i+1],tangent=(b-a).normalized,inward=new Vector2(tangent.y,-tangent.x);
                int samples=Mathf.Max(1,Mathf.CeilToInt(Vector2.Distance(a,b)/.3f));
                for(int j=0;j<samples;j++)
                {
                    var surface=Vector2.Lerp(a,b,(j+.2f+pass*.15f)/samples);var start=surface+inward*(level.playerRadius+.25f);
                    if(Physics2D.OverlapCircle(start,level.playerRadius)!=null)continue;
                    level.player.position=start;Physics2D.SyncTransforms();
                    for(int k=0;k<20;k++)move.Invoke(level,new object[]{-inward*.04f});
                    Vector2 stop=level.player.position;float gap=Vector2.Distance(stop,edge.ClosestPoint(stop));
                    // Another nearby obstacle may stop the tank first; overlap is never allowed.
                    Require(gap>=level.playerRadius-.008f,"Perimeter overlap in room "+roomIndex+", segment "+i);
                    bool retreatFree=Physics2D.OverlapCircle(stop+inward*.2f,level.playerRadius)==null;
                    move.Invoke(level,new object[]{inward*.2f});
                    if(retreatFree)Require(Vector2.Dot((Vector2)level.player.position-stop,inward)>.175f,"Tank sticks at boundary "+i+", room "+roomIndex+", stop "+stop);
                    contacts++;
                }
            }
            var enemies=roomObject.GetComponentsInChildren<TutorialTarget>();
            var reachable=Reachable(level);
            foreach(var target in enemies)
            {
                if(target.IsShielded)
                {
                    Require(FindChargedRoute(level,target,reachable,out var origin,out var direction),"No playable red-mirror path to "+target.name);
                    var ball=level.player.GetComponent<FirstLevelTankAim>().projectileSprite;
                    var preview=new List<FirstLevelAimPreview.Segment>();FirstLevelAimPreview.Trace(origin,direction,.035f,80,preview);
                    var trajectory=new List<string>{"Origin "+origin+" direction "+direction};
                    foreach(var segment in preview)trajectory.Add("Preview "+segment.start+" -> "+segment.end);
                    var shot=FirstLevelShot.Create(origin,direction,ball,level);
                    int reflections=0;
                    for(int flight=0;flight<1600 && shot!=null;flight++)
                    {
                        var position=shot.transform.position;shot.Step(.05f);
                        if(shot==null){trajectory.Add("Impact near "+position);break;}
                        if(shot.Reflections!=reflections){reflections=shot.Reflections;trajectory.Add("Actual bounce "+reflections+" at "+shot.transform.position+" direction "+shot.Direction);}
                    }
                    File.WriteAllText("Tools/FigmaLevel2/last-shot-diagnostics.txt",string.Join("\n",trajectory));
                    Require(!target.IsShielded && target.Health==5,"Actual charged projectile failed shield path for "+target.name);
                    FirstLevelShot.ClearAll();FirstLevelEffects.Clear();paths++;
                }
                else if(target.MaxHealth==2)
                {
                    target.Hit(false,false);Require(target.Health==1,"Promoted robot first hit.");target.Hit(false,false);Require(target.IsDestroyed,"Promoted robot must die on two ordinary shots.");
                    target.Configure(false,null,2);target.gameObject.SetActive(true);target.Hit(true,false);Require(target.IsDestroyed,"Promoted robot must die on one ricochet.");
                }
                targets++;
            }
            foreach(var enemy in enemies)enemy.Complete();
            foreach(var door in roomObject.GetComponentsInChildren<WorkshopDoor>())door.Refresh();Physics2D.SyncTransforms();
            reachable=Reachable(level,roomIndex==3 ? .025f : .1f);
            if(roomIndex==3)File.WriteAllText("Tools/FigmaLevel2/solid-bounds.txt",string.Join("\n",Object.FindObjectsByType<Collider2D>(FindObjectsSortMode.None).Where(c=>c.enabled).Select(c=>c.name+" "+c.bounds.min+" .. "+c.bounds.max)));
            File.WriteAllText("Tools/FigmaLevel2/navigation-diagnostics.txt","Room "+roomIndex+" cells "+reachable.Count+" maxX "+reachable.Max(p=>p.x)+" radius "+level.playerRadius);
            bool exitReachable=reachable.Any(p=>p.x>level.exitX && Mathf.Abs(p.y-level.doorwayY)<level.doorwayHalfHeight);
            if(roomIndex==2)Require(reachable.Any(p=>p.x>1.95f && p.x<2.35f && p.y>2.54f && p.y<2.7f),"The passage above the shortened conveyor must fit the tank.");
            report.Add("Room 2."+(roomIndex+1)+": "+enemies.Length+" targets, exit reachable="+exitReachable+", accepted perimeter preserved.");
            if(!exitReachable)report.Add("BLOCKED_NAVIGATION: "+roomIndex);
        }
        Require(targets==10 && paths==4,"Incorrect room roster or shield routes.");
        File.WriteAllText("Tools/full-second-level-result.txt",string.Join("\n",report)+"\nFIVE_PERIMETER_PASSES: "+contacts+" contacts.\nCHARGED_PROJECTILE_PATHS: "+paths+".\nPROMOTED_TWO_DIRECT_OR_ONE_REFLECTED: passed.");
        Require(!report.Any(s=>s.StartsWith("BLOCKED_NAVIGATION")),"A room exit is unreachable; see navigation report.");
    }
    public static List<Vector2> Reachable(FirstLevelRooms level,float step=.1f)
    {
        var start=new Vector2(-level.spawnX,level.doorwayY+.003f);
        var points=new List<Vector2>();var queue=new Queue<Vector2Int>();var seen=new HashSet<Vector2Int>();queue.Enqueue(Vector2Int.zero);seen.Add(Vector2Int.zero);
        while(queue.Count>0)
        {
            var id=queue.Dequeue();var p=start+(Vector2)id*step;points.Add(p);
            foreach(var d in new[]{Vector2Int.up,Vector2Int.down,Vector2Int.left,Vector2Int.right})
            {
                var next=id+d;if(seen.Contains(next))continue;var q=start+(Vector2)next*step;
                if(Mathf.Abs(q.x)>Mathf.Max(8,level.exitX+.5f) || Mathf.Abs(q.y)>4.6f)continue;
                if(Physics2D.CircleCast(p,level.playerRadius,(q-p).normalized,step).collider!=null)continue;
                seen.Add(next);queue.Enqueue(next);
            }
        }
        return points;
    }
    public static bool FindChargedRoute(FirstLevelRooms level,TutorialTarget target,List<Vector2> accessible,out Vector2 origin,out Vector2 direction)
    {
        // Search actual reflective polygon normals, then verify with the shared projectile cast.
        var targetCollider=target.GetComponent<Collider2D>();Vector2 goalCenter=targetCollider.bounds.center;
        foreach(Vector2 aimOffset in new[]{Vector2.zero,Vector2.left,Vector2.right,Vector2.up,Vector2.down,new Vector2(.7f,.7f),new Vector2(-.7f,.7f),new Vector2(.7f,-.7f),new Vector2(-.7f,-.7f)})
        {
        Vector2 goal=goalCenter+aimOffset*targetCollider.bounds.extents.x*.7f;
        foreach(var surface in Object.FindObjectsByType<MirrorRicochetSurface>(FindObjectsSortMode.None).Where(s=>s.chargesShot))
        {
            var polygon=surface.GetComponent<PolygonCollider2D>();var pts=polygon.points;
            for(int i=0;i<pts.Length;i++)
            {
                Vector2 a=polygon.transform.TransformPoint(pts[i]),b=polygon.transform.TransformPoint(pts[(i+1)%pts.Length]);
                var tangent=(b-a).normalized;var normal=new Vector2(-tangent.y,tangent.x);
                for(int sample=1;sample<10;sample++)
                {
                    var contact=Vector2.Lerp(a,b,sample/10f);var outgoing=(goal-contact).normalized;var incoming=Vector2.Reflect(outgoing,normal);
                    for(float distance=1;distance<9;distance+=.2f)
                    {
                        var p=contact-incoming*distance;
                        if(!accessible.Any(q=>(p-q).sqrMagnitude<.035f) || Physics2D.OverlapCircle(p,level.playerRadius)!=null)continue;
                        var hit=FirstLevelShotPath.Cast(p,incoming,.035f,distance+.1f);
                        if(hit.mirror!=surface)continue;
                        var bounce=p+incoming*hit.distance;var reflected=hit.Reflect(incoming);
                        var end=FirstLevelShotPath.Cast(bounce+reflected*.003f,reflected,.035f,20,hit.mirrorCollider);
                        if(end.solid==null || end.solid.GetComponentInParent<TutorialTarget>()!=target)continue;
                        if(!TraceCharged(p,incoming,target,.055f) || !TraceChargedStepped(p,incoming,target))continue;
                        origin=p;direction=incoming;return true;
                    }
                }
            }
        }
        }
        // Also allow several reflections and hits off-centre. Narrow routes may pass between conveyors.
        foreach(var surface in Object.FindObjectsByType<MirrorRicochetSurface>().Where(s=>s.chargesShot))
        {
            var polygon=surface.GetComponent<PolygonCollider2D>();var pts=polygon.points;
            for(int point=0;point<pts.Length;point++)
            for(int sample=0;sample<4;sample++)
            {
                Vector2 reflection=polygon.transform.TransformPoint(Vector2.Lerp(pts[point],pts[(point+1)%pts.Length],sample/4f));
                for(int start=0;start<accessible.Count;start+=2)
                {
                    var p=accessible[start];var ray=(reflection-p).normalized;
                    if(TraceCharged(p,ray,target) && TraceCharged(p,ray,target,.055f) && TraceChargedStepped(p,ray,target)){origin=p;direction=ray;return true;}
                }
            }
        }
        File.WriteAllText("Tools/FigmaLevel2/route-diagnostics.txt",target.name+" reachable cells "+accessible.Count+" target "+goalCenter);
        origin=direction=Vector2.zero;return false;
    }
    static bool TraceCharged(Vector2 origin,Vector2 direction,TutorialTarget target,float radius=.035f)
    {
        bool charged=false;Collider2D last=null;float remaining=60;
        for(int bounce=0;bounce<=FirstLevelShotPath.MaxReflections;bounce++)
        {
            var hit=FirstLevelShotPath.Cast(origin,direction,radius,remaining,last);
            if(hit.mirror==null)return charged && hit.solid!=null && hit.solid.GetComponentInParent<TutorialTarget>()==target;
            charged|=hit.mirror.chargesShot;remaining-=hit.distance;
            origin+=direction*hit.distance;direction=hit.Reflect(direction);origin+=direction*FirstLevelShotPath.Separation;last=hit.mirrorCollider;
        }
        return false;
    }
    static bool TraceChargedStepped(Vector2 origin,Vector2 direction,TutorialTarget target)
    {
        Vector2 playerOrigin=origin;
        var level=Object.FindAnyObjectByType<FirstLevelRooms>();
        float playerClearance=(level!=null ? level.playerRadius : .54f)+.035f;
        bool charged=false;Collider2D last=null;int reflections=0;
        for(int frame=0;frame<1600;frame++)
        {
            float remaining=.05f;
            for(int pass=0;pass<FirstLevelShotPath.MaxReflections+2 && remaining>0;pass++)
            {
                var hit=FirstLevelShotPath.Cast(origin,direction,.035f,remaining,last);
                float travel=hit.Hit ? hit.distance : remaining;
                if(reflections>0)
                {
                    float along=Mathf.Clamp(Vector2.Dot(playerOrigin-origin,direction),0,travel);
                    if(Vector2.Distance(origin+direction*along,playerOrigin)<playerClearance)return false;
                }
                origin+=direction*travel;remaining-=travel;
                if(hit.mirror!=null)
                {
                    if(reflections++>=FirstLevelShotPath.MaxReflections)return false;
                    charged|=hit.mirror.chargesShot;direction=hit.Reflect(direction);
                    last=hit.mirrorCollider;origin+=direction*FirstLevelShotPath.Separation;
                    remaining=Mathf.Max(0,remaining-FirstLevelShotPath.Separation);
                }
                else if(hit.solid!=null)return charged && hit.solid.GetComponentInParent<TutorialTarget>()==target;
                else break;
            }
        }
        return false;
    }
    public static void Capture(string path)
    {
        var camera=Camera.main;
        if(camera==null)throw new InvalidOperationException("A camera is required for capture");
        var canvases=Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        var modes=canvases.Select(c=>c.renderMode).ToArray();var cameras=canvases.Select(c=>c.worldCamera).ToArray();var planes=canvases.Select(c=>c.planeDistance).ToArray();
        var previousTarget=camera.targetTexture;var previousActive=RenderTexture.active;float previousSize=camera.orthographicSize;Vector3 previousPosition=camera.transform.position;
        var target=new RenderTexture(1920,1080,24);Texture2D image=null;
        try
        {
            foreach(var canvas in canvases){canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;}
            camera.targetTexture=target;var fit=camera.GetComponent<FirstLevelCameraFit>();if(fit!=null)fit.Apply();Canvas.ForceUpdateCanvases();camera.Render();
            RenderTexture.active=target;image=new Texture2D(1920,1080,TextureFormat.RGB24,false);
            image.ReadPixels(new Rect(0,0,1920,1080),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture=previousTarget;camera.orthographicSize=previousSize;camera.transform.position=previousPosition;RenderTexture.active=previousActive;
            for(int i=0;i<canvases.Length;i++)if(canvases[i]!=null){canvases[i].renderMode=modes[i];canvases[i].worldCamera=cameras[i];canvases[i].planeDistance=planes[i];}
            Object.DestroyImmediate(target);if(image!=null)Object.DestroyImmediate(image);Canvas.ForceUpdateCanvases();
        }
    }
}

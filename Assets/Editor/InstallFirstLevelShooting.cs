using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class InstallFirstLevelShooting
{
    [Serializable] class Layers { public float pivotX,pivotY,offsetX,offsetY,aimOffset,muzzleDistance; }
    const string Art="Assets/UI/Level1/";
    [MenuItem("Bounce Club/Install First Level Shooting")]
    public static void Install()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode){Debug.LogWarning("Stop Play Mode before installing shooting.");return;}
        ConfigureLayers();
        var scene=EditorSceneManager.OpenScene(BuildFirstLevelRooms.ScenePath);
        ConfigurePlayer(UnityEngine.Object.FindAnyObjectByType<FirstLevelRooms>());
        ConnectBlueMirrors(UnityEngine.Object.FindAnyObjectByType<FirstLevelRooms>());
        EditorSceneManager.SaveScene(scene);
        Verify();
        PreviewFirstLevel.RenderRoom(0);
    }
    public static void ConnectBlueMirrors(FirstLevelRooms level)
    {
        foreach(var room in level.rooms)
        foreach(var renderer in room.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if(!renderer.name.StartsWith("Blue Mirror"))continue;
            var surface=renderer.GetComponent<MirrorRicochetSurface>();
            if(surface!=null){if(renderer.name.Contains("3067") || renderer.name.Contains("3068")){surface.reflectFromCollider=false;if(renderer.name.Contains("3067"))surface.localReflectionPoint=new Vector2(0,-.08f);}continue;}
            surface=renderer.gameObject.AddComponent<MirrorRicochetSurface>();
            surface.localReflectionPoint=new Vector2(0,renderer.name.Contains("3068") ? -.125f : renderer.name.Contains("3067") ? -.08f : .045f);
            surface.chargesShot=false;
            surface.reflectFromCollider=false;
        }
    }
    static void ConfigureLayers()
    {
        var data=JsonUtility.FromJson<Layers>(File.ReadAllText(Art+"PlayerLayers.json"));
        foreach(string name in new[]{"PlayerBody","PlayerTurret"})
        {
            string path=Art+name+".png";AssetDatabase.ImportAsset(path);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            Level1TextureQuality.Configure(importer,path);
            importer.spritePixelsPerUnit=400;importer.mipmapEnabled=true;importer.filterMode=FilterMode.Trilinear;
            var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);
            settings.spriteAlignment=(int)SpriteAlignment.Custom;
            settings.spritePivot=name=="PlayerTurret" ? new Vector2(data.pivotX,data.pivotY) : new Vector2(.5f,.5f);
            importer.SetTextureSettings(settings);importer.SaveAndReimport();
        }
    }
    public static void ConfigurePlayer(FirstLevelRooms level)
    {
        ConfigureLayers();
        var data=JsonUtility.FromJson<Layers>(File.ReadAllText(Art+"PlayerLayers.json"));
        var body=level.player.GetComponent<SpriteRenderer>();
        body.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(Art+"PlayerBody.png");
        var turret=level.player.Find("Turret");
        if(turret==null)turret=new GameObject("Turret").transform;
        turret.SetParent(level.player,false);
        turret.localPosition=new Vector3(data.offsetX,data.offsetY,0);
        turret.localRotation=Quaternion.identity;turret.localScale=Vector3.one;
        var render=turret.GetComponent<SpriteRenderer>();if(render==null)render=turret.gameObject.AddComponent<SpriteRenderer>();
        render.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(Art+"PlayerTurret.png");render.sortingOrder=body.sortingOrder+1;
        var aim=level.player.GetComponent<FirstLevelTankAim>();if(aim==null)aim=level.player.gameObject.AddComponent<FirstLevelTankAim>();
        aim.level=level;aim.turret=turret;aim.spriteAimOffset=data.aimOffset;aim.muzzleDistance=data.muzzleDistance;
        aim.projectileRadius=.035f;
        aim.projectileSprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Circle.png");
    }
    [MenuItem("Bounce Club/Verify First Level Shooting")]
    public static void Verify()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode){Debug.LogWarning("Stop Play Mode before verifying shooting.");return;}
        var scene=EditorSceneManager.OpenScene(BuildFirstLevelRooms.ScenePath);
        // This isolated projectile check supplies its own target at the future boss point.
        foreach(var enemy in UnityEngine.Object.FindObjectsByType<TutorialTarget>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            enemy.gameObject.SetActive(false);
        var level=UnityEngine.Object.FindAnyObjectByType<FirstLevelRooms>();
        var aim=level.player.GetComponent<FirstLevelTankAim>();
        Quaternion bodyRotation=level.player.rotation;
        foreach(Vector2 direction in new[]{Vector2.up,Vector2.down,Vector2.left,Vector2.right})
        {
            aim.AimAt((Vector2)aim.turret.position+direction*5);
            Vector2 barrel=aim.turret.TransformDirection(Quaternion.Euler(0,0,aim.spriteAimOffset)*Vector3.right);
            Require(Vector2.Dot(barrel.normalized,direction)>.999f,"Turret aligns to cursor");
            Require(Quaternion.Angle(level.player.rotation,bodyRotation)<.001f,"Aiming leaves body rotation unchanged");
        }
        for(int i=0;i<3;i++)level.rooms[i].SetActive(i==1);
        foreach(var blue in level.rooms[1].GetComponentsInChildren<SpriteRenderer>())
        {
            if(!blue.name.StartsWith("Blue Mirror"))continue;
            var surface=blue.GetComponent<MirrorRicochetSurface>();
            Require(surface!=null,"Room 2 blue mirror must have a reflection surface");
            Vector2 incoming=blue.name.Contains("3068") ? Vector2.down : Vector2.up;
            var blueShot=FirstLevelShot.Create(surface.ReflectionPoint-incoming*.9f,incoming,null,level);
            blueShot.Step(1f);
            Require(blueShot!=null && blueShot.Reflections==1 && !blueShot.Charged && Vector2.Dot(blueShot.Direction,incoming)<-.9f,"Room 2 blue mirror reflects a normal shot: "+blue.name+" / "+(blueShot==null?"destroyed":blueShot.Reflections+" "+blueShot.Direction));
            FirstLevelShot.ClearAll();
        }
        for(int i=0;i<3;i++)level.rooms[i].SetActive(i==2);
        Physics2D.SyncTransforms();
        var route=UnityEngine.Object.FindAnyObjectByType<FirstLevelRicochetRoute>();
        var shot=FirstLevelShot.Create(route.shotOrigin,Vector2.down,null,level,10,.035f);
        float first=Vector2.Distance(route.shotOrigin,route.redMirror.ReflectionPoint);
        shot.Step(first+.01f);
        Require(shot!=null && shot.Charged && shot.Reflections==1 && shot.Damage==2,"Red mirror charge");
        shot.Step(Vector2.Distance(shot.transform.position,route.blueMirror.ReflectionPoint)+.01f);
        Require(shot!=null && shot.Charged && shot.Reflections==2,"Blue preserves charge");
        Require(Vector2.Dot(shot.Direction,(route.futureBossPosition-(Vector2)shot.transform.position).normalized)>.999f,"Ricochet direction");
        var targetObject=new GameObject("Temporary shot target");targetObject.transform.position=route.futureBossPosition;
        targetObject.AddComponent<CircleCollider2D>().radius=.2f;
        var target=targetObject.AddComponent<FirstLevelShotTarget>();target.shielded=true;
        shot.Step(10);Require(target.health==3 && !target.shielded,"Charged damage through shield");
        UnityEngine.Object.DestroyImmediate(targetObject);
        var blocked=new GameObject("Temporary shield target").AddComponent<FirstLevelShotTarget>();blocked.shielded=true;
        blocked.Hit(1,false);Require(blocked.health==5 && blocked.shielded,"Normal shot blocked by shield");
        blocked.shielded=false;blocked.Hit(1,false);Require(blocked.health==4,"Normal damage");UnityEngine.Object.DestroyImmediate(blocked.gameObject);
        var wall=FirstLevelShot.Create(new Vector2(-4.45f,-.55f),Vector2.left,null,level);
        wall.Step(15);Require(wall==null || wall.Finished,"Wall absorbs swept fast shot");
        FirstLevelShot.ClearAll();EditorSceneManager.OpenScene(BuildFirstLevelRooms.ScenePath);
        Debug.Log("FIRST_LEVEL_SHOOTING_VERIFIED: red charge, blue reflection, preserved charge, double damage, shield rules, swept wall collision.");
    }
    static void Require(bool result,string message){if(!result)throw new Exception(message);}
}

using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

public static class VerifyEndlessArena
{
    public static void Require(bool valid,string message){if(!valid)throw new Exception(message);}
    [MenuItem("Bounce Club/Verify Endless Arena Geometry")]
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/EndlessArena.unity");
        var level=Object.FindAnyObjectByType<FirstLevelRooms>();var arena=level.GetComponent<EndlessArena>();
        Require(level.rooms.Length==3 && arena.waveSpawns.Length==3,"Three authored waves are required.");
        File.WriteAllText("Tools/endless-geometry-result.txt","Endless arena geometry and original Figma sprite references\n");
        for(int i=0;i<level.rooms.Length;i++)
        {
            for(int j=0;j<level.rooms.Length;j++)level.rooms[j].SetActive(i==j);
            level.player.position=arena.SpawnFor(i);Physics2D.SyncTransforms();
            var room=level.rooms[i];var targets=room.GetComponentsInChildren<TutorialTarget>();
            Require(targets.Length==new[]{3,5,4}[i],"Incorrect wave enemy roster.");
            foreach(var image in room.GetComponentsInChildren<SpriteRenderer>())Require(image.sprite!=null,"Missing sprite: "+image.name);
            Require(!Physics2D.OverlapCircleAll(arena.SpawnFor(i),level.playerRadius).Any(c=>!c.isTrigger),"Blocked Figma player spawn: wave "+(i+1));
            foreach(var mirror in room.GetComponentsInChildren<MirrorRicochetSurface>())
            {
                Require(PrefabUtility.IsPartOfPrefabInstance(mirror),"Mirror must remain a Vlad prefab instance.");
                var edge=mirror.GetComponent<EdgeCollider2D>();Require(edge!=null && edge.enabled && !edge.isTrigger,"Mirror requires the physical edge.");
                Require(mirror.GetComponents<Collider2D>().Length==1,"Duplicate mirror collider.");
                var points=edge.points;Vector2 a=edge.transform.TransformPoint(points[0]),b=edge.transform.TransformPoint(points[1]);
                Vector2 normal=new Vector2(-(b-a).y,(b-a).x).normalized,mid=(a+b)/2;
                bool accessible=false;
                foreach(float side in new[]{-1f,1f})
                {
                    var contact=FirstLevelShotPath.Cast(mid+normal*.25f*side,-normal*side,.035f,.5f);
                    if(contact.mirror==mirror)accessible=true;
                }
                Require(accessible,"Both sides of mirror face blocked: "+mirror.name);
            }
            foreach(var target in targets)Require(target.MaxHealth==(target.IsRedRobot?4:2),"Incorrect robot health.");
            // Obstacles remain: remove only robots so navigation tests real authored cover.
            foreach(var target in targets)foreach(var c in target.GetComponentsInChildren<Collider2D>())c.enabled=false;
            level.spawnX=-arena.SpawnFor(i).x;level.doorwayY=arena.SpawnFor(i).y;
            var pointsReachable=BuildFullSecondLevel.Reachable(level,.15f);
            foreach(var mirror in room.GetComponentsInChildren<MirrorRicochetSurface>())
            {
                var edge=mirror.GetComponent<EdgeCollider2D>();var p=edge.points;
                Vector2 mid=(edge.transform.TransformPoint(p[0])+edge.transform.TransformPoint(p[1]))*.5f;
                Require(pointsReachable.Any(origin=>FirstLevelShotPath.Cast(origin,(mid-origin).normalized,.035f,Vector2.Distance(origin,mid)+.07f).mirror==mirror),"Mirror cannot be reached by a bullet from the playing floor: "+mirror.name);
            }
            Require(pointsReachable.Count>700,"Floor navigation is excessively restricted.");
            Require(pointsReachable.Any(p=>p.x>5 && p.y>1),"Upper-right floor cannot be reached.");
            Require(pointsReachable.Any(p=>p.x>5 && p.y<-1),"Lower-right floor cannot be reached.");
            foreach(var target in targets)foreach(var c in target.GetComponentsInChildren<Collider2D>(true))c.enabled=true;
            foreach(var target in targets.Where(t=>t.IsShielded))
            {
                // Edit-mode validation needs the same shield setup used by Awake.
                var field=typeof(TutorialTarget).GetField("shieldVisual",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
                target.Configure(true,(GameObject)field.GetValue(target),target.MaxHealth,false,target.IsRedRobot);
                Physics2D.SyncTransforms();
                Require(ChargedRoute(target,room,pointsReachable),"No charged route to shield from reachable floor: "+target.name);
            }
            File.AppendAllText("Tools/endless-geometry-result.txt","Wave "+(i+1)+": "+targets.Length+" enemies, "+room.GetComponentsInChildren<MirrorRicochetSurface>().Length+" prefab edges, "+pointsReachable.Count+" reachable floor samples.\n");
        }
        EditorSceneManager.OpenScene("Assets/Scenes/EndlessArena.unity");
        level=Object.FindAnyObjectByType<FirstLevelRooms>();arena=level.GetComponent<EndlessArena>();
        arena.ConfigureCombat(level);level.GetComponent<FirstLevelPresentation>().EnsureUI();level.blackout.color=Color.clear;
        for(int i=0;i<3;i++)
        {
            for(int j=0;j<3;j++)level.rooms[j].SetActive(i==j);level.player.position=arena.SpawnFor(i);
            BuildFullSecondLevel.Capture("Tools/Endless-wave"+(i+1)+"-preview.png");
        }
        File.AppendAllText("Tools/endless-geometry-result.txt","PASSED\n");
        EditorSceneManager.OpenScene("Assets/Scenes/EndlessArena.unity");
    }
    static bool ChargedRoute(TutorialTarget target,GameObject room,System.Collections.Generic.List<Vector2> origins)
    {
        Vector2 goal=target.ShieldCollider.bounds.center;
        foreach(var mirror in room.GetComponentsInChildren<MirrorRicochetSurface>().Where(m=>m.chargesShot))
        {
            var edge=mirror.GetComponent<EdgeCollider2D>();var points=edge.points;
            Vector2 a=edge.transform.TransformPoint(points[0]),b=edge.transform.TransformPoint(points[1]);
            Vector2 normal=new Vector2(-(b-a).y,(b-a).x).normalized;
            Vector2 virtualGoal=goal-2*normal*Vector2.Dot(goal-a,normal);
            foreach(var origin in origins)
            {
                Vector2 p=origin,d=(virtualGoal-origin).normalized;Collider2D previous=null;bool charged=false;
                for(int bounce=0;bounce<6;bounce++)
                {
                    var hit=FirstLevelShotPath.Cast(p,d,.035f,30,previous);
                    if(hit.mirror!=null)
                    {
                        charged|=hit.mirror.chargesShot;p+=d*hit.distance;d=hit.Reflect(d);p+=d*FirstLevelShotPath.Separation;previous=hit.mirrorCollider;
                    }
                    else
                    {
                        if(charged && hit.solid!=null && hit.solid.GetComponentInParent<TutorialTarget>()==target)return true;
                        break;
                    }
                }
            }
        }
        return false;
    }
}

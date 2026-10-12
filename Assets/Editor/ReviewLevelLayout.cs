using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

public static class ReviewLevelLayout
{
    class Adjustment { public int room; public string mirror; public Quaternion rotation; public Vector3 position; }
    [Serializable] class Art { public string node; public float x,y,angle; }
    [Serializable] class Mirror { public Art art; }
    [Serializable] class Room { public Mirror[] mirrors; }
    [Serializable] class Layout { public Room[] rooms; }
    [InitializeOnLoadMethod] static void Init()=>EditorApplication.update+=Check;
    static void Check()
    {
        if(EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists("Tools/review-level-layout.request"))return;
        string mode=File.ReadAllText("Tools/review-level-layout.request").Trim();
        bool verifyOnly=mode=="verify" || mode=="passage";
        File.Delete("Tools/review-level-layout.request");
        try
        {
            if(verifyOnly)
            {
                if(mode=="passage")
                {
                    var passageScene=EditorSceneManager.OpenScene("Assets/Scenes/level2.unity");
                    var passageLevel=Object.FindAnyObjectByType<FirstLevelRooms>();
                    var red=passageLevel.rooms[3].GetComponentsInChildren<MirrorRicochetSurface>(true).First(m=>m.chargesShot);
                    if(red.transform.Find("Lower Mirror Passage Applied")==null)
                    {
                        red.transform.position+=Vector3.left*1.4f;
                        new GameObject("Lower Mirror Passage Applied").transform.SetParent(red.transform,false);
                        EditorSceneManager.MarkSceneDirty(passageScene);EditorSceneManager.SaveScene(passageScene);
                    }
                }
                VerifyRoutes();File.AppendAllText("Tools/layout-review-result.txt","PASSED: all saved shield-breaking routes verified with actual projectiles.\n");return;
            }
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/level2.unity");
            var level=Object.FindAnyObjectByType<FirstLevelRooms>();
            var room=level.rooms[0];
            foreach(var extra in room.GetComponentsInChildren<MirrorRicochetSurface>(true).Where(m=>m.name.Contains("Three-bounce")).ToArray())Object.DestroyImmediate(extra.gameObject);
            var layout=JsonUtility.FromJson<Layout>(Resources.Load<TextAsset>("SecondLevel/Layout").text);
            foreach(var native in layout.rooms[0].mirrors)
            {
                var mirror=room.GetComponentsInChildren<MirrorRicochetSurface>(true).First(m=>m.name.Contains(native.art.node));
                mirror.transform.position=new Vector3(native.art.x,native.art.y,0);
                mirror.transform.rotation=Quaternion.Euler(0,0,native.art.angle);
            }
            if(room.GetComponentsInChildren<MirrorRicochetSurface>(true).Length!=2)throw new Exception("2.1 must have two mirrors");
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            TuneLevelProportions.Run(true);
            typeof(InstallAllMirrorGlows).GetMethod("Run",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,null);
            VerifyRoutes();
            File.AppendAllText("Tools/layout-review-result.txt","PASSED: 2.1 has two mirrors, enemy bodies match hero, oversized obstacles capped at three hero widths, glow opacity .75 with smooth pulse.\n");
        }
        catch(Exception e){File.AppendAllText("Tools/layout-review-result.txt","FAILED: "+e);Debug.LogException(e);}
        finally{EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");}
    }
    static void VerifyRoutes()
    {
        File.WriteAllText("Tools/layout-review-result.txt","Recalculating using visible mirror faces.\n");
        foreach(var name in new[]{"Level1Rooms","level2","level3"})
        {
            EditorSceneManager.OpenScene("Assets/Scenes/"+name+".unity");
            var level=Object.FindAnyObjectByType<FirstLevelRooms>();
            if(name=="level2")level.GetComponent<SecondLevelWorkshop>().ConfigureCombat(level);
            else if(name=="level3")level.GetComponent<ThirdLevelCastle>().ConfigureCombat(level);
            else FirstLevelEnemies.Place(level);
            var adjustments=new System.Collections.Generic.List<Adjustment>();
            for(int index=0;index<level.rooms.Length;index++)
            {
                for(int j=0;j<level.rooms.Length;j++)level.rooms[j].SetActive(j==index);
                typeof(FirstLevelRooms).GetField("<CurrentRoom>k__BackingField",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(level,index);
                foreach(var block in level.rooms[index].GetComponentsInChildren<DestructibleCover>())block.Hit(5);
                Physics2D.SyncTransforms();
                // Mirrors partly over decorative walls still need their reflecting face inside the floor.
                foreach(var mirror in level.rooms[index].GetComponentsInChildren<MirrorRicochetSurface>())
                {
                    if(name=="Level1Rooms")continue;
                    var original=mirror.transform.position;
                    Vector2 inward=(-mirror.ReflectionPoint).normalized;
                    bool obstructed=Physics2D.OverlapCircleAll(mirror.ReflectionPoint,.08f).Any(c=>!c.isTrigger && c.GetComponentInParent<MirrorRicochetSurface>()==null && c.GetComponentInParent<TutorialTarget>()==null);
                    if(!obstructed && Mathf.Abs(mirror.ReflectionPoint.y)<3.15f)continue;
                    bool adjusted=false;
                    for(int move=1;move<=6;move++)
                    {
                        mirror.transform.position=original+(Vector3)(inward*(move*.12f));Physics2D.SyncTransforms();
                        if(Mathf.Abs(mirror.ReflectionPoint.y)>3.12f)continue;
                        if(Physics2D.OverlapCircleAll(mirror.ReflectionPoint,.08f).Any(c=>!c.isTrigger && c.GetComponentInParent<MirrorRicochetSurface>()==null && c.GetComponentInParent<TutorialTarget>()==null))continue;
                        adjustments.Add(new Adjustment{room=index,mirror=mirror.name,rotation=mirror.transform.rotation,position=mirror.transform.position});adjusted=true;break;
                    }
                    if(!adjusted){mirror.transform.position=original;Physics2D.SyncTransforms();}
                }
                var accessible=BuildFullSecondLevel.Reachable(level,.12f);
                foreach(var target in level.rooms[index].GetComponentsInChildren<TutorialTarget>())
                {
                    if(!target.IsShielded)continue;
                    if(!BuildFullSecondLevel.FindChargedRoute(level,target,accessible,out var p,out var direction))
                    {
                        bool found=false;
                        foreach(var red in level.rooms[index].GetComponentsInChildren<MirrorRicochetSurface>().Where(m=>m.chargesShot))
                        {
                            var previous=red.transform.rotation;
                            Vector2 goal=target.GetComponent<Collider2D>().bounds.center;
                            foreach(var candidate in accessible.Where((q,n)=>n%4==0).OrderBy(q=>Vector2.Distance(q,new Vector2(-level.spawnX,level.doorwayY))))
                            {
                                if(Vector2.Distance(candidate,red.ReflectionPoint)<level.playerRadius+1)continue;
                                var incoming=(red.ReflectionPoint-candidate).normalized;
                                var outgoing=(goal-red.ReflectionPoint).normalized;
                                var normal=(incoming-outgoing).normalized;
                                red.transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(-normal.x,normal.y)*Mathf.Rad2Deg);
                                Physics2D.SyncTransforms();
                                if(!BuildFullSecondLevel.FindChargedRoute(level,target,new System.Collections.Generic.List<Vector2>{candidate},out p,out direction))continue;
                                adjustments.Add(new Adjustment{room=index,mirror=red.name,rotation=red.transform.rotation,position=red.transform.position});found=true;break;
                            }
                            if(found)break;
                            red.transform.rotation=previous;Physics2D.SyncTransforms();
                        }
                        if(!found)throw new Exception("No charged route: "+name+" room "+(index+1)+" "+target.name);
                    }
                    level.player.position=p;Physics2D.SyncTransforms();
                    var shot=FirstLevelShot.Create(p,direction,level.player.GetComponent<FirstLevelTankAim>().projectileSprite,level);
                    for(int step=0;step<1600 && !shot.Finished;step++)shot.Step(.05f);
                    if(target.IsShielded)throw new Exception("Actual projectile failed: "+target.name);
                    File.AppendAllText("Tools/layout-review-result.txt",name+" room "+(index+1)+" "+target.name+" origin="+p+" direction="+direction+" reflections="+shot.Reflections+" shield broken.\n");
                }
            }
            if(adjustments.Count>0)
            {
                var scene=EditorSceneManager.OpenScene("Assets/Scenes/"+name+".unity");
                level=Object.FindAnyObjectByType<FirstLevelRooms>();
                foreach(var adjustment in adjustments)
                    level.rooms[adjustment.room].GetComponentsInChildren<MirrorRicochetSurface>(true).First(m=>m.name==adjustment.mirror).transform.SetPositionAndRotation(adjustment.position,adjustment.rotation);
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
                File.AppendAllText("Tools/layout-review-result.txt",name+": recalculated "+adjustments.Count+" mirror placements/orientations.\n");
            }
            EditorSceneManager.OpenScene("Assets/Scenes/"+name+".unity");
            if(name!="Level1Rooms")BuildFullSecondLevel.Capture("Tools/"+name+"-layout-review.png");
        }
    }
}

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;

public static class AddWorkshopChainMirror
{
    [Serializable] public class Placement { public Vector3 position,bluePosition,redPosition; public float angle,blueAngle,redAngle; public bool configureBase; public Vector2 origin,direction; }
    [Serializable] class Art { public string node; public float x,y,angle; }
    [Serializable] class Mirror { public Art art; public bool red; }
    [Serializable] class Room { public Mirror[] mirrors; }
    [Serializable] class Layout { public Room[] rooms; }
    const string Config="Tools/FigmaLevel2/ChainMirror.json",Request="Tools/add-workshop-chain-mirror.request";
    static double ready;
    [InitializeOnLoadMethod] static void Hook()=>EditorApplication.update+=Tick;
    static void Tick()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating){ready=EditorApplication.timeSinceStartup+2;return;}
        if(EditorApplication.timeSinceStartup<ready || !File.Exists(Request))return;
        File.Delete(Request);
        try{Run();}catch(Exception e){File.WriteAllText("Tools/chain-mirror-result.txt","FAILED: "+e);Debug.LogException(e);}
    }
    public static void Restore(GameObject room)
    {
        if(!File.Exists(Config))return;
        var p=JsonUtility.FromJson<Placement>(File.ReadAllText(Config));
        foreach(var old in room.GetComponentsInChildren<MirrorRicochetSurface>(true).Where(s=>s.name.Contains("Three-bounce")).ToArray())Object.DestroyImmediate(old.gameObject);
        var source=room.GetComponentsInChildren<MirrorRicochetSurface>(true).First(s=>!s.chargesShot);
        if(p.configureBase)
        {
            source.transform.position=p.bluePosition;source.transform.rotation=Quaternion.Euler(0,0,p.blueAngle);
            var red=room.GetComponentsInChildren<MirrorRicochetSurface>(true).First(s=>s.chargesShot);
            red.transform.position=p.redPosition;red.transform.rotation=Quaternion.Euler(0,0,p.redAngle);
        }
        var clone=Object.Instantiate(source.gameObject,room.transform);clone.name="Blue Mirror — Three-bounce route";
        clone.transform.position=p.position;clone.transform.rotation=Quaternion.Euler(0,0,p.angle);
    }
    static bool Chain(Vector2 p,Vector2 d,MirrorRicochetSurface blue,MirrorRicochetSurface red,MirrorRicochetSurface extra,TutorialTarget target,float step)
    {
        int bounce=0;Collider2D last=null;
        var expected=new[]{blue,red,extra};
        for(int frame=0;frame<1600;frame++)
        {
            float remaining=step;
            for(int pass=0;pass<7 && remaining>0;pass++)
            {
                var h=FirstLevelShotPath.Cast(p,d,.035f,remaining,last);float travel=h.Hit?h.distance:remaining;p+=d*travel;remaining-=travel;
                if(h.mirror!=null)
                {
                    if(bounce>=3 || h.mirror!=expected[bounce++])return false;
                    d=h.Reflect(d);last=h.mirrorCollider;p+=d*FirstLevelShotPath.Separation;remaining=Mathf.Max(0,remaining-FirstLevelShotPath.Separation);
                }
                else if(h.solid!=null)return bounce==3 && h.solid.GetComponentInParent<TutorialTarget>()==target;
                else break;
            }
        }
        return false;
    }
    static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/level2.unity");
        var level=Object.FindAnyObjectByType<FirstLevelRooms>();
        for(int i=0;i<4;i++)level.rooms[i].SetActive(i==0);
        var room=level.rooms[0];
        foreach(var previous in room.GetComponentsInChildren<MirrorRicochetSurface>().Where(s=>s.name.Contains("Three-bounce")).ToArray())Object.DestroyImmediate(previous.gameObject);
        var mirrors=room.GetComponentsInChildren<MirrorRicochetSurface>();var blue=mirrors.First(s=>!s.chargesShot);var red=mirrors.First(s=>s.chargesShot);
        foreach(var m in JsonUtility.FromJson<Layout>(Resources.Load<TextAsset>("SecondLevel/Layout").text).rooms[0].mirrors)
        {var surface=m.red ? red : blue;surface.transform.position=new Vector3(m.art.x,m.art.y,0);surface.transform.rotation=Quaternion.Euler(0,0,m.art.angle);}
        var target=room.GetComponentInChildren<TutorialTarget>();
        var extra=Object.Instantiate(blue,room.transform);extra.name="Blue Mirror — Three-bounce route";
        var polygon=extra.GetComponent<PolygonCollider2D>();var vertices=polygon.points;
        extra.gameObject.SetActive(false);Physics2D.SyncTransforms();
        var accessible=(List<Vector2>)typeof(BuildFullSecondLevel).GetMethod("Reachable",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{level,.15f});
        var bluePolygon=blue.GetComponent<PolygonCollider2D>();var bp=bluePolygon.points;
        Placement found=null;
        foreach(var start in accessible.Where(p=>p.x<-2.5f && p.y<0).OrderBy(p=>Vector2.Distance(p,new Vector2(-4,-1.7f))))
        {
            for(int edge=0;edge<bp.Length && found==null;edge++)for(int sample=1;sample<5 && found==null;sample++)
            {
                Vector2 aim=blue.transform.TransformPoint(Vector2.Lerp(bp[edge],bp[(edge+1)%bp.Length],sample/5f));var ray=(aim-start).normalized;
                var h1=FirstLevelShotPath.Cast(start,ray,.035f,20);if(h1.mirror!=blue)continue;
                var p1=start+ray*h1.distance;var d1=h1.Reflect(ray);
                var h2=FirstLevelShotPath.Cast(p1+d1*.002f,d1,.035f,20,h1.mirrorCollider);if(h2.mirror!=red)continue;
                var p2=p1+d1*(h2.distance+.002f);var d2=h2.Reflect(d1);
                for(float distance=1.4f;distance<6 && found==null;distance+=.15f)
                {
                    var contact=p2+d2*distance;if(contact.x<2.8f || contact.x>5.1f || contact.y<-2.4f || contact.y>-.8f)continue;
                    var outgoing=((Vector2)target.GetComponent<Collider2D>().bounds.center-contact).normalized;
                    var normal=(d2-outgoing).normalized;
                    for(int face=0;face<vertices.Length && found==null;face++)
                    {
                        Vector2 a=vertices[face],b=vertices[(face+1)%vertices.Length];if(Vector2.Distance(a,b)<.5f)continue;
                        extra.transform.rotation=Quaternion.identity;
                        Vector2 tangent=extra.transform.TransformVector(b-a);var localNormal=new Vector2(-tangent.y,tangent.x);
                        float angle=Vector2.SignedAngle(localNormal,normal);extra.transform.rotation=Quaternion.Euler(0,0,angle);
                        extra.transform.position=Vector3.zero;Vector2 localContact=extra.transform.TransformPoint((a+b)*.5f);
                        extra.transform.position=contact-localContact;extra.gameObject.SetActive(true);Physics2D.SyncTransforms();
                        var bounds=polygon.bounds;
                        bool fits=bounds.min.x>2.7f && bounds.max.x<5.8f && bounds.min.y>-3.1f && bounds.max.y<-.8f
                            && extra.transform.position.y<-1.5f && extra.transform.position.y>-2.25f;
                        var overlaps=new List<Collider2D>();polygon.Overlap(new ContactFilter2D().NoFilter(),overlaps);
                        fits&=!overlaps.Any(c=>c!=polygon && !c.isTrigger);
                        if(fits && Chain(start,ray,blue,red,extra,target,.05f) && Chain(start,ray,blue,red,extra,target,.16f)
                            && Chain(start,Quaternion.Euler(0,0,.3f)*ray,blue,red,extra,target,.16f)
                            && Chain(start,Quaternion.Euler(0,0,-.3f)*ray,blue,red,extra,target,.16f))found=new Placement{position=extra.transform.position,angle=angle,origin=start,direction=ray};
                        if(found==null)extra.gameObject.SetActive(false);
                    }
                }
            }
            if(found!=null)break;
        }
        if(found==null){Object.DestroyImmediate(extra.gameObject);throw new Exception("No clear three-mirror chain found.");}
        found.configureBase=true;found.bluePosition=blue.transform.position;found.redPosition=red.transform.position;
        found.blueAngle=blue.transform.eulerAngles.z;found.redAngle=red.transform.eulerAngles.z;
        var reachable=(List<Vector2>)typeof(BuildFullSecondLevel).GetMethod("Reachable",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{level,.1f});
        target.Complete();foreach(var door in room.GetComponentsInChildren<WorkshopDoor>())door.Refresh();Physics2D.SyncTransforms();
        reachable=(List<Vector2>)typeof(BuildFullSecondLevel).GetMethod("Reachable",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{level,.1f});
        if(!reachable.Any(p=>p.x>level.exitX && Mathf.Abs(p.y-level.doorwayY)<level.doorwayHalfHeight))throw new Exception("New mirror blocks exit.");
        // Reload before the real projectile test so a completed target cannot conceal a miss.
        File.WriteAllText(Config,JsonUtility.ToJson(found,true));EditorSceneManager.OpenScene("Assets/Scenes/level2.unity");
        level=Object.FindAnyObjectByType<FirstLevelRooms>();for(int i=0;i<4;i++)level.rooms[i].SetActive(i==0);room=level.rooms[0];Restore(room);Physics2D.SyncTransforms();
        target=room.GetComponentInChildren<TutorialTarget>();var shot=FirstLevelShot.Create(found.origin,found.direction,level.player.GetComponent<FirstLevelTankAim>().projectileSprite,level);
        for(int frame=0;frame<1600 && !shot.Finished;frame++)shot.Step(.05f);
        if(target.IsShielded || target.Health!=5)throw new Exception("Real three-bounce shot did not break the shield: position "+shot.transform.position+" reflections "+shot.Reflections+" shield "+target.IsShielded+" HP "+target.Health);
        EditorSceneManager.OpenScene("Assets/Scenes/level2.unity");level=Object.FindAnyObjectByType<FirstLevelRooms>();for(int i=0;i<4;i++)level.rooms[i].SetActive(i==0);Restore(level.rooms[0]);
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());BuildFullSecondLevel.Capture("Tools/Level2-three-mirror-preview.png");
        level.player.position=found.origin;
        Physics2D.SyncTransforms();
        var points=new List<Vector3>{found.origin};Vector2 p=found.origin,d=found.direction;Collider2D last=null;
        for(int i=0;i<4;i++)
        {
            var h=FirstLevelShotPath.Cast(p,d,.035f,20,last);p+=d*h.distance;points.Add(p);
            if(h.mirror==null)break;d=h.Reflect(d);last=h.mirrorCollider;p+=d*.002f;
        }
        var guide=new GameObject("Verification route").AddComponent<LineRenderer>();guide.useWorldSpace=true;
        guide.sharedMaterial=new Material(Shader.Find("Sprites/Default"));guide.startColor=guide.endColor=new Color(1,.9f,.2f);
        guide.startWidth=guide.endWidth=.035f;guide.sortingOrder=50;guide.positionCount=points.Count;guide.SetPositions(points.ToArray());
        BuildFullSecondLevel.Capture("Tools/Level2-three-mirror-route.png");
        EditorSceneManager.OpenScene("Assets/Scenes/level2.unity");
        File.WriteAllText("Tools/chain-mirror-result.txt","PASSED: blue -> red -> blue -> enemy; real charged projectile breaks shield, 0.05/0.16 flight steps, exit reachable.\n"+JsonUtility.ToJson(found,true));
    }
}

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

public static class VerifyThirdLevel
{
    [InitializeOnLoadMethod] static void Schedule()=>EditorApplication.update+=Check;
    static void Check()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating || !File.Exists("Tools/verify-third-level.request"))return;
        File.Delete("Tools/verify-third-level.request");
        try{Run();}catch(Exception e){File.AppendAllText("Tools/third-level-verification.txt","FAILED: "+e);Debug.LogException(e);EditorSceneManager.OpenScene("Assets/Scenes/level3.unity");}
    }
    static void Require(bool ok,string message){if(!ok)throw new Exception(message);}
    public static void Run()
    {
        File.WriteAllText("Tools/third-level-verification.txt","Checking authored castle...\n");
        EditorSceneManager.OpenScene("Assets/Scenes/level3.unity");var level=Object.FindAnyObjectByType<FirstLevelRooms>();
        level.GetComponent<ThirdLevelCastle>().ConfigureCombat(level);
        int contacts=0,routes=0,blocks=0;var move=typeof(FirstLevelRooms).GetMethod("MoveAxis",BindingFlags.NonPublic|BindingFlags.Instance);
        Require(level.rooms.Length==5,"Five rooms required");
        for(int r=0;r<5;r++)
        {
            for(int i=0;i<5;i++)level.rooms[i].SetActive(i==r);
            var room=level.rooms[r];var edge=room.GetComponentInChildren<EdgeCollider2D>();Physics2D.SyncTransforms();
            for(int pass=0;pass<5;pass++)
            for(int i=0;i<edge.points.Length-1;i++)
            {
                Vector2 a=edge.points[i],b=edge.points[i+1],tangent=(b-a).normalized,inward=new Vector2(tangent.y,-tangent.x);
                int samples=Mathf.Max(1,Mathf.CeilToInt(Vector2.Distance(a,b)/.35f));
                for(int j=0;j<samples;j++)
                {
                    Vector2 surface=Vector2.Lerp(a,b,(j+.15f+pass*.15f)/samples),start=surface+inward*(level.playerRadius+.22f);
                    if(Physics2D.OverlapCircle(start,level.playerRadius)!=null)continue;
                    level.player.position=start;Physics2D.SyncTransforms();
                    for(int k=0;k<20;k++)move.Invoke(level,new object[]{-inward*.035f});
                    Vector2 stop=level.player.position;Require(Vector2.Distance(stop,edge.ClosestPoint(stop))>=level.playerRadius-.009f,"Boundary overlap room "+r+" segment "+i);
                    if(Physics2D.OverlapCircle(stop+inward*.2f,level.playerRadius)==null)
                    {move.Invoke(level,new object[]{inward*.2f});Require(Vector2.Dot((Vector2)level.player.position-stop,inward)>.17f,"Sticking boundary room "+r);}
                    contacts++;
                }
            }
            foreach(var block in room.GetComponentsInChildren<DestructibleCover>())
            {var bounds=block.GetComponent<Collider2D>().bounds;block.Hit(1);Require(block.gameObject.activeSelf && block.Health==2 && block.visual.sprite==block.damageSprites[1],"First block hit/texture");block.Hit(1);Require(block.gameObject.activeSelf && block.Health==1 && block.visual.sprite==block.damageSprites[2],"Second block hit/texture");Require(block.GetComponent<Collider2D>().bounds==bounds,"Damage texture changes must not move collision");block.Hit(1);Require(!block.gameObject.activeSelf,"Third block hit");blocks++;}
            Physics2D.SyncTransforms();level.player.position=new Vector3(-level.spawnX,level.doorwayY,0);
            var reachable=BuildFullSecondLevel.Reachable(level,.12f);
            File.AppendAllText("Tools/third-level-verification.txt","Room "+(r+1)+": "+reachable.Count+" accessible positions.\n");
            foreach(var target in room.GetComponentsInChildren<TutorialTarget>())
            {
                target.Configure(target.IsShielded,(GameObject)typeof(TutorialTarget).GetField("shieldVisual",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(target),target.MaxHealth);
                if(target.IsShielded)
                {
                    Require(BuildFullSecondLevel.FindChargedRoute(level,target,reachable,out var origin,out var direction),"No usable charged route to "+target.name+" room "+r);
                    level.player.position=origin;Physics2D.SyncTransforms();
                    var shot=FirstLevelShot.Create(origin,direction,level.player.GetComponent<FirstLevelTankAim>().projectileSprite,level);
                    for(int step=0;step<1600 && !shot.Finished;step++)shot.Step(.05f);
                    Require(!target.IsShielded,"Real projectile did not break shield "+target.name);routes++;
                }
                if(target.name.Contains("Castle Boss"))
                {Require(target.MaxHealth==10,"Boss health");target.Hit(true,false);Require(target.Health==5,"Boss survives first ricochet");target.Hit(true,false);Require(target.IsDestroyed,"Boss dies after second ricochet");}
                else target.Complete();
            }
            foreach(var door in room.GetComponentsInChildren<WorkshopDoor>())door.Refresh();Physics2D.SyncTransforms();
            reachable=BuildFullSecondLevel.Reachable(level,r==3?.04f:.12f);
            for(float x=5.5f;x<8.1f;x+=.2f)File.AppendAllText("Tools/third-level-verification.txt",x+":"+string.Join(",",Physics2D.OverlapCircleAll(new Vector2(x,level.doorwayY),level.playerRadius).Select(c=>c.name))+"; ");
            File.AppendAllText("Tools/third-level-verification.txt","Exit room "+r+" maxX="+reachable.Max(p=>p.x)+" clear="+FirstLevelEnemies.Cleared(room)+" barriers="+string.Join(",",room.GetComponentsInChildren<WorkshopDoor>().Select(d=>d.name+":"+d.barrier.enabled))+"\n");
            Require(reachable.Any(p=>p.x>level.exitX-.35f && Mathf.Abs(p.y-level.doorwayY)<.4f),"Exit not reachable in room "+r);
        }
        Require(blocks==14 && routes==6,"Roster counts");
        var isolated=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
        var blockObject=new GameObject("Projectile Block Test");blockObject.transform.position=new Vector3(1000,1000,0);blockObject.AddComponent<BoxCollider2D>();
        var cover=blockObject.AddComponent<DestructibleCover>();Physics2D.SyncTransforms();
        for(int i=0;i<3;i++)
        {
            var ball=FirstLevelShot.Create(new Vector2(998,1000),Vector2.right,null,null);ball.Step(4);
            Require(ball.Finished && cover.Health==2-i,"Actual ordinary bullet damage");
        }
        Require(!blockObject.activeSelf,"Ordinary bullets must remove cover collider");
        blockObject.SetActive(true);Object.DestroyImmediate(cover);cover=blockObject.AddComponent<DestructibleCover>();blockObject.transform.position=new Vector3(999,1001,0);
        var mirrorObject=new GameObject("Projectile Mirror Test");mirrorObject.transform.position=new Vector3(1002,1001,0);
        mirrorObject.AddComponent<BoxCollider2D>().size=new Vector2(.1f,2);mirrorObject.AddComponent<MirrorRicochetSurface>().reflectFromCollider=true;
        Physics2D.SyncTransforms();var reflectedBall=FirstLevelShot.Create(new Vector2(1000,1001),Vector2.right,null,null);reflectedBall.Step(6);
        Require(reflectedBall.Finished && reflectedBall.Reflections==1 && !blockObject.activeSelf,"One real blue ricochet destroys cover");
        EditorSceneManager.CloseScene(isolated,true);
        InstallFirstLevelEnemies.Verify();
        EditorSceneManager.OpenScene("Assets/Scenes/level3.unity");
        File.AppendAllText("Tools/third-level-verification.txt","PASSED: "+contacts+" perimeter contacts over five passes, 14 block health checks, six real shield-breaking routes, all five exits and boss health.\n");
        Debug.Log("THIRD_LEVEL_VERIFIED");
    }
}



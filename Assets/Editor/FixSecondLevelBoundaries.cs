using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class FixSecondLevelBoundaries
{
    const string Request="Tools/fix-second-level-boundaries.request";
    const string BottomRequest="Tools/check-second-level-bottom.request";
    static double readyAt=-1;
    [InitializeOnLoadMethod]
    static void Schedule()
    {
        if(!File.Exists(Request) && !File.Exists(BottomRequest))return;
        EditorApplication.update+=Pending;
    }
    static void Pending()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating){readyAt=-1;return;}
        if(readyAt<0){readyAt=EditorApplication.timeSinceStartup+1;return;}
        if(EditorApplication.timeSinceStartup<readyAt)return;
        EditorApplication.update-=Pending;
        bool bottomOnly=File.Exists(BottomRequest);
        File.Delete(bottomOnly ? BottomRequest : Request);
        Run(bottomOnly);
    }
    [MenuItem("Bounce Club/Fix Second Level Boundaries")]
    public static void Fix()
    {
        Run(false);
    }
    [MenuItem("Bounce Club/Check Second Level Bottom Five Times")]
    public static void CheckBottom()=>Run(true);
    static void Run(bool bottomOnly)
    {
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/level2.unity");
        var level=UnityEngine.Object.FindAnyObjectByType<FirstLevelRooms>();
        var edge=GameObject.Find("Workshop Floor Boundary").GetComponent<EdgeCollider2D>();
        if(!bottomOnly)edge.points=BuildEmptySecondLevel.BoundaryPixels.Select(p=>new Vector2((p.x-793)/100f,(496-p.y)/100f)).ToArray();
        Physics2D.SyncTransforms();
        var move=typeof(FirstLevelRooms).GetMethod("MoveAxis",BindingFlags.NonPublic|BindingFlags.Instance);
        Vector3 spawn=level.player.position;
        int tested=0;
        var contacts=new System.Collections.Generic.List<Vector2>();
        for(int pass=0;pass<(bottomOnly ? 5 : 1);pass++)
        for(int i=0;i<edge.points.Length-1;i++)
        {
            Vector2 a=edge.points[i],b=edge.points[i+1];
            if(bottomOnly && (a.y>-.59f || b.y>-.59f))continue;
            Vector2 tangent=(b-a).normalized;
            Vector2 inward=new Vector2(tangent.y,-tangent.x);
            int samples=Mathf.Max(1,Mathf.CeilToInt(Vector2.Distance(a,b)/.25f));
            for(int j=0;j<samples;j++)
            {
                Vector2 surface=Vector2.Lerp(a,b,(j+(bottomOnly ? .2f+pass*.15f : .5f))/samples);
                Vector2 start=surface+inward*(level.playerRadius+.25f);
                // Tiny recesses narrower than the tank are handled by their neighboring edges.
                if(Physics2D.OverlapCircle(start,level.playerRadius)!=null)continue;
                level.player.position=start;Physics2D.SyncTransforms();
                for(int k=0;k<20;k++)
                {
                    Vector2 step=-inward*(.03f+pass*.02f);
                    move.Invoke(level,new object[]{new Vector2(step.x,0)});
                    move.Invoke(level,new object[]{new Vector2(0,step.y)});
                }
                Vector2 stopped=level.player.position;
                float gap=Vector2.Distance(stopped,edge.ClosestPoint(stopped));
                if(gap<level.playerRadius-.006f || gap>level.playerRadius+.015f)
                    throw new Exception("Incorrect perimeter contact at segment "+i+": "+gap);
                move.Invoke(level,new object[]{inward*.2f});
                if(Vector2.Dot((Vector2)level.player.position-stopped,inward)<.18f)
                    throw new Exception("Cannot leave perimeter at segment "+i);
                if(bottomOnly)
                {
                    foreach(float sign in new[]{-1f,1f})
                    {
                        level.player.position=stopped;Physics2D.SyncTransforms();
                        for(int k=0;k<8;k++)
                        {
                            Vector2 slide=tangent*sign*.025f;
                            move.Invoke(level,new object[]{new Vector2(slide.x,0)});
                            move.Invoke(level,new object[]{new Vector2(0,slide.y)});
                            Vector2 p=level.player.position;
                            if(Vector2.Distance(p,edge.ClosestPoint(p))<level.playerRadius-.006f)
                                throw new Exception("Sliding penetrated lower edge at segment "+i);
                        }
                    }
                }
                tested++;contacts.Add(stopped);
            }
        }
        if(tested<100)throw new Exception("Not enough perimeter contacts verified: "+tested);
        level.player.position=spawn;Physics2D.SyncTransforms();
        if(!bottomOnly)EditorSceneManager.SaveScene(scene);
        ContactPreview(level,contacts,bottomOnly ? "Tools/Level2-bottom-contacts.png" : "Tools/Level2-boundary-contacts.png");
        level.player.position=spawn;
        int segments=edge.points.Length-1;
        EditorSceneManager.OpenScene("Assets/Scenes/level2.unity");
        File.WriteAllText(bottomOnly ? "Tools/second-level-bottom-result.txt" : "Tools/second-level-boundaries-result.txt",
            (bottomOnly ? "BOTTOM_FIVE_PASSES_VERIFIED: " : "PERIMETER_VERIFIED: ")+tested+" approaches, retreats and slide checks; outline has "+segments+" segments.");
        Debug.Log((bottomOnly ? "SECOND_LEVEL_BOTTOM_FIVE_PASSES_VERIFIED: " : "SECOND_LEVEL_PERIMETER_VERIFIED: ")+tested+" contacts and retreats.");
    }
    static void ContactPreview(FirstLevelRooms level,System.Collections.Generic.List<Vector2> contacts,string output)
    {
        var camera=Camera.main;
        var fit=camera.GetComponent<FirstLevelCameraFit>();fit.enabled=false;
        var previous=RenderTexture.active;
        var target=new RenderTexture(400,300,24);camera.targetTexture=target;camera.orthographicSize=1.5f;
        var sheet=new Texture2D(1600,900,TextureFormat.RGB24,false);
        var tile=new Texture2D(400,300,TextureFormat.RGB24,false);
        for(int i=0;i<12;i++)
        {
            Vector2 point=contacts[Mathf.Min(contacts.Count-1,i*contacts.Count/12)];
            level.player.position=point;camera.transform.position=new Vector3(point.x,point.y,-10);
            camera.Render();RenderTexture.active=target;
            tile.ReadPixels(new Rect(0,0,400,300),0,0);tile.Apply();
            sheet.SetPixels(i%4*400,(2-i/4)*300,400,300,tile.GetPixels());
        }
        sheet.Apply();File.WriteAllBytes(output,sheet.EncodeToPNG());
        camera.targetTexture=null;RenderTexture.active=previous;
        UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(tile);UnityEngine.Object.DestroyImmediate(sheet);
    }
}

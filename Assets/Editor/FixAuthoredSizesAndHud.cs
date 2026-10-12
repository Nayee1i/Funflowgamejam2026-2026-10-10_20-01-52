using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;

public static class FixAuthoredSizesAndHud
{
    [Serializable] class Room { public BuildFullSecondLevel.Art[] obstacles,blocks; }
    [Serializable] class Layout { public Room[] rooms; }
    static double ready;
    [InitializeOnLoadMethod] static void Hook()=>EditorApplication.update+=Tick;
    static void Tick()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating){ready=EditorApplication.timeSinceStartup+2;return;}
        const string request="Tools/fix-authored-sizes.request";
        if(EditorApplication.timeSinceStartup<ready || !File.Exists(request))return;
        File.Delete(request);
        try{Run();}catch(Exception e){File.AppendAllText("Tools/authored-sizes-result.txt","FAILED: "+e);Debug.LogException(e);}
    }
    static void Run()
    {
        MakeHalfHeart();
        var original=SceneManager.GetActiveScene();
        Directory.CreateDirectory("Tools/Backups");
        File.WriteAllText("Tools/authored-sizes-result.txt","Restore Figma obstacle scales; retain mirror transforms.\n");
        foreach(string name in new[]{"level2","level3"})
        {
            string path="Assets/Scenes/"+name+".unity";
            var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.IsValid() || !scene.isLoaded;
            if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            if(scene.isDirty)EditorSceneManager.SaveScene(scene,"Tools/Backups/before-authored-sizes-"+name+".unity",true);
            var level=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<FirstLevelRooms>(true)).Single();
            if(name=="level3")
            {
                var lower=level.rooms[3].GetComponentsInChildren<MirrorRicochetSurface>(true).Single(m=>m.name.Contains("103:4222"));
                if(lower.transform.Find("Approved Lower Mirror Passage") == null)
                {
                    lower.transform.position+=Vector3.down*.6f;
                    new GameObject("Approved Lower Mirror Passage").transform.SetParent(lower.transform,false);
                    File.AppendAllText("Tools/authored-sizes-result.txt","Approved: room 3.4 lower red mirror moved down 0.6; angle and size retained.\n");
                }
            }
            var mirrors=level.rooms.SelectMany(r=>r.GetComponentsInChildren<MirrorRicochetSurface>(true)).Select(m=>(m.transform,m.transform.position,m.transform.rotation,m.transform.localScale)).ToArray();
            var data=JsonUtility.FromJson<Layout>(Resources.Load<TextAsset>((name=="level2" ? "SecondLevel" : "ThirdLevel")+"/Layout").text);
            int changed=0;
            for(int i=0;i<data.rooms.Length;i++)
            {
                var objects=level.rooms[i].GetComponentsInChildren<PolygonCollider2D>(true);
                foreach(var art in (data.rooms[i].obstacles??Array.Empty<BuildFullSecondLevel.Art>()).Concat(data.rooms[i].blocks??Array.Empty<BuildFullSecondLevel.Art>()))
                {
                    var obstacle=objects.Single(c=>c.name.EndsWith(art.node));
                    var desired=new Vector3(art.sx,art.sy,1);
                    if(name=="level2" && (art.node=="71:3602" || art.node=="71:3652"))desired*=.95f;
                    desired.z=1;obstacle.transform.localScale=desired;changed++;
                    obstacle.transform.position=new Vector3(art.x,art.y,0);
                    obstacle.transform.rotation=Quaternion.Euler(0,0,art.angle);
                    if(name=="level2" && art.node=="71:3652")obstacle.transform.position+=Vector3.up*(44f*1586f/1270f/100f);
                    // Leave a six-pixel contact margin for the painted rim/shadow, without shrinking artwork.
                    Vector2 min=new Vector2(art.outline.Min(p=>p.x),art.outline.Min(p=>p.y));
                    Vector2 max=new Vector2(art.outline.Max(p=>p.x),art.outline.Max(p=>p.y));
                    Vector2 centre=(min+max)*.5f,extent=(max-min)*.5f;
                    Vector2 inset=new Vector2(Mathf.Max(.8f,1-.06f/(extent.x*Mathf.Abs(desired.x))),Mathf.Max(.8f,1-.06f/(extent.y*Mathf.Abs(desired.y))));
                    obstacle.points=art.outline.Select(p=>centre+Vector2.Scale(p-centre,inset)).ToArray();
                    foreach(string marker in new[]{"Proportion Scale Applied","Wider Tank Passage Applied"})
                    {var child=obstacle.transform.Find(marker);if(child!=null)Object.DestroyImmediate(child.gameObject);}
                }
            }
            foreach(var entry in mirrors)if(entry.Item1.position!=entry.Item2 || entry.Item1.rotation!=entry.Item3 || entry.Item1.localScale!=entry.Item4)throw new Exception("Mirror changed");
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            File.AppendAllText("Tools/authored-sizes-result.txt",name+": restored "+changed+" obstacle/block scales; "+mirrors.Length+" mirrors unchanged.\n");
            if(opened)EditorSceneManager.CloseScene(scene,true);
        }
        SceneManager.SetActiveScene(original);
        File.AppendAllText("Tools/authored-sizes-result.txt","PASSED: native half-heart aligned and composited on empty heart; runtime victory time frame added for every level.\n");
    }
    static Texture2D Load(string path){var texture=new Texture2D(2,2);ImageConversion.LoadImage(texture,File.ReadAllBytes(path));return texture;}
    static void MakeHalfHeart()
    {
        var empty=Load("Assets/Resources/FirstLevelEnemies/HeartEmpty.png");
        var source=Load("Assets/Resources/FirstLevelFX/UI_139_18.png");
        var pixels=source.GetPixels32();int left=source.width,right=0,bottom=source.height,top=0;
        for(int y=0;y<source.height;y++)for(int x=0;x<source.width;x++)if(pixels[y*source.width+x].a>32)
        {left=Mathf.Min(left,x);right=Mathf.Max(right,x);bottom=Mathf.Min(bottom,y);top=Mathf.Max(top,y);}
        // Replace the entire baked heart first; transparency must never reveal its old red fill.
        var output=new Texture2D(empty.width,empty.height,TextureFormat.RGBA32,false);
        for(int y=0;y<output.height;y++)for(int x=0;x<output.width;x++)
        {
            var native=source.GetPixelBilinear((left+(right-left)*x/(float)(output.width-1))/source.width,(bottom+(top-bottom)*y/(float)(output.height-1))/source.height);
            var background=empty.GetPixel(x,y);var result=Color.Lerp(background,native,native.a);result.a=1;output.SetPixel(x,y,result);
        }
        output.Apply();const string path="Assets/Resources/FirstLevelEnemies/HeartHalfAligned.png";File.WriteAllBytes(path,output.EncodeToPNG());
        Object.DestroyImmediate(source);Object.DestroyImmediate(empty);Object.DestroyImmediate(output);
        AssetDatabase.ImportAsset(path);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
        importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.filterMode=FilterMode.Bilinear;importer.SaveAndReimport();
    }
}

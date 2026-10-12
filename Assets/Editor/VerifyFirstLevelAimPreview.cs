using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Uses the authored physical edges rather than coordinates from an old layout.
public static class VerifyFirstLevelAimPreview
{
 const string Request="Tools/verify-aim-preview.request";
 [InitializeOnLoadMethod]static void Hook()=>EditorApplication.update+=Check;
 static void Check(){if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists(Request))return;File.Delete(Request);Verify();}
 [MenuItem("Bounce Club/Verify Ricochet Aim Preview")]
 public static void Verify()
 {
  string original=EditorSceneManager.GetActiveScene().path;int tested=0;
  try{foreach(string name in new[]{"Level1Rooms","level2","level3"}){
   EditorSceneManager.OpenScene("Assets/Scenes/"+name+".unity");var level=UnityEngine.Object.FindAnyObjectByType<FirstLevelRooms>();level.player.position=new Vector3(1000,1000,0);
   foreach(var room in level.rooms){foreach(var other in level.rooms)other.SetActive(other==room);Physics2D.SyncTransforms();
    foreach(var mirror in room.GetComponentsInChildren<MirrorRicochetSurface>()){
     var edge=mirror.GetComponent<EdgeCollider2D>();Require(edge!=null,"Mirror is missing Vlad's edge: "+mirror.name);bool checkedMirror=false;
     for(int i=1;i<edge.points.Length&&!checkedMirror;i++){
      Vector2 a=edge.transform.TransformPoint(edge.points[i-1]+edge.offset),b=edge.transform.TransformPoint(edge.points[i]+edge.offset);Vector2 tangent=(b-a).normalized,normal=new Vector2(-tangent.y,tangent.x);
      foreach(float side in new[]{-1f,1f}){
       Vector2 origin=(a+b)*.5f+normal*side*.6f,direction=-normal*side;var hit=FirstLevelShotPath.Cast(origin,direction,.035f,1.2f);if(hit.mirror!=mirror)continue;
       float travel=hit.distance+.08f;var segments=new List<FirstLevelAimPreview.Segment>();FirstLevelAimPreview.Trace(origin,direction,.035f,travel,segments);
       var shot=FirstLevelShot.Create(origin,direction,null,null);
       try{shot.Step(travel);Require(!shot.Finished&&shot.Reflections==1,"Actual reflection failed: "+mirror.name);Require(segments.Count==2,"Preview bounce missing: "+mirror.name);Require(Vector2.Distance(shot.transform.position,segments[1].end)<.008f,"Preview/projectile mismatch: "+mirror.name);Require(shot.Charged==mirror.chargesShot,"Wrong red charge: "+mirror.name);}finally{if(shot!=null)UnityEngine.Object.DestroyImmediate(shot.gameObject);}
       tested++;checkedMirror=true;break;
      }
     }
     Require(checkedMirror,"No accessible reflecting side: "+mirror.name);
    }
   }
  }Directory.CreateDirectory("Tools");File.WriteAllText("Tools/aim-preview-result.txt","PASSED: "+tested+" authored mirror edges across three levels; preview/projectile endpoints and red charge agree.");}
  finally{if(!string.IsNullOrEmpty(original))EditorSceneManager.OpenScene(original);}
 }
 static void Require(bool value,string message){if(!value)throw new Exception(message);}
}

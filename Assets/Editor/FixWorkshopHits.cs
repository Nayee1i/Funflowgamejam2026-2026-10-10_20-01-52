using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
[InitializeOnLoad] public static class FixWorkshopHits {
 static double ready;static FixWorkshopHits(){EditorApplication.update+=Tick;}
 static void Require(bool b,string s){if(!b)throw new Exception(s);}
 static void Tick(){if(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating){ready=EditorApplication.timeSinceStartup+3;return;}if(EditorApplication.timeSinceStartup<ready || !File.Exists("Tools/fix-workshop-hits.request"))return;File.Delete("Tools/fix-workshop-hits.request");var original=SceneManager.GetActiveScene();var scene=EditorSceneManager.OpenScene("Assets/Scenes/level2.unity",OpenSceneMode.Additive);SceneManager.SetActiveScene(scene);try{
 var level=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<FirstLevelRooms>(true)).First();
 Directory.CreateDirectory("Tools/Backups");if(!File.Exists("Tools/Backups/level2-before-hit-and-passage-fix.unity"))EditorSceneManager.SaveScene(scene,"Tools/Backups/level2-before-hit-and-passage-fix.unity",true);
 foreach(var t in level.GetComponentsInChildren<TutorialTarget>(true))FixTarget(t);
 foreach(var room in level.rooms)foreach(var t in room.GetComponentsInChildren<TutorialTarget>(true))FixTarget(t);
 var upper=level.rooms[2].GetComponentsInChildren<Transform>(true).First(t=>t.name=="Obstacle 5");
 var lower=level.rooms[2].GetComponentsInChildren<Transform>(true).First(t=>t.name=="Obstacle 6");
 upper.position=new Vector3(2.22f,.70f,upper.position.z);lower.position=new Vector3(2.20f,-.70f,lower.position.z);
 EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
 var report=new System.Collections.Generic.List<string>();
 for(int i=0;i<3;i++){
 for(int j=0;j<level.rooms.Length;j++)level.rooms[j].SetActive(i==j);
 foreach(var t in level.rooms[i].GetComponentsInChildren<TutorialTarget>(true)){
 Physics2D.SyncTransforms();var c=t.GetComponent<CircleCollider2D>();var body=t.transform.Find("Body").GetComponent<SpriteRenderer>();
 Require(Vector2.Distance(c.bounds.center,body.bounds.center)<.2f,"Body hitbox offset: "+t.name);
 var shield=new SerializedObject(t).FindProperty("shieldVisual").objectReferenceValue as GameObject;
 bool had=t.IsShielded;int hp=t.MaxHealth;bool red=t.IsRedRobot;t.Configure(false,shield,hp,false,red);t.gameObject.SetActive(true);Physics2D.SyncTransforms();
 for(int hit=0;hit<hp;hit++){var centre=(Vector2)c.bounds.center;var shot=FirstLevelShot.Create(centre+Vector2.left*.8f,Vector2.right,null,level);shot.Step(1.6f);Require(t.Health==hp-hit-1,"Projectile missed actual body / wrong damage: "+t.name);if(shot!=null)UnityEngine.Object.DestroyImmediate(shot.gameObject);}
 t.gameObject.SetActive(true);t.Configure(had,shield,hp,false,red);report.Add("ROOM "+(i+1)+" "+t.name+": direct shots="+hp+", hitbox aligned");}
 }
 for(int j=0;j<level.rooms.Length;j++)level.rooms[j].SetActive(j==2);
 var accessible=BuildFullSecondLevel.Reachable(level,.1f);
 report.Add("ROOM 3 reachable cells="+accessible.Count+" maxX="+accessible.Max(p=>p.x));
 foreach(var t in level.rooms[2].GetComponentsInChildren<TutorialTarget>().Where(t=>t.IsShielded).ToArray()){
 Require(BuildFullSecondLevel.FindChargedRoute(level,t,accessible,out var start,out var direction),"No red-mirror route to "+t.name);
 report.Add("RED MIRROR ROUTE "+t.name+": start="+start+" direction="+direction);}
 foreach(var t in level.rooms[2].GetComponentsInChildren<TutorialTarget>())t.Complete();foreach(var door in level.rooms[2].GetComponentsInChildren<WorkshopDoor>())door.Refresh();Physics2D.SyncTransforms();var cleared=BuildFullSecondLevel.Reachable(level,.1f);Require(cleared.Any(p=>p.x>level.exitX),"Room 3 cleared exit blocked maxX="+cleared.Max(p=>p.x));report.Add("ROOM 3: cleared exit reachable with full tank radius");File.WriteAllLines("Tools/workshop-hit-fix-result.txt",report.Concat(new[]{"PASSED"}));
 }catch(Exception e){File.WriteAllText("Tools/workshop-hit-fix-result.txt","FAILED: "+e);Debug.LogException(e);}finally{foreach(var root in scene.GetRootGameObjects())foreach(var t in root.GetComponentsInChildren<TutorialTarget>(true))FixTarget(t);var owner=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<FirstLevelRooms>(true)).First();for(int i=0;i<owner.rooms.Length;i++)owner.rooms[i].SetActive(i==0);foreach(var room in owner.rooms)foreach(var door in room.GetComponentsInChildren<WorkshopDoor>(true))door.Refresh();EditorSceneManager.SaveScene(scene);EditorSceneManager.CloseScene(scene,true);if(original.IsValid() && original.isLoaded)SceneManager.SetActiveScene(original);}}
 static void FixTarget(TutorialTarget t){var healthBar=t.transform.Find("Health Bar");if(healthBar!=null)UnityEngine.Object.DestroyImmediate(healthBar.gameObject);t.AlignBodyHitbox();if(!t.IsBoss){bool red=t.name.StartsWith("Promoted Robot") || t.IsRedRobot;var shield=new SerializedObject(t).FindProperty("shieldVisual").objectReferenceValue as GameObject;t.gameObject.SetActive(true);t.Configure(shield!=null,shield,red?4:2,false,red);EditorUtility.SetDirty(t);}}
}







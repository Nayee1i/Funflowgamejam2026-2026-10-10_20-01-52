using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
[InitializeOnLoad] public static class InspectWorkshopHits {
 static double ready;
 static InspectWorkshopHits(){EditorApplication.update+=Tick;}
 static void Tick(){if(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating){ready=EditorApplication.timeSinceStartup+3;return;}if(EditorApplication.timeSinceStartup<ready || !File.Exists("Tools/inspect-workshop.request"))return;File.Delete("Tools/inspect-workshop.request");var original=SceneManager.GetActiveScene();var scene=EditorSceneManager.OpenScene("Assets/Scenes/level2.unity",OpenSceneMode.Additive);try{var level=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<FirstLevelRooms>(true)).First();var lines=new System.Collections.Generic.List<string>();for(int i=0;i<level.rooms.Length;i++){lines.Add("ROOM "+i);foreach(var t in level.rooms[i].GetComponentsInChildren<TutorialTarget>(true)){var c=t.GetComponent<CircleCollider2D>();lines.Add(t.name+" hp="+t.MaxHealth+" red="+t.IsRedRobot+" shield="+t.IsShielded+" pos="+t.transform.position+" scale="+t.transform.lossyScale+" collider="+c.offset+" radius="+c.radius);foreach(var s in t.GetComponentsInChildren<SpriteRenderer>(true).Where(s=>s.name=="Body" || s.name=="Turret Artwork"))lines.Add("  "+s.name+" asset="+s.sprite.name+" pos="+s.transform.position+" bounds="+s.bounds+" scale="+s.transform.lossyScale);}
foreach(var s in level.rooms[i].GetComponentsInChildren<SpriteRenderer>(true).Where(s=>s.GetComponent<PolygonCollider2D>()!=null && s.GetComponentInParent<TutorialTarget>()==null))lines.Add("PROP "+s.name+" pos="+s.transform.position+" bounds="+s.bounds);}
File.WriteAllLines("Tools/workshop-hit-inspection.txt",lines);for(int i=0;i<level.rooms.Length;i++)level.rooms[i].SetActive(i==2);level.blackout.color=Color.clear;var hidden=UnityEngine.Object.FindObjectsByType<Camera>().Where(c=>c.gameObject.scene!=scene).Select(c=>c.gameObject).Concat(UnityEngine.Object.FindObjectsByType<Canvas>().Where(c=>c.gameObject.scene!=scene).Select(c=>c.gameObject)).Distinct().ToArray();try{foreach(var g in hidden)g.SetActive(false);BuildFullSecondLevel.Capture("Tools/workshop-room3-passage-fixed.png");}finally{foreach(var g in hidden)g.SetActive(true);}}catch(Exception e){File.WriteAllText("Tools/workshop-hit-inspection.txt",e.ToString());}finally{EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(original);}}
}


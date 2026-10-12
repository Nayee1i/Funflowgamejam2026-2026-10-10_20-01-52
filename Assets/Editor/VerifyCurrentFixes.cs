using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class VerifyCurrentFixes {
 public static void Run(){try{
 var scene=EditorSceneManager.OpenScene("Assets/Scenes/level2.unity");var level=UnityEngine.Object.FindAnyObjectByType<FirstLevelRooms>();for(int i=0;i<level.rooms.Length;i++)level.rooms[i].SetActive(i==0);
 var mirror=level.rooms[0].GetComponentsInChildren<MirrorRicochetSurface>().First(m=>!m.chargesShot);var edge=mirror.GetComponent<EdgeCollider2D>();Require(edge!=null && mirror.GetComponents<Collider2D>().Length==1,"Blue mirror needs one physical edge");Require(Vector2.Distance(edge.points[0],new Vector2(-.68f,-.015f))<.001f && Vector2.Distance(edge.points[1],new Vector2(.67f,-.015f))<.001f,"Blue edge override not applied");Physics2D.SyncTransforms();var mid=(Vector2)mirror.transform.TransformPoint((edge.points[0]+edge.points[1])*.5f);var up=(Vector2)mirror.transform.up;foreach(float sign in new[]{-1f,1f}){var hit=FirstLevelShotPath.Cast(mid+up*sign*.4f,-up*sign,.035f,.8f);Require(hit.mirror==mirror,"Blue mirror must reflect from both sides");}
 scene=EditorSceneManager.OpenScene("Assets/Scenes/level3.unity");level=UnityEngine.Object.FindAnyObjectByType<FirstLevelRooms>();for(int i=0;i<level.rooms.Length;i++)level.rooms[i].SetActive(i==0);Physics2D.SyncTransforms();var covers=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<DestructibleCover>(true)).ToArray();Require(covers.Length>0,"No castle blocks");foreach(var cover in covers){Require(cover.visual!=null && cover.visual.transform.IsChildOf(cover.transform),"Detached block image "+cover.name);Require(level.rooms.Any(r=>cover.transform.IsChildOf(r.transform)),"Orphan block "+cover.name);}
 foreach(float x in new[]{-.8f,-.4f,0f,.4f,.8f})Require(!Physics2D.OverlapCircleAll(new Vector2(x,0),level.playerRadius).Any(c=>!c.isTrigger),"Room 3.1 centre blocked at "+x);
 File.WriteAllText("Tools/current-fixes-result.txt","PASSED: room 2.1 blue glass edge overrides, one collider, actual reflections from both sides; castle blocks retain images and room parents; full-size hero can cross centre of 3.1.");
 }catch(Exception e){File.WriteAllText("Tools/current-fixes-result.txt","FAILED: "+e);throw;}}
 static void Require(bool value,string reason){if(!value)throw new Exception(reason);}
}

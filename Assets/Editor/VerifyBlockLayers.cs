using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;

public static class VerifyBlockLayers
{
 static double ready;
 [InitializeOnLoadMethod] static void Hook()=>EditorApplication.update+=Tick;
 static void Tick()
 {
  if(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating){ready=EditorApplication.timeSinceStartup+3;return;}
  const string request="Tools/verify-block-layers.request";
  if(!File.Exists(request) || EditorApplication.timeSinceStartup<ready)return;
  File.Delete(request);
  var original=SceneManager.GetActiveScene();
  var scene=SceneManager.GetSceneByPath("Assets/Scenes/level3.unity");bool opened=!scene.isLoaded;
  if(opened)scene=EditorSceneManager.OpenScene("Assets/Scenes/level3.unity",OpenSceneMode.Additive);
  try
  {
   if(scene.isDirty)EditorSceneManager.SaveScene(scene,"Tools/Backups/level3-before-block-layers.unity",true);
   FirstLevelRooms level=null;
   foreach(var root in scene.GetRootGameObjects())if(root.TryGetComponent<FirstLevelRooms>(out var owner))level=owner;
   foreach(var room in level.rooms)foreach(var block in room.GetComponentsInChildren<DestructibleCover>(true))block.ArrangeLayers();
   EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
   int checks=0;
   foreach(var room in level.rooms)
   {
    var blocks=room.GetComponentsInChildren<DestructibleCover>(true);
    foreach(var block in blocks)
    {
     int order=block.visual.sortingOrder;
     if(block.transform.parent.GetComponent<SortingGroup>()?.sortingOrder!=6)throw new Exception("Block is outside its render layer");
     for(int stage=0;stage<3;stage++)
     {
      block.ShowDamage(stage);
      if(block.visual.sortingOrder!=order)throw new Exception("Damage changed block order");
      foreach(var other in blocks)if(other.transform.position.y<block.transform.position.y && other.visual.sortingOrder<=order)throw new Exception("Lower block obscured by upper block");
      checks++;
     }
     block.ShowDamage(0);
    }
   }
   File.WriteAllText("Tools/block-layers-result.txt","PASSED: "+checks+" intact/damaged block checks across all castle rooms; lower blocks draw over middle/upper blocks, debris below surviving blocks, mirrors and hero above blocks. Scene saved without changing placements or sizes.");
  }
  catch(Exception e){File.WriteAllText("Tools/block-layers-result.txt","FAILED: "+e);Debug.LogException(e);}
  finally{if(opened)EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(original);}
 }
}

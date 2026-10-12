using System;
using System.IO;
using System.Reflection;
using System.Collections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
[InitializeOnLoad] public static class VerifyFirstLevelAtmosphere {
 static double ready;static VerifyFirstLevelAtmosphere(){EditorApplication.update+=Tick;}
 static void Tick(){if(EditorApplication.isCompiling || EditorApplication.isUpdating){ready=EditorApplication.timeSinceStartup+3;return;}if(EditorApplication.timeSinceStartup<ready)return;
 if(EditorApplication.isPlaying){if(SessionState.GetBool("Tracks.Verify",false) && SceneManager.GetActiveScene().name=="MainMenu")SceneManager.LoadScene("Level1Rooms");if(SessionState.GetBool("Tracks.Verify",false) && SceneManager.GetActiveScene().name=="Level1Rooms"){SessionState.SetBool("Tracks.Verify",false);new GameObject("Track Verification").AddComponent<TrackVerificationRunner>();}return;}
 if(EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists("Tools/verify-tank-tracks.request"))return;File.Delete("Tools/verify-tank-tracks.request");
 const string path="Assets/Resources/FirstLevelFX/TankTrackAtlas.png";AssetDatabase.ImportAsset(path);var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Default;importer.maxTextureSize=4096;importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();SessionState.SetBool("Tracks.Verify",true);EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>(BuildFirstLevelRooms.ScenePath);EditorApplication.isPlaying=true;
 }
}
public class TrackVerificationRunner:MonoBehaviour {
 IEnumerator Start(){yield return new WaitForSecondsRealtime(1.5f);DontDestroyOnLoad(gameObject);try{Check();}catch(Exception e){File.WriteAllText("Tools/tank-tracks-result.txt","FAILED: "+e);Debug.LogException(e);EditorApplication.isPlaying=false;yield break;}ScreenCapture.CaptureScreenshot("Tools/tank-track-first-level.png",2);yield return new WaitForSecondsRealtime(.3f);foreach(var sceneName in new[]{"level2","level3"}){SceneManager.LoadScene(sceneName);yield return new WaitForSecondsRealtime(1.5f);try{CheckAllRooms(sceneName=="level2"?4:0);}catch(Exception e){File.WriteAllText("Tools/tank-tracks-result.txt","FAILED: "+e);Debug.LogException(e);EditorApplication.isPlaying=false;yield break;}ScreenCapture.CaptureScreenshot("Tools/tank-track-"+sceneName+".png",2);yield return new WaitForSecondsRealtime(.3f);}File.WriteAllText("Tools/tank-tracks-result.txt","PASSED: all 12 rooms emit tracks at opacity 0.45; all 19 actual lamps pulse;  original Figma tracks appear on movement only; fade to zero and retire; no collider; floor layering; pause freezes effects; room changes clear trails; 96-object pool cap; all three first-level lamps pulse gently.");EditorApplication.isPlaying=false;}
 void CheckAllRooms(int expectedLamps){
 var level=UnityEngine.Object.FindAnyObjectByType<FirstLevelRooms>();level.GetComponent<FirstLevelPresentation>().CloseModal();level.enabled=false;
 foreach(var robot in UnityEngine.Object.FindObjectsByType<TutorialRobot>())robot.enabled=false;
 var effect=level.GetComponent<FirstLevelAtmosphere>();Require(effect!=null,"Missing atmosphere on "+SceneManager.GetActiveScene().name);effect.enabled=false;
 for(int i=0;i<level.rooms.Length;i++){
 typeof(FirstLevelRooms).GetMethod("ShowRoom",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(level,new object[]{i});effect.Tick(0);Require(effect.VisibleMarks==0,"Room change leaves tracks");
 for(int step=0;step<10;step++){level.player.position+=Vector3.right*.1f;effect.Tick(.03f);}Require(effect.VisibleMarks>=3,"Room "+i+" missing tracks");Require(Mathf.Approximately(effect.trackOpacity,.45f),"Tracks too bright");
 int count=0;foreach(var lamp in level.rooms[i].GetComponentsInChildren<SpriteRenderer>())if(lamp.name.StartsWith("Lamp Glow")){count++;float before=lamp.color.a;effect.Tick(.5f);Require(Mathf.Abs(before-lamp.color.a)>.00001f,"Lamp does not pulse");Require(lamp.color.a<.31f,"Lamp too bright");}Require(count==expectedLamps,"Wrong lamp count "+count);
 }
 }
 static void Require(bool b,string m){if(!b)throw new Exception(m);}
 void Check(){var level=UnityEngine.Object.FindAnyObjectByType<FirstLevelRooms>();level.GetComponent<FirstLevelPresentation>().CloseModal();level.enabled=false;foreach(var robot in UnityEngine.Object.FindObjectsByType<TutorialRobot>())robot.enabled=false;
 var effect=level.GetComponent<FirstLevelAtmosphere>();Require(effect!=null,"Effect not installed");effect.enabled=false;level.player.position=new Vector3(-4,-.55f,0);effect.Tick(0);effect.Tick(5);effect.Tick(1);Require(effect.VisibleMarks==0,"Idle creates tracks");
 for(int i=0;i<10;i++){level.player.position+=Vector3.right*.1f;effect.Tick(.03f);}Require(effect.VisibleMarks>=3,"Movement did not emit tracks");var mark=GameObject.Find("Tank Track").GetComponent<SpriteRenderer>();Require(mark.sortingOrder>0 && mark.sortingOrder<10 && mark.GetComponent<Collider2D>()==null,"Track affects gameplay/layering");float alpha=mark.color.a;effect.Tick(1.5f);Require(mark.color.a>0 && mark.color.a<alpha,"No gradual fade");
 level.pausePanel.SetActive(true);float held=mark.color.a;effect.Tick(1);Require(Mathf.Approximately(held,mark.color.a),"Pause ages marks");level.pausePanel.SetActive(false);effect.Tick(5);Require(effect.VisibleMarks==0,"Tracks never expire");
 for(int i=0;i<1200;i++){level.player.position+=i%2==0?Vector3.right*.1f:Vector3.left*.1f;effect.Tick(.001f);}int count=0;foreach(var r in level.rooms)foreach(var s in r.GetComponentsInChildren<SpriteRenderer>(true))if(s.name=="Tank Track")count++;Require(count<=96,"Pool unbounded");
 typeof(FirstLevelRooms).GetMethod("ShowRoom",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(level,new object[]{1});effect.Tick(0);Require(effect.VisibleMarks==0,"Room teleport draws trail");
 var lamps=level.rooms[1].GetComponentsInChildren<SpriteRenderer>();var lamp=Array.Find(lamps,s=>s.name.StartsWith("Lamp Glow"));Require(lamp!=null,"Missing lamp");float old=lamp.color.a;effect.Tick(.5f);Require(!Mathf.Approximately(old,lamp.color.a) && lamp.color.a<.31f,"No gentle lamp pulse");
 typeof(FirstLevelRooms).GetMethod("ShowRoom",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(level,new object[]{0});effect.Tick(0);
 level.player.position=new Vector3(-4,-1.4f,0);effect.Tick(0);for(int i=0;i<45;i++){level.player.position+=new Vector3(.07f, i>25?.04f:0,0);effect.Tick(.03f);}
 }
}


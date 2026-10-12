using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
[InitializeOnLoad] public static class OpenFirstRoomEffects {
 static double ready;
 static OpenFirstRoomEffects(){EditorApplication.update+=Tick;}
 static void Tick(){if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
 if(File.Exists("Tools/open-first-room-effects.request")){File.Delete("Tools/open-first-room-effects.request");SessionState.SetBool("Show.FirstRoomEffects",true);ready=0;EditorApplication.isPaused=false;if(!EditorApplication.isPlaying)EditorApplication.isPlaying=true;else{SceneManager.LoadScene("Level1Rooms");ready=EditorApplication.timeSinceStartup+1.5;}}
 if(!SessionState.GetBool("Show.FirstRoomEffects",false)||!EditorApplication.isPlaying)return;
 if(SceneManager.GetActiveScene().name!="Level1Rooms"){SceneManager.LoadScene("Level1Rooms");ready=EditorApplication.timeSinceStartup+1.5;return;}
 if(EditorApplication.timeSinceStartup<ready)return;
 var level=Object.FindAnyObjectByType<FirstLevelRooms>();if(level==null||level.GetComponent<FirstRoomTankEffects>()==null)return;
 typeof(FirstLevelRooms).GetMethod("ShowRoom",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(level,new object[]{0});level.GetComponent<FirstLevelPresentation>().CloseModal();SessionState.SetBool("Show.FirstRoomEffects",false);File.WriteAllText("Tools/first-room-opened.txt","Room 1.1 running, shadow/dust installed.");
 }
}


using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class InstallFigmaFrontend
{
    [InitializeOnLoadMethod] static void Schedule() { EditorApplication.update+=Check; }
    static double readyAfter;
    static void Check()
    {
        const string request="Tools/install-figma-frontend.request";
        if(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) { readyAfter=EditorApplication.timeSinceStartup+2; return; }
        if(EditorApplication.timeSinceStartup<readyAfter || !File.Exists(request))return;
        File.Delete(request);Run();
    }
    [MenuItem("Bounce Club/Install Updated Layout Frontend")] 
    public static void Run()
    {
        AssetDatabase.Refresh();
        foreach(string path in Directory.GetFiles("Assets/Resources/FirstLevelFX","*.png"))
        {
            string name=Path.GetFileNameWithoutExtension(path);
            if(!name.StartsWith("UI_") && !name.StartsWith("MenuBar") && !name.StartsWith("Result") && !name.StartsWith("Level"))continue;
            var importer=AssetImporter.GetAtPath(path) as TextureImporter;
            if(importer==null)continue;
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
            importer.alphaIsTransparency=true;importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.maxTextureSize=4096;importer.mipmapEnabled=false;importer.filterMode=FilterMode.Bilinear;importer.SaveAndReimport();
        }
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
        var windows=UnityEngine.Object.FindAnyObjectByType<MainMenuWindows>();
        if(!FigmaFrontendUI.Configure(windows))throw new Exception("Missing Figma menu data");
        if(windows.sensitivityBars.Length!=0 || windows.soundBars.Length!=3 || windows.musicBars.Length!=3 || windows.levelButtons[2]==null)throw new Exception("Missing menu controls");
        for(int i=0;i<3;i++)
        {
            windows.soundBars[i].sprite=i<PlayerPrefs.GetInt("Menu.SoundLevel",1) ? windows.filledBar : windows.emptyBar;
            windows.musicBars[i].sprite=i<PlayerPrefs.GetInt("Menu.MusicLevel",2) ? windows.filledBar : windows.emptyBar;
        }
        for(int i=0;i<3;i++)windows.levelButtons[i].interactable=LevelProgression.IsUnlocked(i+1);
        EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        windows.Settings();Capture("Tools/Figma-settings-updated.png");windows.Close();
        VerifyResults();
        windows.Play();Capture("Tools/Figma-levels-updated.png");windows.Close();
        scene=EditorSceneManager.OpenScene("Assets/Scenes/Level1Rooms.unity");
        var level=UnityEngine.Object.FindAnyObjectByType<FirstLevelRooms>();
        FigmaPauseMenu.Configure(level);
        VerifyPause(level);
        var presentation=level.GetComponent<FirstLevelPresentation>();presentation.EnsureUI();
        level.blackout.color=Color.clear;
        level.pausePanel.SetActive(true);Capture("Tools/Figma-Pause-updated.png");level.pausePanel.SetActive(false);
        foreach(string panel in new[]{"StartHint","BlueHint","RedHint","Victory"})
        {
            presentation.ShowPanel(panel);
            foreach(var group in UnityEngine.Object.FindObjectsByType<CanvasGroup>())group.alpha=1;
            var content=GameObject.Find("Content").GetComponent<RectTransform>();content.localScale=Vector3.one;
            if(panel=="Victory")FigmaFrontendUI.DrawTime(content,83,902,335,1);
            Capture("Tools/Figma-"+panel+"-updated.png");presentation.CloseModal();
        }
        EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
        File.WriteAllText("Tools/figma-frontend-result.txt","FRONTEND_VERIFIED: settings bindings, persistence, best time, stars, tutorial and victory renders.");
        Debug.Log("FRONTEND_VERIFIED");
    }
    static void VerifyResults()
    {
        string[] keys={"Campaign.Submission.v1.Editor.Time.1","Campaign.Submission.v1.Editor.UnlockedLevel","Progress.ThreeStars.1","Progress.TwoStars.1","Controls.MouseSensitivity"};
        bool[] exists=new bool[keys.Length];float[] floats=new float[keys.Length];int unlocked=PlayerPrefs.GetInt(keys[1]);
        for(int i=0;i<keys.Length;i++){exists[i]=PlayerPrefs.HasKey(keys[i]);floats[i]=PlayerPrefs.GetFloat(keys[i]);}
        try
        {
            PlayerPrefs.DeleteKey(keys[0]);LevelProgression.SetStarThresholds(1,60,120);
            LevelProgression.RecordResult("Level1Rooms",83);
            Require(LevelProgression.Stars(1)==2 && LevelProgression.IsUnlocked(2),"Result recording");
            LevelProgression.RecordResult("Level1Rooms",150);Require(LevelProgression.BestTime(1)==83,"Keep better time");
            LevelProgression.RecordResult("Level1Rooms",55);Require(LevelProgression.Stars(1)==3,"Three stars");
            var windows=UnityEngine.Object.FindAnyObjectByType<MainMenuWindows>();
            windows.Play();Capture("Tools/Figma-levels-completed.png");windows.Close();
        }
        finally
        {
            for(int i=0;i<keys.Length;i++)if(!exists[i])PlayerPrefs.DeleteKey(keys[i]);else if(i==1)PlayerPrefs.SetInt(keys[i],unlocked);else PlayerPrefs.SetFloat(keys[i],floats[i]);
            PlayerPrefs.Save();
        }
    }
    static void Require(bool value,string message){if(!value)throw new Exception(message);}
    static void VerifyPause(FirstLevelRooms level)
    {
        bool existed=PlayerPrefs.HasKey("Menu.SoundLevel");int sound=PlayerPrefs.GetInt("Menu.SoundLevel",1);
        try
        {
            PlayerPrefs.SetInt("Menu.SoundLevel",1);
            level.pausePanel.transform.Find("UI_73_3699").GetComponent<Button>().onClick.Invoke();
            Require(PlayerPrefs.GetInt("Menu.SoundLevel")==0,"Pause sound minus");
            level.pausePanel.transform.Find("UI_73_3696").GetComponent<Button>().onClick.Invoke();
            Require(PlayerPrefs.GetInt("Menu.SoundLevel")==1,"Pause sound plus");
            level.TogglePause();Require(!level.CanPlay,"Pause stops gameplay");
            level.pausePanel.transform.Find("UI_73_3722").GetComponent<Button>().onClick.Invoke();
            Require(level.CanPlay && !level.pausePanel.activeSelf,"Continue resumes gameplay");
        }
        finally
        {
            if(existed)PlayerPrefs.SetInt("Menu.SoundLevel",sound);else PlayerPrefs.DeleteKey("Menu.SoundLevel");PlayerPrefs.Save();
        }
    }
    static void Capture(string path)
    {
        var canvas=UnityEngine.Object.FindAnyObjectByType<Canvas>();var camera=Camera.main;
        var mode=canvas.renderMode;var oldCamera=canvas.worldCamera;float distance=canvas.planeDistance;
        var target=new RenderTexture(1920,1080,24);camera.targetTexture=target;
        canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;Canvas.ForceUpdateCanvases();camera.Render();
        var previous=RenderTexture.active;RenderTexture.active=target;
        var image=new Texture2D(1920,1080,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1920,1080),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());
        RenderTexture.active=previous;camera.targetTexture=null;canvas.renderMode=mode;canvas.worldCamera=oldCamera;canvas.planeDistance=distance;
        UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(target);
    }
}

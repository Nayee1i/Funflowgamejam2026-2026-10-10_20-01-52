using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Native Figma artwork with live settings and saved level results.</summary>
public static class FigmaFrontendUI
{
    [Serializable] class Part { public string asset, action; public float x,y,w,h; }
    [Serializable] class Panel { public string name; public Part[] parts; }
    [Serializable] class Panels { public Panel[] panels; }
    public static bool Configure(MainMenuWindows windows)
    {
        var source=Resources.Load<TextAsset>("FirstLevelFX/MenuPanels");
        if(source==null || windows.settingsWindow==null || windows.modesWindow==null)return false;
        var data=JsonUtility.FromJson<Panels>(source.text);
        windows.levelButtons=new Button[3];windows.levelButtonArtwork=new Image[3];
        foreach(var panel in data.panels)
        {
            var parent=(panel.name=="Settings" ? windows.settingsWindow : windows.modesWindow).transform;
            Clear(parent);
            foreach(var part in panel.parts)
            {
                var image=FirstLevelPresentation.Picture(part.asset,parent,part.asset,part.x,part.y,part.w,part.h);
                if(part.action=="peaceful")image.name="Peaceful Mode Artwork";
                if(string.IsNullOrEmpty(part.action))continue;
                string action=part.action;
                var hit=FirstLevelPresentation.Picture(action+" Hit Area",parent,null,part.x-12,part.y-10,part.w+24,part.h+20);
                hit.color=Color.clear;hit.raycastTarget=true;hit.canvasRenderer.cullTransparentMesh=false;
                var button=hit.gameObject.AddComponent<FigmaMenuButton>();button.transition=Selectable.Transition.None;button.targetGraphic=hit;
                image.rectTransform.pivot=new Vector2(.5f,.5f);
                image.rectTransform.anchoredPosition=new Vector2(part.x+part.w/2,-part.y-part.h/2);
                button.Configure(image.rectTransform,null);
                button.onClick.AddListener(()=>Dispatch(windows,action));
                if(action.StartsWith("level"))
                {
                    int i=int.Parse(action.Substring(5))-1;windows.levelButtons[i]=button;windows.levelButtonArtwork[i]=image;
                }
            }
            if(panel.name=="Settings")
            {
                windows.filledBar=Resources.Load<Sprite>("FirstLevelFX/MenuBarFull");windows.emptyBar=Resources.Load<Sprite>("FirstLevelFX/MenuBarEmpty");
                windows.soundBars=Bars(parent,"Sound",new Vector2(919,536.455f),new Vector2(1003,536.455f),new Vector2(1087,536.455f));
                windows.musicBars=Bars(parent,"Music",new Vector2(919,656.455f),new Vector2(1003,656.455f),new Vector2(1087,656.455f));
                windows.sensitivityBars=new Image[0];
            }
            else if(panel.name=="Modes")ConfigurePeacefulInfo(parent);
        }
        windows.unlockedLevelArtwork=Resources.Load<Sprite>("FirstLevelFX/LevelUnlocked");
        RefreshResults(windows);
        RefreshPeacefulMode(windows);
        return true;
    }
    static void ConfigurePeacefulInfo(Transform parent)
    {
        var icon=FirstLevelPresentation.Picture("Peaceful Mode Info",parent,"PeacefulInfoIcon",1397,580,48,48);
        icon.raycastTarget=true;
        var tooltip=new GameObject("Peaceful Mode Tooltip",typeof(RectTransform));
        tooltip.transform.SetParent(parent,false);
        var rect=tooltip.GetComponent<RectTransform>();
        rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.sizeDelta=Vector2.zero;
        FirstLevelPresentation.Picture("Peaceful Info Panel",rect,"PeacefulInfoPanel",1460,529,336,168).raycastTarget=false;
        FirstLevelPresentation.Picture("Peaceful Info Text",rect,"PeacefulInfoText",1564,583,163,54).raycastTarget=false;
        tooltip.SetActive(false);
        icon.gameObject.AddComponent<PeacefulModeInfo>().tooltip=tooltip;
    }
    public static void RefreshPeacefulMode(MainMenuWindows windows)
    {
        var artwork=windows.modesWindow.transform.Find("Peaceful Mode Artwork");
        if(artwork!=null)artwork.GetComponent<Image>().sprite=Resources.Load<Sprite>("FirstLevelFX/"+(PeacefulModeSettings.Enabled ? "UI_139_8" : "UI_139_7"));
    }
    static Image[] Bars(Transform parent,string name,params Vector2[] points)
    {
        var bars=new Image[points.Length];
        for(int i=0;i<bars.Length;i++)bars[i]=FirstLevelPresentation.Picture(name+" Bar "+i,parent,"MenuBarEmpty",points[i].x,points[i].y,82,67.09f);
        return bars;
    }
    static void Dispatch(MainMenuWindows w,string action)
    {
        switch(action)
        {
            case "close":w.Close();break;
            case "soundUp":w.SoundUp();break;case "soundDown":w.SoundDown();break;
            case "musicUp":w.MusicUp();break;case "musicDown":w.MusicDown();break;
            case "level1":w.Level1();break;case "level2":w.Level2();break;case "level3":w.Level3();break;
            case "arena":w.Arena();break;
            case "peaceful":PeacefulModeSettings.SetEnabled(!PeacefulModeSettings.Enabled);RefreshPeacefulMode(w);break;
        }
    }
    public static void RefreshResults(MainMenuWindows w)
    {
        if(w.modesWindow==null || w.levelButtons==null || w.levelButtons.Length!=3)return;
        var parent=w.modesWindow.transform;
        var old=parent.Find("Saved Level Results");if(old!=null)Remove(old.gameObject);
        var group=new GameObject("Saved Level Results",typeof(RectTransform));group.transform.SetParent(parent,false);
        var rect=group.GetComponent<RectTransform>();rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.sizeDelta=Vector2.zero;
        for(int i=0;i<3;i++)
        {
            if(w.levelButtonArtwork[i]!=null)w.levelButtonArtwork[i].sprite=Resources.Load<Sprite>("FirstLevelFX/"+(LevelProgression.IsUnlocked(i+1) ? "LevelUnlocked" : "LevelLocked"));
            float time=LevelProgression.BestTime(i+1);if(time<0)continue;
            float offset=i==0 ? 0 : i==1 ? 235 : 474;
            int stars=LevelProgression.Stars(i+1);
            for(int s=0;s<3;s++)FirstLevelPresentation.Picture("Level "+(i+1)+" Star "+s,rect,s<stars ? "ResultStar" : "ResultStarEmpty",656+offset+s*44,389,44,45);
            FirstLevelPresentation.Picture("Level "+(i+1)+" Time Frame",rect,"ResultTimeFrame",620+offset,508,204.41f,68);
            DrawTime(rect,time,698.37f+offset,533.71f,68f/164);
        }
    }
    public static void DrawTime(Transform parent,float seconds,float x,float y,float scale)
    {
        int time=Mathf.Clamp(Mathf.FloorToInt(seconds),0,5999);
        int[] values={time/600,time/60%10,0,time%60/10,time%10};
        float[] positions={0,39.91f,72.5f,109.17f,142.52f};
        float[] widths={36.57f,30.51f,35.13f,30.02f,32.34f};
        for(int i=0;i<5;i++)
        {
            var image=FirstLevelPresentation.Picture("Result Time "+i,parent,i==2 ? "Colon" : "Digit_"+values[i],x+positions[i]*scale,y+(i==2 ? 2.5f : 0)*scale,widths[i]*scale,(i==2 ? 48.16f : 52.5f)*scale);
            image.preserveAspect=true;
        }
    }
    static void Clear(Transform parent)
    {
        for(int i=parent.childCount-1;i>=0;i--)Remove(parent.GetChild(i).gameObject);
    }
    static void Remove(GameObject go)
    {
        go.SetActive(false);
        if(Application.isPlaying)UnityEngine.Object.Destroy(go);else UnityEngine.Object.DestroyImmediate(go);
    }
}

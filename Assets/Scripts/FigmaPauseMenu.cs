using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class FigmaPauseMenu : MonoBehaviour
{
    [Serializable] class Part { public string asset,action; public float x,y,w,h; }
    [Serializable] class Panel { public Part[] parts; }
    [Serializable] class Panels { public Panel[] panels; }
    FirstLevelRooms level;
    Image[] sound,music;
    public static void Configure(FirstLevelRooms level)
    {
        if(level.pausePanel==null)return;
        if(level.pausePanel.GetComponent<FigmaPauseMenu>()!=null)return;
        var source=Resources.Load<TextAsset>("FirstLevelFX/PausePanel");if(source==null)return;
        var parent=level.pausePanel.transform;
        for(int i=parent.childCount-1;i>=0;i--)
        {
            var child=parent.GetChild(i).gameObject;child.SetActive(false);
            if(Application.isPlaying)Destroy(child);else DestroyImmediate(child);
        }
        var controller=level.pausePanel.AddComponent<FigmaPauseMenu>();controller.level=level;
        foreach(var part in JsonUtility.FromJson<Panels>(source.text).panels[0].parts)
        {
            var image=FirstLevelPresentation.Picture(part.asset,parent,part.asset,part.x,part.y,part.w,part.h);
            if(string.IsNullOrEmpty(part.action))continue;
            string action=part.action;
            image.raycastTarget=true;var button=image.gameObject.AddComponent<Button>();button.transition=Selectable.Transition.None;
            image.rectTransform.pivot=new Vector2(.5f,.5f);image.rectTransform.anchoredPosition=new Vector2(part.x+part.w/2,-part.y-part.h/2);
            image.gameObject.AddComponent<FirstLevelUiHover>();
            button.onClick.AddListener(()=>controller.Dispatch(action));
        }
        controller.sound=Bars(parent,"Sound",new Vector2(904,407),new Vector2(985,411),new Vector2(1072,411));
        controller.music=Bars(parent,"Music",new Vector2(905,510),new Vector2(991,510),new Vector2(1074,514));
        controller.Refresh();
    }
    static Image[] Bars(Transform parent,string name,params Vector2[] points)
    {
        var result=new Image[points.Length];for(int i=0;i<result.Length;i++)result[i]=FirstLevelPresentation.Picture(name+" Bar "+i,parent,"MenuBarEmpty",points[i].x,points[i].y,82,67.09f);
        return result;
    }
    void OnEnable(){Refresh();}
    void Refresh()
    {
        if(sound==null || music==null)return;
        var full=Resources.Load<Sprite>("FirstLevelFX/MenuBarFull");var empty=Resources.Load<Sprite>("FirstLevelFX/MenuBarEmpty");
        int s=Mathf.Clamp(PlayerPrefs.GetInt("Menu.SoundLevel",1),0,3),m=Mathf.Clamp(PlayerPrefs.GetInt("Menu.MusicLevel",2),0,3);
        for(int i=0;i<3;i++){sound[i].sprite=i<s ? full : empty;music[i].sprite=i<m ? full : empty;}
        AudioListener.volume=s/3f;
    }
    void Dispatch(string action)
    {
        switch(action)
        {
            case "close":case "continue":level.TogglePause();break;
            case "restart":SceneManager.LoadScene(SceneManager.GetActiveScene().path);break;
            case "menu":level.OpenMainMenu();break;
            default:
                string key=action.StartsWith("sound") ? "Menu.SoundLevel" : "Menu.MusicLevel";
                int value=PlayerPrefs.GetInt(key,key=="Menu.SoundLevel" ? 1 : 2);
                PlayerPrefs.SetInt(key,Mathf.Clamp(value+(action.EndsWith("Up") ? 1 : -1),0,3));PlayerPrefs.Save();Refresh();break;
        }
    }
}

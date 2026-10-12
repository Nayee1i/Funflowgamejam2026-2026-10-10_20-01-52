using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class FirstLevelPresentation : MonoBehaviour
{
    [Serializable] class Part { public string asset,action; public float x,y,w,h; public bool close; }
    [Serializable] class Panel { public string name; public Part[] parts; }
    [Serializable] class Panels { public Panel[] panels; }
    FirstLevelRooms level;
    FirstLevelTankAim gun;
    RectTransform root;
    GameObject ammoPanel,modal;
    Image[] ammo=new Image[4],clock=new Image[5];
    Image reloadTrack,reloadFill;
    Sprite full,empty;
    Sprite[] digits=new Sprite[10];
    bool[] hintsSeen=new bool[3];
    public string panelResource="FirstLevelFX/Panels";
    public string[] roomHints={"StartHint","BlueHint","RedHint"};
    public Vector2 victoryTimePosition=new Vector2(902,335);
    float elapsed;
    public float ElapsedSeconds => elapsed;
    int previousSecond=-1;
    CanvasGroup modalGroup;
    RectTransform modalContent;
    float modalAge;
    public void EnsureUI()
    {
        if(root!=null)return;
        level=GetComponent<FirstLevelRooms>();gun=level.player.GetComponent<FirstLevelTankAim>();
        hintsSeen=new bool[Mathf.Max(1,roomHints.Length)];
        var canvas=GameObject.Find("Level HUD").GetComponent<Canvas>();
        canvas.sortingOrder=100;
        root=canvas.GetComponentInChildren<AspectRatioFitter>().GetComponent<RectTransform>();
        full=Resources.Load<Sprite>("FirstLevelFX/AmmoFull");empty=Resources.Load<Sprite>("FirstLevelFX/AmmoEmpty");
        ammoPanel=new GameObject("Figma Magazine",typeof(RectTransform));ammoPanel.transform.SetParent(root,false);
        var ammoRect=ammoPanel.GetComponent<RectTransform>();ammoRect.anchorMin=Vector2.zero;ammoRect.anchorMax=Vector2.one;ammoRect.sizeDelta=Vector2.zero;
        var frame=Picture("Magazine Frame",ammoPanel.transform,"AmmoFrame",1666,958,221,83);
        frame.preserveAspect=false;
        for(int i=0;i<4;i++)
        {
            ammo[i]=Picture("Ammo "+(i+1),ammoPanel.transform,"AmmoFull",1666+(334+i*299)*221f/1540-19.35f,958+(355-50)*83f/600-29.75f,38.7f,59.5f);
        }
        reloadTrack=Picture("Reload Track",ammoPanel.transform,null,1678,1048,197,12);reloadTrack.color=new Color(.16f,.09f,.04f,.95f);
        reloadFill=Picture("Reload Progress",ammoPanel.transform,null,1680,1050,193,8);reloadFill.color=new Color(1,.72f,.05f);
        var placeholder=GameObject.Find("Timer — Figma Placeholder");
        if(placeholder==null)foreach(var image in root.GetComponentsInChildren<Image>())if(image.name.StartsWith("Timer")){placeholder=image.gameObject;break;}
        if(placeholder!=null)placeholder.SetActive(false);
        for(int i=0;i<10;i++)digits[i]=Resources.Load<Sprite>("FirstLevelFX/Digit_"+i);
        for(int i=0;i<5;i++)clock[i]=Picture("Clock "+i,root,i==2 ? "Colon" : "Digit_0",823+(i<2 ? i*58 : i==2 ? 116 : 161+(i-3)*58),43.5f,i==2 ? 42 : 55,i==2 ? 58 : 104);
        clock[2].rectTransform.anchoredPosition+=Vector2.down*23;
        RefreshHud();
    }
    public static Image Picture(string name,Transform parent,string asset,float x,float y,float w,float h)
    {
        var image=new GameObject(name,typeof(RectTransform),typeof(Image)).GetComponent<Image>();
        image.transform.SetParent(parent,false);
        var r=image.rectTransform;r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);
        r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);
        if(asset!=null)image.sprite=Resources.Load<Sprite>(asset.Contains("/") ? asset : "FirstLevelFX/"+asset);
        image.raycastTarget=false;return image;
    }
    void Update()
    {
        if(root==null)return;
        if(level.CanPlay)elapsed+=Time.deltaTime;
        RefreshHud();
        if(modal!=null)
        {
            modalAge+=Time.unscaledDeltaTime;
            float t=Mathf.SmoothStep(0,1,Mathf.Clamp01(modalAge/.5f));
            modalGroup.alpha=t;modalContent.localScale=Vector3.one*Mathf.Lerp(.88f,1,t);
            if(!level.Finished && Keyboard.current!=null && Keyboard.current.escapeKey.wasPressedThisFrame)CloseModal();
        }
    }
    public void RefreshHud()
    {
        if(root==null)return;
        for(int i=0;i<4;i++)ammo[i].sprite=i<gun.BulletsRemaining ? full : empty;
        reloadTrack.gameObject.SetActive(gun.IsReloading);reloadFill.gameObject.SetActive(gun.IsReloading);
        reloadFill.rectTransform.sizeDelta=new Vector2(193*gun.ReloadProgress,8);
        int seconds=Mathf.Min(5999,Mathf.FloorToInt(elapsed));
        if(seconds==previousSecond)return;previousSecond=seconds;
        int[] values={seconds/60/10,seconds/60%10,0,seconds%60/10,seconds%10};
        for(int i=0;i<5;i++)if(i!=2)clock[i].sprite=digits[values[i]];
    }
    public void ShowHint(int room)
    {
        EnsureUI();if(room<0 || room>=hintsSeen.Length || hintsSeen[room])return;
        hintsSeen[room]=true;if(!string.IsNullOrEmpty(roomHints[room]))ShowPanel(roomHints[room]);
    }
    public void ShowVictory()
    {
        EnsureUI();LevelProgression.RecordResult(SceneManager.GetActiveScene().name, elapsed);ShowPanel("Victory");
        FigmaFrontendUI.DrawTime(modalContent, elapsed, victoryTimePosition.x, victoryTimePosition.y, 1);
        FirstLevelEffects.Celebrate(new Vector2(-3,1),level);FirstLevelEffects.Celebrate(new Vector2(3,1),level);
    }
    public void ShowPanel(string name)
    {
        CloseModal();level.SetModal(true);modalAge=0;
        modal=new GameObject("Figma "+name,typeof(RectTransform),typeof(CanvasGroup));modal.transform.SetParent(root,false);
        var rect=modal.GetComponent<RectTransform>();rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.sizeDelta=Vector2.zero;
        modalGroup=modal.GetComponent<CanvasGroup>();modalGroup.alpha=0;
        var backdrop=Picture("Dim",modal.transform,null,0,0,1920,1080);backdrop.color=new Color(0,0,0,name=="NewRobotHint" || name=="DestructibleHint" ? 0 : .54f);backdrop.raycastTarget=true;
        modalContent=new GameObject("Content",typeof(RectTransform)).GetComponent<RectTransform>();modalContent.SetParent(modal.transform,false);
        modalContent.anchorMin=Vector2.zero;modalContent.anchorMax=Vector2.one;modalContent.sizeDelta=Vector2.zero;
        modalContent.localScale=Vector3.one*.88f;
        var data=JsonUtility.FromJson<Panels>(Resources.Load<TextAsset>(panelResource).text.TrimStart('\ufeff'));
        var panel=Array.Find(data.panels,p=>p.name==name);
        foreach(var part in panel.parts)
        {
            var image=Picture(part.asset,modalContent,part.asset,part.x,part.y,part.w,part.h);
            if(!part.close && string.IsNullOrEmpty(part.action))continue;
            image.raycastTarget=true;var button=image.gameObject.AddComponent<Button>();button.transition=Selectable.Transition.None;
            image.rectTransform.pivot=new Vector2(.5f,.5f);image.rectTransform.anchoredPosition=new Vector2(part.x+part.w/2,-part.y-part.h/2);
            if(part.close)button.onClick.AddListener(CloseModal);
            else if(part.action=="restart")button.onClick.AddListener(()=>SceneManager.LoadScene(SceneManager.GetActiveScene().path));
            else if(part.action=="next")button.onClick.AddListener(()=>SceneManager.LoadScene("level"+(LevelProgression.LevelNumber(SceneManager.GetActiveScene().name)+1)));
            else button.onClick.AddListener(level.OpenMainMenu);
            image.gameObject.AddComponent<FirstLevelUiHover>();
        }
    }
    public void CloseModal()
    {
        if(modal!=null){modal.SetActive(false);if(Application.isPlaying)Destroy(modal);else DestroyImmediate(modal);}
        modal=null;if(level!=null)level.SetModal(false);
    }
}

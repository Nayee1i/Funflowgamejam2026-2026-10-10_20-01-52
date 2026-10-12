using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class FirstLevelPresentation : MonoBehaviour
{
    [Serializable] class Part { public string asset,action; public float x,y,w,h; public bool close,flipX; }
    [Serializable] class Panel { public string name; public Part[] parts; }
    [Serializable] class Panels { public Panel[] panels; }
    FirstLevelRooms level;
    FirstLevelTankAim gun;
    RectTransform root;
    GameObject ammoPanel,modal;
    Image[] ammo=new Image[4],clock=new Image[5];
    Image reloadTrack,reloadFill;
    Sprite full,empty;
    static Sprite victoryPlate,victoryGold;
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
    int modalCreatedFrame;
    readonly System.Collections.Generic.List<Image> earnedStars=new System.Collections.Generic.List<Image>();
    bool terminalPanel;
    public static float RevealAlpha(float age,float delay=0) => Mathf.SmoothStep(0,1,Mathf.Clamp01((age-delay)/.7f));
    public static float StarScale(float age,float delay)
    {
        float t=Mathf.Clamp01((age-delay)/.7f);
        return t<.65f ? Mathf.Lerp(.12f,1.18f,Mathf.SmoothStep(0,1,t/.65f)) : Mathf.Lerp(1.18f,1,Mathf.SmoothStep(0,1,(t-.65f)/.35f));
    }
    public void EnsureUI()
    {
        if(root!=null)return;
        level=GetComponent<FirstLevelRooms>();gun=level.player.GetComponent<FirstLevelTankAim>();
        hintsSeen=new bool[Mathf.Max(1,roomHints.Length)];
        var canvas=GameObject.Find("Level HUD").GetComponent<Canvas>();
        canvas.sortingOrder=100;
        root=canvas.GetComponentInChildren<AspectRatioFitter>().GetComponent<RectTransform>();
        full=Resources.Load<Sprite>("FirstLevelFX/AmmoFull");empty=Resources.Load<Sprite>("FirstLevelFX/AmmoEmpty");
        ammoPanel=new GameObject("Magazine",typeof(RectTransform));ammoPanel.transform.SetParent(root,false);
        var ammoRect=ammoPanel.GetComponent<RectTransform>();ammoRect.anchorMin=Vector2.zero;ammoRect.anchorMax=Vector2.one;ammoRect.sizeDelta=Vector2.zero;
        var frame=Picture("Magazine Frame",ammoPanel.transform,"AmmoFrame",1666,958,221,83);
        frame.preserveAspect=false;
        for(int i=0;i<4;i++)
        {
            ammo[i]=Picture("Ammo "+(i+1),ammoPanel.transform,"AmmoFull",1666+(334+i*299)*221f/1540-19.35f,958+(355-50)*83f/600-29.75f,38.7f,59.5f);
        }
        reloadTrack=Picture("Reload Track",ammoPanel.transform,null,1678,1048,197,12);reloadTrack.color=new Color(.16f,.09f,.04f,.95f);
        reloadFill=Picture("Reload Progress",ammoPanel.transform,null,1680,1050,193,8);reloadFill.color=new Color(1,.72f,.05f);
        var placeholder=GameObject.Find("Timer Placeholder");
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
            // Resource loading on the opening frame must never skip the reveal.
            if(Time.frameCount>modalCreatedFrame)modalAge+=Mathf.Min(Time.unscaledDeltaTime,1f/30f);
            float t=RevealAlpha(modalAge);
            modalGroup.alpha=t;modalContent.localScale=Vector3.one*Mathf.Lerp(.88f,1,t);
            modalGroup.interactable=modalAge>=.7f;
            for(int i=0;i<earnedStars.Count;i++)
            {
                float delay=.85f+i*.7f;
                earnedStars[i].color=new Color(1,1,1,Mathf.SmoothStep(0,1,Mathf.Clamp01((modalAge-delay)/.22f)));
                earnedStars[i].rectTransform.localScale=Vector3.one*StarScale(modalAge,delay);
            }
            if(!terminalPanel && !level.Finished && !level.Defeated && Keyboard.current!=null && Keyboard.current.escapeKey.wasPressedThisFrame)CloseModal();
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
        GameAudio.Play("victory");
        EnsureUI();LevelProgression.RecordResult(SceneManager.GetActiveScene().name, elapsed);ShowPanel("Victory");
        if(modalContent.Find("UI_115_4")==null)
            Picture("Victory Time Frame",modalContent,"UI_115_4",victoryTimePosition.x-189,victoryTimePosition.y-62,493,164);
        FigmaFrontendUI.DrawTime(modalContent, elapsed, 902,335, 1);
        FirstLevelEffects.Celebrate(new Vector2(-3,1),level);FirstLevelEffects.Celebrate(new Vector2(3,1),level);
    }
    public void ShowDefeat()
    {
        EnsureUI();level.SetModal(true);
        level.GetComponent<FirstLevelHealthHud>()?.Refresh();
        StartCoroutine(DeathSequence());
    }
    public void ShowArenaResult()
    {
        EnsureUI();ShowPanel("ArenaResult");
        var arena=level.GetComponent<EndlessArena>();
        var label=new GameObject("Completed Waves Label",typeof(RectTransform),typeof(Text)).GetComponent<Text>();
        label.transform.SetParent(modalContent,false);
        var r=label.rectTransform;r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);
        r.anchoredPosition=new Vector2(700,-665);r.sizeDelta=new Vector2(520,65);
        label.font=Resources.Load<Font>("Endless/B52") ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");label.fontSize=38;
        label.color=new Color(.32f,.15f,.04f);label.alignment=TextAnchor.MiddleCenter;label.raycastTarget=false;
        label.text="ПРОЙДЕНО ВОЛН: "+arena.CompletedWaves;
    }
    IEnumerator DeathSequence()
    {
        var flame=new GameObject("Hero Death Fire").AddComponent<SpriteRenderer>();
        flame.transform.position=level.player.position;flame.sortingOrder=40;
        var frames=new Sprite[6];
        for(int i=0;i<6;i++)frames[i]=Resources.Load<Sprite>("FirstLevelFX/Ending_143_"+(106+i));
        flame.transform.localScale=Vector3.one*(level.playerRadius*3.8f/frames[0].bounds.size.x);
        float age=0;bool revealed=false;
        while(flame!=null)
        {
            age+=Time.unscaledDeltaTime;flame.sprite=frames[Mathf.FloorToInt(age*10)%6];
            flame.color=new Color(1,1,1,RevealAlpha(age)*Mathf.Clamp01(2.6f-age));
            if(age>=.45f)foreach(var renderer in level.player.GetComponentsInChildren<SpriteRenderer>())renderer.color=new Color(.25f,.2f,.18f,1);
            if(age>=.9f && !revealed){if(level.GetComponent<EndlessArena>()!=null)ShowArenaResult();else ShowPanel("Defeat");revealed=true;}
            if(age>=2.6f){Destroy(flame.gameObject);yield break;}
            yield return null;
        }
    }
    public void ShowPanel(string name)
    {
        CloseModal();level.SetModal(true);modalAge=0;modalCreatedFrame=Time.frameCount;
        terminalPanel=name=="Victory" || name=="Defeat" || name=="ArenaResult";
        modal=new GameObject(""+name,typeof(RectTransform),typeof(CanvasGroup));modal.transform.SetParent(root,false);
        var rect=modal.GetComponent<RectTransform>();rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.sizeDelta=Vector2.zero;
        modalGroup=modal.GetComponent<CanvasGroup>();modalGroup.alpha=0;
        modalGroup.interactable=false;
        var backdrop=Picture("Dim",modal.transform,null,0,0,1920,1080);backdrop.color=new Color(0,0,0,name=="NewRobotHint" || name=="DestructibleHint" ? 0 : .54f);backdrop.raycastTarget=true;
        modalContent=new GameObject("Content",typeof(RectTransform)).GetComponent<RectTransform>();modalContent.SetParent(modal.transform,false);
        modalContent.anchorMin=Vector2.zero;modalContent.anchorMax=Vector2.one;modalContent.sizeDelta=Vector2.zero;
        modalContent.localScale=Vector3.one*.88f;
        var data=JsonUtility.FromJson<Panels>(Resources.Load<TextAsset>(panelResource).text.TrimStart('\ufeff'));
        var panel=Array.Find(data.panels,p=>p.name==name);
        if(name=="Victory")
        {
            string advance=LevelProgression.LevelNumber(SceneManager.GetActiveScene().name)>=3 ? "restart" : "next";
            panel=new Panel{name=name,parts=new[]{
                new Part{asset="UI_56_3110",x=734,y=400,w=452,h=569},
                new Part{asset="Victory Stars",x=637,y=48,w=646,h=258},
                new Part{asset="UI_115_4",x=714,y=273,w=493,h=164},
                new Part{asset="UI_56_3112",x=819,y=652,w=117,h=134,action="menu"},
                new Part{asset="UI_56_3113",x=960,y=647,w=165,h=143,action=advance}}};
        }
        if(name=="Defeat")panel=new Panel{name=name,parts=new[]{
            new Part{asset="Ending_141_101",x=536,y=286,w=848,h=508},
            new Part{asset="Ending_141_102",x=764,y=520,w=138,h=138,action="restart"},
            new Part{asset="Ending_141_103",x=1026,y=514,w=131,h=150,action="menu"}}};
        foreach(var part in panel.parts)
        {
            if(name=="Victory" && part.asset=="Victory Stars"){CreateVictoryStars(part);continue;}
            var image=Picture(part.asset,modalContent,part.asset,part.x,part.y,part.w,part.h);
            if(name=="NewRobotHint" && part.asset=="SecondLevel/Art_71_3600")
            {
                // Keep the hint editable: its exported bitmap still says two hits.
                image.enabled=false;
                var label=new GameObject("Red robot health hint",typeof(RectTransform),typeof(Text)).GetComponent<Text>();
                label.transform.SetParent(image.transform,false);
                var hintRect=label.rectTransform;hintRect.anchorMin=Vector2.zero;hintRect.anchorMax=Vector2.one;hintRect.offsetMin=hintRect.offsetMax=Vector2.zero;
                label.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");label.fontSize=20;
                label.resizeTextForBestFit=true;label.resizeTextMinSize=16;label.resizeTextMaxSize=20;
                label.color=Color.black;label.alignment=TextAnchor.LowerLeft;label.raycastTarget=false;
                label.text="БЕРЕГИСЬ ЭТИХ РОБОТОВ. ЧТОБЫ ИХ ПОБЕДИТЬ — ПОРАЗИ ЕГО 4 РАЗА!";
            }
            if(part.flipX)
            {
                image.rectTransform.pivot=new Vector2(.5f,.5f);
                image.rectTransform.anchoredPosition=new Vector2(part.x+part.w/2,-part.y-part.h/2);
                image.rectTransform.localScale=new Vector3(-1,1,1);
            }
            if(!part.close && string.IsNullOrEmpty(part.action))continue;
            image.raycastTarget=true;var button=image.gameObject.AddComponent<Button>();button.transition=Selectable.Transition.None;
            image.canvasRenderer.SetColor(Color.white);
            image.rectTransform.pivot=new Vector2(.5f,.5f);image.rectTransform.anchoredPosition=new Vector2(part.x+part.w/2,-part.y-part.h/2);
            if(part.close)button.onClick.AddListener(CloseModal);
            else if(part.action=="restart")button.onClick.AddListener(()=>SceneManager.LoadScene(SceneManager.GetActiveScene().path));
            else if(part.action=="next")button.onClick.AddListener(()=>SceneManager.LoadScene("level"+(LevelProgression.LevelNumber(SceneManager.GetActiveScene().name)+1)));
            else button.onClick.AddListener(level.OpenMainMenu);
            image.gameObject.AddComponent<FirstLevelUiHover>();
        }
    }
    void CreateVictoryStars(Part banner)
    {
        var atlas=Resources.Load<Texture2D>("FirstLevelFX/VictoryAtlas");
        var plate=Picture("Empty Star Banner",modalContent,null,banner.x,banner.y,banner.w,banner.h);
        if(victoryPlate==null)victoryPlate=Sprite.Create(atlas,new Rect(0,620,768,306),new Vector2(.5f,.5f),100);
        if(victoryGold==null)victoryGold=Sprite.Create(atlas,new Rect(483,27,267,310),new Vector2(.5f,.5f),100);
        plate.sprite=victoryPlate;
        var gold=victoryGold;
        int count=LevelProgression.StarsForTime(LevelProgression.LevelNumber(SceneManager.GetActiveScene().name),elapsed);
        float[] xs={155,273,390};
        for(int i=0;i<count;i++)
        {
            // Award the two outside stars before the large centre star, as in Figma's two-star plate.
            int slot=i==0 ? 0 : i==1 ? 2 : 1;
            var star=Picture("Earned Star "+(i+1),modalContent,null,banner.x+xs[slot],banner.y+50,115,133);
            star.sprite=gold;
            var r=star.rectTransform;r.pivot=new Vector2(.5f,.5f);r.anchoredPosition+=new Vector2(r.sizeDelta.x*.5f,-r.sizeDelta.y*.5f);
            // Fill the socket's outline while keeping the star centred during its animation.
            r.sizeDelta*=1.08f;
            r.anchoredPosition+=new Vector2(2,-2);
            star.color=new Color(1,1,1,0);r.localScale=Vector3.one*.12f;earnedStars.Add(star);
        }
    }
    public void CloseModal()
    {
        if(modal!=null){modal.SetActive(false);if(Application.isPlaying)Destroy(modal);else DestroyImmediate(modal);}
        modal=null;earnedStars.Clear();if(level!=null)level.SetModal(false);
    }
}

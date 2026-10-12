using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Authored waves reuse the existing player, enemy, damage and ricochet systems.
public class EndlessArena : SecondLevelWorkshop
{
    [Serializable] class Banner { public string asset; public float x,y,w,h; }
    [Serializable] class Banners { public Banner[] waves; }
    public Vector2[] waveSpawns;
    public int CompletedWaves { get; private set; }
    public bool ShowingWaveTitle => title != null;
    FirstLevelRooms level;
    CanvasGroup title;
    RectTransform banner;
    float titleAge;
    int titleFrame;
    bool ending;
    Banners artwork;
    const float TitleDuration=2f;

    public Vector3 SpawnFor(int index) => waveSpawns[index];
    public void ShowWaveTitle(int index)
    {
        level=GetComponent<FirstLevelRooms>();
        if(artwork==null)artwork=JsonUtility.FromJson<Banners>(Resources.Load<TextAsset>("Endless/Waves").text);
        ClearTitle();
        var root=GameObject.Find("Level HUD").GetComponentInChildren<AspectRatioFitter>().transform;
        title=new GameObject("Wave "+(index+1)+" Announcement",typeof(RectTransform),typeof(CanvasGroup)).GetComponent<CanvasGroup>();
        var r=title.GetComponent<RectTransform>();r.SetParent(root,false);r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.sizeDelta=Vector2.zero;
        title.alpha=0;title.blocksRaycasts=true;title.interactable=false;
        var dim=FirstLevelPresentation.Picture("Wave Dim",r,null,0,0,1920,1080);dim.color=new Color(0,0,0,.48f);dim.raycastTarget=true;
        var data=artwork.waves[index];
        var image=FirstLevelPresentation.Picture("Wave "+(index+1),r,data.asset,data.x,data.y,data.w,data.h);
        banner=image.rectTransform;banner.pivot=new Vector2(.5f,.5f);banner.anchoredPosition+=new Vector2(data.w/2,-data.h/2);
        banner.localScale=Vector3.one*.92f;titleAge=0;titleFrame=Time.frameCount;
        // The scene fade is beneath the title; the banner fades independently.
        r.SetAsLastSibling();
    }
    void Update()
    {
        if(title!=null)
        {
            if(Time.frameCount>titleFrame)titleAge+=Mathf.Min(Time.unscaledDeltaTime,1f/30f);
            float enter=Mathf.SmoothStep(0,1,Mathf.Clamp01(titleAge/.5f));
            float leave=1-Mathf.SmoothStep(0,1,Mathf.Clamp01((titleAge-1.4f)/.6f));
            title.alpha=enter*leave;banner.localScale=Vector3.one*Mathf.Lerp(.92f,1,enter);
            if(titleAge>=TitleDuration)ClearTitle();
        }
        if(level==null || !level.CanPlay || ending || !FirstLevelEnemies.Cleared(level.rooms[level.CurrentRoom]) || FirstLevelEffects.HasPendingImpacts(level))return;
        CompletedWaves++;
        FirstLevelShot.ClearAll();
        if(level.CurrentRoom+1<level.rooms.Length)StartCoroutine(level.AdvanceArenaWave(level.CurrentRoom+1));
        else
        {
            ending=true;level.FinishArena();
            GameAudio.Play("victory");
            level.GetComponent<FirstLevelPresentation>().ShowArenaResult();
        }
    }
    void ClearTitle()
    {
        if(title!=null){title.gameObject.SetActive(false);Destroy(title.gameObject);}
        title=null;
    }
}

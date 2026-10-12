using UnityEngine;
using UnityEngine.UI;

public class FirstLevelHealthHud : MonoBehaviour
{
    public TutorialPlayer player;
    readonly Image[] hearts=new Image[3];
    Sprite full,empty;
    int displayed=-1;
    void Start()
    {
        full=Resources.Load<Sprite>("FirstLevelEnemies/HeartFull");
        empty=Resources.Load<Sprite>("FirstLevelEnemies/HeartEmpty");
        var frame=GameObject.Find("Health Frame");
        if(frame==null)return;
        for(int i=0;i<3;i++)
        {
            var heart=new GameObject("Live Heart "+(i+1),typeof(RectTransform),typeof(Image));
            var rect=heart.GetComponent<RectTransform>();rect.SetParent(frame.transform,false);
            rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);
            rect.sizeDelta=new Vector2(81.75f,67.25f);rect.anchoredPosition=new Vector2(-84+i*83.75f,1.375f);
            hearts[i]=heart.GetComponent<Image>();hearts[i].raycastTarget=false;
        }
        Refresh();
    }
    void Update(){if(player!=null && displayed!=player.Health)Refresh();}
    void Refresh()
    {
        if(player==null)return;
        displayed=player.Health;
        for(int i=0;i<hearts.Length;i++)if(hearts[i]!=null)hearts[i].sprite=i<displayed ? full : empty;
    }
}

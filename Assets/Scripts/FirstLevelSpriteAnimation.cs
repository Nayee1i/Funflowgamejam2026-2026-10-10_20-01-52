using UnityEngine;

public class FirstLevelSpriteAnimation : MonoBehaviour
{
    Sprite[] frames;
    SpriteRenderer picture;
    FirstLevelRooms level;
    float elapsed,fps;
    bool loop;
    public bool IsPendingImpact(FirstLevelRooms owner) => !loop && level==owner &&
        frames!=null && frames.Length>0 && gameObject.activeInHierarchy;
    public void Initialize(Sprite[] sequence,float rate,bool repeat,FirstLevelRooms owner)
    {
        frames=sequence;fps=rate;loop=repeat;level=owner;picture=GetComponent<SpriteRenderer>();
        if(frames.Length>0)picture.sprite=frames[0];
    }
    void Update()
    {
        if(frames==null || frames.Length==0 || (level!=null && !level.CanPlay))return;
        elapsed+=Time.deltaTime;
        int index=Mathf.FloorToInt(elapsed*fps);
        if(!loop && index>=frames.Length){Destroy(gameObject);return;}
        picture.sprite=frames[index%frames.Length];
    }
}

using UnityEngine;
// Optional cover never counts as an enemy required to open the exit.
public class DestructibleCover : MonoBehaviour
{
 public int maximumHealth=3;
 public SpriteRenderer visual;
 public Sprite[] damageSprites;
 public Vector2[] damageScales,damageOffsets;
 public FirstLevelRooms level;
 public int Health { get; private set; }
 void Awake(){Health=maximumHealth;ShowDamage(0);}
 public void ArrangeLayers()
 {
  if(level==null || visual==null)return;
  Transform room=null;
  foreach(var candidate in level.rooms)if(transform.IsChildOf(candidate.transform)){room=candidate.transform;break;}
  if(room==null)return;
  var root=room.Find("Destructible Block Layers");
  if(root==null)
  {
   root=new GameObject("Destructible Block Layers").transform;root.SetParent(room,false);
   var group=root.gameObject.AddComponent<UnityEngine.Rendering.SortingGroup>();
   group.sortingLayerID=visual.sortingLayerID;group.sortingOrder=6;
  }
  var blocks=room.GetComponentsInChildren<DestructibleCover>(true);
  System.Array.Sort(blocks,(a,b)=>
  {
   // Lower blocks are nearer to the viewer and must cover the blocks behind them.
   int order=b.transform.position.y.CompareTo(a.transform.position.y);
   return order!=0 ? order : a.transform.position.x.CompareTo(b.transform.position.x);
  });
  for(int i=0;i<blocks.Length;i++)
  {
   blocks[i].transform.SetParent(root,true);
   if(blocks[i].visual!=null)blocks[i].visual.sortingOrder=i;
  }
 }
 public void ShowDamage(int stage)
 {
  if(visual==null || damageSprites==null || stage>=damageSprites.Length)return;
  visual.sprite=damageSprites[stage];
  visual.transform.localScale=new Vector3(damageScales[stage].x,damageScales[stage].y,1);
  visual.transform.localPosition=damageOffsets[stage];
  ArrangeLayers();
 }
 public void Hit(int damage)
 {
  if(!gameObject.activeSelf)return;
  if(Health<=0)Health=maximumHealth;
  Health=Mathf.Max(0,Health-damage);
  GameAudio.Play("block_hit");
  if(Health==0)GameAudio.Play("block_break");
  if(Health==0)
  {
   if(visual!=null && damageSprites!=null && damageSprites.Length>3)
   {
    ShowDamage(3);
    // The last Figma frame becomes debris. It has no collider and renders below tanks.
    var debris=new GameObject("Destroyed Block Debris");debris.transform.SetParent(transform.parent,false);
    debris.transform.SetPositionAndRotation(visual.transform.position,visual.transform.rotation);debris.transform.localScale=visual.transform.lossyScale;
    var picture=debris.AddComponent<SpriteRenderer>();picture.sprite=visual.sprite;picture.sortingLayerID=visual.sortingLayerID;picture.sortingOrder=-1;
    debris.AddComponent<DestructibleBlockDebris>().Configure(level,picture);
   }
   gameObject.SetActive(false);
  }
  else ShowDamage(Mathf.Clamp(maximumHealth-Health,0,2));
 }
}

// Runtime-only debris waits during pause and hints, like the projectile effects.
public class DestructibleBlockDebris : MonoBehaviour
{
 FirstLevelRooms level;SpriteRenderer picture;float age;
 public void Configure(FirstLevelRooms owner,SpriteRenderer renderer){level=owner;picture=renderer;}
 void Update()
 {
  if(level!=null && !level.CanPlay)return;
  age+=Time.deltaTime;
  if(age>.45f)picture.color=new Color(1,1,1,Mathf.Clamp01(1-(age-.45f)/.35f));
  if(age>=.8f)Destroy(gameObject);
 }
}

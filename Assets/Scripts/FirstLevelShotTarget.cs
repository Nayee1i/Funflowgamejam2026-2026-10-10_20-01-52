using UnityEngine;

// Optional damage receiver for future enemies. No enemies are placed by this change.
public class FirstLevelShotTarget : MonoBehaviour
{
    public int health=5;
    public bool shielded;
    public void Hit(int damage,bool charged)
    {
        if(shielded && !charged)return;
        if(charged)shielded=false;
        health=Mathf.Max(0,health-damage);
        if(health==0)gameObject.SetActive(false);
    }
}

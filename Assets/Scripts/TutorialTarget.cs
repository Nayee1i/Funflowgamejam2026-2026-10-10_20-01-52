using UnityEngine;

public class TutorialTarget : MonoBehaviour
{
    [SerializeField] private bool shielded;
    [SerializeField] private bool requireReflectedHit;
    [SerializeField] private GameObject shieldVisual;
    public bool IsDestroyed { get; private set; }
    public bool IsShielded => shielded;

    public void Hit(bool reflected, bool shieldBreaking)
    {
        if (IsDestroyed) return;
        if (shielded)
        {
            if (shieldBreaking)
            {
                shielded = false;
                if (shieldVisual != null) shieldVisual.SetActive(false);
            }
            return;
        }
        if (requireReflectedHit && !reflected) return;
        IsDestroyed = true;
        gameObject.SetActive(false);
    }
}

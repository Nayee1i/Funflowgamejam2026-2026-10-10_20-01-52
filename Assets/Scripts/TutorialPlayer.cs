using UnityEngine;

public class TutorialPlayer : MonoBehaviour
{
    [SerializeField] private TutorialLevel level;

    public void Hit()
    {
        if (level != null) level.Restart();
    }
}

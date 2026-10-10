using UnityEngine;
using UnityEngine.SceneManagement;

public class TutorialLevelMenu : MonoBehaviour
{
    private void OnGUI()
    {
        GUIStyle style = new GUIStyle(GUI.skin.label) { fontSize = 28, alignment = TextAnchor.MiddleCenter };
        float x = Screen.width * .5f - 160;
        float y = Screen.height * .5f - 100;
        GUI.Label(new Rect(x, y, 320, 50), "Выбор уровня", style);
        if (GUI.Button(new Rect(x, y + 70, 320, 50), "Обучение")) SceneManager.LoadScene("Tutorial");
        if (GUI.Button(new Rect(x, y + 132, 320, 50), "Арена")) SceneManager.LoadScene("TankArena");
    }
}

using UnityEngine;

[RequireComponent(typeof(Camera))]
[ExecuteAlways]
public class FirstLevelCameraFit : MonoBehaviour
{
    public float minimumHalfHeight = 5.15f;
    public float horizontalHalfWidth = 8.2f;
    public float verticalOffset = 0f;
    private void LateUpdate() => Apply();
    public void Apply()
    {
        var camera = GetComponent<Camera>();
        camera.orthographicSize = Mathf.Max(minimumHalfHeight, horizontalHalfWidth / Mathf.Max(.01f,camera.aspect));
        var position=transform.position;
        position.y=verticalOffset;
        transform.position=position;
    }
}

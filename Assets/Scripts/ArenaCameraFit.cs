using UnityEngine;

[RequireComponent(typeof(Camera))]
public class ArenaCameraFit : MonoBehaviour
{
    private Camera arenaCamera;

    private void Awake()
    {
        arenaCamera = GetComponent<Camera>();
    }

    private void LateUpdate()
    {
        arenaCamera.orthographicSize = Mathf.Max(6f, 9f / arenaCamera.aspect);
    }
}

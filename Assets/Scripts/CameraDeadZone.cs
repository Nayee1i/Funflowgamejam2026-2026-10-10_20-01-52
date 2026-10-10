using UnityEngine;

[DefaultExecutionOrder(100)]
[RequireComponent(typeof(Camera))]
public class CameraDeadZone : MonoBehaviour
{
    [SerializeField] private Transform target;
    [Tooltip("Width and height of the dead zone as fractions of the camera view.")]
    [SerializeField] private Vector2 deadZone = new Vector2(0.1f, 0.1f);
    [SerializeField] private float smoothTime = 0.22f;
    private Camera view;
    private Vector3 velocity;

    public void SetTarget(Transform player) => target = player;
    public void ResetSmoothing() => velocity = Vector3.zero;

    private void Awake() => view = GetComponent<Camera>();

    private void LateUpdate()
    {
        if (target == null) return;
        Vector3 position = transform.position;
        float halfHeight = view.orthographicSize * Mathf.Clamp01(deadZone.y);
        float halfWidth = view.orthographicSize * view.aspect * Mathf.Clamp01(deadZone.x);
        Vector3 offset = target.position - position;
        Vector3 desired = position;
        desired.x += offset.x - Mathf.Clamp(offset.x, -halfWidth, halfWidth);
        desired.y += offset.y - Mathf.Clamp(offset.y, -halfHeight, halfHeight);
        transform.position = Vector3.SmoothDamp(position, desired, ref velocity, Mathf.Max(0.01f, smoothTime));
    }
}

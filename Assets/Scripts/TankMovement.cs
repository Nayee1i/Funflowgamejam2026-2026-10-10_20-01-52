using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
public class TankMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private Transform turret;
    [SerializeField] private Transform muzzle;
    [SerializeField] private Bullet bulletPrefab;
    [SerializeField] private Camera gameCamera;
    [SerializeField] private float shotInterval = 0.4f;

    private Rigidbody2D body;
    private Collider2D ownCollider;
    private Vector2 movement;
    private float nextShotTime;

    public void Configure(Transform turretTransform, Transform muzzleTransform,
        Bullet projectilePrefab, Camera camera)
    {
        turret = turretTransform;
        muzzle = muzzleTransform;
        bulletPrefab = projectilePrefab;
        gameCamera = camera;
    }

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        ownCollider = GetComponent<BoxCollider2D>();
        body.gravityScale = 0f;
        body.freezeRotation = true;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;

        if (gameCamera == null)
            gameCamera = Camera.main;
    }

    private void Update()
    {
        movement = Vector2.zero;

        var keyboard = Keyboard.current;

        if (keyboard != null)
        {
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) movement.y += 1;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) movement.y -= 1;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) movement.x += 1;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) movement.x -= 1;
        }

        // Диагональ не должна давать дополнительную скорость.
        movement = movement.normalized;

        var mouse = Mouse.current;

        if (mouse == null || gameCamera == null || turret == null || muzzle == null)
            return;

        Vector2 screenPosition = mouse.position.ReadValue();

        Vector3 cursorWorld = gameCamera.ScreenToWorldPoint(
            new Vector3(
                screenPosition.x,
                screenPosition.y,
                turret.position.z - gameCamera.transform.position.z
            )
        );

        Vector2 aim = (Vector2)(cursorWorld - turret.position);

        if (aim.sqrMagnitude > 0.001f)
        {
            float angle =
                Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg - 90f;

            turret.rotation = Quaternion.Euler(0, 0, angle);
        }

        if (Time.timeScale > 0 &&
            mouse.leftButton.isPressed &&
            Time.time >= nextShotTime && bulletPrefab != null)
        {
            nextShotTime = Time.time + shotInterval;

            Bullet bullet = Instantiate(
                bulletPrefab,
                muzzle.position,
                muzzle.rotation
            );

            bullet.gameObject.SetActive(true);
            bullet.Launch(muzzle.up, ownCollider);
        }
    }

    private void FixedUpdate()
    {
        body.linearVelocity = movement * moveSpeed;
    }

    private void OnDisable()
    {
        if (body != null) body.linearVelocity = Vector2.zero;
    }
}

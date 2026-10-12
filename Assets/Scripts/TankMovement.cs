using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
public class TankMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float turnSpeed = 540f;
    [SerializeField] private Transform turret;
    [SerializeField] private Transform muzzle;
    [SerializeField] private Bullet bulletPrefab;
    [SerializeField] private Camera gameCamera;
    [SerializeField] private float shotInterval = 0.4f;
    [SerializeField, Min(0.01f)] private float reloadDuration = 1.5f;
    [SerializeField] private Sprite ammoIcon;

    public const int MagazineSize = 4;
    public int BulletsRemaining { get; private set; } = MagazineSize;
    public bool IsReloading { get; private set; }
    public float ReloadProgress => IsReloading
        ? Mathf.Clamp01((Time.time - reloadStartedAt) / reloadDuration) : 1f;
    public Sprite AmmoIcon => ammoIcon;

    private Rigidbody2D body;
    private Collider2D ownCollider;
    private Vector2 movement;
    private float nextShotTime;
    private float reloadStartedAt;

    public void Configure(Transform turretTransform, Transform muzzleTransform,
        Bullet projectilePrefab, Camera camera, Sprite magazineIcon = null)
    {
        turret = turretTransform;
        muzzle = muzzleTransform;
        bulletPrefab = projectilePrefab;
        gameCamera = camera;
        if (magazineIcon != null) ammoIcon = magazineIcon;
    }

    private void Start()
    {
        if (gameCamera == null) return;
        if (!TryGetComponent<TutorialPlayer>(out var health))
            health = gameObject.AddComponent<TutorialPlayer>();
        var hud = new GameObject("Player Ammo HUD").AddComponent<PlayerAmmoHud>();
        hud.Initialize(this, gameCamera);
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
        if (gameCamera != null)
        {
            CameraDeadZone follow = gameCamera.GetComponent<CameraDeadZone>();
            if (follow == null) follow = gameCamera.gameObject.AddComponent<CameraDeadZone>();
            follow.SetTarget(transform);
        }
    }

    private void Update()
    {
        if (IsReloading && Time.time >= reloadStartedAt + reloadDuration)
        {
            BulletsRemaining = MagazineSize;
            IsReloading = false;
        }

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
    }

    private void LateUpdate()
    {
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
            !IsReloading && BulletsRemaining > 0 &&
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
            BulletsRemaining--;
            if (BulletsRemaining == 0)
            {
                reloadStartedAt = Time.time;
                IsReloading = true;
            }
        }
    }

    private void FixedUpdate()
    {
        body.linearVelocity = movement * moveSpeed;
        if (movement.sqrMagnitude > 0.001f)
        {
            float angle = Mathf.Atan2(movement.y, movement.x) * Mathf.Rad2Deg - 90f;
            body.MoveRotation(Mathf.MoveTowardsAngle(body.rotation, angle, turnSpeed * Time.fixedDeltaTime));
        }
    }

    private void OnDisable()
    {
        if (body != null) body.linearVelocity = Vector2.zero;
    }
}

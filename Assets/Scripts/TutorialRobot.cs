using UnityEngine;

public class TutorialRobot : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private Transform turret;
    [SerializeField] private Transform muzzle;
    [SerializeField] private Bullet bulletPrefab;
    [SerializeField] private TutorialLevel level;
    [SerializeField] private int roomIndex = 2;
    [SerializeField] private float shotInterval = 2f;
    [SerializeField] private float firstShotDelay = 1f;
    private float nextShot;
    private bool activated;

    private void Update()
    {
        if (level == null || level.CurrentRoom != roomIndex || level.Finished ||
            player == null || turret == null || muzzle == null || bulletPrefab == null) return;
        if (!activated)
        {
            activated = true;
            nextShot = Time.time + firstShotDelay;
        }
        Vector2 aim = player.position - turret.position;
        turret.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg - 90f);
        if (Time.time < nextShot) return;
        nextShot = Time.time + shotInterval;
        Bullet ball = Instantiate(bulletPrefab, muzzle.position, muzzle.rotation);
        ball.gameObject.SetActive(true);
        ball.Launch(muzzle.up, GetComponent<Collider2D>(), true);
    }
}

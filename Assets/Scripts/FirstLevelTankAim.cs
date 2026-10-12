using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class FirstLevelTankAim : MonoBehaviour
{
    public FirstLevelRooms level;
    public Transform turret;
    public Sprite projectileSprite;
    public float spriteAimOffset = 60.44853f;
    public float muzzleDistance = .802134f;
    public float projectileSpeed = 10f;
    public float projectileRadius = .035f;
    public float mouseTurnSensitivity
    {
        get => MouseSensitivitySettings.Value;
        set => MouseSensitivitySettings.Set(value);
    }
    Vector2 mouseAimOffset = Vector2.up * 160f;
    public Vector2 AimDirection { get; private set; } = Vector2.up;
    public const int MagazineSize=4;
    public float reloadDuration=1.5f;
    public float shotInterval=.4f;
    public int BulletsRemaining { get; private set; }=MagazineSize;
    public bool IsReloading => reloadRemaining>0;
    public float ReloadProgress => IsReloading ? 1-reloadRemaining/Mathf.Max(.01f,reloadDuration) : 1;
    float reloadRemaining,shotCooldown;
    public void AdvanceReload(float delta)
    {
        if(level==null || !level.CanPlay)return;
        shotCooldown=Mathf.Max(0,shotCooldown-delta);
        if(!IsReloading)return;
        reloadRemaining=Mathf.Max(0,reloadRemaining-delta);
        if(!IsReloading)BulletsRemaining=MagazineSize;
    }
    bool previousVisible;
    CursorLockMode previousLock;
    bool mouseControlActive;
    FirstLevelAimPreview aimPreview;

    void OnEnable() { previousVisible=Cursor.visible; previousLock=Cursor.lockState; }
    void OnDisable() { if(aimPreview!=null)aimPreview.Hide(); Cursor.visible=previousVisible; Cursor.lockState=previousLock; FirstLevelShot.ClearAll(); }
    void OnApplicationFocus(bool focused) { if(!focused) { Cursor.visible=true; mouseControlActive=false; } }
    void LateUpdate()
    {
        AdvanceReload(Time.deltaTime);
        bool gameplay=Application.isFocused && level!=null && level.CanPlay;
        Cursor.visible=!gameplay;
        Cursor.lockState=gameplay ? CursorLockMode.Locked : CursorLockMode.None;
        if(!gameplay || turret==null || Mouse.current==null)
        {
            mouseControlActive=false;
            if(aimPreview!=null)aimPreview.Hide();
            return;
        }
        // Ignore the capture frame: opening the game or closing a panel may warp the cursor.
        Vector2 mouseDelta=mouseControlActive ? Mouse.current.delta.ReadValue() : Vector2.zero;
        mouseControlActive=true;
        ApplyMouseAim(mouseDelta);
        if(aimPreview==null)
        {
            var preview=new GameObject("Ricochet Aim Preview");
            preview.transform.SetParent(level.transform,false);aimPreview=preview.AddComponent<FirstLevelAimPreview>();
        }
        if(Mouse.current.rightButton.isPressed)aimPreview.Show(this);else aimPreview.Hide();
        if(Mouse.current.leftButton.wasPressedThisFrame && level.AllowMouseShotThisFrame &&
            (EventSystem.current==null || !EventSystem.current.IsPointerOverGameObject()))Fire();
    }
    public void ApplyMouseAim(Vector2 mouseDelta)
    {
        // The hidden aiming point travels with the tank, but mouse motion moves it
        // on both screen axes. Circular mouse motion therefore gives a full turn.
        mouseAimOffset+=mouseDelta*mouseTurnSensitivity;
        if(mouseAimOffset.sqrMagnitude>=16f)AimDirection=mouseAimOffset.normalized;
        float angle=Mathf.Atan2(AimDirection.y,AimDirection.x)*Mathf.Rad2Deg;
        if(turret!=null)turret.rotation=Quaternion.Euler(0,0,angle-spriteAimOffset);
    }
    public bool AimAt(Vector2 world)
    {
        if(turret==null)return false;
        Vector2 delta=world-(Vector2)turret.position;
        if(delta.sqrMagnitude<.0001f)return false;
        AimDirection=delta.normalized;
        mouseAimOffset=AimDirection*160f;
        turret.rotation=Quaternion.Euler(0,0,Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg-spriteAimOffset);
        return true;
    }
    public FirstLevelShot Fire()
    {
        if(level==null || !level.CanPlay || IsReloading || BulletsRemaining<=0 || shotCooldown>0)return null;
        BulletsRemaining--;shotCooldown=shotInterval;
        if(BulletsRemaining==0)reloadRemaining=Mathf.Max(.01f,reloadDuration);
        Vector2 origin=turret.position;
        // Sweep from the turret to the muzzle too, so firing beside a wall cannot skip it.
        var shot=FirstLevelShot.Create(origin,AimDirection,projectileSprite,level,projectileSpeed,projectileRadius);
        shot.Step(muzzleDistance*Mathf.Abs(transform.lossyScale.x));
        return shot;
    }
}

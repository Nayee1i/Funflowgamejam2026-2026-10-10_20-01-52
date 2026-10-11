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
    public Vector2 AimDirection { get; private set; } = Vector2.up;
    bool previousVisible;
    CursorLockMode previousLock;

    void OnEnable() { previousVisible=Cursor.visible; previousLock=Cursor.lockState; }
    void OnDisable() { Cursor.visible=previousVisible; Cursor.lockState=previousLock; FirstLevelShot.ClearAll(); }
    void OnApplicationFocus(bool focused) { if(!focused) Cursor.visible=true; }
    void LateUpdate()
    {
        bool gameplay=Application.isFocused && level!=null && level.CanPlay;
        Cursor.visible=!gameplay;
        Cursor.lockState=gameplay ? CursorLockMode.Confined : CursorLockMode.None;
        if(!gameplay || turret==null || Mouse.current==null || Camera.main==null)return;
        Vector2 screen=Mouse.current.position.ReadValue();
        if(!Camera.main.pixelRect.Contains(screen))return;
        Vector3 world=Camera.main.ScreenToWorldPoint(new Vector3(screen.x,screen.y,turret.position.z-Camera.main.transform.position.z));
        if(!AimAt(world))return;
        if(Mouse.current.leftButton.wasPressedThisFrame &&
            (EventSystem.current==null || !EventSystem.current.IsPointerOverGameObject()))Fire();
    }
    public bool AimAt(Vector2 world)
    {
        if(turret==null)return false;
        Vector2 delta=world-(Vector2)turret.position;
        if(delta.sqrMagnitude<.0001f)return false;
        AimDirection=delta.normalized;
        turret.rotation=Quaternion.Euler(0,0,Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg-spriteAimOffset);
        return true;
    }
    public FirstLevelShot Fire()
    {
        if(level==null || !level.CanPlay)return null;
        Vector2 origin=turret.position;
        // Sweep from the turret to the muzzle too, so firing beside a wall cannot skip it.
        var shot=FirstLevelShot.Create(origin,AimDirection,projectileSprite,level,projectileSpeed,projectileRadius);
        shot.Step(muzzleDistance*Mathf.Abs(transform.lossyScale.x));
        return shot;
    }
}

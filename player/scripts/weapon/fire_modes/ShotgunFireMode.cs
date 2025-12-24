using Godot;
using System;

public partial class ShotgunFireMode : IFireMode
{
    // Shotgun FireMode is almost identical to the Semi-Auto FireMode with some differences in the 
    // OnTriggerPressed function. 
    private WeaponBase CurrentWeapon;
    // Assume that the weapon trigger has been released
    private bool CanFire = true;

    public ShotgunFireMode(WeaponBase weapon)
    {
        CurrentWeapon = weapon;
    }

    public void OnTriggerPressed()
    {
        // Can only tell the weapon to fire if the trigger has been released, the weapon is not reloading
        // or the weapon is not currently in the middle of its firing animation
        if (!CanFire || CurrentWeapon.IsReloading || CurrentWeapon.IsFiring)
            return;

        // The weapon only really cares on how the fire is implemented and needs to be told when to fire 
        // Since this is a multi-hit fire mode we would need to tell the weapon to fire multiple times at the same time
        CurrentWeapon.Fire();
        CanFire = false;
    }

    public void OnTriggerReleased()
    {   
        // Need to let go of the trigger before the weapon can shoot again. This is the core
        // of a semi-auto fire mode
        CanFire = true;
    }

    // We dont implement anything in Update since we allow the trigger to be pressed and released as many
    // times as possible. We could put a cadence here if needed though
    public void Update(double delta) { }
}

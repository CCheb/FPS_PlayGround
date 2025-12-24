using Godot;
using System;

public partial class SemiAutoFireMode : IFireMode
{
    private WeaponBase CurrentWeapon;
    private bool CanFire = true;

    public SemiAutoFireMode(WeaponBase weapon)
    {
        CurrentWeapon = weapon;
    }

    public void OnTriggerPressed()
    {
        if (!CanFire || CurrentWeapon.IsReloading || CurrentWeapon.IsFiring)
            return;

        // The weapon only really cares on how the fire is implemented and needs to be told when to fire 
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

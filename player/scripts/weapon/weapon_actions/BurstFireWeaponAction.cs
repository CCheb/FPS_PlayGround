using Godot;
using System;

public partial class BurstFireWeaponAction : IWeaponAction
{
    private WeaponBase CurrentWeapon;
    private bool CanFire = true;

    public BurstFireWeaponAction(WeaponBase weapon)
    {
        CurrentWeapon = weapon;
    }

    public async void OnActionPressed()
    {
        if (!CanFire || CurrentWeapon.IsReloading || CurrentWeapon.IsFiring)
            return;

        // The weapon only really cares on how the fire is implemented and needs to be told when to fire 
        for(int i = 0; i < 3; i++)
        {
            CurrentWeapon.Fire();
            await CurrentWeapon.ToSignal(CurrentWeapon.GetTree().CreateTimer(0.08f), "timeout");
        }
        CanFire = false;
    }

    public void OnActionReleased()
    {   
        // Need to let go of the trigger before the weapon can shoot again. This is the core
        // of a semi-auto fire mode
        CanFire = true;
    }

    // We dont implement anything in Update since we allow the trigger to be pressed and released as many
    // times as possible. We could put a cadence here if needed though
    public void Update(double delta) { }
    
}

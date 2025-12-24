using Godot;
using System;

public static partial class FireModeFactory
{
    public static IFireMode Create(WeaponResource WeaponData, WeaponBase CurrentWeapon, Globals.FireModes CurrentMode)
    {
        switch(CurrentMode)
        {
            // Based on the CurrentFireMode we return the appropriate FireMode object. If the WeaponResource specified
            // FireMode Shotgun then the factory will return the ShotgunFireMode and so on
            case Globals.FireModes.FullAuto :
                return new FullAutoFireMode(CurrentWeapon, WeaponData.FireRate);
            case Globals.FireModes.SemiAuto :
                return new SemiAutoFireMode(CurrentWeapon);
            case Globals.FireModes.Shotgun :
                return new ShotgunFireMode(CurrentWeapon);
            default :
                throw null;
        }
    }
}

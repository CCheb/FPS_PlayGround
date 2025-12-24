using Godot;
using System;
using System.ComponentModel;

public abstract partial class WeaponBase : Node3D
{
    // Need WeaponData to supply vital information to the weapon
    // e.g fire rate, weapon scene, all of its important nodes, etc
    protected WeaponResource WeaponData;
    // Need Controller since we might have to signal information to it
    // e.g notify that ammo changed
    protected WeaponController Controller;
    
    // Root of every Weapon Scene must be a Node3D (i.e. WeaponPivot)
    // Note that some of these variables could be moved over to the specific WeaponTypes
    // Since not all weapon will fill these variables out (e.g. think of a melee weapon)
    protected Node3D WeaponScene;
    protected AnimationPlayer WeaponAnimPlayer;
    protected AudioStreamPlayer3D GunSound;
    protected AudioStreamPlayer3D GunSoundEmpty;
    protected MuzzleFlash MuzzleFlashRef;
    protected float fireRate;
    public bool IsReloading = false;
    public bool IsFiring = false;

    // Abstract functions that each children (a particular weapon type) must implement
    public abstract void Initiallize(WeaponResource WeaponData, WeaponController Controller);
    public abstract void Fire();
    // Reload Function here
    public abstract void Reload();

}

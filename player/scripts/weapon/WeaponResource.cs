using Godot;
using System;

// GlobalClass means that the class will be visible in the editor
[GlobalClass]
public partial class WeaponResource : Resource
{
    // The idea is to have each weapon fill out this resource
    // since each of them will be different in transform, mesh, etc
    // This is the cartrige that gets inserted into the Weapon 'console'
    // we can always swap cartriges in and out
    
    [Export] public string Name;
    [Export] public Globals.WeaponTypes WeaponType;
    [Export] public Globals.FireModes FireMode;

     /* Weapon Transform*/
    //------------------------
    [ExportGroup("Weapon Transform")]
    [Export] public Vector3 Position;
    [Export] public Vector3 Rotation;
    [Export] public Vector3 Scale;

     /* Weapon Sway */
    //------------------------
    [ExportGroup("Weapon Sway")]
    // Clamp the amount of mouse movement that will be used to sway the weapon
    // Total sway ranges. How much you can sway left and right
    [Export] public Vector2 SwayMin = new Vector2(-20.0f, -20.0f);
    [Export] public Vector2 SwayMax = new Vector2(20.0f, 20.0f);
    // Speed will set the alphas of the lerp. How fast is the sway
    [Export(PropertyHint.Range, "0, 0.2, 0.01")] public float SwaySpeedPosition = 0.07f;
    [Export(PropertyHint.Range, "0, 0.2, 0.01")] public float SwaySpeedRotation = 0.1f;
    // Manipulate mouse movement value
    [Export(PropertyHint.Range, "0, 0.50, 0.01")] public float SwayAmountPosition = 0.1f;
    [Export(PropertyHint.Range, "0, 0.50, 0.1")] public float SwayAmountRotation = 30.0f;

    /* Random Idle Sway */
    //------------------------
    [ExportGroup("Random Idle Sway")]
    // Adjust idle sway amount
    [Export] public float IdleSwayAdjustment = 10.0f;
    // Adjust strength or rotation
    [Export] public float IdleSwayRotationStength = 300.0f;
    // Adjust the strength of the sine wave
    [Export] public float RandomSwayAmmount = 5.0f;

    /* Visual Settings */
    //------------------------
    [ExportGroup("Visual Settings")]
    [Export] public PackedScene WeaponScene;

    /* Camera Specific Recoil */
    //------------------------
    [ExportGroup("Camera Recoil")]
    [Export] public Vector3 CameraRecoilAmount = Vector3.Zero;
    [Export] public float CameraSnapAmount = 0.0f;
    [Export] public float CameraRecoverySpeed = 0.0f;
    
    /* Weapon Specific Recoil */
    //------------------------
    [ExportGroup("Weapon Recoil")]
    [Export] public Vector3 WeaponRecoilAmount = Vector3.Zero;
    [Export] public float WeaponSnapAmount = 0.0f;
    [Export] public float WeaponRecoverySpeed = 0.0f;
    [Export] public float FireRate = 0.0f;
    [Export] public int AmmoCount = 0;
    [Export] public int AmmoCapacity = 0;

    [ExportGroup("Animations")]
    [Export] public AnimationProfile Fire;
    [Export] public AnimationProfile Reload;
    [Export] public AnimationProfile Pump = null;

}

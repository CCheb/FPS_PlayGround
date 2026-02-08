using Godot;
using System;
using System.Runtime.CompilerServices;
using System.Xml.Serialization;

public partial class Hitscan : WeaponBase
{
    [Export] private PackedScene ImpactEffect;
    [Export] private PackedScene WeaponDecal;
    [Export] private PackedScene ShellCasingScene;
    [Export] private Marker3D ShellEjectionMarker;
    private float FireAnimationSpeed = 1.0f;
    

    // Its vital that we initialize the corresponding WeaponData and Controller variables
    // before we start passing out information from WeaponData
    public override void Initiallize(WeaponResource WeaponData, WeaponController weaponController)
    {
        this.WeaponData = WeaponData;
        this.weaponController = weaponController;
    }

    public override  void _Ready()
    {
        base._Ready();
        
        // No need to initialize Position, Rotation, and Scale here since the WeaponController is already doing that for us
        // We do however need to initialize more weapon specific things like nodes
        SetWeaponNodes();
        FireAnimationSpeed = CalculateFireAnimationSpeed();
        TryPlayingDrawAnimation();
    }

    private void SetWeaponNodes()
    {
        CameraReloadProxy = GetNode<Node3D>("./CameraReloadProxy");
        MuzzleFlashRef = GetNode<MuzzleFlash>("./MuzzleFlash");
        //GunSoundEmpty = GetNode<AudioStreamPlayer3D>("GunSoundEmpty");
        GunSound = GetNode<AudioStreamPlayer3D>("GunSound");
        WeaponAnimPlayer = GetNode<AnimationPlayer>("./Meshes/AnimationPlayer");
    }

    // Function gets called at very specific moments during the firing animation
    public void EjectShell()
    {
        ShellEjection Shell = ShellCasingScene.Instantiate<ShellEjection>();
        GetTree().CurrentScene.AddChild(Shell);
        Shell.GlobalTransform = ShellEjectionMarker.GlobalTransform;
        Vector3 EjectDir = ShellEjectionMarker.GlobalTransform.Basis.X.Normalized();
        Shell.Eject(EjectDir, 4.5f);
    }

    public override async void Fire()
    {
        IsFiring = true;
        // Shoot a ray cast from the center of the screen
		// straight outwards until it either collides with a body or reaches limit

        Godot.Collections.Dictionary collisionResult = CalculateRay();

		// If the ray collided with something then we are safe to "fire" the weapon 
		// We send the position of contact and the normal vector of the surface
        if(collisionResult.Count != 0)
        {
            // Request the camera and recoil nodes to activate appropriately
            weaponController.CameraRecoilRef.EmitSignal("AddCameraRecoilSignal");
            weaponController.WeaponRecoilRef.EmitSignal("WeaponFiredSignal");
            MuzzleFlashRef.EmitSignal("MuzzleFlashSignal", WeaponData.FireRate);

            // Weapon animations should be reactive not authorative in nature
            // Also animation name should be abstracted out to keep it dynamic
            WeaponAnimPlayer.Play(WeaponData.Fire.AnimationName, WeaponData.Fire.BlendAmount,FireAnimationSpeed);
            // Update Ammo here
            // Nudge animation forward to the "kick" pose
            WeaponAnimPlayer.Seek(0.02f, true);
            // Gun Sound here
            GunSound.Play();
            // This gets ignored by certain fire modes like FullAuto
            await ToSignal(WeaponAnimPlayer, "animation_finished");
        }

        IsFiring = false;
    }

    private Godot.Collections.Dictionary CalculateRay(float length = 1000.0f)
    {
        // Grab a reference to the players world camera. (Camera Controller is the world camera)
		Camera3D camera = Globals.player.WorldCameraController.Camera;
		// Grab the worlds 3D physics state/sandbox. This state is where all of the physics occurs and its handled by the physics server
		var spaceState = camera.GetWorld3D().DirectSpaceState;
		// Need to find the center of the screen to create origin point. GetViewport here is the weapon camera viewport but since its always
		// following the player then we can assume that its the same as getting the world camera viewport
		Vector2 screenCenter = (Vector2)GetViewport().Get("size") / 2;
		// Start point of the ray in this case in the center of the screen. We are picking a point on the screen. 
		// Its important that we project the ray from the world camera
		Vector3 origin = camera.ProjectRayOrigin(screenCenter);
		// The end of ray is 1000m out from the cameras normal
		Vector3 end = origin + camera.ProjectRayNormal(screenCenter) * length;

		// Create the ray which will return back a dictionary with metadata on any
		// physics collisions. Make sure to enable collision with bodies or areas
		var queryCollisions = PhysicsRayQueryParameters3D.Create(origin, end);
        queryCollisions.CollideWithBodies = true;
		queryCollisions.CollideWithAreas = true;
		// Detect layers 1, 2, and 3
		queryCollisions.CollisionMask = (1 << 0) | (1 << 1) | (1 << 2);
		// Find out if the ray intersected with a body. It will return nothing if not
		// We are essentially creating a dictionary holding a number of keys that pertain to the collision information
		var collisionResult = spaceState.IntersectRay(queryCollisions);

        return collisionResult;
    }

    // Could make this abstract so that all guns must implement reload
    public override async void Reload()
    {
        // To prevent spam reloads
        if(IsReloading || IsFiring) 
            return;

        // Lock the weapon from firing then play and wait for the recoil animation before unlocking the weapon
        IsReloading = true;
        WeaponAnimPlayer.Play(WeaponData.Reload.AnimationName, WeaponData.Reload.BlendAmount, WeaponData.Reload.AnimationSpeed);
        await ToSignal(WeaponAnimPlayer, "animation_finished");
        IsReloading = false;

    }

    
}

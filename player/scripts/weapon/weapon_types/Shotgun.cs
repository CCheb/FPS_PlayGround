using Godot;
using System;

public partial class Shotgun : WeaponBase
{
    [Export] private PackedScene ImpactEffect;
    [Export] private PackedScene WeaponDecal;
    [Export] private PackedScene ShellCasingScene;
    [Export] private PackedScene TestDecal;
    [Export] private Marker3D ShellEjectionMarker;
    private float FireAnimationSpeed = 1.0f;
    

    // Its vital that we initialize the corresponding WeaponData and Controller variables
    // before we start passing out information from WeaponData
    public override void Initiallize(WeaponResource WeaponData, WeaponController Controller)
    {
        this.WeaponData = WeaponData;
        this.Controller = Controller;
    }

    public override void _Ready()
    {
        base._Ready();
        
        // No need to initialize Position, Rotation, and Scale here since the WeaponController is already doing that for us
        // We do however need to initialize more weapon specific things like nodes

        // Make sure to instantiate the Weapon Scene because without it Rifle is just pure logic
        WeaponScene = WeaponData.WeaponScene.Instantiate<Node3D>();

        MuzzleFlashRef = GetNode<MuzzleFlash>("./MuzzleFlash");
        if(MuzzleFlashRef == null)
            GD.Print("Empty!");

        /*
        GunSoundEmpty = GetNode<AudioStreamPlayer3D>("GunSoundEmpty");
        */
        GunSound = GetNode<AudioStreamPlayer3D>("GunSound");
        WeaponAnimPlayer = GetNode<AnimationPlayer>("./Meshes/AnimationPlayer");

        float FireAnimLength = WeaponAnimPlayer.GetAnimation(WeaponData.Fire.AnimationName).Length;
        float RoundsPerSecond = WeaponData.FireRate / 60.0f;
        float desiredInterval = 1f / RoundsPerSecond;
        FireAnimationSpeed = FireAnimLength / desiredInterval;
        
        
    }

    // Function gets called at very specific moments during the firing animation
    // Some guns like revolvers might not even call this function and thats fine.
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

        for(int i = 0; i < 12; i++)
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
		    Vector3 end = origin + camera.ProjectRayNormal(screenCenter) * 1000;
            // This is probably in pixels
            end.Y += (float)GD.RandRange(-100f, 100f);
            end.X += (float)GD.RandRange(-100f, 100f);

		    // Create the ray which will return back a dictionary with metadata on any
		    // physics collisions. Make sure to enable collision with bodies or areas
		    var query = PhysicsRayQueryParameters3D.Create(origin, end);
		    query.CollideWithBodies = true;
		    query.CollideWithAreas = true;
		    // Detect layers 1, 2, and 3
		    query.CollisionMask = (1 << 0) | (1 << 1) | (1 << 2);
		    // Find out if the ray intersected with a body. It will return nothing if not
		    // We are essentially creating a dictionary holding a number of keys that pertain to the collision information
		    var result = spaceState.IntersectRay(query);
		    // If the ray collided with something then we are safe to "fire" the weapon 
		    // We send the position of contact and the normal vector of the surface

            // Bullet decal 
            SpawnDecal((Vector3)result["position"]);
            
        }

        Controller.CameraRecoilRef.EmitSignal("AddCameraRecoilSignal");
        Controller.WeaponRecoilRef.EmitSignal("WeaponFiredSignal");
        MuzzleFlashRef.EmitSignal("MuzzleFlashSignal", WeaponData.FireRate);
        // Update Ammo here
        // Gun Sound here
        GunSound.Play();
    
        // Weapon animations should be reactive not authorative in nature
        // Also animation name should be abstracted out to keep it dynamic

        // Fire animation
        WeaponAnimPlayer.Play(WeaponData.Fire.AnimationName, WeaponData.Fire.BlendAmount,FireAnimationSpeed);
        await ToSignal(WeaponAnimPlayer, "animation_finished");
        
        // Pump/rack animation 
        if (WeaponData.Pump != null)
        {
            WeaponAnimPlayer.Play("Pump Animation");
            await ToSignal(WeaponAnimPlayer, "animation_finished");
        }

        IsFiring = false;
    }

    private async void SpawnDecal(Vector3 position)
    {
        // This can be offloaded to a seperate decal script
        MeshInstance3D decal = TestDecal.Instantiate<MeshInstance3D>();
        GetTree().Root.AddChild(decal);
        decal.Position = position;
        await ToSignal(GetTree().CreateTimer(3.0f), "timeout");
        decal.QueueFree();
    }

    // Could make this abstract so that all guns must implement reload
    public override async void Reload()
    {
        // To prevent spam reloads
        if(IsReloading || IsFiring) 
            return;

        IsReloading = true;
        WeaponAnimPlayer.Play(WeaponData.Reload.AnimationName, WeaponData.Reload.BlendAmount, WeaponData.Reload.AnimationSpeed);
        await ToSignal(WeaponAnimPlayer, "animation_finished");
        IsReloading = false;

    }
}

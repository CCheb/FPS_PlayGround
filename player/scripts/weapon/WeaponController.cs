using Godot;
using System;


public partial class WeaponController : Node3D
{
    // Custom signal emmited by the MovementStateMachine that will let the Controller know if the
    // the current player movement state has changed
    [Signal] public delegate void MovementChangedEventHandler(State NewMovementState);
    // Default to the Idle movement state
    private Globals.MovementStates CurrentMovementState = Globals.MovementStates.Idle;
    // We initiallize CurrentWeaponMovementProfile to the Idle profile by default since the signal wont be called automatically
    private Globals.WeaponMovementProfle CurrentWeaponMovementProfie = new Globals.WeaponMovementProfle
    {
      IsIdle = true,
      BobSpeed = 0.0f,
      BobH = 0.0f,
      BobV = 0.0f
    };

    // Array that keeps data files for each available weapon. We make sure to preload them
    // so that they will be ready to go when they get used by the current weapon.
    private WeaponResource[] Arsenal =
    {
        GD.Load<WeaponResource>("res://player/assets/weapons/rifle/Rigged_WeaponResource.tres"),
        GD.Load<WeaponResource>("res://player/assets/weapons/pistol/PistolWeaponResource.tres"),
        GD.Load<WeaponResource>("res://player/assets/weapons/sniper/SniperWeaponResource.tres"),
        GD.Load<WeaponResource>("res://player/assets/weapons/shotgun/ShotgunWeaponResource.tres")
    };
    // Indexing the Arsenal array
    private int CurrentWeaponIndex = 3;
    // CurrentWeapon holds the Weapon Scene of any of the arsenal weapon resources
    private WeaponBase CurrentWeapon;
    // CurrentFireMode will hold the current fire mode (full, semi, burst, shotgun) specified by the WeaponResource
    private IFireMode CurrentFireMode;
    // Noise Texture is what give the idle sway the randomness
	[Export] private NoiseTexture2D SwayNoise;
    // Need reference to the world camera so that we can give it a recoil effect
	[Export] public CameraRecoil CameraRecoilRef;
    // Need reference to the WeaponRecoil Node thats under this node so that we can signal it to recoil the weapon back
    [Export] public WeaponRecoil WeaponRecoilRef;
	// How fast should the random sway be
	[Export] private float SwaySpeed = 1.2f;
    // Need to capture the mouse movement for our weapon sway
	private Vector2 MouseMovement = Vector2.Zero;
    // Vector for holding the bob values
	private Vector2 BobAmount = Vector2.Zero;
    // Time helps in generating random sway for sin and cos
	private float Time = 0.0f;
    // Factor multiplied into noise calculation (doesnt do much)
	private float IdleSwayAdjustment;
	// How strong the Idle Sway Rotation should be
	private float IdleSwayRotationStength;
    // Random Idle sway for x
	private float RandomSwayX;
    // Random Idle sway for y
	private float RandomSwayY;
	private float RandomSwayAmount;
    private float SwayAmountPosition;
    private float SwaySpeedPosition;
    private float SwayAmountRotation;
    private float SwaySpeedRotation;
    private Vector2 SwayMin;
    private Vector2 SwayMax;
    private Vector3 WeaponOriginPos;
    private Vector3 WeaponOriginRot;
    public override void _Ready()
    {
        base._Ready();
        // Subscribe the OnMovementStateChange to the MovementChanged signal
        // The idea is that this is triggered everytime the player movement changes to a new state
        MovementChanged += OnMovementStateChange;
        // Immediately load the specified weapon based on the CurrentWeaponIndex
        LoadWeapon();

    }

    // _Input() here
    public override void _Input(InputEvent @event)
    {
        base._Input(@event);
        if (@event is InputEventMouseMotion)
		{
			// Need to cast event over to InputEventMouseMotion, copy that into a local variable and
			// pass the Relative (mouse deltas between frames) over to MouseMovement 
			InputEventMouseMotion MouseEvent = (InputEventMouseMotion)@event;
			MouseMovement = MouseEvent.Relative;
		}

        // The ? Signifies that CurrentFire should only execure this function if its not null
        // if it is null then a null exception is thrown automatically. In _Input, the event actions
        // are not polled and are only triggered once everytime the key is pressed
        if(@event.IsActionPressed("shoot"))
            CurrentFireMode?.OnTriggerPressed();

        // CurrentFireMode will receive this and handle it accordingly 
        if(@event.IsActionReleased("shoot"))
            CurrentFireMode?.OnTriggerReleased();

        // FireMode only cares on when the current weapon should shoot. Thus Reload should be kept within the Weapon
        if(@event.IsActionPressed("reload"))
            CurrentWeapon?.Reload();
    }

    private void LoadWeapon()
    {
        // Ask WeaponFactory to Create the appropriate weapon object based on what the WeaponResource specified
        CurrentWeapon = WeaponFactory.Create(Arsenal[CurrentWeaponIndex], this);
        if(CurrentWeapon == null)
        {
            GD.PrintErr("CurrentWeapon is null (Invalid Weapon Type)");
            return;
        }

        // Ask FireModeFactory to Create the appropriate firemode object based on what the WeaponResource specified
        CurrentFireMode = FireModeFactory.Create(Arsenal[CurrentWeaponIndex], CurrentWeapon, Arsenal[CurrentWeaponIndex].FireMode);
        if(CurrentFireMode == null)
        {
            GD.PrintErr("CurrentFireMode is null (Invalid Fire Mode Type)");
            return;
        }
        
        // Some data off of the WeaponResource stays with the WeaponController while others go to the Current Weapon (e.g. FireRate)
        Position = Arsenal[CurrentWeaponIndex].Position;
        RotationDegrees = Arsenal[CurrentWeaponIndex].Rotation;
        Scale = Arsenal[CurrentWeaponIndex].Scale;

        // CurrentWeapon is not the WeaponResource itself like the other code. In this case its now the root of the weapon tree
        // and because of this we cannot rely on it giving correct information. So we pull straight from the Arsenal array
        WeaponOriginPos = Position;
        WeaponOriginRot = RotationDegrees;

        // Need to grab some common swaying data from the weapon data. Since all weapons will bob in a similar way
        // and this procedural sway involves manipulating some nodes then we leave it in the controller
        IdleSwayAdjustment = Arsenal[CurrentWeaponIndex].IdleSwayAdjustment;
        IdleSwayRotationStength = Arsenal[CurrentWeaponIndex].IdleSwayRotationStength;
		RandomSwayAmount = Arsenal[CurrentWeaponIndex].RandomSwayAmmount;

        SwayAmountPosition = Arsenal[CurrentWeaponIndex].SwayAmountPosition;
        SwayAmountRotation = Arsenal[CurrentWeaponIndex].SwayAmountRotation;

        SwaySpeedPosition = Arsenal[CurrentWeaponIndex].SwaySpeedPosition;
        SwaySpeedRotation = Arsenal[CurrentWeaponIndex].SwaySpeedRotation;

        SwayMin = Arsenal[CurrentWeaponIndex].SwayMin;
        SwayMax = Arsenal[CurrentWeaponIndex].SwayMax;

        // Send over Camera and Weapon recoil values to the respective nodes
        CameraRecoilRef.recoilAmount = Arsenal[CurrentWeaponIndex].CameraRecoilAmount;
        CameraRecoilRef.snapAmount = Arsenal[CurrentWeaponIndex].CameraSnapAmount;
        CameraRecoilRef.speed = Arsenal[CurrentWeaponIndex].CameraRecoverySpeed;

        WeaponRecoilRef.recoilAmount = Arsenal[CurrentWeaponIndex].WeaponRecoilAmount;
        WeaponRecoilRef.snapAmount = Arsenal[CurrentWeaponIndex].WeaponSnapAmount;
        WeaponRecoilRef.speed = Arsenal[CurrentWeaponIndex].WeaponRecoverySpeed;

        // Finally insert the Current Weapon scene as a child of the recoil node (for now)
        WeaponRecoilRef.AddChild(CurrentWeapon);
    }

    public void WeaponBob(double delta, float BobSpeed, float BobH, float BobV)
	{
		// Time gives us a new value always
		Time += (float)delta;

		// Sin(X/Y * frequency) * amplitude
		BobAmount.X = Mathf.Sin(Time * BobSpeed) * BobH;
		BobAmount.Y = Mathf.Abs(Mathf.Cos(Time * BobSpeed) * BobV);
	}
    private float GetSwayNoise()
	{
		// Default fallback if noise isn’t assigned
		if (SwayNoise == null || SwayNoise.Noise == null)
			return 0.0f;

		Vector3 PlayerPosition = Vector3.Zero;

		// Only access Globals when in play mode. Grab the current players position
		// Only want to do this while in play mode
		if (!Engine.IsEditorHint() && Globals.player != null)
			PlayerPosition = Globals.player.GlobalPosition;

		// Pseudo random value that will be fed into the procedural weapon system
		return SwayNoise.Noise.GetNoise2D(PlayerPosition.X, PlayerPosition.Y);
	}

    private  void SwayHelper(ref Vector3 Pos, ref Vector3 RotDeg, double delta, bool isMoving, float RandomSwayX = 0.0f, float RandomSwayY = 0.0f, float IdleSwayRotationStength = 0.0f)
	{
		// Lerp weapon Pos based on mouse movement. Alot of mistypes here (float -> double)
		// If MouseMovement is 0 then the only thing left would be the currentWeapon.Position.X/Y - RandomSwayX/Y
		Pos.X = (float)Mathf.Lerp(Pos.X, WeaponOriginPos.X - (MouseMovement.X *
			SwayAmountPosition + RandomSwayX + (isMoving ? BobAmount.X : 0.0f)) * delta, SwaySpeedPosition);
		Pos.Y = (float)Mathf.Lerp(Pos.Y, WeaponOriginPos.Y - (MouseMovement.Y *
			SwayAmountPosition + RandomSwayY + (isMoving ? BobAmount.Y : 0.0f)) * delta, SwaySpeedPosition);
		// Lerp weapon rotation based on mouse movement
		// Similar concept to position. If MouseMovement.X/Y is 0 then the only thing left would be the
		// CurrentWeapon.Rotation.Y/X +/- RandomSwayY/X * IdleSwayRotationStrength. This is what causes the idle sway
		RotDeg.Y = (float)Mathf.Lerp(RotDeg.Y, WeaponOriginRot.Y - (MouseMovement.X *
			SwayAmountRotation + (RandomSwayY * IdleSwayRotationStength)) * delta, SwaySpeedRotation);
		RotDeg.X = (float)Mathf.Lerp(RotDeg.X, WeaponOriginRot.X - (MouseMovement.Y *
			SwayAmountRotation + (RandomSwayX * IdleSwayRotationStength)) * delta, SwaySpeedRotation);   
    }

    public void SwayWeapon(double delta, bool isIdle)
	{
		// Return to base position if the mouse is not moving
		MouseMovement = MouseMovement.Lerp(Vector2.Zero, (float)(delta * 6.0));

		// Make sure to clamp the sway ammounts 
		MouseMovement = MouseMovement.Clamp(SwayMin, SwayMax);
		Vector3 Pos = Position;
		Vector3 RotDeg = RotationDegrees;

		// Only play random sway when in idle not when moving
		if (isIdle)
		{
			// Noise gives us random values based on position
			float SwayRandom = GetSwayNoise();
			// Will always return a random value based on player position and we tone it down with IdleSwayAdjustment
			float SwayRandomAdjusted = SwayRandom * IdleSwayAdjustment; // Adjust sway strength (factor)

			// create time with delta and set two sine values for x and y
			Time += (float)delta * (SwaySpeed + SwayRandom); // Notice how we add Randomization
															 // Create a bit of random sin wave with SwayRandomAdjusted
			// The + and - provide a wave shift for more added randomness
			// The stronger the RandomSwayAmount the less suttle the total sway
			RandomSwayX = (float)Mathf.Sin(Time * 1.5 + SwayRandomAdjusted) / RandomSwayAmount;
			RandomSwayY = (float)Mathf.Sin(Time - SwayRandomAdjusted) / RandomSwayAmount;

			// ref key word allows to pass arguments by reference
			SwayHelper(ref Pos, ref RotDeg, delta, false, RandomSwayX, RandomSwayY, IdleSwayRotationStength);
		}
		else
		{
			SwayHelper(ref Pos, ref RotDeg, delta, true);
		}

		// Set the Weapon position and rotation in degrees
		Position = Pos;
		RotationDegrees = RotDeg;
	}

    private void ApplyWeaponMovement(double delta)
    {
        // Take the current movement weapon profile and apply it specified values
        // over to the procedural weapon system. We call SwayWeapon only if the profile specified
        // IsIdle and call WeaponBob by passing the Bob values.
        SwayWeapon(delta, CurrentWeaponMovementProfie.IsIdle);

        // If bob speed is < 0 then it means that not weapon bob should take place
        if(CurrentWeaponMovementProfie.BobSpeed > 0.0f)
            WeaponBob(
                delta,
                CurrentWeaponMovementProfie.BobSpeed,
                CurrentWeaponMovementProfie.BobH,
                CurrentWeaponMovementProfie.BobV
            );
    }


    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        // Apply procedural weapon sway and bobbing based on the currently loaded movement profile
        // which is specified by each of the movement states. This approach is better since we prevent
        // a potentially large condition tree that specifies each movement state. 
        ApplyWeaponMovement(delta);
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        // Apply FireMode update which will update the FireCooldown. Once it reaches <= 0 then the CurrentWeapon can now fire
        CurrentFireMode?.Update(delta);

    }

    private void OnMovementStateChange(State NextMovementState)
    {
        // Triggered every time a new movement state is loaded. In this case we
        // grab the new state's name and specified weapon profile
        CurrentMovementState = NextMovementState.GetStateName();
        CurrentWeaponMovementProfie = NextMovementState.GetWeaponProfile();
    }
}

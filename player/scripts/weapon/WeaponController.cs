using Godot;
using System;


public partial class WeaponController : Node3D
{
    // Custom signal emmited by the MovementStateMachine that will let the Controller know if the
    // the current player movement state has changed
    [Signal] public delegate void MovementChangedEventHandler(State NewMovementState);
    // Default to the Idle movement state. Only receives the movement state enum
    private Globals.MovementStates CurrentMovementState = Globals.MovementStates.Idle;
    // We initiallize CurrentWeaponMovementProfile to the Idle profile by default since the signal wont be called automatically
    private Globals.WeaponMovementProfle CurrentWeaponMovementProfie = new Globals.WeaponMovementProfle
    {
        IsIdle = true,
        BobSpeed = 0.0f,
        HorizontalBobAmount= 0.0f,
        VerticalBobAmount = 0.0f
    };

    // Array that keeps data files for each available weapon. We make sure to preload them
    // so that they will be ready to go when they get used by the current weapon.
    // Everything about the weapon starts here
    private WeaponResource[] Arsenal =
    {
        GD.Load<WeaponResource>("res://player/assets/weapons/rifle/Rigged_WeaponResource.tres"),
        GD.Load<WeaponResource>("res://player/assets/weapons/pistol/PistolWeaponResource.tres"),
        GD.Load<WeaponResource>("res://player/assets/weapons/sniper/SniperWeaponResource.tres"),
        GD.Load<WeaponResource>("res://player/assets/weapons/shotgun/ShotgunWeaponResource.tres")
    };
    // Indexing the Arsenal array. Already initiallized to load the first weapon at index 0
    private int CurrentWeaponIndex = 0;
    private const int MAX_WEAPON_AMMOUNT = 4;
    // CurrentWeapon holds the Weapon Scene of any of the arsenal weapon resources
    private WeaponBase CurrentWeapon;
    // PrimaryFireMode will hold the current fire mode (full, semi, burst, shotgun) specified by the WeaponResource
    private IFireMode PrimaryFireMode;
    private IFireMode SecondaryFireMode;
    // Need reference to the Camera Controller so that we can pass it over to the Current Weapon Object
    [Export] public CameraController CameraControllerRef;
    // Need reference to the Camera Recoil node so that we can give it a recoil effect
	[Export] public CameraRecoilLayer CameraRecoilRef;
    // Need reference to the WeaponRecoil Node thats under this node so that we can signal it to recoil the weapon back
    [Export] public WeaponRecoil WeaponRecoilRef;
    [Export] public JumpRecoil JumpRecoilRef;
    // Noise Texture is what give the idle sway the randomness
	[Export] private NoiseTexture2D RandSwayNoise;
	// How fast should the random sway be
	[Export] private float RandSwaySpeed = 1.2f;
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
	private float RandIdleSwayX;
    // Random Idle sway for y
	private float RandIdleSwayY;
	private float RandSwayStrength;
    private float PositionSwaySpeed;
    private float RotationSwaySpeed;
    private float MouseInputPositionAmount;
    private float MouseInputRotationAmount;
    private Vector2 MouseSwayMin;
    private Vector2 MouseSwayMax;
    private Vector3 WeaponViewportPos;
    private Vector3 WeaponViewportRot;
    public override void _Ready()
    {
        base._Ready();
        // Subscribe the OnMovementStateChange to the MovementChanged signal
        // The idea is that this is triggered everytime the player movement changes to a new state
        MovementChanged += OnMovementStateChange;
        // Immediately load the specified weapon based on the CurrentWeaponIndex
        LoadWeapon();
    }

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
        if(@event.IsActionPressed("primary_action"))
            PrimaryFireMode?.OnTriggerPressed();

        // PrimaryFireMode will receive this and handle it accordingly 
        if(@event.IsActionReleased("primary_action"))
            PrimaryFireMode?.OnTriggerReleased();
    
        if(@event.IsActionPressed("secondary_action"))
            SecondaryFireMode?.OnTriggerPressed();
        
        if(@event.IsActionReleased("secondary_action"))
            SecondaryFireMode?.OnTriggerReleased();
        

        // FireMode only cares on when the current weapon should shoot. Thus Reload should be kept within the Weapon
        if(@event.IsActionPressed("reload"))
            CurrentWeapon?.Reload();

        // When in input is pressed, no matter where it is, godot will broadcast that input to
        // all implemented _Input() functions throughout the project
        for(int i = 1; i <= MAX_WEAPON_AMMOUNT; i++)
        {   
            // Query to see if what we pressed matches weapon_X
            if(@event.IsActionPressed($"weapon_{i}"))
                TryWeaponSwap(i - 1);
        }
    }

    private void TryWeaponSwap(int ProposedWeapon)
    {
        // If the proposed weapon is already the same as the CurrentWeaponIndex then dont do anything
        if(ProposedWeapon == CurrentWeaponIndex)
            return;
        
        // Perform the swap
        CurrentWeaponIndex = ProposedWeapon;
        SwapWeapon();
    }

    private void SwapWeapon()
    {
        CurrentWeapon?.QueueFree();
        // By this time the CurrentWeaponIndex has already moved to the next weapon
        // thus we only need to call LoadWeapon() without needing to pass it anything else
        LoadWeapon();
    }

    private void LoadWeapon()
    {
        // Ask WeaponFactory to Create the appropriate weapon object based on what the WeaponResource specified
        // Not everything gets setup in the Controller thus we pass its WeaponData and Controller forward 
        CurrentWeapon = WeaponFactory.Create(Arsenal[CurrentWeaponIndex], this);
        if(CurrentWeapon == null)
        {
            GD.PrintErr("CurrentWeapon is null (Invalid Weapon Type)");
            return;
        }

        // Ask FireModeFactory to Create the appropriate firemode object based on what the WeaponResource specified
        PrimaryFireMode = FireModeFactory.Create(Arsenal[CurrentWeaponIndex], CurrentWeapon, Arsenal[CurrentWeaponIndex].PrimaryFireMode);
        if(PrimaryFireMode == null)
        {
            GD.PrintErr("PrimaryFireMode is null (Invalid Fire Mode Type)");
            return;
        }

        SecondaryFireMode = FireModeFactory.Create(Arsenal[CurrentWeaponIndex], CurrentWeapon, Arsenal[CurrentWeaponIndex].SecondaryFireMode);
        if(PrimaryFireMode == null)
        {
            GD.PrintErr("SecondaryFireMode is null (Invalid Fire Mode Type)");
            return;
        }
        
        // Some data off of the WeaponResource stays with the WeaponController while others go to the Current Weapon (e.g. FireRate)
        Position = Arsenal[CurrentWeaponIndex].ViewportPosition;
        RotationDegrees = Arsenal[CurrentWeaponIndex].ViewportRotation;
        Scale = Arsenal[CurrentWeaponIndex].ViewportScale;

        // CurrentWeapon is not the WeaponResource itself like the other code. In this case its now the root of the weapon tree
        // and because of this we cannot rely on it giving correct information. So we pull straight from the Arsenal array
        WeaponViewportPos = Position;
        WeaponViewportRot = RotationDegrees;

        // Need to grab some common swaying data from the weapon data. Since all weapons will bob in a similar way
        // and this procedural sway involves manipulating some nodes then we leave it in the controller
        IdleSwayAdjustment = Arsenal[CurrentWeaponIndex].IdleSwayAdjustment;
        IdleSwayRotationStength = Arsenal[CurrentWeaponIndex].IdleSwayRotationStength;
		RandSwayStrength = Arsenal[CurrentWeaponIndex].RandomSwayAmmount;

        MouseInputPositionAmount = Arsenal[CurrentWeaponIndex].MouseInputPositionOffset;
        MouseInputRotationAmount = Arsenal[CurrentWeaponIndex].MouseInputRotationAmount;

        PositionSwaySpeed = Arsenal[CurrentWeaponIndex].PositionSwaySpeed;
        RotationSwaySpeed = Arsenal[CurrentWeaponIndex].RotationSwaySpeed;

        MouseSwayMin = Arsenal[CurrentWeaponIndex].MouseSwayMin;
        MouseSwayMax = Arsenal[CurrentWeaponIndex].MouseSwayMax;

        // Send over Camera and Weapon recoil values to the respective nodes
        CameraRecoilRef.recoilAmount = Arsenal[CurrentWeaponIndex].CameraRecoilAmount;
        CameraRecoilRef.snapAmount = Arsenal[CurrentWeaponIndex].CameraSnapAmount;
        CameraRecoilRef.speed = Arsenal[CurrentWeaponIndex].CameraRecoverySpeed;

        WeaponRecoilRef.recoilAmount = Arsenal[CurrentWeaponIndex].WeaponRecoilAmount;
        WeaponRecoilRef.snapAmount = Arsenal[CurrentWeaponIndex].WeaponSnapAmount;
        WeaponRecoilRef.speed = Arsenal[CurrentWeaponIndex].WeaponRecoverySpeed;

        // Might want to redesign this in the case you want melee weapons since they dont have reloads

        // Finally insert the Current Weapon scene as a child of the recoil node (for now)
        JumpRecoilRef.AddChild(CurrentWeapon);
        CameraControllerRef.SetCameraReloadLayer(CurrentWeapon.CameraReloadProxy);
    }

    public void WeaponBob(double delta, float BobSpeed, float BobH, float BobV)
	{
		// Time gives us a new value always
		Time += (float)delta;

		// Sin(X/Y * frequency) * amplitude
		BobAmount.X = Mathf.Sin(Time * BobSpeed) * BobH;
		BobAmount.Y = Mathf.Abs(Mathf.Cos(Time * BobSpeed) * BobV);
	}

    private float GetRandNoiseValue()
	{
		// Default fallback if noise isn’t assigned
		if (RandSwayNoise == null || RandSwayNoise.Noise == null)
			return 0.0f;

		Vector3 PlayerPosition = Vector3.Zero;

		// Only access Globals when in play mode. Grab the current players position
		// Only want to do this while in play mode
		if (!Engine.IsEditorHint() && Globals.player != null)
			PlayerPosition = Globals.player.GlobalPosition;

		// Pseudo random value that will be fed into the procedural weapon system
		return RandSwayNoise.Noise.GetNoise2D(PlayerPosition.X, PlayerPosition.Y);
	}

    private  void SwayHelper(ref Vector3 WeaponPos, ref Vector3 WeaponRotDeg, double delta, bool isPlayerMoving, float RandIdleSwayX = 0.0f, float RandIdleSwayY = 0.0f, float RandIdleSwayRotationStength = 0.0f)
	{
		// Lerp weapon Pos based on mouse movement.
		// If MouseMovement is 0 then the only thing left would be the currentWeapon.Position.X/Y - RandomSwayX/Y
		WeaponPos.X = (float)Mathf.Lerp(WeaponPos.X, WeaponViewportPos.X - (MouseMovement.X *
			MouseInputPositionAmount + RandIdleSwayX + (isPlayerMoving ? BobAmount.X : 0.0f)) * delta, PositionSwaySpeed);
		WeaponPos.Y = (float)Mathf.Lerp(WeaponPos.Y, WeaponViewportPos.Y - (MouseMovement.Y *
			MouseInputPositionAmount + RandIdleSwayY + (isPlayerMoving ? BobAmount.Y : 0.0f)) * delta, PositionSwaySpeed);
		// Lerp weapon rotation based on mouse movement
		// Similar concept to position. If MouseMovement.X/Y is 0 then the only thing left would be the
		// CurrentWeapon.Rotation.Y/X +/- RandomSwayY/X * IdleSwayRotationStrength. This is what causes the idle sway
		WeaponRotDeg.Y = (float)Mathf.Lerp(WeaponRotDeg.Y, WeaponViewportRot.Y - (MouseMovement.X *
			MouseInputRotationAmount + (RandIdleSwayY * RandIdleSwayRotationStength)) * delta, RotationSwaySpeed);
		WeaponRotDeg.X = (float)Mathf.Lerp(WeaponRotDeg.X, WeaponViewportRot.X - (MouseMovement.Y *
			MouseInputRotationAmount + (RandIdleSwayX * IdleSwayRotationStength)) * delta, RotationSwaySpeed);   
    }

    public void SwayWeapon(double delta, bool isPlayerIdle)
	{
		// Return to base position if the mouse is not moving
		MouseMovement = MouseMovement.Lerp(Vector2.Zero, (float)(delta * 6.0));

		// Make sure to clamp the sway ammounts 
		MouseMovement = MouseMovement.Clamp(MouseSwayMin, MouseSwayMax);
		Vector3 WeaponPos = Position;
		Vector3 WeaponRotDeg = RotationDegrees;

		// Only play random sway when in idle not when moving
		if (isPlayerIdle)
		{
			// Noise gives us random values based on position
			float RandNoiseValue = GetRandNoiseValue();

			// create time with delta and set two sine values for x and y
			Time += (float)delta * (RandSwaySpeed + RandNoiseValue); // Notice how we add Randomization
															 // Create a bit of random sin wave with AdjustedRandNoiseValue
			// The + and - provide a wave shift for more added randomness
			// The stronger the RandomSwayAmount the less suttle the total sway
            // RandNoiseValue is toned down with IdleSwayAdjustment
			RandIdleSwayX = (float)Mathf.Sin(Time * 1.5 + RandNoiseValue * IdleSwayAdjustment) / RandSwayStrength;
			RandIdleSwayY = (float)Mathf.Sin(Time - RandNoiseValue * IdleSwayAdjustment) / RandSwayStrength;

			// ref key word allows to pass arguments by reference
			SwayHelper(ref WeaponPos, ref WeaponRotDeg, delta, false, RandIdleSwayX, RandIdleSwayY, IdleSwayRotationStength);
		}
		else
		{
			SwayHelper(ref WeaponPos, ref WeaponRotDeg, delta, true);
		}

		// Set the Weapon position and rotation in degrees
		Position = WeaponPos;
		RotationDegrees = WeaponRotDeg;
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
                CurrentWeaponMovementProfie.HorizontalBobAmount,
                CurrentWeaponMovementProfie.VerticalBobAmount
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
        PrimaryFireMode?.Update(delta);
        
        SecondaryFireMode?.Update(delta);

    }

    private void OnMovementStateChange(State NextMovementState)
    {
        // Triggered every time a new movement state is loaded. In this case we
        // grab the new state's name and specified weapon profile
        CurrentMovementState = NextMovementState.GetStateName();
        CurrentWeaponMovementProfie = NextMovementState.GetWeaponProfile();
    }
}

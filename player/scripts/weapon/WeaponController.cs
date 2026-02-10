using Godot;
using System;

public partial class WeaponController : Node3D
{
    // Will let the WeaponController know if the the current player movement state has changed
    [Signal] public delegate void MovementChangedEventHandler(State NewMovementState);
    private Globals.MovementStates CurrentMovementState = Globals.MovementStates.Idle;
    // We initiallize CurrentWeaponMovementProfile to the Idle profile by default since the signal wont be called automatically
    private Globals.WeaponMovementProfle CurrentWeaponMovementProfile = new Globals.WeaponMovementProfle
    {
        IsIdle = true,
        BobSpeed = 0.0f,
        HorizontalBobAmount= 0.0f,
        VerticalBobAmount = 0.0f
    };

    // Everything about the weapon starts here
    private WeaponResource[] Arsenal =
    {
        GD.Load<WeaponResource>("res://player/assets/weapons/rifle/Rigged_WeaponResource.tres"),
        GD.Load<WeaponResource>("res://player/assets/weapons/pistol/PistolWeaponResource.tres"),
        GD.Load<WeaponResource>("res://player/assets/weapons/sniper/SniperWeaponResource.tres"),
        GD.Load<WeaponResource>("res://player/assets/weapons/burstRifle/burstRifle.tres")
    }; 
    private int CurrentWeaponIndex = 0;
    private const int MAX_WEAPON_AMMOUNT = 4;
    private WeaponBase CurrentWeapon;
    private IWeaponAction CurrentPrimaryWeaponAction;
    private IWeaponAction CurrentSecondaryWeaponAction;
    [Export] public CameraController CameraControllerRef;
	[Export] public CameraRecoilLayer CameraRecoilRef;
    [Export] public CameraZoomLayer CameraZoomLayerRef;
    [Export] public WeaponRecoil WeaponRecoilRef;
    [Export] public JumpRecoil JumpRecoilRef;
	[Export] private NoiseTexture2D RandSwayNoise;
	private float IdleSwaySpeed = 1.2f;
	private Vector2 MouseMovement = Vector2.Zero;
	private Vector2 BobAmount = Vector2.Zero;
	private float Time = 0.0f;
	private float IdleSwayAdjustment;
	private float IdleSwayRotationStength;
	private float IdleSwayX;
	private float IdleSwayY;
	private float IdleSwayStrength;
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
        MovementChanged += OnMovementStateChange;
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

        // In _Input, the event actions are not polled and are only triggered once everytime the key is pressed 
        if(@event.IsActionPressed("primary_action"))
            CurrentPrimaryWeaponAction?.OnActionPressed();

        if(@event.IsActionReleased("primary_action"))
            CurrentPrimaryWeaponAction?.OnActionReleased();
    
        if(@event.IsActionPressed("secondary_action"))
            CurrentSecondaryWeaponAction?.OnActionPressed();
        
        if(@event.IsActionReleased("secondary_action"))
            CurrentSecondaryWeaponAction?.OnActionReleased();
        
        // WeaponAction only cares on when the current weapon should shoot.
        if(@event.IsActionPressed("reload"))
            CurrentWeapon?.Reload();

        // When in input is pressed, no matter where it is, godot will broadcast that input to
        // all implemented _Input() functions throughout the project
        for(int i = 1; i <= MAX_WEAPON_AMMOUNT; i++)
        {   
            if(@event.IsActionPressed($"weapon_{i}"))
                TryWeaponSwap(i - 1);
        }
    }

    private void TryWeaponSwap(int ProposedWeapon)
    {
        // If the proposed weapon is already the same as the CurrentWeaponIndex then dont do anything
        if(ProposedWeapon == CurrentWeaponIndex)
            return;
    
        CurrentWeaponIndex = ProposedWeapon;
        SwapWeapon();
    }

    private void SwapWeapon()
    {
        CurrentWeapon?.QueueFree();
        // By this time the CurrentWeaponIndex has already moved to the next weapon
        LoadWeapon();
    }

    private void ParseWeaponResource(in WeaponResource weaponResource)
    {
        Position = weaponResource.ViewportPosition;
        RotationDegrees = weaponResource.ViewportRotation;
        Scale = weaponResource.ViewportScale;

        WeaponViewportPos = Position;
        WeaponViewportRot = RotationDegrees;

        IdleSwayAdjustment = weaponResource.IdleSwayAdjustment;
        IdleSwayRotationStength = weaponResource.IdleSwayRotationStength;
		IdleSwayStrength = weaponResource.IdleSwayAmmount;
        IdleSwaySpeed = weaponResource.IdleSwaySpeed;

        MouseInputPositionAmount = weaponResource.MouseInputPositionOffset;
        MouseInputRotationAmount = weaponResource.MouseInputRotationAmount;

        PositionSwaySpeed = weaponResource.PositionSwaySpeed;
        RotationSwaySpeed = weaponResource.RotationSwaySpeed;

        MouseSwayMin = weaponResource.MouseSwayMin;
        MouseSwayMax = weaponResource.MouseSwayMax;

        CameraRecoilRef.recoilAmount = weaponResource.CameraRecoilAmount;
        CameraRecoilRef.snapAmount = weaponResource.CameraSnapAmount;
        CameraRecoilRef.speed = weaponResource.CameraRecoverySpeed;

        WeaponRecoilRef.recoilAmount = weaponResource.WeaponRecoilAmount;
        WeaponRecoilRef.snapAmount = weaponResource.WeaponSnapAmount;
        WeaponRecoilRef.speed = weaponResource.WeaponRecoverySpeed;
    }

    private void UpdateCurrentWeapon()
    {
        CurrentWeapon = WeaponFactory.Create(Arsenal[CurrentWeaponIndex], this);
        if(CurrentWeapon == null)
        {
            GD.PrintErr("CurrentWeapon is null (Invalid Weapon Type)");
            return;
        }
    }

    private void UpdateCurrentWeaponActions()
    {
        // Ask FireModeFactory to Create the appropriate firemode object based on what the WeaponResource specified
        if(Arsenal[CurrentWeaponIndex].PrimaryWeaponAction == Globals.WeaponActions.NoAction)
            return;
    
        CurrentPrimaryWeaponAction = FireModeFactory.CreateNewWeaponAction(this, Arsenal[CurrentWeaponIndex], CurrentWeapon, Arsenal[CurrentWeaponIndex].PrimaryWeaponAction);
        if(CurrentPrimaryWeaponAction == null)
        {
            GD.PrintErr("PrimaryFireMode is null (Invalid Fire Mode Type)");
        }

        if(Arsenal[CurrentWeaponIndex].SecondaryWeaponAction == Globals.WeaponActions.NoAction)
            return;

        CurrentSecondaryWeaponAction = FireModeFactory.CreateNewWeaponAction(this, Arsenal[CurrentWeaponIndex], CurrentWeapon, Arsenal[CurrentWeaponIndex].SecondaryWeaponAction);
        if(CurrentSecondaryWeaponAction == null )
        {
            GD.PrintErr("SecondaryFireMode is null (Invalid Fire Mode Type)");
        } 
    }

    private void LoadWeapon()
    {
        UpdateCurrentWeapon();
        UpdateCurrentWeaponActions();
        ParseWeaponResource(in Arsenal[CurrentWeaponIndex]);
        JumpRecoilRef.AddChild(CurrentWeapon);
        CameraControllerRef.SetCameraReloadLayer(CurrentWeapon.CameraReloadProxy);
    }

    public void CalculateWeaponBob(double delta)
	{
		// Time gives us a new value always
		Time += (float)delta;

		// Sin(X/Y * frequency) * amplitude
		BobAmount.X = Mathf.Sin(Time * CurrentWeaponMovementProfile.BobSpeed) * CurrentWeaponMovementProfile.HorizontalBobAmount;
		BobAmount.Y = Mathf.Abs(Mathf.Cos(Time * CurrentWeaponMovementProfile.BobSpeed) * CurrentWeaponMovementProfile.VerticalBobAmount);
	}

    private float GetRandNoiseValue()
	{
		if (RandSwayNoise == null || RandSwayNoise.Noise == null)
			return 0.0f;

		Vector3 PlayerPosition = Vector3.Zero;

		// Only access Globals when in play mode. 
		if (!Engine.IsEditorHint() && Globals.player != null)
			PlayerPosition = Globals.player.GlobalPosition;

		return RandSwayNoise.Noise.GetNoise2D(PlayerPosition.X, PlayerPosition.Y);
	}

    private void InterpolateMouseMovement(double delta)
    {
		MouseMovement = MouseMovement.Lerp(Vector2.Zero, (float)(delta * 6.0));
		MouseMovement = MouseMovement.Clamp(MouseSwayMin, MouseSwayMax); 
    }

    private void CalculateWeaponSway(ref Vector3 WeaponPos, ref Vector3 WeaponRotDeg, bool PlayerIsIdle, double delta)
    {
        // Only play random sway when in idle not when moving
		if (PlayerIsIdle)
		{
			float RandNoiseValue = GetRandNoiseValue();

			// create time with delta and set two sine values for x and y
			Time += (float)delta * (IdleSwaySpeed + RandNoiseValue); // Notice how we add Randomization
															 // Create a bit of random sin wave with AdjustedRandNoiseValue
			// The + and - provide a wave shift for more added randomness
			// The stronger the RandomSwayAmount the less suttle the total sway
            // RandNoiseValue is toned down with IdleSwayAdjustment
			IdleSwayX = (float)Mathf.Sin(Time * 1.5 + RandNoiseValue * IdleSwayAdjustment) / IdleSwayStrength;
			IdleSwayY = (float)Mathf.Sin(Time - RandNoiseValue * IdleSwayAdjustment) / IdleSwayStrength;
		}

        // Lerp weapon Pos based on mouse movement.
		// If MouseMovement is 0 then the only thing left would be the currentWeapon.Position.X/Y - RandomSwayX/Y
		WeaponPos.X = (float)Mathf.Lerp(WeaponPos.X, WeaponViewportPos.X - (MouseMovement.X *
			MouseInputPositionAmount + IdleSwayX + (!PlayerIsIdle ? BobAmount.X : 0.0f)) * delta, PositionSwaySpeed);
		WeaponPos.Y = (float)Mathf.Lerp(WeaponPos.Y, WeaponViewportPos.Y - (MouseMovement.Y *
			MouseInputPositionAmount + IdleSwayY + (!PlayerIsIdle ? BobAmount.Y : 0.0f)) * delta, PositionSwaySpeed);
		// Lerp weapon rotation based on mouse movement
		// Similar concept to position. If MouseMovement.X/Y is 0 then the only thing left would be the
		// CurrentWeapon.Rotation.Y/X +/- RandomSwayY/X * IdleSwayRotationStrength. This is what causes the idle sway
		WeaponRotDeg.Y = (float)Mathf.Lerp(WeaponRotDeg.Y, WeaponViewportRot.Y - (MouseMovement.X *
			MouseInputRotationAmount + (IdleSwayY * IdleSwayRotationStength)) * delta, RotationSwaySpeed);
		WeaponRotDeg.X = (float)Mathf.Lerp(WeaponRotDeg.X, WeaponViewportRot.X - (MouseMovement.Y *
			MouseInputRotationAmount + (IdleSwayX * IdleSwayRotationStength)) * delta, RotationSwaySpeed); 

    }
    public void ApplyWeaponSway(double delta, bool isPlayerIdle)
	{
        // Must constantly set the captured mouse movement back to base origin
		InterpolateMouseMovement(delta);

		Vector3 WeaponPos = Position;
		Vector3 WeaponRotDeg = RotationDegrees;

		CalculateWeaponSway(ref WeaponPos, ref WeaponRotDeg, isPlayerIdle, delta);

		Position = WeaponPos;
		RotationDegrees = WeaponRotDeg;
	}

    private void ApplyProceduralWeaponMovement(double delta)
    {
        ApplyWeaponSway(delta, CurrentWeaponMovementProfile.IsIdle);

        // If bob speed is < 0 then it means that not weapon bob should take place
        if(CurrentWeaponMovementProfile.BobSpeed > 0.0f)
            CalculateWeaponBob(delta);
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
         
        ApplyProceduralWeaponMovement(delta);
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        CurrentPrimaryWeaponAction?.Update(delta);
        CurrentSecondaryWeaponAction?.Update(delta);
    }

    // Triggered every movement state change
    private void OnMovementStateChange(State NextMovementState)
    {
        CurrentMovementState = NextMovementState.GetStateName();
        CurrentWeaponMovementProfile = NextMovementState.GetWeaponProfile();
    }
}

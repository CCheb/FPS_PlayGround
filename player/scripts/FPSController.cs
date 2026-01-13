using Godot;
using System;

public partial class FPSController : CharacterBody3D
{
    /* Player stats */
	//-------------------------------------------
	[ExportGroup("Player Stats")]
	[Export]public float speed = 6.0f;
	
	/* Mouse parameters */
	//-------------------------------------------
	[ExportGroup("Mouse Parameters")]
	[Export] public float MouseSensitivity = 0.1f;
	// How far down we can look
	[Export] public float TiltLowerLimit { get; set; } = Mathf.DegToRad(-90.0f);
	// How far up we can look
	[Export] public float TiltUpperLimit { get; set; } = Mathf.DegToRad(90.0f);
	/* Camera settings */
	//-------------------------------------------
	[ExportGroup("Camera Settings")]
	// Camera controller that we will manipulate in script
	[Export] public Camera3D WORLDCAMERA { get; set; }
	[Export] private InputLayer InputCameraLayer;
	[Export] public float DefaultFov = 120.0f;
	// Help detect if mouse is moving
	private bool mouseInput = false;
	// unsanitized mouse rotaition
	private Vector3 mouseRotation;
	// Total Y rotation since last frame
	private float rotationInput;
	// Total X rotation since last frame
	private float tiltInput;
	// Horizontal player rotation
	private Vector3 playerRotation;
	// vertical camera rotation
	private Vector3 cameraRotation;
	// Used by sliding state
	public float _currentRotation;


	/* PLAYER API */
	//------------------------------------------
	[ExportGroup("Player API")]
	// Animation player node
	[Export] public AnimationPlayer ANIMATION;
	// Sphere shapecast above the player
	[Export] public  ShapeCast3D crouchShapeCast;
	// Reference so that the movement states are able to access the WeaponController
	// Player acts as the middle man between the movement and weapon states
	[Export] public WeaponController WEAPON;


	public override void _Ready()
	{
		base._Ready();

		// Make this script globally accessible
		Globals.player = this;
	
		// Set the mouse to capture mode off rip. This will capture the mouse
		// to be at the center of the screen. We then want the player and camera to rotate with it
		Input.MouseMode = Input.MouseModeEnum.Captured;
		// Set the camera fov accordingly
		WORLDCAMERA.Fov = DefaultFov;
		// Added shapecast exception. We want the shapecast to ignore ourselfs. Couls have done this with layers
		crouchShapeCast.AddException(this);
	}

	// _Input > UI > _UnhandledInput. We use _UnhandledInput here since we dont want any mouse movement
	// when we focus on a UI, menu, or button.
	public override void _UnhandledInput(InputEvent @event)
	{
		base._UnhandledInput(@event);

		
		// Determine if the mouse is captured and is moving
		mouseInput = (@event is InputEventMouseMotion) && (Input.MouseMode == Input.MouseModeEnum.Captured);

		// Screen space to world space
		if (mouseInput)
		{
			// Grab the MouseMotionEvent 
			InputEventMouseMotion motionEvent = (InputEventMouseMotion)@event;
			// Converting mouse movement into radians that we will pass over to the player and camera
			// In this case we are grabbing the total ammount the mouse moved since the last frame
			// This is cornverting to radians per pixel (MouseSensitivity). From here we decide to use radians or degrees

			// How much has the mouse moved in the last frame. Convert that into rad / pixel

			// Its important that we negate these values because turning right in screen space is + but in world space will
			// be negative. Thats why we take the screen space rotation and negate it over to world space rotation 
			rotationInput = -motionEvent.Relative.X * MouseSensitivity;
			tiltInput = -motionEvent.Relative.Y * MouseSensitivity;

			//GD.Print($"({rotationInput}, {tiltInput})");
		}

        
	}

	private void UpdateCamera(double delta)
	{
		// Grab the current side to side rotation. This will be used by the sliding state
		// to determine the tilt ammount
		_currentRotation = rotationInput;

		// Horizontal rotation. The Vertical rotation is strictly for the camera and we send those values over to it
		mouseRotation.Y += rotationInput * (float)delta;

		// Form vectors to be applied to the player and camera rotations respectively
		// If we look horizontally we want the player to rotate which will rotate the camera with it since its a child
		playerRotation = new Vector3(0.0f, mouseRotation.Y, 0.0f);

		// We only want the camera to pitch up and down
		//cameraRotation = new Vector3(mouseRotation.X, 0.0f, 0.0f);

		// Player rotation, want horizontal rotation
		Basis = Basis.FromEuler(playerRotation);

		// Send the pitch values over to the InputCameraLayer. In this case the player owns side ways rotation while
		// the camera controller handles pitch which is the only rotation applied to the camera
		InputCameraLayer.AddPitch(tiltInput * (float)delta);

		// Dont want previous frame rotation inputs to affect the current frame accidentally so we set them to 0.0f since the specifically serve as deltas
		rotationInput = 0.0f;
		tiltInput = 0.0f;

	}

	public override void _Input(InputEvent @event)
	{
		base._Input(@event);
		// Helper input for exiting the game more easily
		if (@event.IsActionPressed("exit"))
			GetTree().Quit();
	}

	public override void _Process(double delta)
	{
		base._Process(delta);
		// Its essential to place camera movement in process that way we get snappy and precise input as compared to _PhysicsProcess
		UpdateCamera(delta);
		
	}

	// Callable by our state scripts. The idea is that they can call these functions and customize speed properties
	public void UpdateGravity(double delta)
	{
		// Only add gravity when in the air
		if (!IsOnFloor())
		{	
			// Its essential to only update the players Velocity and keep vel local 
			Vector3 velocity = Velocity;
			velocity += GetGravity() * (float)delta;
			Velocity = velocity;
		}
	}

	public void UpdateInput(float speed, float acceleration, float deceleration)
	{
		//Globals.debug.AddDebugProperty("Movement", speed, 0, new Color(1, 0, 0));

		Vector3 velocity = Velocity;
		// Get the input direction and handle the movement/deceleration.
		Vector2 inputDir = Input.GetVector("move_left", "move_right", "move_forward", "move_backward");
		// want the direction to always be in terms of the local characters coordinate/basis. If we rotate
		// and move forward the input will rotate the same amount and move forward
		Vector3 direction = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();
		if (direction != Vector3.Zero)
		{
			// You will reach the maximum speed over time (linearly) and not instantly
			// Its important for the first argument to be changing or else it will
			// never move to its intended target speed. The current velocity will always be changing
			velocity.X = Mathf.Lerp(velocity.X, direction.X * speed, acceleration);
			velocity.Z = Mathf.Lerp(velocity.Z, direction.Z * speed, deceleration);
		}
		else
		{
			// MoveToward is like Lerp in that it provides movement smoothing. In this case
			// when the player stops moving it will smoothing approach 0. We provide 
			// a decelaration weight so in the case we want the player to decelerate at a different
			// speed when compared to acceleration 
			velocity.X = Mathf.MoveToward(Velocity.X, 0, deceleration);
			velocity.Z = Mathf.MoveToward(Velocity.Z, 0, deceleration);
		}

		Velocity = velocity;
	}
	
	public void UpdateVelocity()
	{
		//Globals.debug.AddDebugProperty("Velocity", Velocity, 0, new Color(0, 1, 0));
		// Before we updated velocity here by doing Velocity = velocity for the sake of keeping
		// a global private velocity. That then caused the jump velocity to be cancelled since this
		// velocity did not know of any jumps (Y = 0) and overwrote the jump velocity
		MoveAndSlide();
		
	}

	




	
	

}

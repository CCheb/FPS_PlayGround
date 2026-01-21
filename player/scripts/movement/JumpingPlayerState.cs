using Godot;
using System;

public partial class JumpingPlayerState : PlayerMovementState
{
	// Jumping specific movement variables. Used in player's UpdateInput()
	[Export] public float acceleration = 0.1f;
	[Export] public float decelaration = 0.25f;
	// How high the player can jump
	[Export] public float jumpVelocity = 4.5f;
	[Export] public float doubleJumpVelocity = 5.5f;
	private bool doubleJump = false;
	// How strong the players input should be mid-air
	[Export(PropertyHint.Range, "0.5, 1.0, 0.01")] public float inputMultiplier = 0.85f;
	[Export] public float speedAddOn = 0.0f;
	private float speed = 6.0f;

    public override void Init()
    {
		StateName = Globals.MovementStates.Jump;
		MovementProfle = default;
    }

	public override void Enter(State prevState)
	{
		//GD.Print("Entered Jumping state");
		base.Enter(prevState);
		// Need to pass true player velocity over to local velocity var, change it and pass it back
		Vector3 velocity = PLAYER.Velocity;
		velocity.Y = jumpVelocity;
		PLAYER.Velocity = velocity;
		// We play a custom animation here
		ANIMATION.Play("JumpStart");
		speed = PLAYER.speed + speedAddOn;
	}

	public override void Exit()
	{
		// Make sure to reset doubleJump back to false if this is not the
		// first time we have not entered the jump state
		base.Exit();
		doubleJump = false;

		WEAPON_CONTROLLER.JumpRecoilRef.EmitSignal("AddJumpRecoil");
		CAMERA_CONTROLLER.CameraJumpingLayer.EmitSignal("AddJumpRecoil");
		
	}

	public override void Update(double delta)
	{
		base.Update(delta);

		// Update player movment accordingly
		PLAYER.UpdateGravity(delta);
		// Small change to passed velocity to limit the player movement
		PLAYER.UpdateInput(speed * inputMultiplier, acceleration, decelaration);
		PLAYER.UpdateVelocity();

		//WEAPON.SwayWeapon(delta, false);

		// Enables a second jump mid-air
		if (Input.IsActionJustPressed("jump") && !doubleJump)
		{
			Vector3 velocity = PLAYER.Velocity;
			velocity.Y = doubleJumpVelocity;
			PLAYER.Velocity = velocity;
			doubleJump = true;
		}

		if(Input.IsActionJustReleased("jump"))
		{
			if(PLAYER.Velocity.Y > 0)
			{
				Vector3 velocity = PLAYER.Velocity;
				velocity.Y /= 2.0f;
				PLAYER.Velocity = velocity;
				
			}
		}
		// Once we land on the floor then transition back to Idle state and from there
		// transition into other states
		if (PLAYER.IsOnFloor())
		{
			ANIMATION.Play("JumpEnd");
			EmitSignal(SignalName.Transition, "IdlePlayerState");
		}

	}
}

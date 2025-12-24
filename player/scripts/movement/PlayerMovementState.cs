using Godot;
using System;

public partial class PlayerMovementState : State
{
	// Want the movement states to access common objects such as player
    // animation, camera and weapon properties
    protected FPSController PLAYER;
    protected AnimationPlayer ANIMATION;
    protected Camera3D CAMERA;
    public WeaponController WEAPON;

    public override async void _Ready()
    {
        base._Ready();
        // Wait for the root node to be ready before continuing
		// Its essential that StateMachine calls the parents (this) ready function to initialize the variables
        await ToSignal(Owner, "ready");
        // PLAYER API
        PLAYER = (FPSController)Owner;
        ANIMATION = PLAYER.ANIMATION;
        CAMERA = PLAYER.WORLDCAMERA;
        WEAPON = PLAYER.WEAPON;

    }
}

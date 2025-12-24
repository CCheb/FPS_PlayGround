using Godot;
using System;
using System.Resources;

public partial class Globals : Node
{
	// Reference to player conponent. This will act as our singleton
    // Its static here since we dont want to create a Globals object
    static public FPSController player { get; set; }

    public enum WeaponTypes
    {
        Hitscan,
        Projectile,
        Shotgun,
        Beam
    }

    public enum FireModes
    {
        FullAuto,
        SemiAuto,
        Shotgun,
        BurstFire,
    }

    public enum MovementStates
    {
        Idle,
        Walk,
        Sprint,
        Slide,
        Crouch,
        Jump,
        Fall
    }

    public struct WeaponMovementProfle
    {
        public bool IsIdle;

        public float BobSpeed;
        public float BobH;
        public float BobV;
    }


}

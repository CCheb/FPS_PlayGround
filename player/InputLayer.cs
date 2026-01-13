using Godot;
using System;

public partial class InputLayer : CameraLayer
{
    /*
    Player input is split into two parts: horizontal and vertical rotatation.
    Horizontal is handled by the player and it essentially rotates the player itself.
    Vertical recoil strickly rotates the camera up and down and that whats handled in the InputLayer
    */

    // Since this is vertical rotation we need to set some limits on how far up and down the player rotates. 
    // In this case they can look directly up and down
    [Export] public float TiltLowerLimit = Mathf.DegToRad(-90f);
    [Export] public float TiltUpperLimit = Mathf.DegToRad(90f);

    // _pitch the internal variable that changes everytime the player looks around
    private float _pitch;

    // RotationOffset serves as the unifed property that all CameraLayers must initialize/use for the CameraController
    // In this case we create a vector with the pitch value to represent only up and down rotation (X-axis).
    // From the perspective of the CameraController we simply apply (not add) the rotation offset 
    public override Vector3 RotationOffset => new Vector3(_pitch, 0f, 0f);

    // AddPitch gets called everytime the player rotates/looks around
    public void AddPitch(float deltaPitch)
    {
        // We add the new rotation to the current base one
        // and make sure to clamp the value in the case it went overboard
        _pitch += deltaPitch;
        _pitch = Mathf.Clamp(_pitch, TiltLowerLimit, TiltUpperLimit);
    }
}

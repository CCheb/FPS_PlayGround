using Godot;
using System;

public partial class InputLayer : CameraLayer
{
    [Export] public float TiltLowerLimit = Mathf.DegToRad(-90f);
    [Export] public float TiltUpperLimit = Mathf.DegToRad(90f);

    private float _pitch;

    public override Vector3 RotationOffset => new Vector3(_pitch, 0f, 0f);

    public void AddPitch(float deltaPitch)
    {
        _pitch += deltaPitch;
        _pitch = Mathf.Clamp(_pitch, TiltLowerLimit, TiltUpperLimit);
    }
}

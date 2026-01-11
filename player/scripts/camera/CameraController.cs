using Godot;
using System;
using System.ComponentModel;

public partial class CameraController : Node3D
{   
    [Export] private Vector3 BaseOffset = new Vector3(0.0f, 1.456f, 0.0f);
    [Export] private CameraLayer InputCameraLayer;
    [Export] private CameraLayer MovementLayer;
    [Export] private CameraLayer CameraRecoilLayer;
    [Export] private CameraLayer CameraReload;
    
    public override void _Process(double delta)
    {
        base._Process(delta);
        // Its essential that we reset these values every frame
        Vector3 finalPosition = BaseOffset;
        Vector3 finalRotation = Vector3.Zero;

        if(InputCameraLayer != null)
        {
            finalRotation = InputCameraLayer.RotationOffset;
        }

        if(MovementLayer != null)
        {
            finalPosition += MovementLayer.PositionOffset;
            finalRotation += MovementLayer.RotationOffset;
        }

        if(CameraRecoilLayer != null)
        {
            finalRotation += CameraRecoilLayer.RotationOffset;
        }

        if(CameraReload != null)
        {
            finalRotation += CameraReload.RotationOffset;
        }
        

        // If applied to itself the camera will follow. It must stay dumb
        Position = finalPosition;
        Rotation = finalRotation;
    }

    public void SetCameraReloadLayer(Node3D Proxy)
    {
        if(CameraReload is CameraReloadLayer RL)
        {
           RL.SetProxy(Proxy);
        }
    }
}

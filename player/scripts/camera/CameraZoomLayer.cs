using Godot;
using System;
using System.Runtime.CompilerServices;

public partial class CameraZoomLayer : Node
{
    [Signal] public delegate void AddCameraZoomEventHandler();
    [Signal] public delegate void RemoveCameraZoomEventHandler();
    
    public float currentFov = 90.0f;
    public float targetFov = 90.0f;

    public override void _Ready()
    {
        base._Ready();

        AddCameraZoom += AddZoom;
        RemoveCameraZoom += RemoveZoom;
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        currentFov = Mathf.Lerp(currentFov, targetFov, 5.0f*(float)delta);
    }

    private void AddZoom()
    {
        targetFov = 50.0f;
    }

    private void RemoveZoom()
    {
        targetFov = 90.0f;
    }
}

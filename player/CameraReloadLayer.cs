using Godot;
using System;

public partial class CameraReloadLayer : CameraLayer
{
    public Node3D ReloadProxy;

    private Transform3D _neutralTransform;

    public void SetProxy(Node3D proxy)
    {
        ReloadProxy = proxy;

        if (ReloadProxy != null)
        {
            _neutralTransform = ReloadProxy.Transform;
        }
    }

    public override Vector3 PositionOffset
    {
        get
        {
            if (ReloadProxy == null)
                return Vector3.Zero;

            return ReloadProxy.Transform.Origin - _neutralTransform.Origin;
        }
    }

    public override Vector3 RotationOffset
    {
        get
        {
            if (ReloadProxy == null)
            {
                GD.Print("Enpty");
                return Vector3.Zero;
            }

            // delta rotation relative to neutral
            Basis deltaBasis =
                _neutralTransform.Basis.Inverse() * ReloadProxy.Transform.Basis;

            return deltaBasis.GetEuler();
        }
    }
}

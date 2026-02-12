using Godot;
using System;

public partial class ZoomWeaponAction : IWeaponAction
{
    private readonly WeaponController weaponController;

    private bool isHeld = false;
    public ZoomWeaponAction(WeaponController weaponController)
    {
        this.weaponController = weaponController;
    }
    public void OnActionPressed()
    {  
        if(!isHeld)
            //weaponController.CameraZoomLayerRef.EmitSignal("AddCameraZoom");
            weaponController.CameraControllerRef.RequestCameraZoom();
            
        isHeld = true;
    }
    public void OnActionReleased()
    {   
        if(isHeld)
            //weaponController.CameraZoomLayerRef.EmitSignal("RemoveCameraZoom");
            weaponController.CameraControllerRef.RequestDeCameraZoom();

        isHeld = false;
    } 

    public void Update(double delta) { }
}

using Godot;

public partial class PlayerRotationHandler : Node
{
    [Export] public required Node3D MeshRoot { get; set; }
    [Export] public bool IsJoystickControls { get; set; } = false;
    [Export] public float JoystickSensitivity { get; set; } = 3.0f;
    [Export] public float MinTargetDistance { get; set; } = 0.1f;

    public override void _Ready()
    {
        Log.Debug("PlayerRotationHandler ready");
        Input.MouseMode = Input.MouseModeEnum.Visible; // or Confined if you want it window-locked
    }

    public override void _Process(double delta)
    {
        if (IsJoystickControls)
        {
            HandleJoystick((float)delta);
        }
        else
        {
            HandleMouse();
        }
    }

    private void HandleMouse()
    {
        Camera3D camera = GetViewport().GetCamera3D();
        if (camera == null) return;

        Vector2 mousePos = GetViewport().GetMousePosition();
        Vector3 rayOrigin = camera.ProjectRayOrigin(mousePos);
        Vector3 rayDir = camera.ProjectRayNormal(mousePos);

        if (Mathf.IsZeroApprox(rayDir.Y)) return; // ray parallel to ground, no valid hit

        float planeY = MeshRoot.GlobalPosition.Y;
        float t = (planeY - rayOrigin.Y) / rayDir.Y;
        if (t < 0) return; // plane is behind the camera ray

        Vector3 hitPoint = rayOrigin + rayDir * t;
        FaceWorldPoint(hitPoint);
    }

    private void HandleJoystick(float delta)
    {
        Vector2 stick = Input.GetVector("joy_camera_left", "joy_camera_right", "joy_camera_up", "joy_camera_down");
        if (stick.Length() < 0.2f) return; // deadzone

        float targetAngle = Mathf.Atan2(stick.X, stick.Y);
        Vector3 rot = MeshRoot.Rotation;
        rot.Y = Mathf.LerpAngle(rot.Y, targetAngle, JoystickSensitivity * delta);
        MeshRoot.Rotation = rot;
    }

    private void FaceWorldPoint(Vector3 worldPoint)
    {
        Vector3 toTarget = worldPoint - MeshRoot.GlobalPosition;
        toTarget.Y = 0;
        if (toTarget.Length() < MinTargetDistance) return;

        Vector3 rot = MeshRoot.Rotation;
        rot.Y = Mathf.Atan2(toTarget.X, toTarget.Z);
        MeshRoot.Rotation = rot;
    }
}
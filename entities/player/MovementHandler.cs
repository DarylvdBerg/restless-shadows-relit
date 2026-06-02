using Godot;
using System;
using System.Numerics;

public partial class MovementHandler : Node
{
    [Export]
    public required VelocityComponent VelocityComponent { get; set; }

    [Export]
    public bool IsJoystickControls { get; set; } = false;

    [Export]
    public bool IsThirdPerson { get; set; } = true;

    public override void _Ready()
    {
        Log.Debug("MovementHandler ready");
        Log.Debug("Control scheme: "+ (IsJoystickControls ? "Joystick" : IsThirdPerson ? "Third Person" : "First Person"));
    }


    public override void _PhysicsProcess(double delta)
    {
        var input = IsJoystickControls ? GetJoystickInput() : 
            IsThirdPerson ? GetThirdPersonInput() :
            GetFirstPersonInput();

        VelocityComponent.Move(new Godot.Vector3(input.X, 0, input.Y));
    }


    private Godot.Vector2 GetThirdPersonInput() => Input.GetVector("move_left", "move_right", "move_up", "move_down");
    private Godot.Vector2 GetFirstPersonInput() => Input.GetVector("move_left", "move_right", "move_forward", "move_back");
    private Godot.Vector2 GetJoystickInput() => new Godot.Vector2(Input.GetActionStrength("joy_move_right") - Input.GetActionStrength("joy_move_left"), Input.GetActionStrength("joy_move_forward") - Input.GetActionStrength("joy_move_backward"));
}

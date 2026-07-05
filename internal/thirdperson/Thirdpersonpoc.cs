using Godot;
using System;

public partial class Thirdpersonpoc : Node3D
{

    private Area3D _detectionArea;
    
    public override void _Ready()
    {
        _detectionArea = GetNode<Area3D>("Area3D") ?? 
            throw new ArgumentException("DetectionArea not found");

        _detectionArea.BodyEntered += _OnBodyEntered;
    }

    private void _OnBodyEntered(Node3D body)
    {
        Log.Debug($"Body entered detection area: {body.Name}");
    }

}

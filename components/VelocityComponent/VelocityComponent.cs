using Godot;
using System;

public partial class VelocityComponent : Node
{
    
    /// <summary>
    /// The actual body we'll need to move.
    /// </summary>
    [Export]
    public CharacterBody3D Body { get; set; }

    [Export]
    public float Speed { get; set; }


    public override void _Ready()
    {
        Body = GetParent<CharacterBody3D>() 
            ?? throw new ArgumentNullException("Character body 3d needs to be set. Make sure the component is directly under the body.");
    }

    /// <summary>
    /// Move the CharacterBody3D every physics frame given direction and configured speed.
    /// The direction will be normalized here.
    /// Call this method only in the owner's _PhysicsProces.
    /// </summary>
    public void Move(Vector3 direction)
    {
        // If we normalize the direction without a guard, and it's below zero, fun things will happen!
        if(direction.LengthSquared() > 0)
        {
            direction = direction.Normalized();
        }

        Body.Velocity = direction * Speed;
        Body.MoveAndSlide();
    }
}

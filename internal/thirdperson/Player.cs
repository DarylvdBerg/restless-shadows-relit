using Godot;
using System;

public partial class Player : CharacterBody3D
{
    private VelocityComponent _velocityComponent;
    
    [Export]
    private AnimationPlayer animationPlayer;

    [Export]
    public float RotationSpeed { get; set; } = 5f;


    public override void _Ready()
    {
        _velocityComponent = GetNode<VelocityComponent>("VelocityComponent");
        if(_velocityComponent is null)
        {
            throw new InvalidOperationException("VelocityComponent not found in Player node.");
        }

        if(animationPlayer is null)
        {
            throw new InvalidOperationException("AnimationPlayer not set in Player node.");
        }
    }

    
    public override void _PhysicsProcess(double delta)
    {
        AnimationStateMachine();
    }
    
    private void AnimationStateMachine()
    {
        var horizontalVelocity = new Vector2(Velocity.X, Velocity.Z);
        var isMoving = horizontalVelocity.Length() > 0;

        if(isMoving)
        {
            animationPlayer.Play("player/Root_Run");
        }
        else
        {
            animationPlayer.Play("player/Root_Idle");
        }
    }
}

using Godot;
using System;

public partial class Enemy : CharacterBody3D
{
    [Export]
    public AnimationPlayer animationPlayer;
    
    private VelocityComponent _velocityComponent;
    private Area3D _detectionArea;
    private Player _player;
    private bool _playerInRange = false;

    public override void _Ready()
    {
        _velocityComponent = GetNode<VelocityComponent>("VelocityComponent") ?? 
            throw new ArgumentException("VelocityComponent not found");

        _detectionArea = GetNode<Area3D>("DetectionArea") ?? 
            throw new ArgumentException("DetectionArea not found");

        _detectionArea.BodyEntered += _OnBodyEntered;
        _detectionArea.BodyExited += _OnBodyExited;

        _player = GetTree().Root.GetNode<Player>("Thirdpersonpoc/Player");
        Log.Debug($"Player node found: {_player.Name}");
    }

    private void _OnBodyEntered(Node3D body)
    {
        Log.Debug($"Body entered detection area: {body.Name}");
        if(body is Player)
        {
            _playerInRange = true;
        }
    }

    private void _OnBodyExited(Node3D body)
    {
        Log.Debug($"Body exited detection area: {body.Name}");
        if(body is Player)
        {
            _playerInRange = false;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        AnimationStateMachine();
        FollowPlayer();
    }

    private void FollowPlayer()
    {
        if (_playerInRange)
        {
            if (_player != null)
            {
                Vector3 direction = (_player.GlobalPosition - GlobalPosition).Normalized();
                LookAt(_player.GlobalPosition, Vector3.Up, useModelFront: true);
                _velocityComponent.Move(direction);
            }
        }
        else
        {
            _velocityComponent.Stop();
        }
    }

    private void AnimationStateMachine()
    {
        var horizontalVelocity = new Vector2(Velocity.X, Velocity.Z);
        var isMoving = horizontalVelocity.Length() > 0;

        if(isMoving)
        {
            animationPlayer.Play("enemy/Root_Run");
        }
        else
        {
            animationPlayer.Play("enemy/Root_Idle");
        }
    }
}

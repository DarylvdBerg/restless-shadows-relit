using Godot;
using System;

public partial class HitboxComponent : Area3D
{
    [Signal]
    public delegate void HitLandedEventHandler(Area3D target);
    
    [Export]
    public int Damage { get; set; }

    [Export]
    public bool Active { get; set; }

    /// <summary>
    /// The actual shape that should be considered on the "CollisionShape3D".
    /// This needs to be generic because different objects have different "hit" boxes.
    /// </summary>
    [Export]
    public Shape3D Shape { get; set; }

    private CollisionShape3D _collisionShape;

    public override void _Ready()
    {
        _collisionShape = GetNode<CollisionShape3D>("CollisionShape3D");
        
        // This will make sure that the hitbox itself cannot detect collision, but others can detect this hitbox coming in.
        Monitoring = false;
        Monitorable = Active;

        _collisionShape.Shape = Shape ?? throw new ArgumentNullException("Collision Shape needs to be set.");
    }

    public void Enable()
    {
        Log.Debug("Enabling hitbox.");
        SetDeferred(Area3D.PropertyName.Monitorable, true);
    }

    public void Disable()
    {
        Log.Debug("Disabling hitbox.");
        SetDeferred(Area3D.PropertyName.Monitorable, false);
    }
    

    public void ReportHit(Area3D target)
    {
        Log.Debug($"Hit: {target.GetParent().Name}");
        EmitSignal(SignalName.HitLanded, target);
    }
}

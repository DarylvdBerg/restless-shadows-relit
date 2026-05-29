using Godot;
using System;

public partial class HurtboxComponent : Area3D
{
    
    /// <summary>
    /// This will detect incoming hits from the hitbox component.
    /// </summary>
    /// <param name="source"></param>
    [Signal]
    public delegate void HitEventHandler(HitboxComponent source);

    [Export]
    public required HealthComponent Health { get; set; }

    /// <summary>
    /// Just like the hitbox component, the shape will differ per entity.
    /// </summary>
    [Export]
    public required Shape3D Shape { get; set; }

    private CollisionShape3D _collisionShape;

    public override void _Ready()
    {
        _collisionShape = GetNode<CollisionShape3D>("CollisionShape3D");
        
        // Reverse of hitbox component, it can detect what comes in 'self' not the otherway around.
        Monitoring = true;
        Monitorable = false;

        _collisionShape.Shape = Shape;

        AreaEntered += OnHurtboxEntered;
    }

    /// <summary>
    /// Will be wired to the "onAreaEntered" event.
    /// This will handle the logic of the hitbox and hurtbox colliding.
    /// </summary>
    /// <param name="area"></param>
    private void OnHurtboxEntered(Area3D area)
    {
        if(area is not HitboxComponent hitbox)
        {
            return;
        }

        Log.Debug($"Entity got hit: {GetParent().Name} by {hitbox.GetParent().Name}");
        Health.Decrease(hitbox.Damage);
        hitbox.ReportHit(this); // Emit the signal that a hit has landed.
        EmitSignal(SignalName.Hit, hitbox); // Emit the signal that entity got hit.
    }
}

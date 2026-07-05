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

    [Export]
    public required CollisionShape3D CollisionShape { get; set; }

    public override void _Ready()
    {
        // Reverse of hitbox component, it can detect what comes in 'self' not the otherway around.
        Monitoring = true;
        Monitorable = false;

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

        Log.Debug($"Entity: {GetParent().Name} got hit by: {hitbox.GetParent().Name}");
        Health.Decrease(hitbox.Damage);
        hitbox.ReportHit(this); // Emit the signal that a hit has landed.
        EmitSignal(SignalName.Hit, hitbox); // Emit the signal that entity got hit.
    }
}

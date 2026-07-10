using Godot;

public partial class Projectile : Area3D
{
    [Export]
    public float Lifetime { get; set; } = 3.0f;

    private Vector3 _direction = Vector3.Forward;
    private float _speed;

    private HitboxComponent _hitbox;
    private Timer _lifetimeTimer;

    /// <summary>
    /// Configure the projectile BEFORE it is added to the tree.
    /// Must not touch GlobalPosition (node is not in the tree yet).
    /// </summary>
    public void Initialize(Vector3 direction, float damage, float speed)
    {
        _direction = direction;
        _speed = speed;

        _hitbox ??= GetNode<HitboxComponent>("HitboxComponent");
        _hitbox.Damage = (int)damage;
    }

    public override void _Ready()
    {
        _hitbox ??= GetNode<HitboxComponent>("HitboxComponent");
        _lifetimeTimer = GetNode<Timer>("LifetimeTimer");

        _lifetimeTimer.OneShot = true;
        _lifetimeTimer.WaitTime = Lifetime;
        _lifetimeTimer.Timeout += QueueFree;
        _lifetimeTimer.Start();

        _hitbox.HitLanded += OnHitLanded;
    }

    public override void _PhysicsProcess(double delta)
    {
        GlobalPosition += _direction * _speed * (float)delta;
    }

    private void OnHitLanded(Area3D target)
    {
        QueueFree();
    }
}

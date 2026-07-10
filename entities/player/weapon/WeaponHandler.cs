using Godot;
using System;

public partial class WeaponHandler : Node
{
    [Export]
    public required Weapon weapon { get; set; }

    [Export]
    public required Node3D WeaponModelParent { get; set; }

    [Export]
    public required Node3D Muzzle { get; set; }

    [Export]
    public PackedScene ProjectileScene { get; set; }

    private VelocityComponent _velocityComponent { get; set; }

    private Timer _reloadTimer;

    private Timer _fireRateTimer;

    private Node3D _weaponModelInstance;

    private bool _isReloading = false;

    private bool _isFiring = false;

    public override void _Ready()
    {
        _velocityComponent = GetNode<VelocityComponent>("VelocityComponent") ?? 
            throw new ArgumentException("VelocityComponent not found");

        InitializeFireRateTimer();
        InitializeReloadTimer();
        SpawnWeapon();
    }

    public override void _Process(double delta)
    {
        Fire();
        Reload();
    }

    public void Fire()
    {
        if(Input.IsActionJustPressed("fire") && canFire())
        {
            _isFiring = true;
            _fireRateTimer.Start();
            weapon.Ammo--;
            Log.Debug($"Firing weapon: {weapon.Name}");

            SpawnProjectile();
        }
    }

    private void SpawnProjectile()
    {
        if(ProjectileScene is null)
        {
            Log.Debug("ProjectileScene not set; cannot fire projectile.");
            return;
        }

        var projectile = ProjectileScene.Instantiate<Projectile>();

        // characterMedium (the Muzzle's parent) faces the aim point with +Z
        // (PlayerRotationHandler sets yaw = Atan2(x, z)), so +Z is forward.
        Vector3 direction = ApplySpread(Muzzle.GlobalTransform.Basis.Z, weapon.Spread);
        projectile.Initialize(direction.Normalized(), weapon.Damage, weapon.ProjectileSpeed);

        if(weapon.ProjectileModel is not null)
        {
            var model = weapon.ProjectileModel.Instantiate<Node3D>();
            projectile.AddChild(model);
        }

        GetTree().CurrentScene.AddChild(projectile);
        projectile.GlobalPosition = Muzzle.GlobalPosition;
    }

    private static Vector3 ApplySpread(Vector3 direction, float spread)
    {
        if(spread <= 0f)
        {
            return direction;
        }

        float angle = (GD.Randf() * 2f - 1f) * spread; // radians, in [-spread, +spread]
        return direction.Rotated(Vector3.Up, angle);
    }

    public void Reload()
    {
        if(Input.IsActionJustPressed("reload") && !_isReloading)
        {
            _isReloading = true;
            Log.Debug($"Reloading weapon: {weapon.Name}");
            _reloadTimer.Start();
        }
    }

    private void SpawnWeapon()
    {
        if(_weaponModelInstance != null)
        {
            Log.Debug($"Removing existing weapon model instance: {_weaponModelInstance.Name}");
            _weaponModelInstance.QueueFree();
        }

        if(weapon.WeaponModel is not null)
        {  
            Log.Debug($"Spawning weapon model: {weapon.WeaponModel.ResourcePath}");
            _weaponModelInstance = weapon.WeaponModel.Instantiate<Node3D>();
            WeaponModelParent.AddChild(_weaponModelInstance);

            // Counteract parent's global scale component-wise so the model
            // always renders at its intended native size.
            var parentScale = WeaponModelParent.GlobalTransform.Basis.Scale;
            _weaponModelInstance.Scale = new Vector3(
                1.0f / parentScale.X,
                1.0f / parentScale.Y,
                1.0f / parentScale.Z);
        }
    }

    private void InitializeReloadTimer()
    {
        _reloadTimer = GetNode<Timer>("ReloadTimer") ??
            throw new Exception("ReloadTimer node not found in the scene tree.");
        _reloadTimer.OneShot = true;
        _reloadTimer.WaitTime = weapon.ReloadTime;

        _reloadTimer.Timeout += OnReloadTimerDone;
    }

    private void OnReloadTimerDone()
    {
        _isReloading = false;
        weapon.Ammo = weapon.MaxAmmo; // Reset ammo to full capacity
        Log.Debug($"Weapon reloaded: {weapon.Name}");       
    }

    private void InitializeFireRateTimer()
    {
        _fireRateTimer = GetNode<Timer>("FireRateTimer") ??
            throw new ArgumentException("FireRateTimer node not found in the scene tree.");
        _fireRateTimer.OneShot = true;
        _fireRateTimer.WaitTime = 1.0 / weapon.FireRate;

        _fireRateTimer.Timeout += OnFireRateTimerDone;
    }

    private void OnFireRateTimerDone()
    {
        _isFiring = false;
    }

    private bool canFire() => weapon.Ammo > 0 && !_isReloading && !_isFiring;
}

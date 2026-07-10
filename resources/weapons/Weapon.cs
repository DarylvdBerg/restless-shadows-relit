using Godot;
using System;

public partial class Weapon : Resource
{
    [Export]
    public string Name { get; set; } = "New Weapon";
    
    [Export]
    public string Description {get; set;} = "A new weapon";

    [Export]
    public PackedScene WeaponModel { get; set; }

    [Export]
    public PackedScene ProjectileModel { get; set; }

    [Export]
    public float Damage { get; set; }

    [Export]
    public int MaxAmmo { get; set; }

    [Export] 
    public int Ammo { get; set; }

    [Export]
    public double ReloadTime { get; set; }

    [Export]
    public float Spread { get; set; }

    // Non-zero default so a weapon with no explicit ProjectileSpeed still fires
    // moving projectiles. Godot strips exported values equal to the script
    // default from .tres files, so relying on a 0 default silently breaks motion.
    [Export]
    public float ProjectileSpeed { get; set; } = 20.0f;

    [Export]
    public float FireRate { get; set; }

    [Export]
    public Vector3 WeaponPosition { get; set; } = new Vector3(0, 0, 0);
}

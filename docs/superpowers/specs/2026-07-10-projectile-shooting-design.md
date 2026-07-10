# Projectile Shooting — Design

**Date:** 2026-07-10
**Branch:** feat/poc-third-person
**Status:** Approved

## Goal

When `WeaponHandler.Fire()` runs, spawn a projectile that flies forward from the
weapon muzzle and damages enemies on contact. Reuse the existing
Hitbox/Hurtbox damage pipeline — no new damage code.

## Existing architecture (reused)

- `HurtboxComponent` (Area3D, `Monitoring=true`) sits on damageable bodies. On
  `AreaEntered` it checks `area is HitboxComponent`, then calls
  `Health.Decrease(hitbox.Damage)` and `hitbox.ReportHit(this)`.
- `HitboxComponent` (Area3D) carries `Damage`, `Active`, `Shape`. Sets
  `Monitorable=Active`. Emits `HitLanded` when a hurtbox reports a hit.
- **Implication:** a projectile that carries a `HitboxComponent` plugs straight
  into the existing damage flow. The enemy hurtbox detects it; damage applies.
- `Weapon` resource already has `ProjectileModel` (PackedScene visual = `bullet.tscn`),
  `Damage`, `Spread`.
- `WeaponHandler` is a plain `Node` (no transform); it holds `WeaponModelParent`
  (a `BoneAttachment3D`). Player aims via `PlayerRotationHandler` rotating the
  mesh root Y toward the mouse.

## Flight model

Straight-line arcade: an `Area3D` moved manually each physics frame at constant
speed, no gravity. Predictable, fits the nerf-gun POC.

## Components

### New: `Projectile` scene — `entities/projectile/Projectile.tscn` + `Projectile.cs`

Scene tree:
- Root `Area3D` with `Projectile.cs`.
- `HitboxComponent` child — `Active=true`, `Shape=SphereShape3D` (small radius,
  e.g. 0.2), `collision_layer=2`, `collision_mask` irrelevant (Monitoring=false).
- `LifetimeTimer` (`Timer`, one-shot).

`Projectile.cs`:
- Private fields: `_direction` (Vector3), `_speed` (float).
- `[Export] float Lifetime { get; set; } = 3.0f` (default; tune later).
- `public void Initialize(Vector3 direction, float damage, float speed)` —
  called before the node is added to the tree. Stores direction/speed and pushes
  `damage` into the child `HitboxComponent.Damage`. (Cache the hitbox in `_Ready`
  or look it up in `Initialize` via `GetNode`.)
- `_Ready`: start `LifetimeTimer` with `WaitTime = Lifetime`; connect
  `LifetimeTimer.Timeout -> QueueFree`; connect the child
  `HitboxComponent.HitLanded -> QueueFree` (despawn on hit).
- `_PhysicsProcess(delta)`: `GlobalPosition += _direction * _speed * (float)delta`.
- No visual node in the scene — `WeaponHandler` injects `weapon.ProjectileModel`
  as a child after instancing.

Note: `HitboxComponent.Damage` is typed `int`; `Weapon.Damage` is `float`. Cast
`(int)` on assignment for now (NerfGun damage is 1.0). Fine for POC.

### Edit: `WeaponHandler.cs`

Add exports:
```csharp
[Export] public required Node3D Muzzle { get; set; }
[Export] public PackedScene ProjectileScene { get; set; }   // = Projectile.tscn
```

In `Fire()`, replace the `// Spawn bullet or projectile here` comment with a call
to `SpawnProjectile()`:
```csharp
private void SpawnProjectile()
{
    var projectile = ProjectileScene.Instantiate<Projectile>();

    Vector3 dir = -Muzzle.GlobalTransform.Basis.Z;   // muzzle forward
    dir = ApplySpread(dir, weapon.Spread);

    projectile.Initialize(dir.Normalized(), weapon.Damage, weapon.ProjectileSpeed);

    if (weapon.ProjectileModel is not null)
    {
        var model = weapon.ProjectileModel.Instantiate<Node3D>();
        projectile.AddChild(model);
    }

    GetTree().CurrentScene.AddChild(projectile);
    projectile.GlobalPosition = Muzzle.GlobalPosition;
}
```

`ApplySpread`: rotate `dir` by a random yaw angle in `[-spread, +spread]` radians
around `Vector3.Up`, using `GD.Randf`. Horizontal-only spread for the POC.

Order note: `Initialize` runs before `AddChild`, so it must NOT touch
`GlobalPosition`. Position is set after `AddChild`. `Initialize` only stores
direction/speed and sets hitbox damage.

### Edit: `WeaponHandler.tscn`

Assign the `ProjectileScene` export to `res://entities/projectile/Projectile.tscn`
(ext_resource + property line on the WeaponHandler node).

### Edit: `Weapon.cs`

Add `[Export] public float ProjectileSpeed { get; set; }`.

### Edit: `NerfGun.tres`

Set `ProjectileSpeed` (e.g. `20.0`).

### Edit: `player.tscn`

Add a `Muzzle` (`Marker3D`) at the barrel tip — child of the weapon model parent
or mesh root so it inherits aim. Wire the WeaponHandler node's `Muzzle` export to
its NodePath. (`WeaponModelParent` already wires to the BoneAttachment3D at
`../characterMedium/Root/Skeleton3D/BoneAttachment3D`; place the Muzzle relative
to that or the mesh root.)

### Edit: `enemy.tscn`

Set the `HurtboxComponent` node's `collision_mask = 2` so it detects
projectile hitboxes on layer 2.

## Self-hit prevention (collision layers)

- Projectile `HitboxComponent` → `collision_layer = 2` only (not layer 1).
- Enemy `HurtboxComponent` → `collision_mask = 2` (edit `enemy.tscn`) → takes damage.
- Player `HurtboxComponent` → stays default `collision_mask = 1` → ignores the
  player's own projectiles. No edit needed.

Area detection rule: hurtbox (Monitoring) detects hitbox (Monitorable) iff
`hurtbox.collision_mask ∩ hitbox.collision_layer != 0`.

## Data flow

1. Player presses fire → `Fire()` passes `canFire()` (ammo, not reloading, not on
   cooldown) → `weapon.Ammo--`, fire-rate timer starts.
2. `SpawnProjectile()` instances `Projectile.tscn`, computes muzzle-forward
   direction with spread, calls `Initialize`, injects the visual, adds to the
   current scene, positions at the muzzle.
3. Projectile flies straight each physics frame.
4. Projectile's `HitboxComponent` overlaps an enemy `HurtboxComponent` → enemy
   `Health.Decrease(damage)`, `HitLanded` fires → projectile `QueueFree`.
5. If nothing is hit, `LifetimeTimer` times out → `QueueFree`.

## Testing

- Fire at an enemy → enemy `HealthComponent` decreases by `weapon.Damage`,
  projectile despawns on contact.
- Fire away from any enemy → projectile despawns after `Lifetime` seconds.
- Fire repeatedly → projectiles do NOT damage the player (self-hit prevented).
- Fire-rate / ammo / reload gating unchanged (`canFire()` still governs).

## Out of scope (POC)

- Wall/environment collision (lifetime timer handles cleanup instead).
- Enemies shooting back / enemy hitboxes.
- Vertical spread, projectile gravity/arc, ricochet.
- Object pooling (instance + free per shot is fine at POC scale).

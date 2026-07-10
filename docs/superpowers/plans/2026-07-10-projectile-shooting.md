# Projectile Shooting Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Spawn a straight-flying projectile from the weapon muzzle on `WeaponHandler.Fire()` that damages enemies via the existing Hitbox/Hurtbox pipeline.

**Architecture:** A new `Projectile` scene (Area3D root + a child `HitboxComponent` on collision layer 2) self-propels each physics frame. `WeaponHandler.Fire()` instances it at a `Muzzle` marker, aims it along the muzzle's forward axis (with weapon spread), and injects the weapon's `ProjectileModel` as the visual. Enemy `HurtboxComponent` (given a real child shape + mask for layer 2) detects the hitbox and applies damage. Player hurtbox stays on mask 1, so the shooter is not hurt by its own shots.

**Tech Stack:** Godot 4 (C# / .NET), existing component scripts (`HitboxComponent`, `HurtboxComponent`, `HealthComponent`, `Weapon`).

**Verification note:** This repo has no unit-test harness, and the changed code is Godot scene/runtime code (needs the engine + scene tree). Verification per task is: (a) `dotnet build` must succeed with no new errors, and (b) an in-editor playtest of `internal/thirdperson/thirdpersonpoc.tscn` where noted. There are no `pytest`-style steps because there is nothing to run them with.

---

### Task 1: Add `ProjectileSpeed` to the Weapon resource

**Files:**
- Modify: `resources/weapons/Weapon.cs`
- Modify: `resources/weapons/NerfGun.tres`

- [ ] **Step 1: Add the export to `Weapon.cs`**

Add after the `Spread` property (currently around line 31):

```csharp
    [Export]
    public float ProjectileSpeed { get; set; }
```

- [ ] **Step 2: Set the value in `NerfGun.tres`**

In the `[resource]` block, add a line after `FireRate = 3.0`:

```
ProjectileSpeed = 20.0
```

- [ ] **Step 3: Build to verify it compiles**

Run: `dotnet build`
Expected: Build succeeded, 0 errors.

- [ ] **Step 4: Commit**

```bash
git add resources/weapons/Weapon.cs resources/weapons/NerfGun.tres
git commit -m "feat: add ProjectileSpeed to Weapon resource"
```

---

### Task 2: Create the `Projectile` script

**Files:**
- Create: `entities/projectile/Projectile.cs`

- [ ] **Step 1: Write the script**

Create `entities/projectile/Projectile.cs`:

```csharp
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
```

Notes for the implementer:
- `Instantiate()` builds all child nodes immediately, so `GetNode<HitboxComponent>("HitboxComponent")` works inside `Initialize` even before `AddChild`.
- `HitboxComponent.Damage` is `int`; `Weapon.Damage` is `float`. The `(int)` cast is intentional (NerfGun damage is `1.0`).
- `HitboxComponent.HitLanded` has signature `void(Area3D target)` — matches `OnHitLanded`.

- [ ] **Step 2: Build to verify it compiles**

Run: `dotnet build`
Expected: Build succeeded, 0 errors. (`HitLanded` and `Damage` resolve against `HitboxComponent`.)

- [ ] **Step 3: Commit**

```bash
git add entities/projectile/Projectile.cs
git commit -m "feat: add Projectile script"
```

---

### Task 3: Create the `Projectile` scene

**Files:**
- Create: `entities/projectile/Projectile.tscn`

- [ ] **Step 1: Write the scene file**

Create `entities/projectile/Projectile.tscn`:

```
[gd_scene load_steps=4 format=3 uid="uid://cprojectilescn0"]

[ext_resource type="Script" path="res://entities/projectile/Projectile.cs" id="1_proj"]
[ext_resource type="PackedScene" uid="uid://bk40bhq2ek0uk" path="res://components/HitboxComponent/HitboxComponent.tscn" id="2_hbox"]

[sub_resource type="SphereShape3D" id="SphereShape3D_proj"]
radius = 0.2

[node name="Projectile" type="Area3D"]
script = ExtResource("1_proj")

[node name="HitboxComponent" parent="." instance=ExtResource("2_hbox")]
collision_layer = 2
collision_mask = 0
Active = true
Shape = SubResource("SphereShape3D_proj")

[node name="LifetimeTimer" type="Timer" parent="."]
one_shot = true
```

Notes for the implementer:
- The `HitboxComponent` node paths must be exactly `HitboxComponent` and the timer `LifetimeTimer` (the script looks them up by name).
- `collision_layer = 2` puts the hitbox on the "player projectile" layer. `collision_mask = 0` because the hitbox does not need to detect anything (the enemy hurtbox detects it).
- No visual node here — `WeaponHandler` injects `weapon.ProjectileModel` at runtime.
- If Godot rewrites the `uid` on first import, that is expected and fine.

- [ ] **Step 2: Open the project in the Godot editor once so the scene imports**

Open `project.godot` in the Godot editor (or run the editor headless import). Confirm `Projectile.tscn` opens with a `HitboxComponent` child (Active checked, Shape = SphereShape3D) and a `LifetimeTimer`, and that the editor reports no script/resource load errors in the Output panel.
Expected: scene loads clean, no missing-dependency errors.

- [ ] **Step 3: Commit**

```bash
git add entities/projectile/Projectile.tscn entities/projectile/Projectile.cs.uid
git commit -m "feat: add Projectile scene"
```

(The `.cs.uid` file is generated by Godot on import; include it if present.)

---

### Task 4: Fire projectiles from `WeaponHandler`

**Files:**
- Modify: `entities/player/weapon/WeaponHandler.cs`

- [ ] **Step 1: Add the two exports**

In `WeaponHandler.cs`, after the `WeaponModelParent` export (around line 10), add:

```csharp
    [Export]
    public required Node3D Muzzle { get; set; }

    [Export]
    public PackedScene ProjectileScene { get; set; }
```

- [ ] **Step 2: Call `SpawnProjectile()` from `Fire()`**

Replace the `// Spawn bullet or projectile here` comment inside `Fire()` with:

```csharp
            SpawnProjectile();
```

Resulting `Fire()`:

```csharp
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
```

- [ ] **Step 3: Add `SpawnProjectile` and `ApplySpread`**

Add these methods to the class (e.g. just below `Fire()`):

```csharp
    private void SpawnProjectile()
    {
        if(ProjectileScene is null)
        {
            Log.Debug("ProjectileScene not set; cannot fire projectile.");
            return;
        }

        var projectile = ProjectileScene.Instantiate<Projectile>();

        Vector3 direction = ApplySpread(-Muzzle.GlobalTransform.Basis.Z, weapon.Spread);
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
```

Notes for the implementer:
- `Initialize` runs before `AddChild`, so it only stores direction/speed and sets hitbox damage. `GlobalPosition` is assigned after `AddChild` (once the node is in the tree).
- `-Muzzle.GlobalTransform.Basis.Z` is the muzzle's forward. If the playtest in Task 6 shows projectiles firing backward, negate this (use `Muzzle.GlobalTransform.Basis.Z`) or rotate the Muzzle marker 180° about Y in Task 5.

- [ ] **Step 4: Build to verify it compiles**

Run: `dotnet build`
Expected: Build succeeded, 0 errors. (`Projectile`, `weapon.ProjectileSpeed`, `weapon.ProjectileModel`, `weapon.Spread` all resolve.)

- [ ] **Step 5: Commit**

```bash
git add entities/player/weapon/WeaponHandler.cs
git commit -m "feat: spawn projectiles on fire"
```

---

### Task 5: Wire scenes (ProjectileScene, Muzzle, enemy hurtbox)

**Files:**
- Modify: `entities/player/weapon/WeaponHandler.tscn`
- Modify: `internal/thirdperson/player.tscn`
- Modify: `internal/thirdperson/enemy.tscn`

- [ ] **Step 1: Assign `ProjectileScene` in `WeaponHandler.tscn`**

Add an `ext_resource` after the existing `ext_resource` lines:

```
[ext_resource type="PackedScene" uid="uid://cprojectilescn0" path="res://entities/projectile/Projectile.tscn" id="3_proj"]
```

Then add a property line under the `[node name="WeaponHandler" ...]` block (below `script = ExtResource("1_52ket")`):

```
ProjectileScene = ExtResource("3_proj")
```

- [ ] **Step 2: Add a `Muzzle` marker to `player.tscn` and wire it**

Add a `Marker3D` child under `characterMedium` (the node `PlayerRotationHandler` rotates to aim). Insert this node block immediately after the `[node name="characterMedium" type="Node3D" parent="." ...]` line (around line 213):

```
[node name="Muzzle" type="Marker3D" parent="characterMedium" unique_id=1900000001]
transform = Transform3D(1, 0, 0, 0, 1, 0, 0, 0, 1, 0, 1.2, 0.6)
```

Then update the `WeaponHandler` instance node (currently the last node in the file) so `Muzzle` is exported and wired. Change:

```
[node name="WeaponHandler" parent="." unique_id=1647888515 node_paths=PackedStringArray("WeaponModelParent") instance=ExtResource("12_3sqcr")]
weapon = ExtResource("13_wmfmh")
WeaponModelParent = NodePath("../characterMedium/Root/Skeleton3D/BoneAttachment3D")
```

to:

```
[node name="WeaponHandler" parent="." unique_id=1647888515 node_paths=PackedStringArray("WeaponModelParent", "Muzzle") instance=ExtResource("12_3sqcr")]
weapon = ExtResource("13_wmfmh")
WeaponModelParent = NodePath("../characterMedium/Root/Skeleton3D/BoneAttachment3D")
Muzzle = NodePath("../characterMedium/Muzzle")
```

Notes:
- `characterMedium` is an unscaled `Node3D`, so the Marker3D transform uses normal units: `(0, 1.2, 0.6)` = 1.2m up, 0.6m forward (local -Z is forward in Godot; positive Z here places it in front along +Z — the sign is verified/corrected in Task 6).
- The marker inherits `characterMedium`'s yaw, which tracks the mouse aim.

- [ ] **Step 3: Give the enemy `HurtboxComponent` a real detection shape + layer-2 mask in `enemy.tscn`**

The enemy `HurtboxComponent` is an `Area3D` with NO child `CollisionShape3D` (its `CollisionShape` export points to a sibling, which does not give the Area3D a shape). Add a capsule sub-resource near the top of the file (after the existing `[sub_resource ...]` blocks / before the node list):

```
[sub_resource type="CapsuleShape3D" id="CapsuleShape3D_hurt"]
radius = 0.8
height = 2.0
```

Update the `HurtboxComponent` node to add the layer-2 mask. Change:

```
[node name="HurtboxComponent" parent="." unique_id=366223994 node_paths=PackedStringArray("Health", "CollisionShape") instance=ExtResource("2_p138l")]
transform = Transform3D(1, 0, 0, 0, 1, 0, 0, 0, 1, 0, 1.875783, 0)
Health = NodePath("../HealthComponent")
CollisionShape = NodePath("../CollisionShape3D")
```

to:

```
[node name="HurtboxComponent" parent="." unique_id=366223994 node_paths=PackedStringArray("Health", "CollisionShape") instance=ExtResource("2_p138l")]
transform = Transform3D(1, 0, 0, 0, 1, 0, 0, 0, 1, 0, 1.875783, 0)
collision_mask = 2
Health = NodePath("../HealthComponent")
CollisionShape = NodePath("../CollisionShape3D")
```

Then add a child `CollisionShape3D` under the hurtbox. Insert immediately after the `HurtboxComponent` node block (before the `VelocityComponent` node):

```
[node name="CollisionShape3D" type="CollisionShape3D" parent="HurtboxComponent" unique_id=1900000002]
shape = SubResource("CapsuleShape3D_hurt")
```

Notes:
- `collision_mask = 2` makes the hurtbox detect areas on layer 2 (the projectile hitbox). It keeps its default layer (1); the player hurtbox is unchanged (mask 1) so it ignores layer-2 projectiles → no self-hit.
- The capsule (radius 0.8, height 2.0) sits at the hurtbox's local origin, which is offset to `y ≈ 1.875`; this comfortably spans the muzzle's flight height. Widen if the playtest shows misses.
- Adding a child named `CollisionShape3D` does not conflict with the `CollisionShape` export path `../CollisionShape3D` (that resolves to the sibling body shape).

- [ ] **Step 4: Build to verify nothing broke**

Run: `dotnet build`
Expected: Build succeeded, 0 errors. (Scene edits don't affect the build, but confirm no accidental code change.)

- [ ] **Step 5: Commit**

```bash
git add entities/player/weapon/WeaponHandler.tscn internal/thirdperson/player.tscn internal/thirdperson/enemy.tscn
git commit -m "feat: wire projectile scene, muzzle, and enemy hurtbox"
```

---

### Task 6: End-to-end playtest verification

**Files:** none (manual verification + any small tuning commits)

- [ ] **Step 1: Launch the POC scene**

Open the Godot editor and play `internal/thirdperson/thirdpersonpoc.tscn`.
Expected: the level loads with the player and at least one enemy, no errors in the Output panel.

- [ ] **Step 2: Verify projectile spawns and flies forward**

Aim at an enemy (mouse) and press the `fire` action.
Expected: a `bullet` model spawns at the muzzle and travels toward the aim point.
If it travels backward: in `SpawnProjectile` change `-Muzzle.GlobalTransform.Basis.Z` to `Muzzle.GlobalTransform.Basis.Z` (Task 4, Step 3), OR rotate the `Muzzle` marker 180° about Y in `player.tscn` (Task 5, Step 2). Rebuild and re-test.

- [ ] **Step 3: Verify enemy takes damage**

Hit an enemy with a projectile.
Expected: the enemy `HealthComponent` health decreases by `weapon.Damage` (watch the `[DEBUG] Entity: Enemy got hit by: ...` log from `HurtboxComponent`), and the projectile disappears on contact (`HitLanded` → `QueueFree`).
If no hit registers: confirm the enemy hurtbox capsule (Task 5, Step 3) is large enough / at the right height, and that `collision_mask = 2` is set.

- [ ] **Step 4: Verify no self-damage**

Fire many shots while standing still.
Expected: the player takes no damage from its own projectiles (player hurtbox mask is 1; projectile hitbox is layer 2).

- [ ] **Step 5: Verify lifetime cleanup**

Fire away from all enemies into open space.
Expected: each projectile frees itself after `Lifetime` (3s) — the scene does not accumulate `Projectile` nodes (check the Remote scene tree while playing).

- [ ] **Step 6: Verify gating unchanged**

Empty the magazine, then keep pressing fire.
Expected: no projectiles spawn while `Ammo == 0` or during reload/fire-rate cooldown (`canFire()` still governs). Reload restores ammo and firing resumes.

- [ ] **Step 7: Commit any tuning**

If Steps 2/3 required direction or shape/muzzle tweaks:

```bash
git add -A
git commit -m "fix: tune projectile aim/hurtbox after playtest"
```

---

## Self-Review

**Spec coverage:**
- Flight model (straight-line arcade) → Task 2 (`_PhysicsProcess`), Task 3 (Area3D root). ✓
- Projectile scene = Area3D + HitboxComponent(layer 2) + LifetimeTimer → Task 3. ✓
- `Initialize(dir, damage, speed)`, despawn on `HitLanded` + `LifetimeTimer` → Task 2. ✓
- Visual = injected `weapon.ProjectileModel` → Task 4 `SpawnProjectile`. ✓
- `Muzzle` + `ProjectileScene` exports, spread, spawn into `CurrentScene`, position at muzzle → Task 4 + Task 5. ✓
- `Weapon.ProjectileSpeed` + `NerfGun.tres` → Task 1. ✓
- Self-hit prevention via layers (projectile layer 2, enemy mask 2, player mask 1) → Task 3 + Task 5. ✓
- Muzzle marker in `player.tscn` → Task 5. ✓
- Enemy hurtbox mask → Task 5. **Added:** enemy hurtbox also needed a child CollisionShape3D (it had none); without it the spec's "enemy takes damage" test cannot pass. Covered in Task 5, Step 3.
- Out-of-scope items (walls, enemy fire, vertical spread, pooling) → left out. ✓

**Placeholder scan:** No TBD/TODO; every code and scene step shows full content. ✓

**Type consistency:** `Initialize(Vector3, float, float)` defined in Task 2 and called with matching args in Task 4. `HitboxComponent.Damage` (int) cast from float in Task 2. `ProjectileScene`/`Muzzle` export names match between Task 4 (code) and Task 5 (scene wiring). Node names `HitboxComponent`/`LifetimeTimer` match between Task 2 (`GetNode`) and Task 3 (scene). ✓

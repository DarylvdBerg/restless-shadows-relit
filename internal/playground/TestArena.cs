using Godot;

// ───────────────────────────────────────────────────────────────────────────
//  QUICK COMPONENT TEST
//
//  How to run:
//    1. Add this file to your project.
//    2. Create a new scene with a single Node3D root and attach this script,
//       OR just set this script as the main scene (Project > Run > Main Scene).
//    3. Press Play. Use the ARROW KEYS to move the blue player.
//       The red enemy chases you; when it reaches you, the hit pipeline fires
//       and prints to the Output panel.
//
//  What it exercises:
//    - VelocityComponent : both the player (arrow-key input) and the enemy
//                          (chase AI) drive movement through the same component.
//    - HitboxComponent   : the enemy carries one (always active here), on layer 2.
//    - HurtboxComponent  : the player carries one, masking layer 2, and its
//                          Hit signal is what prints when contact happens.
//
//  Uses the built-in ui_* input actions so it runs with zero Input Map setup.
// ───────────────────────────────────────────────────────────────────────────

public partial class TestArena : Node3D
{
    public override void _Ready()
    {
        // Camera looking down at the play area.
        var camera = new Camera3D();
        AddChild(camera);
        camera.LookAtFromPosition(new Vector3(0, 12, 12), Vector3.Zero, Vector3.Up);

        // Light so the meshes aren't pitch black.
        var light = new DirectionalLight3D();
        AddChild(light);
        light.RotationDegrees = new Vector3(-50, -30, 0);

        // The blue player.
        var player = new TestPlayer { Name = "Player" };
        AddChild(player);

        // The red enemy, offset to the side, targeting the player.
        var enemy = new TestEnemy { Name = "Enemy", Target = player };
        AddChild(enemy);
        enemy.Position = new Vector3(6, 0, 0);

        GD.Print("Arrow keys to move. The red enemy chases you — contact prints a hit.");
    }
}

public partial class TestPlayer : CharacterBody3D
{
    private VelocityComponent _velocity;

    public TestPlayer()
    {
        // Visual (tinted blue).
        var mesh = new MeshInstance3D { Mesh = new CapsuleMesh() };
        mesh.MaterialOverride = new StandardMaterial3D { AlbedoColor = new Color(0.2f, 0.4f, 0.9f) };
        AddChild(mesh);

        // Movement.
        _velocity = new VelocityComponent { Speed = 6.0f };
        AddChild(_velocity);

        // Hurtbox: detector that reacts to hitboxes on layer 2.
        var hurtbox = new HurtboxComponent
        {
            Health = new HealthComponent() { MaxHealth = 10, CurrentHealth = 10 },
            Shape = new CapsuleShape3D { Radius = 0.6f, Height = 2.0f }
            // Health left null on purpose — see note. The Hit signal still fires.
        };
        var hurtShape = new CollisionShape3D { Name = "CollisionShape3D" };
        hurtbox.AddChild(hurtShape);
        hurtbox.SetCollisionMaskValue(1, false);  // ignore default layer
        hurtbox.SetCollisionMaskValue(2, true);   // react to enemy hitboxes
        AddChild(hurtbox);

        hurtbox.Hit += OnHit;
    }

    private void OnHit(HitboxComponent source)
    {
        GD.Print($"Player hit for {source.Damage} damage by {source.GetParent().Name}");
    }

    public override void _PhysicsProcess(double delta)
    {
        Vector2 input = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
        _velocity.Move(new Vector3(input.X, 0, input.Y));
    }
}

public partial class TestEnemy : CharacterBody3D
{
    public Node3D Target { get; set; }
    public float DetectRange { get; set; } = 50.0f;

    private VelocityComponent _velocity;

    public TestEnemy()
    {
        // Visual (tinted red).
        var mesh = new MeshInstance3D { Mesh = new CapsuleMesh() };
        mesh.MaterialOverride = new StandardMaterial3D { AlbedoColor = new Color(0.9f, 0.2f, 0.2f) };
        AddChild(mesh);

        // Movement (slower than the player so you can outrun it).
        _velocity = new VelocityComponent { Speed = 3.0f };
        AddChild(_velocity);

        // Hitbox: the enemy's contact damage, always active for the test, on layer 2.
        var hitbox = new HitboxComponent
        {
            Damage = 10,
            Active = true,
            Shape = new CapsuleShape3D { Radius = 0.7f, Height = 2.0f }
        };
        var hitShape = new CollisionShape3D { Name = "CollisionShape3D" };
        hitbox.AddChild(hitShape);
        hitbox.SetCollisionLayerValue(1, false);
        hitbox.SetCollisionLayerValue(2, true);
        AddChild(hitbox);
    }

    public override void _PhysicsProcess(double delta)
    {
        Vector3 direction = Vector3.Zero;

        if (Target != null)
        {
            Vector3 toTarget = Target.GlobalPosition - GlobalPosition;
            if (toTarget.Length() <= DetectRange)
                direction = new Vector3(toTarget.X, 0, toTarget.Z);
        }

        _velocity.Move(direction);
    }
}
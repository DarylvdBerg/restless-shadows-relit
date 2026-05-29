using Godot;
using System;

public partial class HealthComponent : Node
{
    public int MaxHealth;
    public int CurrentHealth;

    public override void _Ready()
    {
        CurrentHealth = MaxHealth;
    }

    public void Decrease(int amount)
    {
        CurrentHealth -= amount;
    }

    public void Increase(int amount)
    {
        CurrentHealth += amount;
    }
}

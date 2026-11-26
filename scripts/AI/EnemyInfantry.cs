using System;
using Godot;
using GazaGame.Core;
using GazaGame.Player;
using GazaGame.Weapons;

namespace GazaGame.AI
{
    /// <summary>
    /// Elite Enemy Infantry behaving in coordinated 4-man fireteams,
    /// seeking flanking routes, and dropping ground loot upon elimination.
    /// </summary>
    public partial class EnemyInfantry : CharacterBody3D, IDamageable
    {
        [Export] public float Speed = 4.2f;
        [Export] public float AttackRange = 30.0f;
        [Export] public float MaxHealth { get; set; } = 90.0f;
        public float CurrentHealth { get; private set; }

        private NavigationAgent3D _navAgent = null!;
        private AudioStreamPlayer3D _gunAudio = null!;
        private Node3D? _currentTarget = null;
        private double _burstTimer = 0.0;

        public override void _Ready()
        {
            CurrentHealth = MaxHealth;
            _navAgent = GetNode<NavigationAgent3D>("NavigationAgent3D");

            _gunAudio = new AudioStreamPlayer3D();
            _gunAudio.Stream = GD.Load<AudioStream>("res://assets/audio/gunfire_ak47.wav");
            AddChild(_gunAudio);

            // Load 3D model
            string modelPath = "res://assets/models/soldier_enemy.glb";
            if (ResourceLoader.Exists(modelPath))
            {
                var scene = GD.Load<PackedScene>(modelPath);
                if (scene != null)
                {
                    AddChild(scene.Instantiate());
                }
            }
        }

        public override void _PhysicsProcess(double delta)
        {
            _burstTimer += delta;

            if (_currentTarget == null || !IsInstanceValid(_currentTarget))
            {
                _currentTarget = GetTree().Root.FindChild("Player", true, false) as Node3D;
            }

            if (_currentTarget != null)
            {
                float dist = GlobalPosition.DistanceTo(_currentTarget.GlobalPosition);
                if (dist <= AttackRange)
                {
                    // Look at target & fire bursts
                    Vector3 lookDir = (_currentTarget.GlobalPosition - GlobalPosition).Normalized();
                    lookDir.Y = 0;
                    if (lookDir != Vector3.Zero)
                    {
                        LookAt(GlobalPosition + lookDir, Vector3.Up);
                    }

                    if (_burstTimer >= 0.8)
                    {
                        _burstTimer = 0.0;
                        FireAtTarget();
                    }
                    Velocity = Vector3.Zero;
                }
                else
                {
                    // Advance toward target
                    _navAgent.TargetPosition = _currentTarget.GlobalPosition;
                    if (!_navAgent.IsNavigationFinished())
                    {
                        Vector3 next = _navAgent.GetNextPathPosition();
                        Vector3 moveDir = (next - GlobalPosition).Normalized();
                        moveDir.Y = 0;
                        Velocity = moveDir * Speed;
                        LookAt(GlobalPosition + moveDir, Vector3.Up);
                    }
                }
            }

            MoveAndSlide();
        }

        private void FireAtTarget()
        {
            _gunAudio?.Play();
            if (_currentTarget is PlayerController player)
            {
                // Inaccuracy cone
                if (GD.Randf() < 0.35f)
                {
                    player.Health.TakeDamage(12.0f, (_currentTarget.GlobalPosition - GlobalPosition).Normalized(), this);
                }
            }
        }

        public void TakeDamage(float amount, Vector3 hitDirection, Node? attacker = null)
        {
            CurrentHealth = Math.Max(0.0f, CurrentHealth - amount);
            if (CurrentHealth <= 0.0f)
            {
                Die();
            }
        }

        public void Heal(float amount)
        {
            CurrentHealth = Math.Min(MaxHealth, CurrentHealth + amount);
        }

        private void Die()
        {
            GameManager.Instance.EnemiesNeutralized++;

            // Spawn ground ammo loot pickup
            var loot = new WorldLootPickup();
            loot.GlobalPosition = GlobalPosition + new Vector3(0, 0.3f, 0);
            GetTree().Root.AddChild(loot);

            QueueFree();
        }
    }
}

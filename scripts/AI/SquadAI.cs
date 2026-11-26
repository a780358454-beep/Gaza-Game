using System;
using System.Collections.Generic;
using Godot;
using GazaGame.Core;
using GazaGame.Player;
using GazaGame.Weapons;

namespace GazaGame.AI
{
    public enum SquadAIState
    {
        FollowLeader,
        MoveToWaypoint,
        InCover,
        EngagingEnemy,
        PerformingSpecialAction
    }

    /// <summary>
    /// Friendly Squad AI bot navigating using NavigationAgent3D, executing tactical orders,
    /// taking dynamic cover, and supporting the squad with heavy weapons/medical resupply.
    /// </summary>
    public partial class SquadAI : CharacterBody3D, IDamageable
    {
        private static readonly List<SquadAI> ActiveSquadBots = new();

        [Export] public string SquadMemberId = "abu_hamza";
        [Export] public string CharacterNameAr = "أبو حمزة";
        [Export] public float Speed = 4.8f;
        [Export] public float MaxHealth { get; set; } = 100.0f;
        public float CurrentHealth { get; private set; }

        private NavigationAgent3D _navAgent = null!;
        private AudioStreamPlayer3D _audioPlayer = null!;
        private Node3D? _targetEnemy = null;
        private Vector3 _customWaypoint = Vector3.Zero;
        private SquadAIState _state = SquadAIState.FollowLeader;
        private double _shootTimer = 0.0;

        public override void _Ready()
        {
            CurrentHealth = MaxHealth;
            ActiveSquadBots.Add(this);

            _navAgent = GetNode<NavigationAgent3D>("NavigationAgent3D");
            _audioPlayer = GetNode<AudioStreamPlayer3D>("AudioPlayer");

            // Attach 3D Soldier Model
            string modelPath = "res://assets/models/soldier.glb";
            if (ResourceLoader.Exists(modelPath))
            {
                var scene = GD.Load<PackedScene>(modelPath);
                if (scene != null)
                {
                    AddChild(scene.Instantiate());
                }
            }
        }

        public override void _ExitTree()
        {
            ActiveSquadBots.Remove(this);
        }

        public override void _PhysicsProcess(double delta)
        {
            _shootTimer += delta;

            switch (_state)
            {
                case SquadAIState.FollowLeader:
                    FollowSquadLeader();
                    break;
                case SquadAIState.MoveToWaypoint:
                    NavigateToward(_customWaypoint);
                    break;
                case SquadAIState.EngagingEnemy:
                    EngageTarget(delta);
                    break;
            }

            MoveAndSlide();
        }

        private void FollowSquadLeader()
        {
            var leader = GetTree().Root.FindChild("Player", true, false) as Node3D;
            if (leader != null)
            {
                float dist = GlobalPosition.DistanceTo(leader.GlobalPosition);
                if (dist > 3.5f)
                {
                    NavigateToward(leader.GlobalPosition);
                }
                else
                {
                    Velocity = Vector3.Zero;
                }
            }
        }

        private void NavigateToward(Vector3 targetPos)
        {
            _navAgent.TargetPosition = targetPos;
            if (!_navAgent.IsNavigationFinished())
            {
                Vector3 next = _navAgent.GetNextPathPosition();
                Vector3 dir = (next - GlobalPosition).Normalized();
                dir.Y = 0;
                Velocity = dir * Speed;
                LookAt(GlobalPosition + dir, Vector3.Up);
            }
            else
            {
                Velocity = Vector3.Zero;
            }
        }

        private void EngageTarget(double delta)
        {
            if (_targetEnemy != null && IsInstanceValid(_targetEnemy))
            {
                LookAt(_targetEnemy.GlobalPosition, Vector3.Up);
                if (_shootTimer >= 0.25)
                {
                    _shootTimer = 0.0;
                    _audioPlayer?.Play();
                    if (_targetEnemy is IDamageable target)
                    {
                        target.TakeDamage(18.0f, (_targetEnemy.GlobalPosition - GlobalPosition).Normalized(), this);
                    }
                }
            }
            else
            {
                _state = SquadAIState.FollowLeader;
            }
        }

        public void TakeDamage(float amount, Vector3 hitDirection, Node? attacker = null)
        {
            CurrentHealth = Mathf.Max(0.0f, CurrentHealth - amount);
            if (attacker is Node3D attackerNode)
            {
                _targetEnemy = attackerNode;
                _state = SquadAIState.EngagingEnemy;
            }

            if (CurrentHealth <= 0.0f)
            {
                GD.Print($"[SquadAI] {CharacterNameAr} fell in action!");
                QueueFree();
            }
        }

        public void Heal(float amount)
        {
            CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + amount);
        }

        // Static Team Coordination
        public static void BroadcastWaypoint(Vector3 pos)
        {
            foreach (var bot in ActiveSquadBots)
            {
                bot._customWaypoint = pos + new Vector3((float)GD.RandRange(-2, 2), 0, (float)GD.RandRange(-2, 2));
                bot._state = SquadAIState.MoveToWaypoint;
            }
        }

        public static void BroadcastSpecialOrder(string memberId, string action, Vector3 pos)
        {
            foreach (var bot in ActiveSquadBots)
            {
                if (bot.SquadMemberId == memberId)
                {
                    GD.Print($"[SquadAI] Special order '{action}' dispatched to {bot.CharacterNameAr} at {pos}");
                    bot._customWaypoint = pos;
                    bot._state = SquadAIState.MoveToWaypoint;
                }
            }
        }
    }
}

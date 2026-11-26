using System;
using Godot;
using GazaGame.Core;
using GazaGame.Player;

namespace GazaGame.AI
{
    /// <summary>
    /// Counter-sniper stationed in high-rise building with visible laser glint,
    /// targeting standing squad members during extraction in Act 3.
    /// </summary>
    public partial class EnemySniper : Node3D, IDamageable
    {
        [Export] public float MaxHealth { get; set; } = 60.0f;
        public float CurrentHealth { get; private set; }

        private AudioStreamPlayer3D _sniperAudio = null!;
        private double _shotCooldown = 3.5;
        private double _glintPulse = 0.0;
        private bool _isEliminated = false;

        public override void _Ready()
        {
            CurrentHealth = MaxHealth;

            _sniperAudio = new AudioStreamPlayer3D();
            _sniperAudio.Stream = GD.Load<AudioStream>("res://assets/audio/sniper_shot.wav");
            AddChild(_sniperAudio);

            // Add visible laser sight beam
            var laser = new MeshInstance3D();
            laser.Name = "LaserGlint";
            var cyl = new CylinderMesh { TopRadius = 0.02f, BottomRadius = 0.02f, Height = 45.0f };
            laser.Mesh = cyl;
            laser.Rotation = new Vector3(Mathf.DegToRad(90), 0, 0);
            laser.Position = new Vector3(0, 0, -22.5f);
            AddChild(laser);
        }

        public override void _Process(double delta)
        {
            if (_isEliminated) return;

            _shotCooldown -= delta;
            _glintPulse += delta * 4.0;

            var player = GetTree().Root.FindChild("Player", true, false) as PlayerController;
            if (player == null) return;

            LookAt(player.GlobalPosition + new Vector3(0, 1.2f, 0), Vector3.Up);

            if (_shotCooldown <= 0.0)
            {
                _shotCooldown = 3.8;
                AttemptSniperShot(player);
            }
        }

        private void AttemptSniperShot(PlayerController player)
        {
            // If player is Prone or behind cover, sniper misses!
            if (player.CurrentState is ProneState)
            {
                GD.Print("[EnemySniper] Bullet whizzed over player! Prone saved the player.");
                _sniperAudio?.Play();
                return;
            }

            // Raycast check to target
            var space = GetWorld3D().DirectSpaceState;
            var query = PhysicsRayQueryParameters3D.Create(GlobalPosition, player.GlobalPosition + new Vector3(0, 1.2f, 0));
            query.CollisionMask = GameConfig.LayerWorld | GameConfig.LayerPlayer;

            var hit = space.IntersectRay(query);
            if (hit.Count > 0 && hit["collider"].As<Node>() == player)
            {
                _sniperAudio?.Play();
                player.Health.TakeDamage(45.0f, (player.GlobalPosition - GlobalPosition).Normalized(), this);
                GameManager.Instance.DisplaySubtitle("أبو أنس", "قناص في المبنى المقابل! انبطحوا فوراً!", 3.0f);
            }
        }

        public void TakeDamage(float amount, Vector3 hitDirection, Node? attacker = null)
        {
            if (_isEliminated) return;

            CurrentHealth = Math.Max(0.0f, CurrentHealth - amount);
            if (CurrentHealth <= 0.0f)
            {
                _isEliminated = true;
                GD.Print("[EnemySniper] Sniper eliminated!");
                GameManager.Instance.DisplaySubtitle("أبو بكر", "تم إسكات موقع القناص! تابعوا الانسحاب بحذر!", 3.5f);
                QueueFree();
            }
        }

        public void Heal(float amount) {}
    }
}

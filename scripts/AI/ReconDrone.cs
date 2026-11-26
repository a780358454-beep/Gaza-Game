using System;
using Godot;
using GazaGame.Core;
using GazaGame.Player;

namespace GazaGame.AI
{
    /// <summary>
    /// Overhead Reconnaissance Drone that spots players in open daylight and calls artillery strikes.
    /// Obscured by tactical smoke grenades or destroyed by accurate small arms fire.
    /// </summary>
    public partial class ReconDrone : CharacterBody3D, IDamageable
    {
        [Export] public float PatrolRadius = 35.0f;
        [Export] public float Altitude = 14.0f;
        [Export] public float FlightSpeed = 5.0f;
        [Export] public float MaxHealth { get; set; } = 80.0f;
        public float CurrentHealth { get; private set; }

        private double _detectionTimer = 0.0;
        private double _angle = 0.0;
        private AudioStreamPlayer3D _humAudio = null!;
        private bool _isDestroyed = false;

        public override void _Ready()
        {
            CurrentHealth = MaxHealth;
            _humAudio = new AudioStreamPlayer3D();
            _humAudio.Stream = GD.Load<AudioStream>("res://assets/audio/drone_hum.wav");
            _humAudio.Autoplay = true;
            AddChild(_humAudio);

            // Load 3D model
            string modelPath = "res://assets/models/drone.glb";
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
            if (_isDestroyed) return;

            // Circular patrol above map
            _angle += (FlightSpeed / PatrolRadius) * delta;
            float x = (float)Math.Cos(_angle) * PatrolRadius;
            float z = (float)Math.Sin(_angle) * PatrolRadius;
            GlobalPosition = new Vector3(x, Altitude, z);

            ScanForTargets(delta);
        }

        private void ScanForTargets(double delta)
        {
            var player = GetTree().Root.FindChild("Player", true, false) as PlayerController;
            if (player == null) return;

            // Check if player is obscured by smoke cover
            if (IsPlayerInSmoke(player))
            {
                _detectionTimer = Math.Max(0.0, _detectionTimer - delta);
                return;
            }

            // Line of sight raycast
            var space = GetWorld3D().DirectSpaceState;
            var query = PhysicsRayQueryParameters3D.Create(GlobalPosition, player.GlobalPosition);
            query.CollisionMask = GameConfig.LayerWorld | GameConfig.LayerPlayer;

            var hit = space.IntersectRay(query);
            if (hit.Count > 0 && hit["collider"].As<Node>() == player)
            {
                _detectionTimer += delta;
                if (_detectionTimer >= GameConfig.DroneDetectionLightThresholdSeconds)
                {
                    CallArtilleryStrike(player.GlobalPosition);
                    _detectionTimer = 0.0;
                }
            }
            else
            {
                _detectionTimer = Math.Max(0.0, _detectionTimer - delta);
            }
        }

        private bool IsPlayerInSmoke(PlayerController player)
        {
            var space = GetWorld3D().DirectSpaceState;
            var query = new PhysicsShapeQueryParameters3D
            {
                Shape = new SphereShape3D { Radius = 1.0f },
                Transform = player.GlobalTransform,
                CollisionMask = GameConfig.LayerSmokeCover
            };
            return space.IntersectShape(query, 1).Count > 0;
        }

        private void CallArtilleryStrike(Vector3 targetPos)
        {
            GD.Print($"[ReconDrone] TARGET SPOTTED! Calling artillery strike onto ({targetPos.X:F1}, {targetPos.Z:F1})!");
            GameManager.Instance.DisplaySubtitle("طائرة استطلاع", "كشف الهدف! إحداثيات القصف مرسلة للراجمات!", 3.5f);

            // Inbound mortar/artillery barrage after 3 seconds
            GetTree().CreateTimer(3.0).Timeout += () =>
            {
                var explosionAudio = new AudioStreamPlayer3D();
                explosionAudio.Stream = GD.Load<AudioStream>("res://assets/audio/explosion_heavy.wav");
                explosionAudio.GlobalPosition = targetPos;
                GetTree().Root.AddChild(explosionAudio);
                explosionAudio.Play();

                var player = GetTree().Root.FindChild("Player", true, false) as PlayerController;
                if (player != null && player.GlobalPosition.DistanceTo(targetPos) < 10.0f)
                {
                    player.Health.TakeDamage(55.0f, Vector3.Up, this);
                }
            };
        }

        public void TakeDamage(float amount, Vector3 hitDirection, Node? attacker = null)
        {
            if (_isDestroyed) return;

            CurrentHealth = Math.Max(0.0f, CurrentHealth - amount);
            if (CurrentHealth <= 0.0f)
            {
                _isDestroyed = true;
                GD.Print("[ReconDrone] Drone shot down!");
                QueueFree();
            }
        }

        public void Heal(float amount) {}
    }
}

using System;
using Godot;
using GazaGame.Core;
using GazaGame.Player;

namespace GazaGame.AI
{
    public enum VehicleType
    {
        MainBattleTank,
        HeavyAPC
    }

    /// <summary>
    /// Armored vehicle (Tank / APC) patrolling Martyrs' Junction Kill Zone.
    /// Features two-stage damage: 1st hit immobilizes tracks, 2nd hit (or 1 Tandem hit) completely destroys it.
    /// </summary>
    public partial class ArmoredVehicle : CharacterBody3D, IDamageable
    {
        [Export] public VehicleType Type = VehicleType.MainBattleTank;
        [Export] public float MaxArmorHealth = 1000.0f;
        [Export] public float PatrolSpeed = 3.5f;

        public float CurrentArmorHealth { get; private set; }
        public bool IsImmobilized { get; private set; } = false;
        public bool IsDestroyed { get; private set; } = false;

        public float CurrentHealth => CurrentArmorHealth;
        public float MaxHealth => MaxArmorHealth;

        private Node3D? _turretNode;
        private AudioStreamPlayer3D _cannonAudio = null!;
        private AudioStreamPlayer3D _engineAudio = null!;
        private double _cannonCooldown = 0.0;
        private double _patrolTimer = 0.0;

        public override void _Ready()
        {
            CurrentArmorHealth = MaxArmorHealth;
            CollisionLayer = GameConfig.LayerHeavyArmor;

            _cannonAudio = new AudioStreamPlayer3D();
            _cannonAudio.Stream = GD.Load<AudioStream>("res://assets/audio/explosion_heavy.wav");
            AddChild(_cannonAudio);

            // Load 3D model
            string modelPath = (Type == VehicleType.MainBattleTank)
                ? "res://assets/models/tank_merhava.glb"
                : "res://assets/models/apc_namer.glb";

            if (ResourceLoader.Exists(modelPath))
            {
                var scene = GD.Load<PackedScene>(modelPath);
                if (scene != null)
                {
                    var instance = scene.Instantiate();
                    AddChild(instance);
                    _turretNode = instance.FindChild("Turret", true, false) as Node3D;
                }
            }
        }

        public override void _PhysicsProcess(double delta)
        {
            if (IsDestroyed) return;

            _cannonCooldown -= delta;

            if (!IsImmobilized)
            {
                _patrolTimer += delta * 0.5;
                // Patrol back and forth in junction
                float speed = (float)Math.Sin(_patrolTimer) * PatrolSpeed;
                Velocity = -GlobalTransform.Basis.Z * speed;
                MoveAndSlide();
            }

            TrackAndFireAtPlayer(delta);
        }

        private void TrackAndFireAtPlayer(double delta)
        {
            var player = GetTree().Root.FindChild("Player", true, false) as PlayerController;
            if (player == null) return;

            float dist = GlobalPosition.DistanceTo(player.GlobalPosition);
            if (dist < 40.0f)
            {
                // Turret tracking
                if (_turretNode != null)
                {
                    _turretNode.LookAt(player.GlobalPosition, Vector3.Up);
                }

                // Fire cannon
                if (_cannonCooldown <= 0.0)
                {
                    _cannonCooldown = (Type == VehicleType.MainBattleTank) ? 4.5 : 2.5;
                    _cannonAudio?.Play();
                    GD.Print($"[ArmoredVehicle] {Type} fired main gun at player!");

                    if (dist < 15.0f)
                    {
                        player.Health.TakeDamage(40.0f, (player.GlobalPosition - GlobalPosition).Normalized(), this);
                    }
                }
            }
        }

        /// <summary>
        /// Handles anti-armor hit: Al-Yassin Tandem deals 1000f (1-hit kill).
        /// Standard RPG deals 500f (2-hit kill: 1st immobilizes, 2nd destroys).
        /// </summary>
        public void TakeArmorDamage(float amount, bool isTandem)
        {
            if (IsDestroyed) return;

            CurrentArmorHealth -= amount;
            GD.Print($"[ArmoredVehicle] Hit by {(isTandem ? "Al-Yassin Tandem" : "Standard RPG")}! Remaining Armor: {CurrentArmorHealth}/{MaxArmorHealth}");

            if (CurrentArmorHealth <= 500.0f && !IsImmobilized && !isTandem)
            {
                // 1st RPG hit immobilizes tracks
                IsImmobilized = true;
                Velocity = Vector3.Zero;
                GD.Print("[ArmoredVehicle] TRACKS DISABLED! Vehicle immobilized in Kill Zone!");
                GameManager.Instance.DisplaySubtitle("أبو حمزة", "شللنا حركة الدبابة! جهزوا القذيفة الثانية للإجهاز عليها!", 3.5f);
            }

            if (CurrentArmorHealth <= 0.0f || isTandem)
            {
                DestroyVehicle(isTandem);
            }
        }

        public void TakeDamage(float amount, Vector3 hitDirection, Node? attacker = null)
        {
            // Small arms only tick armor negligibly
            TakeArmorDamage(amount * 0.1f, false);
        }

        public void Heal(float amount) {}

        private void DestroyVehicle(bool isTandem)
        {
            IsDestroyed = true;
            IsImmobilized = true;

            if (Type == VehicleType.MainBattleTank)
                GameManager.Instance.TanksDestroyed++;
            else
                GameManager.Instance.ApcsDestroyed++;

            _cannonAudio?.Play();
            string weaponUsed = isTandem ? "بقذيفة الياسين 105 الترادفية مباشرة" : "بقذيفتي آر بي جي";
            GameManager.Instance.DisplaySubtitle("القائد", $"الله أكبر! تم تدمير الآلية {weaponUsed} وتصاعد الدخان الأسود!", 4.0f);

            // Change appearance / spawn black smoke plume
            var smokePlume = new GpuParticles3D();
            AddChild(smokePlume);
        }
    }
}

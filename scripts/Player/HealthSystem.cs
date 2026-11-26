using System;
using Godot;
using GazaGame.Core;

namespace GazaGame.Player
{
    public interface IDamageable
    {
        float CurrentHealth { get; }
        float MaxHealth { get; }
        void TakeDamage(float amount, Vector3 hitDirection, Node? attacker = null);
        void Heal(float amount);
    }

    /// <summary>
    /// Health system featuring out-of-combat health regeneration,
    /// directional damage feedback, and rapid tactical respawn.
    /// </summary>
    public partial class HealthSystem : Node, IDamageable
    {
        [Signal]
        public delegate void HealthChangedEventHandler(float current, float max);

        [Signal]
        public delegate void DamagedEventHandler(float amount, Vector3 hitDirection);

        [Signal]
        public delegate void PlayerDiedEventHandler();

        [Signal]
        public delegate void PlayerRespawnedEventHandler(Vector3 spawnPosition);

        [Export] public float MaxHealth { get; private set; } = GameConfig.DefaultMaxHealth;
        public float CurrentHealth { get; private set; }

        private double _timeSinceLastDamage = 0.0;
        private double _respawnTimer = 0.0;
        private bool _isDead = false;

        public override void _Ready()
        {
            CurrentHealth = MaxHealth;
        }

        public override void _Process(double delta)
        {
            if (_isDead)
            {
                _respawnTimer -= delta;
                if (_respawnTimer <= 0.0)
                {
                    Respawn();
                }
                return;
            }

            // Health Regeneration logic (20 HP/s after 4.0s out of combat)
            _timeSinceLastDamage += delta;
            if (_timeSinceLastDamage >= GameConfig.HealthRegenDelaySeconds && CurrentHealth < MaxHealth)
            {
                CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + (GameConfig.HealthRegenRatePerSec * (float)delta));
                EmitSignal(SignalName.HealthChanged, CurrentHealth, MaxHealth);
            }
        }

        public void TakeDamage(float amount, Vector3 hitDirection, Node? attacker = null)
        {
            if (_isDead) return;

            CurrentHealth = Mathf.Max(0.0f, CurrentHealth - amount);
            _timeSinceLastDamage = 0.0;

            EmitSignal(SignalName.HealthChanged, CurrentHealth, MaxHealth);
            EmitSignal(SignalName.Damaged, amount, hitDirection);

            if (CurrentHealth <= 0.0f)
            {
                Die();
            }
        }

        public void Heal(float amount)
        {
            if (_isDead) return;
            CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + amount);
            EmitSignal(SignalName.HealthChanged, CurrentHealth, MaxHealth);
        }

        private void Die()
        {
            _isDead = true;
            _respawnTimer = GD.RandRange(GameConfig.RespawnTimerMinSeconds, GameConfig.RespawnTimerMaxSeconds);
            EmitSignal(SignalName.PlayerDied);
            GD.Print($"[HealthSystem] Player down! Quick tactical respawn in {_respawnTimer:F1}s...");
        }

        private void Respawn()
        {
            _isDead = false;
            CurrentHealth = MaxHealth;
            _timeSinceLastDamage = 0.0;

            // Rally point position (Underground prayer room or safe rally zone)
            Vector3 rallyPoint = new Vector3(0, 0.5f, 0);
            var parent = GetParent<Node3D>();
            if (parent != null)
            {
                parent.GlobalPosition = rallyPoint;
            }

            EmitSignal(SignalName.HealthChanged, CurrentHealth, MaxHealth);
            EmitSignal(SignalName.PlayerRespawned, rallyPoint);
            GD.Print("[HealthSystem] Player redeployed at squad rally point.");
        }
    }
}

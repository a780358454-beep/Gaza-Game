using System;
using Godot;
using GazaGame.Core;
using GazaGame.Player;
using GazaGame.AI;

namespace GazaGame.Weapons
{
    /// <summary>
    /// Specialized rocket projectile handling Tandem warheads, standard RPGs, and mortar shells.
    /// </summary>
    public partial class RocketProjectile : Area3D
    {
        public float Speed = 55.0f;
        public float Damage = 500.0f;
        public float ArmorDamage = 500.0f;
        public float BlastRadius = 6.0f;
        public bool IsTandem = false;
        public Vector3 Velocity = Vector3.Zero;

        private double _lifetime = 6.0;

        public override void _Ready()
        {
            BodyEntered += OnBodyEntered;
            AreaEntered += OnAreaEntered;
        }

        public override void _PhysicsProcess(double delta)
        {
            GlobalPosition += Velocity * (float)delta;
            _lifetime -= delta;
            if (_lifetime <= 0)
            {
                Explode();
            }
        }

        private void OnBodyEntered(Node3D body)
        {
            Impact(body);
        }

        private void OnAreaEntered(Area3D area)
        {
            Impact(area);
        }

        private void Impact(Node hitNode)
        {
            if (hitNode is ArmoredVehicle armor)
            {
                // Al-Yassin Tandem deals 1000f (1-hit kill). Standard RPG deals 500f (2-hit kill).
                armor.TakeArmorDamage(ArmorDamage, IsTandem);
            }
            else if (hitNode is IDamageable damageable)
            {
                damageable.TakeDamage(Damage, Velocity.Normalized(), this);
            }

            Explode();
        }

        private void Explode()
        {
            // Radial blast damage to nearby infantry
            var space = GetWorld3D().DirectSpaceState;
            var shape = new SphereShape3D { Radius = BlastRadius };
            var query = new PhysicsShapeQueryParameters3D
            {
                Shape = shape,
                Transform = GlobalTransform,
                CollisionMask = GameConfig.LayerEnemy | GameConfig.LayerSquad
            };

            var hits = space.IntersectShape(query, 16);
            foreach (var hit in hits)
            {
                var col = hit["collider"].As<Node>();
                if (col is IDamageable damageable && col != this)
                {
                    damageable.TakeDamage(Damage * 0.5f, Vector3.Up, this);
                }
            }

            QueueFree();
        }
    }

    /// <summary>
    /// Al-Yassin 105 Tandem launcher: 1-hit tank destroyer.
    /// </summary>
    public partial class AlYassinTandem : WeaponBase
    {
        public override bool Fire()
        {
            if (!base.Fire()) return false;

            SpawnRocket(isTandem: true, armorDamage: 1000.0f, speed: 65.0f);
            return true;
        }

        private void SpawnRocket(bool isTandem, float armorDamage, float speed)
        {
            var proj = new RocketProjectile
            {
                Speed = speed,
                Damage = 450.0f,
                ArmorDamage = armorDamage,
                IsTandem = isTandem,
                Velocity = -GlobalTransform.Basis.Z * speed,
                GlobalTransform = GlobalTransform
            };

            GetTree().Root.AddChild(proj);
        }
    }

    /// <summary>
    /// Standard RPG-7 launcher: 2-hit vehicle neutralizer.
    /// </summary>
    public partial class StandardRPG : WeaponBase
    {
        public override bool Fire()
        {
            if (!base.Fire()) return false;

            SpawnRocket(isTandem: false, armorDamage: 500.0f, speed: 50.0f);
            return true;
        }

        private void SpawnRocket(bool isTandem, float armorDamage, float speed)
        {
            var proj = new RocketProjectile
            {
                Speed = speed,
                Damage = 350.0f,
                ArmorDamage = armorDamage,
                IsTandem = isTandem,
                Velocity = -GlobalTransform.Basis.Z * speed,
                GlobalTransform = GlobalTransform
            };

            GetTree().Root.AddChild(proj);
        }
    }

    /// <summary>
    /// Field Mortar deployable tube with top-down coordinate high-arc strike capabilities.
    /// </summary>
    public partial class FieldMortar : WeaponBase
    {
        [Signal]
        public delegate void MortarShellLandedEventHandler(Vector3 targetLocation);

        public void FireAtCoordinates(Vector3 mapTarget)
        {
            if (ReserveAmmo <= 0)
            {
                GD.Print("[Mortar] No shells left!");
                return;
            }

            ReserveAmmo--;
            AudioPlayer?.Play();
            GD.Print($"[Mortar] High-arc shell launched at ({mapTarget.X:F1}, {mapTarget.Z:F1})!");

            // Delay shell landing by 2.5 seconds flight time
            GetTree().CreateTimer(2.5).Timeout += () =>
            {
                DetonateAt(mapTarget);
            };
        }

        private void DetonateAt(Vector3 target)
        {
            EmitSignal(SignalName.MortarShellLanded, target);

            var space = GetWorld3D().DirectSpaceState;
            var shape = new SphereShape3D { Radius = 9.0f };
            var query = new PhysicsShapeQueryParameters3D
            {
                Shape = shape,
                Transform = new Transform3D(Basis.Identity, target),
                CollisionMask = GameConfig.LayerEnemy | GameConfig.LayerHeavyArmor
            };

            var hits = space.IntersectShape(query, 16);
            foreach (var hit in hits)
            {
                var col = hit["collider"].As<Node>();
                if (col is ArmoredVehicle armor)
                {
                    armor.TakeArmorDamage(350.0f, false);
                }
                else if (col is IDamageable damageable)
                {
                    damageable.TakeDamage(250.0f, Vector3.Up, this);
                }
            }
        }
    }
}

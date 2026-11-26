using System;
using Godot;
using GazaGame.Core;
using GazaGame.Player;

namespace GazaGame.Weapons
{
    /// <summary>
    /// Base class for all firearms, heavy ordnance launchers, and tactical equipment.
    /// </summary>
    public partial class WeaponBase : Node3D
    {
        [Signal]
        public delegate void AmmoChangedEventHandler(int currentInMag, int reserve);

        [Signal]
        public delegate void WeaponFiredEventHandler();

        public GameConfig.WeaponData Data { get; private set; } = null!;
        public int CurrentMag { get; protected set; }
        public int ReserveAmmo { get; protected set; }

        protected double FireCooldown = 0.0;
        protected bool IsReloading = false;
        protected double ReloadTimer = 0.0;

        protected AudioStreamPlayer3D AudioPlayer = null!;
        protected Node3D Muzzle = null!;

        public void Initialize(GameConfig.WeaponData data)
        {
            Data = data;
            CurrentMag = data.MagCapacity;
            ReserveAmmo = data.MaxReserve;

            AudioPlayer = new AudioStreamPlayer3D();
            AddChild(AudioPlayer);

            Muzzle = new Node3D();
            Muzzle.Position = new Vector3(0, 0, -0.6f);
            AddChild(Muzzle);

            // Load 3D model if specified
            if (!string.IsNullOrEmpty(data.ModelPath) && ResourceLoader.Exists(data.ModelPath))
            {
                var scene = GD.Load<PackedScene>(data.ModelPath);
                if (scene != null)
                {
                    var model = scene.Instantiate();
                    AddChild(model);
                }
            }

            if (!string.IsNullOrEmpty(data.SoundPath) && ResourceLoader.Exists(data.SoundPath))
            {
                AudioPlayer.Stream = GD.Load<AudioStream>(data.SoundPath);
            }
        }

        public override void _Process(double delta)
        {
            if (FireCooldown > 0)
            {
                FireCooldown -= delta;
            }

            if (IsReloading)
            {
                ReloadTimer -= delta;
                if (ReloadTimer <= 0.0)
                {
                    FinishReload();
                }
            }
        }

        public virtual bool Fire()
        {
            if (FireCooldown > 0 || IsReloading) return false;
            if (CurrentMag <= 0)
            {
                Reload();
                return false;
            }

            CurrentMag--;
            FireCooldown = 60.0 / Data.FireRateRpm;

            AudioPlayer?.Play();
            EmitSignal(SignalName.WeaponFired);
            EmitSignal(SignalName.AmmoChanged, CurrentMag, ReserveAmmo);

            // Perform Raycast bullet hit
            PerformHitscan();
            return true;
        }

        protected virtual void PerformHitscan()
        {
            var space = GetWorld3D().DirectSpaceState;
            Vector3 origin = GlobalPosition;
            Vector3 forward = -GlobalTransform.Basis.Z;
            Vector3 target = origin + forward * 100.0f;

            var query = PhysicsRayQueryParameters3D.Create(origin, target);
            query.CollisionMask = GameConfig.LayerWorld | GameConfig.LayerEnemy | GameConfig.LayerHeavyArmor;

            var result = space.IntersectRay(query);
            if (result.Count > 0)
            {
                var collider = result["collider"].As<Node>();
                var hitPos = result["position"].As<Vector3>();

                if (collider is IDamageable damageable)
                {
                    damageable.TakeDamage(Data.Damage, forward, this);
                }
            }
        }

        public virtual void Reload()
        {
            if (IsReloading || CurrentMag >= Data.MagCapacity || ReserveAmmo <= 0) return;

            IsReloading = true;
            ReloadTimer = 2.2;
            GD.Print($"[Weapon] Reloading {Data.NameAr}...");
        }

        protected virtual void FinishReload()
        {
            int needed = Data.MagCapacity - CurrentMag;
            int taken = Math.Min(needed, ReserveAmmo);
            CurrentMag += taken;
            ReserveAmmo -= taken;
            IsReloading = false;
            EmitSignal(SignalName.AmmoChanged, CurrentMag, ReserveAmmo);
        }

        public void AddAmmo(int amount)
        {
            ReserveAmmo = Math.Min(Data.MaxReserve, ReserveAmmo + amount);
            EmitSignal(SignalName.AmmoChanged, CurrentMag, ReserveAmmo);
        }
    }
}

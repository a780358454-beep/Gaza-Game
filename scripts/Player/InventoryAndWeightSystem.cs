using System;
using System.Collections.Generic;
using Godot;
using GazaGame.Core;
using GazaGame.Weapons;

namespace GazaGame.Player
{
    /// <summary>
    /// Manages player weapons, grenades, ammunition reserves, weight calculation,
    /// and dynamic encumbrance penalties.
    /// </summary>
    public partial class InventoryAndWeightSystem : Node
    {
        [Signal]
        public delegate void EncumbranceChangedEventHandler(bool isOverencumbered, float currentWeight, float maxWeight);

        [Signal]
        public delegate void ActiveWeaponSwitchedEventHandler(string weaponId, string weaponNameAr);

        public List<WeaponBase> EquippedWeapons { get; } = new();
        public int CurrentWeaponIndex { get; private set; } = 0;
        public WeaponBase? PrimaryWeaponInstance => EquippedWeapons.Count > 0 ? EquippedWeapons[CurrentWeaponIndex] : null;

        public int FragGrenadeCount { get; set; } = 2;
        public int SmokeGrenadeCount { get; set; } = 2;
        public int StickyChargeCount { get; set; } = 2;

        public float TotalWeightKg { get; private set; } = 0.0f;
        public bool IsOverencumbered => TotalWeightKg >= GameConfig.HeavyWeightThresholdKg;

        public override void _Ready()
        {
            SetupBaseKit();
        }

        public void SetupBaseKit()
        {
            EquippedWeapons.Clear();

            // Load selected weapons from GameManager or defaults
            var selection = GameManager.Instance != null ? GameManager.Instance.SelectedWeapons : new List<string> { "ak47", "pistol", "alyassin" };

            var parent = GetParent<PlayerController>();
            Node3D mount = (Node3D)parent?.WeaponMount ?? this;

            foreach (var wId in selection)
            {
                if (GameConfig.Weapons.TryGetValue(wId, out var data))
                {
                    WeaponBase weapon;
                    if (wId == "alyassin")
                        weapon = new AlYassinTandem();
                    else if (wId == "rpg7")
                        weapon = new StandardRPG();
                    else if (wId == "mortar")
                        weapon = new FieldMortar();
                    else
                        weapon = new WeaponBase();

                    weapon.Initialize(data);
                    weapon.Visible = false;
                    mount.AddChild(weapon);
                    EquippedWeapons.Add(weapon);
                }
            }

            RecalculateWeight();
            if (EquippedWeapons.Count > 0)
            {
                EquippedWeapons[0].Visible = true;
                EmitSignal(SignalName.ActiveWeaponSwitched, EquippedWeapons[0].Data.Id, EquippedWeapons[0].Data.NameAr);
            }
        }

        public void CycleNextWeapon()
        {
            if (EquippedWeapons.Count <= 1) return;

            EquippedWeapons[CurrentWeaponIndex].Visible = false;
            CurrentWeaponIndex = (CurrentWeaponIndex + 1) % EquippedWeapons.Count;
            EquippedWeapons[CurrentWeaponIndex].Visible = true;

            var active = EquippedWeapons[CurrentWeaponIndex];
            EmitSignal(SignalName.ActiveWeaponSwitched, active.Data.Id, active.Data.NameAr);
            GD.Print($"[Inventory] Switched to: {active.Data.NameAr}");
        }

        public void RecalculateWeight()
        {
            float total = 0.0f;
            foreach (var w in EquippedWeapons)
            {
                total += w.Data.WeightKg;
            }

            total += FragGrenadeCount * 0.4f;
            total += SmokeGrenadeCount * 0.5f;
            total += StickyChargeCount * 2.5f;

            TotalWeightKg = Mathf.Min(GameConfig.MaxWeightKg, total);
            EmitSignal(SignalName.EncumbranceChanged, IsOverencumbered, TotalWeightKg, GameConfig.MaxWeightKg);

            if (IsOverencumbered)
            {
                GD.Print($"[Inventory] WARNING: Heavy Weight ({TotalWeightKg:F1}kg / {GameConfig.MaxWeightKg:F1}kg) - الوزن العملاق!");
            }
        }

        public void ThrowGrenade(bool isSmoke)
        {
            if (isSmoke)
            {
                if (SmokeGrenadeCount <= 0) return;
                SmokeGrenadeCount--;
                GD.Print("[Inventory] Deployed Tactical Smoke Grenade for concealment!");
                DeploySmokeCover();
            }
            else
            {
                if (FragGrenadeCount <= 0) return;
                FragGrenadeCount--;
                GD.Print("[Inventory] Threw Frag Grenade!");
            }
            RecalculateWeight();
        }

        private void DeploySmokeCover()
        {
            var parent = GetParent<PlayerController>();
            if (parent == null) return;

            // Spawn smoke cloud area that masks player from sniper & recon drone
            var smokeArea = new Area3D();
            smokeArea.CollisionLayer = GameConfig.LayerSmokeCover;
            var col = new CollisionShape3D
            {
                Shape = new SphereShape3D { Radius = 8.0f }
            };
            smokeArea.AddChild(col);
            smokeArea.GlobalPosition = parent.GlobalPosition;
            GetTree().Root.AddChild(smokeArea);

            // Auto-dissipate after 12 seconds
            GetTree().CreateTimer(12.0).Timeout += () => smokeArea.QueueFree();
        }
    }
}

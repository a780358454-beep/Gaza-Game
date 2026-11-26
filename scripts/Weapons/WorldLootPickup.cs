using Godot;
using GazaGame.Core;
using GazaGame.Player;

namespace GazaGame.Weapons
{
    public interface ILootable
    {
        string LootNameAr { get; }
        string LootPromptAr { get; }
        void Pickup(PlayerController player);
    }

    /// <summary>
    /// Interactive world pickup node for dropped enemy firearms, ammo crates, and rockets.
    /// </summary>
    public partial class WorldLootPickup : Area3D, ILootable
    {
        [Export] public string WeaponId = "ak47";
        [Export] public int AmmoAmount = 60;
        [Export] public string LootNameAr { get; set; } = "ذخيرة بنادق وسلاح عدو";
        public string LootPromptAr => $"التقاط {LootNameAr} [اضغط هنا]";

        public override void _Ready()
        {
            CollisionLayer = GameConfig.LayerPickups;
            CollisionMask = GameConfig.LayerPlayer;

            BodyEntered += OnBodyEntered;
            BodyExited += OnBodyExited;

            // Load 3D model if exists
            string modelPath = "res://assets/models/ammo_box.glb";
            if (ResourceLoader.Exists(modelPath))
            {
                var scene = GD.Load<PackedScene>(modelPath);
                if (scene != null)
                {
                    AddChild(scene.Instantiate());
                }
            }
        }

        private void OnBodyEntered(Node3D body)
        {
            if (body is PlayerController player)
            {
                GD.Print($"[Loot] Available to pick up: {LootNameAr}");
            }
        }

        private void OnBodyExited(Node3D body)
        {
        }

        public void Pickup(PlayerController player)
        {
            if (player?.Inventory?.PrimaryWeaponInstance != null)
            {
                player.Inventory.PrimaryWeaponInstance.AddAmmo(AmmoAmount);
                player.PlayAudioClip("res://assets/audio/radio_click.wav");
                GD.Print($"[Loot] Collected {AmmoAmount} rounds of {LootNameAr}!");
                QueueFree();
            }
        }
    }
}

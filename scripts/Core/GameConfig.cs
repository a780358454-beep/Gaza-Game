using System.Collections.Generic;
using Godot;

namespace GazaGame.Core
{
    /// <summary>
    /// Global game configuration, physics layers, weapons data, and encumbrance tables.
    /// </summary>
    public static class GameConfig
    {
        public const string GameTitle = "غزة";
        public const string GameTitleEnglish = "Gaza";
        public const string GameVersion = "1.0.0";

        // Physics Layers Bitmasks
        public const uint LayerWorld = 1 << 0;       // 1
        public const uint LayerPlayer = 1 << 1;      // 2
        public const uint LayerSquad = 1 << 2;       // 4
        public const uint LayerEnemy = 1 << 3;       // 8
        public const uint LayerHeavyArmor = 1 << 4;  // 16
        public const uint LayerProjectiles = 1 << 5; // 32
        public const uint LayerPickups = 1 << 6;     // 64
        public const uint LayerSmokeCover = 1 << 7;  // 128

        // Networking Ports
        public const int LanDiscoveryPort = 4242;
        public const int GameServerPort = 7777;
        public const int MaxSquadMembers = 4;

        // Encumbrance & Weight Constants
        public const float MaxWeightKg = 45.0f;
        public const float HeavyWeightThresholdKg = 38.0f; // "الوزن العملاق"
        public const float HeavyEncumbranceSpeedMultiplier = 0.65f; // 35% speed reduction
        public const float HeavyEncumbranceJumpMultiplier = 0.70f;

        // Health Constants
        public const float DefaultMaxHealth = 100.0f;
        public const float HealthRegenDelaySeconds = 4.0f;
        public const float HealthRegenRatePerSec = 20.0f;
        public const float RespawnTimerMinSeconds = 3.0f;
        public const float RespawnTimerMaxSeconds = 5.0f;

        // Drone Detection
        public const float DroneDetectionLightThresholdSeconds = 3.0f;

        // Weapon Definition Struct
        public record WeaponData(
            string Id,
            string NameAr,
            string NameEn,
            float WeightKg,
            int MagCapacity,
            int MaxReserve,
            float Damage,
            float FireRateRpm,
            float ArmorPenetrationDamage,
            string ModelPath,
            string SoundPath
        );

        // Weapon Catalog
        public static readonly Dictionary<string, WeaponData> Weapons = new()
        {
            ["ak47"] = new WeaponData(
                "ak47", "بندقية كلاشنكوف تكتيكية", "Tactical Assault Rifle",
                3.8f, 30, 150, 35.0f, 600.0f, 50.0f,
                "res://assets/models/ak47.glb", "res://assets/audio/gunfire_ak47.wav"
            ),
            ["pistol"] = new WeaponData(
                "pistol", "مسدس 9 ملم مع كاتم صوت", "Suppressed 9mm Pistol",
                0.9f, 15, 60, 28.0f, 400.0f, 15.0f,
                "res://assets/models/pistol.glb", "res://assets/audio/gunfire_ak47.wav"
            ),
            ["knife"] = new WeaponData(
                "knife", "خنجر قتالي تكتيكي", "Combat Knife",
                0.4f, 1, 0, 100.0f, 120.0f, 10.0f,
                "", "res://assets/audio/footstep_rubble.wav"
            ),
            ["alyassin"] = new WeaponData(
                "alyassin", "قاذف الياسين 105 الترادفي", "Al-Yassin 105 Tandem Launcher",
                7.5f, 1, 3, 1000.0f, 20.0f, 1000.0f, // 100% 1-Hit Tank Destroyer
                "res://assets/models/alyassin_tandem.glb", "res://assets/audio/rocket_launch.wav"
            ),
            ["rpg7"] = new WeaponData(
                "rpg7", "قاذف آر بي جي 7 القياسي", "Standard RPG-7 Launcher",
                6.3f, 1, 4, 500.0f, 25.0f, 500.0f, // 50% 2-Hit Tank Destroyer
                "res://assets/models/rpg7.glb", "res://assets/audio/rocket_launch.wav"
            ),
            ["mortar"] = new WeaponData(
                "mortar", "مدفع هاون ميداني عيار 60", "Field Mortar 60mm",
                12.0f, 1, 6, 250.0f, 15.0f, 300.0f,
                "res://assets/models/mortar.glb", "res://assets/audio/mortar_fire.wav"
            ),
            ["sniper"] = new WeaponData(
                "sniper", "بندقية قنص ثقيلة مع ناظور", "Heavy Scoped Sniper Rifle",
                5.2f, 10, 40, 150.0f, 50.0f, 120.0f,
                "res://assets/models/ak47.glb", "res://assets/audio/sniper_shot.wav"
            ),
            ["frag_grenade"] = new WeaponData(
                "frag_grenade", "قنبلة يدوية شظوية", "Frag Grenade",
                0.4f, 1, 4, 180.0f, 60.0f, 80.0f,
                "", "res://assets/audio/explosion_heavy.wav"
            ),
            ["smoke_grenade"] = new WeaponData(
                "smoke_grenade", "قنبلة دخانية حاجية", "Tactical Smoke Grenade",
                0.5f, 1, 4, 0.0f, 60.0f, 0.0f,
                "", "res://assets/audio/radio_click.wav"
            ),
            ["sticky_charge"] = new WeaponData(
                "sticky_charge", "عبوة اختراق لاصقة مدرعة", "Armored Sticky Breaching Charge",
                2.5f, 1, 3, 600.0f, 30.0f, 800.0f,
                "res://assets/models/ammo_box.glb", "res://assets/audio/explosion_heavy.wav"
            )
        };

        // Standard 4-man Squad Roster
        public record SquadCharacter(string Id, string NameAr, string RoleAr, string DefaultHeavyWeaponId);

        public static readonly SquadCharacter[] SquadRoster =
        {
            new("abu_bakr", "أبو بكر", "قائد ميداني / اقتحام وتخطيط", "alyassin"),
            new("abu_hamza", "أبو حمزة", "رامي الدروع والأسلحة الثقيلة", "alyassin"),
            new("abu_al_baraa", "أبو البراء", "خبير العبوات والاختراق الهندسي", "sticky_charge"),
            new("abu_anas", "أبو أنس", "مسعف الميدان ورامي الرشاش الخفيف والدعم", "mortar")
        };
    }
}

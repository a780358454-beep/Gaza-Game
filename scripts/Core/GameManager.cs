using System;
using System.Collections.Generic;
using Godot;

namespace GazaGame.Core
{
    public enum GameMode
    {
        HistoricalCampaign, // طور القصة الموثقة (9 مشاهد)
        FreePlaySolo,       // الطور العادي - لعب فردي
        MultiplayerCoop,    // طور تعاوني شبكي
        TeamDeathmatch,     // مواجهة تنافسية بين فريقين
        CaptureTarget,      // أسر / إنقاذ الهدف
        CaptureFlag         // الاستيلاء على الراية
    }

    public enum CampaignScene
    {
        Scene01_UndergroundPrayerRoom = 1, // مصلى العقدة السفلية - صلاة الفجر
        Scene02_ArchwaysAlley = 2,          // زقاق الأقواس - التسلل
        Scene03_TripleBuilding = 3,         // البناية الثلاثية - الاختراق الصامت
        Scene04_ConvoySpotting = 4,         // رصد الرتل وتوزيع الكمين
        Scene05_ZeroHourExplosion = 5,      // لحظة الصفر - انفجار التقاطع
        Scene06_InfantryCQB = 6,            // اشتباك المشاة القريب
        Scene07_WesternWingCollapse = 7,    // القصف المضاد وانهيار الجناح الغربي
        Scene08_CounterSniper = 8,          // التغطية المتبادلة ومواجهة القناص
        Scene09_CamouflagedTunnelGate = 9   // بوابة النفق المموهة - العودة بسلام
    }

    /// <summary>
    /// Singleton GameManager autoload managing Dual-Mode architecture, campaign progression,
    /// loadout configuration, and mission statistics.
    /// </summary>
    public partial class GameManager : Node
    {
        public static GameManager Instance { get; private set; } = null!;

        [Signal]
        public delegate void GameModeChangedEventHandler(int newMode);

        [Signal]
        public delegate void CampaignSceneChangedEventHandler(int sceneNumber, string titleAr);

        [Signal]
        public delegate void MissionCompletedEventHandler(float accuracy, int vehiclesDestroyed, float evacuationTime, int xpEarned);

        [Signal]
        public delegate void SubtitleUpdatedEventHandler(string speaker, string arabicText, float duration);

        public GameMode CurrentMode { get; set; } = GameMode.HistoricalCampaign;
        public CampaignScene CurrentCampaignScene { get; set; } = CampaignScene.Scene01_UndergroundPrayerRoom;
        public string SelectedCharacterId { get; set; } = "abu_bakr";

        // Current Player Loadout Selection
        public List<string> SelectedWeapons { get; } = new()
        {
            "ak47", "pistol", "knife", "frag_grenade", "smoke_grenade", "alyassin"
        };

        // Mission Stats
        public int TanksDestroyed { get; set; } = 0;
        public int ApcsDestroyed { get; set; } = 0;
        public int EnemiesNeutralized { get; set; } = 0;
        public float MissionElapsedTime { get; set; } = 0.0f;
        public int TacticalOrdersGiven { get; set; } = 0;

        public override void _Ready()
        {
            if (Instance == null)
            {
                Instance = this;
                ProcessMode = ProcessModeEnum.Always;
            }
            else
            {
                QueueFree();
            }
        }

        public override void _Process(double delta)
        {
            if (CurrentMode == GameMode.HistoricalCampaign)
            {
                MissionElapsedTime += (float)delta;
            }
        }

        /// <summary>
        /// Transitions to next campaign scene with proper narrative title and event triggers.
        /// </summary>
        public void AdvanceCampaignScene()
        {
            int next = (int)CurrentCampaignScene + 1;
            if (next <= 9)
            {
                CurrentCampaignScene = (CampaignScene)next;
                string titleAr = GetSceneArabicTitle(CurrentCampaignScene);
                EmitSignal(SignalName.CampaignSceneChanged, (int)CurrentCampaignScene, titleAr);
            }
            else
            {
                CompleteMission();
            }
        }

        public string GetSceneArabicTitle(CampaignScene scene)
        {
            return scene switch
            {
                CampaignScene.Scene01_UndergroundPrayerRoom => "المشهد 01: مصلى العقدة السفلية — صلاة الفجر وتوزيع العتاد",
                CampaignScene.Scene02_ArchwaysAlley => "المشهد 02: زقاق الأقواس — التسلل تحت عيون السماء",
                CampaignScene.Scene03_TripleBuilding => "المشهد 03: البناية الثلاثية — الاختراق الصامت وتأمين الموقع",
                CampaignScene.Scene04_ConvoySpotting => "المشهد 04: رصد الرتل وتوزيع الكمين المزدوج",
                CampaignScene.Scene05_ZeroHourExplosion => "المشهد 05: لحظة الصفر — انفجار التقاطع",
                CampaignScene.Scene06_InfantryCQB => "المشهد 06: اشتباك المشاة القريب وإدارة المخزون",
                CampaignScene.Scene07_WesternWingCollapse => "المشهد 07: القصف المضاد وانهيار الجناح الغربي",
                CampaignScene.Scene08_CounterSniper => "المشهد 08: التغطية المتبادلة ومواجهة القناص",
                CampaignScene.Scene09_CamouflagedTunnelGate => "المشهد 09: بوابة النفق المموهة — العودة بسلام",
                _ => "كمين العقدة"
            };
        }

        public void DisplaySubtitle(string speaker, string arabicText, float duration = 4.0f)
        {
            EmitSignal(SignalName.SubtitleUpdated, speaker, arabicText, duration);
        }

        public void CompleteMission()
        {
            float accuracy = Math.Min(100.0f, 75.0f + TacticalOrdersGiven * 3.5f);
            int destroyed = TanksDestroyed + ApcsDestroyed;
            int xpEarned = (destroyed * 500) + (EnemiesNeutralized * 75) + 1200;
            EmitSignal(SignalName.MissionCompleted, accuracy, destroyed, MissionElapsedTime, xpEarned);
        }

        public float CalculateCurrentLoadoutWeight()
        {
            float total = 0.0f;
            foreach (var wId in SelectedWeapons)
            {
                if (GameConfig.Weapons.TryGetValue(wId, out var w))
                {
                    total += w.WeightKg;
                }
            }
            return total;
        }

        public bool IsOverencumbered()
        {
            return CalculateCurrentLoadoutWeight() >= GameConfig.HeavyWeightThresholdKg;
        }
    }
}

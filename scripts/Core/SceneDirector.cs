using System;
using Godot;
using GazaGame.AI;
using GazaGame.Player;
using GazaGame.Weapons;

namespace GazaGame.Core
{
    /// <summary>
    /// Narrative Scene Director controlling cinematic triggers, dialogues,
    /// building structural collapses, and mission progression across all 9 scenes.
    /// </summary>
    public partial class SceneDirector : Node
    {
        public static SceneDirector Instance { get; private set; } = null!;

        [Export] public Node3D? WesternWingBuilding;
        [Export] public Node3D? TunnelGate;
        [Export] public Camera3D? LevelCamera;

        private AudioStreamPlayer? _ambientAudioPlayer;
        private double _sceneTimer = 0.0;
        private bool _fajrPlayed = false;

        public override void _Ready()
        {
            Instance = this;

            _ambientAudioPlayer = new AudioStreamPlayer();
            AddChild(_ambientAudioPlayer);

            // Start Act 1, Scene 1
            TriggerScene(CampaignScene.Scene01_UndergroundPrayerRoom);
        }

        public void TriggerScene(CampaignScene scene)
        {
            GameManager.Instance.CurrentCampaignScene = scene;
            _sceneTimer = 0.0;

            switch (scene)
            {
                case CampaignScene.Scene01_UndergroundPrayerRoom:
                    PlayFajrPrayerIntro();
                    break;
                case CampaignScene.Scene02_ArchwaysAlley:
                    PlayArchwaysAlleyInfiltration();
                    break;
                case CampaignScene.Scene03_TripleBuilding:
                    PlayTripleBuildingBreach();
                    break;
                case CampaignScene.Scene04_ConvoySpotting:
                    PlayConvoySpotting();
                    break;
                case CampaignScene.Scene05_ZeroHourExplosion:
                    PlayZeroHourAmbush();
                    break;
                case CampaignScene.Scene06_InfantryCQB:
                    PlayInfantryEngagement();
                    break;
                case CampaignScene.Scene07_WesternWingCollapse:
                    PlayWesternWingCollapse();
                    break;
                case CampaignScene.Scene08_CounterSniper:
                    PlayCounterSniperEngagement();
                    break;
                case CampaignScene.Scene09_CamouflagedTunnelGate:
                    PlayTunnelGateExtraction();
                    break;
            }
        }

        private void PlayFajrPrayerIntro()
        {
            GD.Print("[SceneDirector] Act 1, Scene 01: مصلى العقدة السفلية - صلاة الفجر وتوزيع العتاد");

            if (!_fajrPlayed && ResourceLoader.Exists("res://assets/audio/fajr_adhan.wav"))
            {
                _fajrPlayed = true;
                _ambientAudioPlayer!.Stream = GD.Load<AudioStream>("res://assets/audio/fajr_adhan.wav");
                _ambientAudioPlayer.Play();
            }

            GameManager.Instance.DisplaySubtitle("القائد", "تقبل الله منا ومنكم يا إخوان... دقائق وتبدأ ساعة الجد. الرتل المدرع سيعبر تقاطع الشهداء مع أول ضوء، ومهمتنا قطع خط إمداده دون أن نترك أثراً.", 5.0f);

            GetTree().CreateTimer(5.5).Timeout += () =>
            {
                GameManager.Instance.DisplaySubtitle("أبو البراء", "المبنى المطل على التقاطع جدرانه متصدعة من القصف السابق؛ فتحة صغيرة في الجدار الداخلي ستوصلنا للطابق الثاني دون أن نخرج للشارع.", 4.5f);
            };

            GetTree().CreateTimer(10.5).Timeout += () =>
            {
                GameManager.Instance.DisplaySubtitle("أبو أنس", "السماء فوقنا مليئة بطائرات الاستطلاع منذ الليل، الحركة في المناطق المكشوفة ممنوعة تماماً.", 4.5f);
            };
        }

        private void PlayArchwaysAlleyInfiltration()
        {
            GD.Print("[SceneDirector] Act 1, Scene 02: زقاق الأقواس - التسلل تحت عيون السماء");
            GameManager.Instance.DisplaySubtitle("أبو أنس", "استطلاع منخفض... اثبتوا في الظل، لا حركة!", 4.0f);
        }

        private void PlayTripleBuildingBreach()
        {
            GD.Print("[SceneDirector] Act 1, Scene 03: البناية الثلاثية - الاختراق الصامت");
            GameManager.Instance.DisplaySubtitle("القائد", "أبو البراء، افتح لنا ثغرة صامتة في هذا الجدار. البقية، غطوا الزوايا.", 4.0f);
        }

        private void PlayConvoySpotting()
        {
            GD.Print("[SceneDirector] Act 2, Scene 04: رصد الرتل وتوزيع الكمين المزدوج");
            GameManager.Instance.DisplaySubtitle("أبو حمزة", "الرتل دخل التقاطع... دبابة في المقدمة وخلفها ناقلة جند ومجموعة مشاة.", 4.0f);

            GetTree().CreateTimer(4.5).Timeout += () =>
            {
                GameManager.Instance.DisplaySubtitle("القائد", "لا أحد يطلق طلقة واحدة قبل إشارتي. سنطبق عليهم من زاويتين في نفس اللحظة.", 4.0f);
            };
        }

        private void PlayZeroHourAmbush()
        {
            GD.Print("[SceneDirector] Act 2, Scene 05: لحظة الصفر - انفجار التقاطع");
            GameManager.Instance.DisplaySubtitle("القائد", "بسم الله... اضرب يا أبو حمزة!", 3.5f);
        }

        private void PlayInfantryEngagement()
        {
            GD.Print("[SceneDirector] Act 2, Scene 06: اشتباك المشاة القريب وإدارة المخزون");
            GameManager.Instance.DisplaySubtitle("أبو حمزة", "الذخيرة عندي أوشكت على النفاد، والمشاة يتقدمون نحو المدخل!", 4.0f);

            GetTree().CreateTimer(4.2).Timeout += () =>
            {
                GameManager.Instance.DisplaySubtitle("أبو أنس", "أنتقل إليك بصندوق الإمداد... غطّني!", 3.5f);
            };
        }

        private void PlayWesternWingCollapse()
        {
            GD.Print("[SceneDirector] Act 3, Scene 07: القصف المضاد وانهيار الجناح الغربي");

            // Play artillery shock audio
            var boom = new AudioStreamPlayer();
            boom.Stream = GD.Load<AudioStream>("res://assets/audio/explosion_heavy.wav");
            AddChild(boom);
            boom.Play();

            // Collapse western wing physically
            if (WesternWingBuilding != null)
            {
                WesternWingBuilding.Position += new Vector3(0, -2.5f, 0);
                WesternWingBuilding.RotationDegrees += new Vector3(8.0f, 0, 12.0f);
            }

            // Damage nearby player with shrapnel (forces cover to regenerate)
            var player = GetTree().Root.FindChild("Player", true, false) as PlayerController;
            player?.Health.TakeDamage(35.0f, Vector3.Up, this);

            GameManager.Instance.DisplaySubtitle("القائد", "المبنى سينهار! المهمة أُنجزت بنجاح... ارموا الدخان وابدأوا الانسحاب الفوري نحو الشرق!", 5.0f);
        }

        private void PlayCounterSniperEngagement()
        {
            GD.Print("[SceneDirector] Act 3, Scene 08: التغطية المتبادلة ومواجهة القناص");
            GameManager.Instance.DisplaySubtitle("أبو أنس", "قناص في المبنى المقابل، الطابق الرابع! انبطحوا فوراً!", 4.0f);
        }

        private void PlayTunnelGateExtraction()
        {
            GD.Print("[SceneDirector] Act 3, Scene 09: بوابة النفق المموهة - العودة بسلام");
            GameManager.Instance.DisplaySubtitle("أبو حمزة", "الحمد لله... وصلنا جميعاً بعد إنجاز المهمة.", 4.5f);

            GetTree().CreateTimer(5.0).Timeout += () =>
            {
                GameManager.Instance.DisplaySubtitle("القائد", "انتهت جولة، وبقيت معركة... استعدوا للمهمة القادمة.", 4.5f);
            };

            GetTree().CreateTimer(10.0).Timeout += () =>
            {
                GameManager.Instance.CompleteMission();
            };
        }
    }
}

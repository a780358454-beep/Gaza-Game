using System;
using System.Collections.Generic;
using Godot;
using GazaGame.Core;

namespace GazaGame.UI
{
    /// <summary>
    /// Armory Loadout Selection UI: displays weapons, live weight calculation,
    /// and dynamic warning for heavy encumbrance ("الوزن العملاق").
    /// </summary>
    public partial class ArmoryLoadoutUI : Control
    {
        private ProgressBar _weightBar = null!;
        private Label _weightLabel = null!;
        private Label _encumbranceWarning = null!;

        public override void _Ready()
        {
            _weightBar = GetNode<ProgressBar>("WeightBar");
            _weightLabel = GetNode<Label>("WeightLabel");
            _encumbranceWarning = GetNode<Label>("EncumbranceWarning");

            _weightBar.MaxValue = GameConfig.MaxWeightKg;

            SetupWeaponToggle("WeaponsList/BtnTandem", "alyassin");
            SetupWeaponToggle("WeaponsList/BtnRPG", "rpg7");
            SetupWeaponToggle("WeaponsList/BtnMortar", "mortar");
            SetupWeaponToggle("WeaponsList/BtnSniper", "sniper");
            SetupWeaponToggle("WeaponsList/BtnSticky", "sticky_charge");

            RefreshWeightDisplay();
        }

        private void SetupWeaponToggle(string buttonPath, string weaponId)
        {
            var btn = GetNodeOrNull<Button>(buttonPath);
            if (btn != null)
            {
                btn.Pressed += () =>
                {
                    ToggleWeapon(weaponId);
                    RefreshWeightDisplay();
                };
            }
        }

        private void ToggleWeapon(string weaponId)
        {
            var list = GameManager.Instance.SelectedWeapons;
            if (list.Contains(weaponId))
            {
                list.Remove(weaponId);
            }
            else
            {
                if (GameConfig.Weapons.TryGetValue(weaponId, out var data))
                {
                    float current = GameManager.Instance.CalculateCurrentLoadoutWeight();
                    if (current + data.WeightKg <= GameConfig.MaxWeightKg)
                    {
                        list.Add(weaponId);
                    }
                    else
                    {
                        GD.Print("[Armory] Cannot add weapon: exceeds maximum capacity (45kg)!");
                    }
                }
            }
        }

        private void RefreshWeightDisplay()
        {
            float total = GameManager.Instance.CalculateCurrentLoadoutWeight();
            _weightBar.Value = total;
            _weightLabel.Text = $"الوزن الإجمالي: {total:F1} / {GameConfig.MaxWeightKg:F1} كجم";

            bool isHeavy = total >= GameConfig.HeavyWeightThresholdKg;
            _encumbranceWarning.Visible = isHeavy;
            if (isHeavy)
            {
                _encumbranceWarning.Text = "تحذير: الوزن العملاق! ستنخفض سرعة الحركة بنسبة 35% ويتعطل الانزلاق.";
            }
        }
    }
}

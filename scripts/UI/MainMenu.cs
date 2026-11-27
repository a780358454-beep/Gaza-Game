using System;
using Godot;
using GazaGame.Core;

namespace GazaGame.UI
{
    /// <summary>
    /// Main Menu UI with full Arabic RTL layout, mode selection,
    /// character roster, and LAN/Online lobby setup.
    /// </summary>
    public partial class MainMenu : Control
    {
        private Control _panelMain = null!;
        private Control _panelLobby = null!;
        private Control _panelArmory = null!;
        private LineEdit _ipInput = null!;
        private Label _statusLabel = null!;

        public override void _Ready()
        {
            _panelMain = GetNode<Control>("PanelMain");
            _panelLobby = GetNode<Control>("PanelLobby");
            _panelArmory = GetNode<Control>("PanelArmory");
            _ipInput = GetNode<LineEdit>("PanelLobby/IPInput");
            _statusLabel = GetNode<Label>("PanelLobby/StatusLabel");

            // Main Menu Buttons
            GetNode<Button>("PanelMain/BtnCampaign").Pressed += OnCampaignPressed;
            GetNode<Button>("PanelMain/BtnFreePlay").Pressed += OnFreePlayPressed;
            GetNode<Button>("PanelMain/BtnMultiplayer").Pressed += OnMultiplayerPressed;
            GetNode<Button>("PanelMain/BtnArmory").Pressed += OnArmoryPressed;

            // Lobby Buttons
            GetNode<Button>("PanelLobby/BtnHostLAN").Pressed += OnHostLanPressed;
            GetNode<Button>("PanelLobby/BtnJoinIP").Pressed += OnJoinIpPressed;
            GetNode<Button>("PanelLobby/BtnBackLobby").Pressed += ShowMainMenu;

            // Armory Back Button
            var backBtn = _panelArmory.GetNodeOrNull<Button>("BtnBackArmory");
            if (backBtn != null)
            {
                backBtn.Pressed += ShowMainMenu;
            }

            ShowMainMenu();
        }

        private void ShowMainMenu()
        {
            _panelMain.Visible = true;
            _panelLobby.Visible = false;
            _panelArmory.Visible = false;
        }

        private void OnCampaignPressed()
        {
            GameManager.Instance.CurrentMode = GameMode.HistoricalCampaign;
            GameManager.Instance.CurrentCampaignScene = CampaignScene.Scene01_UndergroundPrayerRoom;
            GD.Print("[MainMenu] Launching Historical Campaign: كمين العقدة...");
            CallDeferred(nameof(LoadJunctionScene));
        }

        private void OnFreePlayPressed()
        {
            GameManager.Instance.CurrentMode = GameMode.FreePlaySolo;
            GD.Print("[MainMenu] Launching Free-Play Solo Skirmish...");
            CallDeferred(nameof(LoadJunctionScene));
        }

        private void LoadJunctionScene()
        {
            var err = GetTree().ChangeSceneToFile("res://scenes/maps/JunctionMap.tscn");
            if (err != Error.Ok)
            {
                GD.PrintErr($"[MainMenu] Failed to load JunctionMap: {err}");
            }
        }

        private void OnMultiplayerPressed()
        {
            _panelMain.Visible = false;
            _panelLobby.Visible = true;
            NetworkManager.Instance.StartLanDiscovery();
            _statusLabel.Text = "جاري البحث عن سيرفرات في الشبكة المحلية...";
        }

        private void OnArmoryPressed()
        {
            _panelMain.Visible = false;
            _panelArmory.Visible = true;
        }

        private void OnHostLanPressed()
        {
            _statusLabel.Text = "جاري إنشاء سيرفر محلي على المنفذ 7777...";
            var err = NetworkManager.Instance.HostGame(GameConfig.GameServerPort, 4);
            if (err == Error.Ok)
            {
                CallDeferred(nameof(LoadJunctionScene));
            }
            else
            {
                _statusLabel.Text = $"فشل إنشاء السيرفر: {err}";
            }
        }

        private void OnJoinIpPressed()
        {
            string ip = _ipInput.Text.Trim();
            if (string.IsNullOrEmpty(ip)) ip = "127.0.0.1";

            _statusLabel.Text = $"جاري الاتصال بالسيرفر {ip}...";
            var err = NetworkManager.Instance.JoinGame(ip, GameConfig.GameServerPort);
            if (err == Error.Ok)
            {
                CallDeferred(nameof(LoadJunctionScene));
            }
            else
            {
                _statusLabel.Text = $"فشل الاتصال: {err}";
            }
        }
    }
}

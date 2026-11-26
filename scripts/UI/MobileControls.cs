using System;
using Godot;
using GazaGame.Core;
using GazaGame.Player;
using GazaGame.Tactical;

namespace GazaGame.UI
{
    /// <summary>
    /// Multi-touch Mobile HUD & Controls: Virtual joystick, look touchpad,
    /// dynamic blood vignette, encumbrance warning, and dedicated tactical buttons.
    /// </summary>
    public partial class MobileControls : Control
    {
        // UI Elements
        private Control _joystickBase = null!;
        private Control _joystickHandle = null!;
        private Control _lookTouchArea = null!;
        private TextureRect _bloodVignette = null!;
        private TextureRect _weightWarningIcon = null!;
        private Label _ammoLabel = null!;
        private Label _weaponNameLabel = null!;
        private Label _subtitleLabel = null!;
        private TacticalMapOverlay? _tacticalOverlay;

        // Player reference
        private PlayerController? _player;

        // Joystick tracking
        private int _joystickTouchId = -1;
        private Vector2 _joystickCenter = Vector2.Zero;
        private const float JoystickRadius = 65.0f;

        // Look drag tracking
        private int _lookTouchId = -1;
        private Vector2 _lastLookPos = Vector2.Zero;

        public override void _Ready()
        {
            _joystickBase = GetNode<Control>("LeftJoystickBase");
            _joystickHandle = _joystickBase.GetNode<Control>("Handle");
            _lookTouchArea = GetNode<Control>("RightLookTouchArea");
            _bloodVignette = GetNode<TextureRect>("BloodVignette");
            _weightWarningIcon = GetNode<TextureRect>("WeightWarningIcon");
            _ammoLabel = GetNode<Label>("HUD/AmmoLabel");
            _weaponNameLabel = GetNode<Label>("HUD/WeaponNameLabel");
            _subtitleLabel = GetNode<Label>("HUD/SubtitleLabel");
            _tacticalOverlay = GetNodeOrNull<TacticalMapOverlay>("TacticalMapOverlay");

            _bloodVignette.Modulate = new Color(1, 1, 1, 0);
            _weightWarningIcon.Visible = false;

            // Wire up Buttons
            SetupTouchButton("Buttons/FireButton", () => _player?.ActionFire());
            SetupTouchButton("Buttons/ADSButton", () => _player?.ActionToggleADS());
            SetupTouchButton("Buttons/ReloadButton", () => _player?.ActionReload());
            SetupTouchButton("Buttons/CrouchButton", () => _player?.ActionToggleCrouch());
            SetupTouchButton("Buttons/ProneButton", () => _player?.ActionToggleProne());
            SetupTouchButton("Buttons/SlideButton", () => _player?.ActionTacticalSlide());
            SetupTouchButton("Buttons/LeanLeftButton", () => _player?.ActionLeanLeft());
            SetupTouchButton("Buttons/LeanRightButton", () => _player?.ActionLeanRight());
            SetupTouchButton("Buttons/SwitchButton", () => _player?.ActionSwitchWeapon());
            SetupTouchButton("Buttons/GrenadeButton", () => _player?.ActionThrowGrenade(false));
            SetupTouchButton("Buttons/SmokeButton", () => _player?.ActionThrowGrenade(true));
            SetupTouchButton("Buttons/MapButton", () => _tacticalOverlay?.ToggleTacticalMap());

            if (GameManager.Instance != null)
            {
                GameManager.Instance.SubtitleUpdated += OnSubtitleUpdated;
            }

            CallDeferred(nameof(AutoFindAndBindPlayer));
        }

        private void AutoFindAndBindPlayer()
        {
            if (_player == null)
            {
                var p = GetTree().Root.FindChild("Player", true, false) as PlayerController;
                if (p != null)
                {
                    BindPlayer(p);
                }
            }
        }

        public void BindPlayer(PlayerController player)
        {
            _player = player;

            if (_player.Health != null)
            {
                _player.Health.HealthChanged += OnHealthChanged;
            }

            if (_player.Inventory != null)
            {
                _player.Inventory.EncumbranceChanged += OnEncumbranceChanged;
                _player.Inventory.ActiveWeaponSwitched += OnWeaponSwitched;

                if (_player.Inventory.PrimaryWeaponInstance != null)
                {
                    _player.Inventory.PrimaryWeaponInstance.AmmoChanged += OnAmmoChanged;
                    UpdateAmmoDisplay(_player.Inventory.PrimaryWeaponInstance.CurrentMag, _player.Inventory.PrimaryWeaponInstance.ReserveAmmo);
                }
            }
        }

        private void SetupTouchButton(string path, Action onClick)
        {
            var btn = GetNodeOrNull<Button>(path);
            if (btn != null)
            {
                btn.Pressed += onClick;
            }
        }

        public override void _Input(InputEvent @event)
        {
            if (@event is InputEventScreenTouch touch)
            {
                HandleScreenTouch(touch);
            }
            else if (@event is InputEventScreenDrag drag)
            {
                HandleScreenDrag(drag);
            }
        }

        private void HandleScreenTouch(InputEventScreenTouch touch)
        {
            Vector2 pos = touch.Position;
            if (touch.Pressed)
            {
                // Left half of screen: Virtual Joystick
                if (pos.X < GetViewportRect().Size.X * 0.45f && _joystickTouchId == -1)
                {
                    _joystickTouchId = touch.Index;
                    _joystickCenter = pos;
                    _joystickBase.GlobalPosition = _joystickCenter - (_joystickBase.Size * 0.5f);
                    _joystickHandle.Position = _joystickBase.Size * 0.5f - (_joystickHandle.Size * 0.5f);
                    _joystickBase.Visible = true;
                }
                // Right half of screen: Look area
                else if (pos.X >= GetViewportRect().Size.X * 0.45f && _lookTouchId == -1)
                {
                    _lookTouchId = touch.Index;
                    _lastLookPos = pos;
                }
            }
            else
            {
                if (touch.Index == _joystickTouchId)
                {
                    _joystickTouchId = -1;
                    _joystickBase.Visible = false;
                    if (_player != null) _player.MoveInput = Vector2.Zero;
                }
                else if (touch.Index == _lookTouchId)
                {
                    _lookTouchId = -1;
                    if (_player != null) _player.LookInput = Vector2.Zero;
                }
            }
        }

        private void HandleScreenDrag(InputEventScreenDrag drag)
        {
            if (drag.Index == _joystickTouchId)
            {
                Vector2 diff = drag.Position - _joystickCenter;
                float dist = diff.Length();
                Vector2 clamped = diff.Normalized() * Math.Min(dist, JoystickRadius);
                _joystickHandle.Position = (_joystickBase.Size * 0.5f) + clamped - (_joystickHandle.Size * 0.5f);

                if (_player != null)
                {
                    Vector2 normInput = diff / JoystickRadius;
                    _player.MoveInput = new Vector2(normInput.X, normInput.Y);

                    // Sprint threshold if dragged beyond 85%
                    if (dist > JoystickRadius * 0.85f && _player.CurrentState is StandingState)
                    {
                        _player.TransitionToState(new SprintingState());
                    }
                    else if (dist <= JoystickRadius * 0.85f && _player.CurrentState is SprintingState)
                    {
                        _player.TransitionToState(new StandingState());
                    }
                }
            }
            else if (drag.Index == _lookTouchId)
            {
                Vector2 lookDelta = drag.Position - _lastLookPos;
                _lastLookPos = drag.Position;

                if (_player != null)
                {
                    _player.LookInput = lookDelta;
                }
            }
        }

        private void OnHealthChanged(float current, float max)
        {
            float healthPct = current / max;
            float vignetteAlpha = Mathf.Clamp((1.0f - healthPct) * 0.85f, 0.0f, 0.85f);
            _bloodVignette.Modulate = new Color(1, 1, 1, vignetteAlpha);
        }

        private void OnEncumbranceChanged(bool isOverencumbered, float currentWeight, float maxWeight)
        {
            _weightWarningIcon.Visible = isOverencumbered;
        }

        private void OnWeaponSwitched(string id, string nameAr)
        {
            _weaponNameLabel.Text = nameAr;
            if (_player?.Inventory?.PrimaryWeaponInstance != null)
            {
                UpdateAmmoDisplay(_player.Inventory.PrimaryWeaponInstance.CurrentMag, _player.Inventory.PrimaryWeaponInstance.ReserveAmmo);
            }
        }

        private void OnAmmoChanged(int current, int reserve)
        {
            UpdateAmmoDisplay(current, reserve);
        }

        private void UpdateAmmoDisplay(int current, int reserve)
        {
            _ammoLabel.Text = $"{current} / {reserve}";
        }

        private void OnSubtitleUpdated(string speaker, string arabicText, float duration)
        {
            _subtitleLabel.Text = $"{speaker}: {arabicText}";
            _subtitleLabel.Visible = true;

            GetTree().CreateTimer(duration).Timeout += () =>
            {
                if (_subtitleLabel.Text.Contains(arabicText))
                {
                    _subtitleLabel.Visible = false;
                }
            };
        }
    }
}

using System;
using Godot;
using GazaGame.Core;
using GazaGame.AI;
using GazaGame.Weapons;

namespace GazaGame.Tactical
{
    public enum TacticalCommandType
    {
        MoveWaypoint,
        SuppressiveFireZone,
        OrderBreachCharge,
        CallMortarStrike,
        MedicDropSupply
    }

    /// <summary>
    /// Tactical Map Overlay providing top-down battlefield awareness, slow-motion planning (0.25x),
    /// touch-drag squad orders, and mortar coordinate strikes.
    /// </summary>
    public partial class TacticalMapOverlay : Control
    {
        [Signal]
        public delegate void TacticalOrderIssuedEventHandler(int commandType, Vector3 worldCoord);

        private Camera3D? _tacticalCamera;
        private bool _isOpen = false;
        public TacticalCommandType SelectedCommand { get; set; } = TacticalCommandType.MoveWaypoint;

        public override void _Ready()
        {
            Visible = false;
        }

        public void ToggleTacticalMap()
        {
            _isOpen = !_isOpen;
            Visible = _isOpen;

            if (_isOpen)
            {
                // In singleplayer, slow down time to 0.25x for deliberate tactical planning
                if (NetworkManager.Instance == null || !NetworkManager.Instance.Multiplayer.HasMultiplayerPeer())
                {
                    Engine.TimeScale = 0.25f;
                }
                GD.Print("[TacticalMap] Opened Tactical Map Overlay (TimeScale: 0.25x).");
            }
            else
            {
                Engine.TimeScale = 1.0f;
                GD.Print("[TacticalMap] Closed Tactical Map Overlay.");
            }
        }

        public override void _GuiInput(InputEvent @event)
        {
            if (!_isOpen) return;

            if (@event is InputEventMouseButton mouseBtn && mouseBtn.Pressed && mouseBtn.ButtonIndex == MouseButton.Left)
            {
                ExecuteMapTouch(mouseBtn.Position);
            }
            else if (@event is InputEventScreenTouch screenTouch && screenTouch.Pressed)
            {
                ExecuteMapTouch(screenTouch.Position);
            }
        }

        private void ExecuteMapTouch(Vector2 screenPos)
        {
            // Map 2D screen coordinate to 3D tactical ground plane
            Vector3 worldPos = ProjectScreenToGround(screenPos);
            GD.Print($"[TacticalMap] Order [{SelectedCommand}] dispatched at: ({worldPos.X:F1}, {worldPos.Z:F1})");

            GameManager.Instance.TacticalOrdersGiven++;
            EmitSignal(SignalName.TacticalOrderIssued, (int)SelectedCommand, worldPos);

            // Execute tactical command directly on AI/Systems
            switch (SelectedCommand)
            {
                case TacticalCommandType.MoveWaypoint:
                    SquadAI.BroadcastWaypoint(worldPos);
                    break;
                case TacticalCommandType.CallMortarStrike:
                    ExecuteMortarStrike(worldPos);
                    break;
                case TacticalCommandType.OrderBreachCharge:
                    SquadAI.BroadcastSpecialOrder("abu_al_baraa", "plant_charge", worldPos);
                    break;
                case TacticalCommandType.MedicDropSupply:
                    SquadAI.BroadcastSpecialOrder("abu_anas", "resupply", worldPos);
                    break;
                case TacticalCommandType.SuppressiveFireZone:
                    SquadAI.BroadcastSpecialOrder("abu_anas", "suppress", worldPos);
                    break;
            }
        }

        private Vector3 ProjectScreenToGround(Vector2 screenPos)
        {
            // Approximate ground plane projection from top-down viewport
            Vector2 viewportSize = GetViewportRect().Size;
            float normX = (screenPos.X / viewportSize.X) * 2.0f - 1.0f;
            float normY = (screenPos.Y / viewportSize.Y) * 2.0f - 1.0f;

            // Map coverage area (-60 to +60 in X, -80 to +80 in Z)
            float worldX = normX * 45.0f;
            float worldZ = normY * 55.0f;
            return new Vector3(worldX, 0.0f, worldZ);
        }

        private void ExecuteMortarStrike(Vector3 target)
        {
            var mortar = GetTree().Root.FindChild("Mortar", true, false) as FieldMortar;
            if (mortar != null)
            {
                mortar.FireAtCoordinates(target);
            }
            else
            {
                GD.Print($"[TacticalMap] Mortar strike requested at ({target.X:F1}, {target.Z:F1}). Shell inbound!");
            }
        }
    }
}

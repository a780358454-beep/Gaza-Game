using System;
using Godot;
using GazaGame.Core;
using GazaGame.Weapons;

namespace GazaGame.Player
{
    /// <summary>
    /// Tactical First-Person Character Controller with multi-touch mobile input,
    /// dynamic encumbrance, leaning, and tactical slide momentum.
    /// </summary>
    public partial class PlayerController : CharacterBody3D
    {
        [Export] public float MouseSensitivity = 0.003f;
        [Export] public float TouchSensitivity = 0.004f;
        [Export] public float JumpVelocity = 4.5f;
        [Export] public float Gravity = 12.0f;

        // Node References
        public Node3D CameraPivot { get; private set; } = null!;
        public Camera3D FirstPersonCamera { get; private set; } = null!;
        public Node3D WeaponMount { get; private set; } = null!;
        public AudioStreamPlayer3D AudioPlayer { get; private set; } = null!;
        public HealthSystem Health { get; private set; } = null!;
        public InventoryAndWeightSystem Inventory { get; private set; } = null!;

        // State Machine
        public IPlayerState CurrentState { get; private set; } = new StandingState();

        // Input vectors (from Mobile HUD or Keyboard)
        public Vector2 MoveInput { get; set; } = Vector2.Zero;
        public Vector2 LookInput { get; set; } = Vector2.Zero;
        public bool IsAimingDownSights { get; private set; } = false;
        public bool AutoAimAssistEnabled { get; set; } = true;

        // Leaning and Camera offsets
        private float _currentEyeHeight = 1.70f;
        private float _currentCameraRoll = 0.0f;
        private float _currentLateralOffset = 0.0f;
        private float _cameraPitch = 0.0f;

        public override void _Ready()
        {
            CameraPivot = GetNode<Node3D>("CameraPivot");
            FirstPersonCamera = CameraPivot.GetNode<Camera3D>("Camera3D");
            WeaponMount = CameraPivot.GetNode<Node3D>("WeaponMount");
            AudioPlayer = GetNode<AudioStreamPlayer3D>("AudioPlayer");
            Health = GetNode<HealthSystem>("HealthSystem");
            Inventory = GetNode<InventoryAndWeightSystem>("InventorySystem");

            TransitionToState(new StandingState());
        }

        public override void _UnhandledInput(InputEvent @event)
        {
            if (@event is InputEventMouseMotion mouseMotion && Input.MouseMode == Input.MouseModeEnum.Captured)
            {
                ApplyLookRotation(new Vector2(mouseMotion.Relative.X * -MouseSensitivity, mouseMotion.Relative.Y * -MouseSensitivity));
            }
        }

        public override void _PhysicsProcess(double delta)
        {
            // 1. Apply look delta from mobile right-touch area
            if (LookInput != Vector2.Zero)
            {
                ApplyLookRotation(new Vector2(-LookInput.X * TouchSensitivity, -LookInput.Y * TouchSensitivity));
                LookInput = Vector2.Zero; // Reset per frame
            }

            // 2. State update
            CurrentState.PhysicsUpdate(this, delta);

            // 3. Movement physics
            Vector3 velocity = Velocity;

            // Gravity
            if (!IsOnFloor())
            {
                velocity.Y -= Gravity * (float)delta;
            }

            // Keyboard input fallback if MoveInput is zero
            Vector2 inputDir = MoveInput;
            if (inputDir == Vector2.Zero)
            {
                inputDir = Input.GetVector("move_left", "move_right", "move_forward", "move_back");
            }

            // Direction relative to camera yaw
            Vector3 direction = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();

            // Calculate speed with encumbrance modifier
            float targetSpeed = CurrentState.BaseSpeed;
            if (Inventory != null && Inventory.IsOverencumbered)
            {
                targetSpeed *= GameConfig.HeavyEncumbranceSpeedMultiplier;
            }

            if (direction != Vector3.Zero)
            {
                velocity.X = direction.X * targetSpeed;
                velocity.Z = direction.Z * targetSpeed;
            }
            else
            {
                velocity.X = Mathf.MoveToward(Velocity.X, 0, targetSpeed);
                velocity.Z = Mathf.MoveToward(Velocity.Z, 0, targetSpeed);
            }

            Velocity = velocity;
            MoveAndSlide();

            // 4. Smooth Camera Transformations
            SmoothCamera(delta);
        }

        public void ApplyLookRotation(Vector2 rot)
        {
            // Yaw rotates player body
            RotateY(rot.X);

            // Pitch rotates camera pivot
            _cameraPitch = Mathf.Clamp(_cameraPitch + rot.Y, Mathf.DegToRad(-85.0f), Mathf.DegToRad(85.0f));
            CameraPivot.Rotation = new Vector3(_cameraPitch, 0, _currentCameraRoll);
        }

        private void SmoothCamera(double delta)
        {
            float fDelta = (float)delta * 12.0f;
            _currentEyeHeight = Mathf.Lerp(_currentEyeHeight, CurrentState.TargetEyeHeight, fDelta);
            _currentCameraRoll = Mathf.Lerp(_currentCameraRoll, CurrentState.TargetCameraRoll, fDelta);
            _currentLateralOffset = Mathf.Lerp(_currentLateralOffset, CurrentState.TargetLateralLeanOffset, fDelta);

            CameraPivot.Position = new Vector3(_currentLateralOffset, _currentEyeHeight, 0);
            CameraPivot.Rotation = new Vector3(_cameraPitch, 0, _currentCameraRoll);

            // ADS FOV zoom
            float targetFov = IsAimingDownSights ? 45.0f : 75.0f;
            FirstPersonCamera.Fov = Mathf.Lerp(FirstPersonCamera.Fov, targetFov, fDelta);
        }

        public void TransitionToState(IPlayerState newState)
        {
            // Encumbrance restriction: Disable tactical slide if overencumbered
            if (newState is TacticalSlideState && Inventory != null && Inventory.IsOverencumbered)
            {
                GD.Print("[Player] Cannot slide while carrying heavy encumbrance (الوزن العملاق)!");
                return;
            }

            CurrentState.Exit(this);
            CurrentState = newState;
            CurrentState.Enter(this);
        }

        // Mobile Button Action Handlers
        public void ActionToggleADS()
        {
            IsAimingDownSights = !IsAimingDownSights;
        }

        public void ActionFire()
        {
            Inventory?.PrimaryWeaponInstance?.Fire();
        }

        public void ActionReload()
        {
            Inventory?.PrimaryWeaponInstance?.Reload();
        }

        public void ActionToggleCrouch()
        {
            if (CurrentState is CrouchingState)
                TransitionToState(new StandingState());
            else
                TransitionToState(new CrouchingState());
        }

        public void ActionToggleProne()
        {
            if (CurrentState is ProneState)
                TransitionToState(new StandingState());
            else
                TransitionToState(new ProneState());
        }

        public void ActionTacticalSlide()
        {
            if (IsOnFloor() && Velocity.Length() > 3.0f)
            {
                TransitionToState(new TacticalSlideState());
            }
        }

        public void ActionLeanLeft()
        {
            if (CurrentState is LeaningState lean && lean.Stance == PlayerStance.LeaningLeft)
                TransitionToState(new StandingState());
            else
                TransitionToState(new LeaningState(true));
        }

        public void ActionLeanRight()
        {
            if (CurrentState is LeaningState lean && lean.Stance == PlayerStance.LeaningRight)
                TransitionToState(new StandingState());
            else
                TransitionToState(new LeaningState(false));
        }

        public void ActionSwitchWeapon()
        {
            Inventory?.CycleNextWeapon();
        }

        public void ActionThrowGrenade(bool isSmoke = false)
        {
            Inventory?.ThrowGrenade(isSmoke);
        }

        public void PlayAudioClip(string audioPath)
        {
            if (ResourceLoader.Exists(audioPath))
            {
                var stream = GD.Load<AudioStream>(audioPath);
                AudioPlayer.Stream = stream;
                AudioPlayer.Play();
            }
        }
    }
}

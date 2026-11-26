using Godot;

namespace GazaGame.Player
{
    public enum PlayerStance
    {
        Standing,
        Sprinting,
        Crouching,
        Prone,
        TacticalSlide,
        LeaningLeft,
        LeaningRight
    }

    /// <summary>
    /// Finite State Machine interface for player tactical movements.
    /// </summary>
    public interface IPlayerState
    {
        PlayerStance Stance { get; }
        float TargetEyeHeight { get; }
        float TargetCameraRoll { get; }
        float TargetLateralLeanOffset { get; }
        float BaseSpeed { get; }

        void Enter(PlayerController player);
        void Exit(PlayerController player);
        void PhysicsUpdate(PlayerController player, double delta);
        void HandleInput(PlayerController player, InputEvent @event);
    }

    public class StandingState : IPlayerState
    {
        public PlayerStance Stance => PlayerStance.Standing;
        public float TargetEyeHeight => 1.70f;
        public float TargetCameraRoll => 0.0f;
        public float TargetLateralLeanOffset => 0.0f;
        public float BaseSpeed => 5.0f;

        public void Enter(PlayerController player) {}
        public void Exit(PlayerController player) {}
        public void PhysicsUpdate(PlayerController player, double delta) {}
        public void HandleInput(PlayerController player, InputEvent @event) {}
    }

    public class SprintingState : IPlayerState
    {
        public PlayerStance Stance => PlayerStance.Sprinting;
        public float TargetEyeHeight => 1.70f;
        public float TargetCameraRoll => 0.0f;
        public float TargetLateralLeanOffset => 0.0f;
        public float BaseSpeed => 8.2f;

        public void Enter(PlayerController player) {}
        public void Exit(PlayerController player) {}
        public void PhysicsUpdate(PlayerController player, double delta) {}
        public void HandleInput(PlayerController player, InputEvent @event) {}
    }

    public class CrouchingState : IPlayerState
    {
        public PlayerStance Stance => PlayerStance.Crouching;
        public float TargetEyeHeight => 1.15f;
        public float TargetCameraRoll => 0.0f;
        public float TargetLateralLeanOffset => 0.0f;
        public float BaseSpeed => 3.2f;

        public void Enter(PlayerController player) {}
        public void Exit(PlayerController player) {}
        public void PhysicsUpdate(PlayerController player, double delta) {}
        public void HandleInput(PlayerController player, InputEvent @event) {}
    }

    public class ProneState : IPlayerState
    {
        public PlayerStance Stance => PlayerStance.Prone;
        public float TargetEyeHeight => 0.38f;
        public float TargetCameraRoll => 0.0f;
        public float TargetLateralLeanOffset => 0.0f;
        public float BaseSpeed => 1.6f;

        public void Enter(PlayerController player) {}
        public void Exit(PlayerController player) {}
        public void PhysicsUpdate(PlayerController player, double delta) {}
        public void HandleInput(PlayerController player, InputEvent @event) {}
    }

    public class TacticalSlideState : IPlayerState
    {
        public PlayerStance Stance => PlayerStance.TacticalSlide;
        public float TargetEyeHeight => 0.85f;
        public float TargetCameraRoll => 0.04f;
        public float TargetLateralLeanOffset => 0.0f;
        public float BaseSpeed => 9.5f;

        private double _timer = 0.0;
        private const double SlideDuration = 0.85;

        public void Enter(PlayerController player)
        {
            _timer = 0.0;
            player.PlayAudioClip("res://assets/audio/footstep_rubble.wav");
        }

        public void Exit(PlayerController player) {}

        public void PhysicsUpdate(PlayerController player, double delta)
        {
            _timer += delta;
            if (_timer >= SlideDuration)
            {
                player.TransitionToState(new CrouchingState());
            }
        }

        public void HandleInput(PlayerController player, InputEvent @event) {}
    }

    public class LeaningState : IPlayerState
    {
        private readonly bool _left;
        public PlayerStance Stance => _left ? PlayerStance.LeaningLeft : PlayerStance.LeaningRight;
        public float TargetEyeHeight => 1.55f;
        public float TargetCameraRoll => _left ? Mathf.DegToRad(-14.0f) : Mathf.DegToRad(14.0f);
        public float TargetLateralLeanOffset => _left ? -0.45f : 0.45f;
        public float BaseSpeed => 2.5f;

        public LeaningState(bool left)
        {
            _left = left;
        }

        public void Enter(PlayerController player) {}
        public void Exit(PlayerController player) {}
        public void PhysicsUpdate(PlayerController player, double delta) {}
        public void HandleInput(PlayerController player, InputEvent @event) {}
    }
}

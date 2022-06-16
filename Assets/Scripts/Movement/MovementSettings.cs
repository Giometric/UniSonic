using UnityEngine;

namespace Giometric.UniSonic
{
    [CreateAssetMenu(menuName = "UniSonic/Movement Settings", fileName = "MovementSettings")]
    public class MovementSettings : ScriptableObject
    {
        [Header("Ground Movement")]
        [SerializeField] private float groundAcceleration = 168.75f;
        [SerializeField] private float groundTopSpeed = 360f;
        [SerializeField] private float friction = 168.75f;
        [SerializeField] private float rollingFriction = 84.375f;
        [SerializeField] private float deceleration = 1800f;
        [SerializeField] private float rollingDeceleration = 450f;

        [Header("Air Movement")]
        [SerializeField] private float airAcceleration = 337.5f;
        [SerializeField] private float jumpVelocity = 390f;
        [SerializeField] private float jumpReleaseThreshold = 240f;
        [SerializeField] private float gravity = -787.5f;
        [SerializeField] private float terminalVelocity = 960f;
        [SerializeField] private float airDrag = 0.03125f;

        [Header("Other")]
        [Tooltip("The velocity the character is launched with when they take damage.")]
        [SerializeField] private Vector2 hitStateVelocity = new Vector2(120f, 240f);
        [Tooltip("The gravity used by the character while in the hit state.")]
        [SerializeField] private float hitStateGravity = -675f;

        public float GroundAcceleration { get { return groundAcceleration; } }
        public float GroundTopSpeed { get { return groundTopSpeed; } }
        public float Friction { get { return friction; } }
        public float RollingFriction { get { return rollingFriction; } }
        public float Deceleration { get { return deceleration; } }
        public float RollingDeceleration { get { return rollingDeceleration; } }
        public float AirAcceleration { get { return airAcceleration; } }
        public float JumpVelocity { get { return jumpVelocity; } }
        public float JumpReleaseThreshold { get { return jumpReleaseThreshold; } }
        public float Gravity { get { return gravity; } }
        public float TerminalVelocity { get { return terminalVelocity; } }
        public float AirDrag { get { return airDrag; } }
        public Vector2 HitStateVelocity { get { return hitStateVelocity; } }
        public float HitStateGravity { get { return hitStateGravity; } }
    }
}
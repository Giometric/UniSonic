using UnityEngine;
using System.Collections;

namespace Giometric.UniSonic.Objects
{
    public class SpringTrigger : ObjectTriggerBase
    {
        [SerializeField]
        [Tooltip("The speed the player will be launched at when this spring is activated. Direction is relative to the local up direction of the Transform this script is attached to.")]
        private float launchVelocity = 600f;
        [SerializeField]
        [Tooltip("If the player is airborne after being launched by this spring, the mode determines whether their x velocity, y velocity, or neither will be preserved.")]
        private SpringVelocityMode velocityMode = SpringVelocityMode.Vertical;
        [SerializeField]
        [Tooltip("If true, player will be set airborne when this spring is activated. Necessary for any springs that need to propel the player into the air.")]
        private bool forcePlayerAirborne = false;
        [SerializeField]
        [Tooltip("How long to set the horizontal control lock for the player.")]
        private float horizontalControlLockTime = 0.26666667f;
        [SerializeField]
        [Tooltip("If true, player should use the spinning spring jump animation when launched by this spring.")]
        private bool useJumpSpinAnimation = true;

        [Header("Animation")]
        [SerializeField]
        private Animator animator;
        [SerializeField]
        private string activateAnimTrigger = "Activate";

        private int activateHash;
        protected override Color32 gizmoColor { get { return new Color32(255, 64, 32, 64); } }

        protected override void Awake()
        {
            base.Awake();
            activateHash = Animator.StringToHash(activateAnimTrigger);
        }

        protected override void OnDrawGizmos()
        {
            base.OnDrawGizmos();

            // Draw arrow showing how far a character will be propelled in one frame (at 60 fps)
            Vector2 localUp = transform.up;
            DebugUtils.DrawArrow(transform.position, new Vector2(transform.position.x, transform.position.y) + (localUp * (launchVelocity / 60f)), 6, Color.white);
        }

        protected override void OnPlayerEnterTrigger(Movement player)
        {
            base.OnPlayerEnterTrigger(player);
            Vector2 localUp = transform.up;
            player.SetSpringState(localUp * launchVelocity, forcePlayerAirborne, velocityMode, horizontalControlLockTime, useJumpSpinAnimation);

            if (animator != null)
            {
                animator.SetTrigger(activateHash);
            }
        }
    }
}
using UnityEngine;
using System.Collections;

namespace Giometric.UniSonic.Objects
{
    public class SpringTrigger : MonoBehaviour
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

        private Collider2D collider2d;
        private int activateHash;
        private static readonly Color32 gizmoColor = new Color32(255, 64, 32, 64);

        private void Awake()
        {
            collider2d = GetComponent<Collider2D>();
            activateHash = Animator.StringToHash(activateAnimTrigger);
        }

        private void OnDrawGizmos()
        {
            if (collider2d == null)
            {
                collider2d = GetComponent<Collider2D>();
            }

             if (collider2d != null)
            {
                Gizmos.color = gizmoColor;

                if (collider2d is BoxCollider2D boxCollider2d)
                {
                    Gizmos.matrix = transform.localToWorldMatrix;
                    Gizmos.DrawCube(boxCollider2d.offset, boxCollider2d.size);
                    Gizmos.DrawWireCube(boxCollider2d.offset, boxCollider2d.size);
                }
                else if (collider2d is CircleCollider2D circleCollider2d)
                {
                    Vector3 transformScale = transform.localScale;
                    Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, new Vector3(transformScale.x, transformScale.y, 0f));
                    Gizmos.DrawSphere(circleCollider2d.offset, circleCollider2d.radius);
                    Gizmos.DrawWireSphere(circleCollider2d.offset, circleCollider2d.radius);
                }
                else
                {
                    Bounds bounds = collider2d.bounds;
                    Gizmos.DrawWireCube(bounds.center, bounds.size);
                }
            }

            // Draw arrow showing how far a character will be propelled in one frame (at 60 fps)
            Vector2 localUp = transform.up;
            DebugUtils.DrawArrow(transform.position, new Vector2(transform.position.x, transform.position.y) + (localUp * (launchVelocity / 60f)), 6, Color.white);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var player = other.GetComponent<Movement>();
            if (player != null)
            {
                Vector2 localUp = transform.up;
                player.SetSpringState(localUp * launchVelocity, forcePlayerAirborne, velocityMode, horizontalControlLockTime, useJumpSpinAnimation);

                if (animator != null)
                {
                    animator.SetTrigger(activateHash);
                }
            }
        }
    }
}
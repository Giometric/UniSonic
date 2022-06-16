using UnityEngine;
using System.Collections;

namespace Giometric.UniSonic.Objects
{
    public class DamageTrigger : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("If true, player lose rings when hit by this damage trigger. If false, player will only be knocked away.")]
        private bool doesDamage = true;

        private Collider2D collider2d;
        private static readonly Color32 gizmoColor = new Color32(255, 16, 16, 64);

        private void Awake()
        {
            collider2d = GetComponent<Collider2D>();
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
        }

        // TODO: Use OnTriggerStay2D?
        private void OnTriggerEnter2D(Collider2D other)
        {
            var player = other.GetComponent<Movement>();
            if (player != null && !player.IsInvulnerable)
            {
                player.SetHitState(transform.position, doesDamage);
            }
        }
    }
}
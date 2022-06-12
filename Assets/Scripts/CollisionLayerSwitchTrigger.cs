using UnityEngine;
using System.Collections;

namespace Giometric.UniSonic
{
    public class CollisionLayerSwitchTrigger : MonoBehaviour
    {
        private enum TriggerAction
        {
            None = -1,
            ToLayerA = 0,
            ToLayerB = 1,
        }

        private enum TriggerDirectionMode
        {
            Horizontal = 0,
            Vertical = 1,
        }

        [SerializeField]
        private bool mustBeGrounded;
        [SerializeField]
        private TriggerDirectionMode triggerDirection = TriggerDirectionMode.Horizontal;
        [Header("Horizontal")]
        [SerializeField]
        private TriggerAction fromLeft = TriggerAction.None;
        [SerializeField]
        private TriggerAction fromRight = TriggerAction.None;
        [Header("Vertical")]
        [SerializeField]
        private TriggerAction fromAbove = TriggerAction.None;
        [SerializeField]
        private TriggerAction fromBelow = TriggerAction.None;

        private Collider2D collider2d;

        private static readonly Color32 gizmoColor = new Color32(255, 255, 0, 64);

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

        private void OnTriggerEnter2D(Collider2D other)
        {
            var player = other.GetComponent<Movement>();
            if (player != null)
            {
                if (mustBeGrounded && !player.Grounded)
                {
                    return;
                }

                var center = collider2d != null ? collider2d.bounds.center : transform.position;
                var dif = player.transform.position - collider2d.bounds.center;

                if (triggerDirection == TriggerDirectionMode.Horizontal)
                {
                    if (dif.x >= 0f && fromRight != TriggerAction.None)
                    {
                        player.SetCollisionLayer((int)fromRight);
                    }
                    else if (fromLeft != TriggerAction.None)
                    {
                        player.SetCollisionLayer((int)fromLeft);
                    }
                }
                else
                {
                    if (dif.y >= 0f && fromAbove != TriggerAction.None)
                    {
                        player.SetCollisionLayer((int)fromAbove);
                    }
                    else if (fromBelow != TriggerAction.None)
                    {
                        player.SetCollisionLayer((int)fromBelow);
                    }
                }
            }
        }
    }
}
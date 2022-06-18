using UnityEngine;
using System.Collections;

namespace Giometric.UniSonic.Objects
{
    public class ObjectTriggerBase : MonoBehaviour
    {
        public enum EnterTriggerSide
        {
            Left,
            Right,
            Top,
            Bottom,
        }

        protected Collider2D collider2d;

        protected virtual Color32 gizmoColor { get { return  new Color32(255, 255, 255, 64); } }

        protected virtual void Awake()
        {
            collider2d = GetComponent<Collider2D>();
        }

        protected virtual void OnDrawGizmos()
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
                    if (collider2d.enabled) { Gizmos.DrawCube(boxCollider2d.offset, boxCollider2d.size); }
                    Gizmos.DrawWireCube(boxCollider2d.offset, boxCollider2d.size);
                }
                else if (collider2d is CircleCollider2D circleCollider2d)
                {
                    Vector3 transformScale = transform.localScale;
                    Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, new Vector3(transformScale.x, transformScale.y, 0f));
                    if (collider2d.enabled) { Gizmos.DrawSphere(circleCollider2d.offset, circleCollider2d.radius); }
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
                switch (GetRelativeSide(other.transform.position))
                {
                    case EnterTriggerSide.Left:
                        Debug.DrawLine(transform.position, transform.position + new Vector3(-16f, 0f), Color.white, 0.5f);
                        break;
                    case EnterTriggerSide.Right:
                        Debug.DrawLine(transform.position, transform.position + new Vector3(16f, 0f), Color.white, 0.5f);
                        break;
                    case EnterTriggerSide.Top:
                        Debug.DrawLine(transform.position, transform.position + new Vector3(0f, 16f), Color.white, 0.5f);
                        break;
                    case EnterTriggerSide.Bottom:
                        Debug.DrawLine(transform.position, transform.position + new Vector3(0f, -16f), Color.white, 0.5f);
                        break;
                }

                OnPlayerEnterTrigger(player);
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            var player = other.GetComponent<Movement>();
            if (player != null)
            {
                OnPlayerExitTrigger(player);
            }
        }

        protected EnterTriggerSide GetRelativeSide(Vector2 position)
        {
            Vector2 fromPos = transform.position;
            Vector2 dif = position - fromPos;
            if (Mathf.Abs(dif.y) > Mathf.Abs(dif.x))
            {
                return dif.y > 0f ? EnterTriggerSide.Top : EnterTriggerSide.Bottom;
            }
            else
            {
                return dif.x > 0f ? EnterTriggerSide.Right : EnterTriggerSide.Left;
            }
        }

        protected virtual void OnPlayerEnterTrigger(Movement player) { }
        protected virtual void OnPlayerExitTrigger(Movement player) { }
    }
}
using UnityEngine;
using System.Collections.Generic;

namespace Giometric.UniSonic
{
    public class MovingPlatform : DynamicPlatform
    {
        [SerializeField]
        [Tooltip("The destination offest position of this platform, relative to where it is placed in the scene.")]
        private Vector2 destinationOffset = new Vector2(128f, 0f);
        [SerializeField]
        [Tooltip("The movement speed of the platform, in units per second.")]
        private float moveSpeed = 32f;
        [SerializeField]
        [Tooltip("How long the platform waits at its start and destination positions before moving again.")]
        private float waitDuration = 1.5f;

        private Vector3 startPos, endPos;
        private float waitTimer = 0f;
        private bool movingBack = false;

        protected override void OnDrawGizmos()
        {
            base.OnDrawGizmos();

            if (Application.isPlaying)
            {
                Gizmos.DrawLine(startPos, endPos);
            }
            else
            {
                Gizmos.DrawLine(transform.position, transform.position + new Vector3(destinationOffset.x, destinationOffset.y));
            }
        }

        private void Awake()
        {
            startPos = transform.position;
            endPos = startPos + new Vector3(destinationOffset.x, destinationOffset.y);
        }

        protected override void TickMovement(float deltaTime)
        {
            base.TickMovement(deltaTime);

            if (waitTimer > 0f)
            {
                waitTimer -= deltaTime;
                if (waitTimer > 0f)
                {
                    // Still waiting, no movement
                    return;
                }
            }

            Vector3 dest = movingBack ? startPos : endPos;
            transform.position = Vector3.MoveTowards(transform.position, dest, moveSpeed * deltaTime);
            if (Vector3.SqrMagnitude(dest - transform.position) <= 0.001f)
            {
                transform.position = dest;
                movingBack = !movingBack;
                waitTimer = waitDuration;
            }
        }
    }
}
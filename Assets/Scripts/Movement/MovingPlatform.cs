using UnityEngine;
using System.Collections.Generic;

namespace Giometric.UniSonic
{
    public class MovingPlatform : DynamicPlatform
    {
        [SerializeField]
        [Tooltip("The distance this platform will sink down when a character lands on it.")]
        private float sinkDistance = 4f;
        [SerializeField]
        [Tooltip("The speed, in units per second, at which this platform sinks down when a character lands on it, and raises back up when they jump off.")]
        private float sinkSpeed = 20f;
        [SerializeField]
        [Tooltip("The destination offest position of this platform, relative to where it is placed in the scene.")]
        private Vector2 destinationOffset = new Vector2(128f, 0f);
        [SerializeField]
        [Tooltip("The movement speed of the platform, in units per second.")]
        private float moveSpeed = 32f;
        [SerializeField]
        [Tooltip("How long the platform waits at its start and destination positions before moving again.")]
        private float waitDuration = 1.5f;

        private float sink = 0f;
        private Vector3 startPos;
        private Vector3 endPos;
        private Vector3 currentPos;
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
            currentPos = startPos;
        }

        protected override void TickMovement(float deltaTime)
        {
            base.TickMovement(deltaTime);

            float sinkDest = attachedPlayers.Count > 0 ? sinkDistance : 0f;
            sink = Mathf.MoveTowards(sink, sinkDest, sinkSpeed * deltaTime);

            if (waitTimer > 0f)
            {
                waitTimer -= deltaTime;
            }

            if (waitTimer <= 0f)
            {
                Vector3 dest = movingBack ? startPos : endPos;
                currentPos = Vector3.MoveTowards(currentPos, dest, moveSpeed * deltaTime);
                if (Vector3.SqrMagnitude(dest - currentPos) <= 0.001f)
                {
                    currentPos = dest;
                    movingBack = !movingBack;
                    waitTimer = waitDuration;
                }
            }

            transform.position = currentPos - new Vector3(0f, sink, 0f);
        }
    }
}
using UnityEngine;
using System.Collections.Generic;

namespace Giometric.UniSonic
{
    public class DynamicPlatform : MonoBehaviour
    {
        protected List<Movement> attachedPlayers = new List<Movement>(2);

        protected virtual void OnDrawGizmos()
        {
            foreach (var player in attachedPlayers)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position, player.transform.position);
            }
        }

        private void FixedUpdate()
        {
            float deltaTime = Time.fixedDeltaTime;

            Vector2 prevPosition = transform.position;
            TickMovement(deltaTime);
            Vector2 currentPosition = transform.position;
            Vector2 movement = currentPosition - prevPosition;

            for (int i = attachedPlayers.Count - 1; i >= 0; --i)
            {
                Movement player = attachedPlayers[i];
                if (player == null || !player.Grounded)
                {
                    continue;
                }

                player.PlatformMovement += movement;
            }

            // Players will attach themselves on their next frame if they become grounded again
            attachedPlayers.Clear();
        }

        protected virtual void TickMovement(float deltaTime)
        {
            // TODO: Manually tick animator here? We need animation to happen before the player's next movement frame,
            // otherwise player's platform movement is 1 frame late
        }

        public void PlayerGroundedOnPlatform(Movement player)
        {
            if (player == null)
            {
                return;
            }

            if (attachedPlayers.Contains(player))
            {
                return;
            }

            attachedPlayers.Add(player);
        }
    }
}
using UnityEngine;
using System.Collections;

using Giometric.UniSonic.Objects;

namespace Giometric.UniSonic.Enemies
{
    public class BugBot : MonoBehaviour
    {
        private enum FacingDirection
        {
            Left = -1,
            Right = 1
        }

        [SerializeField]
        [Tooltip("How many hits it takes for the player to destroy this enemy.")]
        private int health = 1;

        [SerializeField]
        private FacingDirection facingDirection = FacingDirection.Right;

        [SerializeField]
        private SpriteRenderer sprite;

        [SerializeField]
        private Transform smokeEmitter;

        [SerializeField]
        private ObjectTriggerBase hitbox;

        [SerializeField]
        private GameObject dieFxPrefab;

        private float smokeEmitterXOffset;

        private void Awake()
        {
            if (hitbox != null)
            {
                hitbox.PlayerEnteredTrigger.AddListener(OnPlayerEnteredTrigger);
            }

            if (smokeEmitter != null)
            {
                smokeEmitterXOffset = smokeEmitter.transform.localPosition.x;
                Vector2 emitterPos = smokeEmitter.transform.localPosition;
                emitterPos.x = facingDirection == FacingDirection.Right ? smokeEmitterXOffset : -smokeEmitterXOffset;
                smokeEmitter.transform.localPosition = emitterPos;
            }

            if (sprite != null)
            {
                sprite.flipX = facingDirection == FacingDirection.Left;
            }
        }

        public void OnPlayerEnteredTrigger(ObjectTriggerBase trigger, Movement player)
        {
            var collisionSide = trigger.GetRelativeSide(player.transform.position);

            // Player hit spikey side, do damage only
            bool hitSpike = collisionSide == ObjectTriggerBase.EnterTriggerSide.Right && facingDirection == FacingDirection.Right ||
                collisionSide == ObjectTriggerBase.EnterTriggerSide.Left && facingDirection == FacingDirection.Left;

            // TODO: Take into account invulnerability powerup
            if (player.IsBall && (!hitSpike || player.IsInvulnerable))
            {
                if (!player.Grounded)
                {
                    Vector2 newVelocity = player.Velocity;
                    // TODO: Move the rebound code to player movement script, or base enemy?
                    if (player.transform.position.y < transform.position.y || player.Velocity.y > 0f)
                    {
                        newVelocity.y -= 60f * Mathf.Sign(newVelocity.y);
                    }
                    else
                    {
                        newVelocity.y = -newVelocity.y;
                    }
                    player.Velocity = newVelocity;
                }
                TakeDamage(1);
            }
            else if (!player.IsInvulnerable)
            {
                player.SetHitState(transform.position);
            }
        }

        private void TakeDamage(int amount)
        {
            health -= amount;
            if (health <= 0)
            {
                Die();
            }
        }

        private void Die()
        {
            if (dieFxPrefab != null)
            {
                var fx = Instantiate(dieFxPrefab, transform.position, Quaternion.identity);
                Destroy(fx, 2f);
            }
            Destroy(gameObject);
        }
    }
}
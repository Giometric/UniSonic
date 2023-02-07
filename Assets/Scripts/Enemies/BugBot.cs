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
        private float moveSpeed = 60f;

        [SerializeField]
        private float turnAroundWaitTime = 1f;

        [SerializeField]
        private float groundRaycastDistance = 17f;

        [SerializeField]
        private float halfHeight = 16f;

        [SerializeField]
        private LayerMask collisionMask;

        [SerializeField]
        private SpriteRenderer sprite;

        [SerializeField]
        private Transform smokeEmitter;

        [SerializeField]
        private ObjectTriggerBase hitbox;

        [SerializeField]
        private GameObject dieFxPrefab;

        private float smokeEmitterXOffset;
        private float waitTimer = 0f;

        private void Awake()
        {
            if (hitbox != null)
            {
                hitbox.PlayerEnteredTrigger.AddListener(OnPlayerEnteredTrigger);
            }
        }

        private void FixedUpdate()
        {
            float deltaTime = Time.fixedDeltaTime;
            Vector3 newPos = transform.position;

            ContactFilter2D filter = new ContactFilter2D();
            filter.SetLayerMask(collisionMask);

            if (waitTimer > 0f)
            {
                waitTimer -= deltaTime;
                if (waitTimer <= 0f)
                {
                    // Turn around after the wait timer is over
                    facingDirection = facingDirection == FacingDirection.Right ? FacingDirection.Left : FacingDirection.Right;
                }
            }

            if (waitTimer <= 0f)
            {
                // If moving, do horizontal movement first, then grounded check to see if where we're going we would still be grounded
                // If it isn't, cancel the horizontal movement and change direction
                newPos.x += (float)facingDirection * moveSpeed * deltaTime;
            }

            if (Utils.GroundRaycast(newPos, Vector2.down, groundRaycastDistance, filter, 1f, false, out var groundHit, true))
            {
                newPos.y = groundHit.point.y + halfHeight;
                transform.position = newPos;
            }
            else if (waitTimer <= 0f)
            {
                // If we were attempting to move horizontally, start the wait timer before turning around
                waitTimer = turnAroundWaitTime;
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
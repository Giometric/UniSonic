using UnityEngine;
using UnityEngine.Pool;

namespace Giometric.UniSonic.Objects
{
    public class ScatterRing : Ring
    {
        [SerializeField]
        [Tooltip("How long after spawning before the ring can be collected.")]
        private float enableCollectDelay = 1.0666667f;
        [SerializeField]
        [Tooltip("How long the ring will last before disappearing.")]
        private float lifetime = 4.266667f;
        [SerializeField]
        [Tooltip("How long before disappearing completely the ring will begin fading out.")]
        private float fadeoutDuration = 1.5f;
        [SerializeField]
        private string fadeoutParamName = "Fadeout";
        [SerializeField]
        [Tooltip("Curve that defines the ring's animation speed from the start to the end of its lifetime (x axis is normalized time).")]
        private AnimationCurve lifetimeAnimationSpeed = AnimationCurve.Linear(0f, 4f, 1f, 0.01f);
        [SerializeField]
        private string speedParamName = "Speed";
        [SerializeField]
        private float gravity = -337.5f;
        [SerializeField]
        [Tooltip("How much of the ring's X velocity is kept when bouncing off a floor / wall.")]
        private float bounceX = 0.9f;
        [SerializeField]
        [Tooltip("How much of the ring's Y velocity is kept when bouncing off a floor / wall.")]
        private float bounceY = 0.75f;
        [SerializeField]
        private LayerMask collisionMask;
        [SerializeField]
        private float collisionRadius = 8f;
        [SerializeField]
        private float collisionSkinWidth = 0.1f;

        /// <Summary>
        /// The current velocity of the ring. Can be set to set the initial velocity on spawn.
        /// </Summary>
        [System.NonSerialized]
        public Vector2 Velocity;

        /// <Summary>
        /// The object pool this ring is placed in, if any.
        /// </Summary>
        public IObjectPool<ScatterRing> Pool;

        private static int speedHash = -1;
        private static int fadeoutHash = -1;
        private float elapsed = 0f;
        private RaycastHit2D[] hitResultsCache = new RaycastHit2D[8];

        protected override bool CanBeCollected { get { return base.CanBeCollected && elapsed >= enableCollectDelay; } }

        protected override void Awake()
        {
            base.Awake();
            if (speedHash == -1 && !string.IsNullOrEmpty(speedParamName))
            {
                speedHash = Animator.StringToHash(speedParamName);
            }

            if (fadeoutHash == -1 && !string.IsNullOrEmpty(fadeoutParamName))
            {
                fadeoutHash = Animator.StringToHash(fadeoutParamName);
            }
        }

        private void FixedUpdate()
        {
            if (IsCollected)
            {
                animator.SetBool(fadeoutHash, false);
                return;
            }

            float deltaTime = Time.fixedDeltaTime;

            elapsed += deltaTime;
            if (elapsed >= lifetime)
            {
                // If out of time, invoke OnPostCollectFinished to disappear (and possibly get re-pooled)
                PostCollectSequenceFinished();
                return;
            }
            else
            {
                if (animator != null)
                {
                    float speed = lifetime > 0f ? lifetimeAnimationSpeed.Evaluate(elapsed / lifetime) : 1f;
                    animator.SetFloat(speedHash, speed);
                    animator.SetBool(fadeoutHash, elapsed >= (lifetime - fadeoutDuration));
                }

                // Apply gravity and clamp to global speed limits
                Velocity.y = Mathf.Min(960f, Velocity.y + (gravity * deltaTime));
                Velocity.x = Mathf.Min(960f, Velocity.x);

                // Apply velocity to position
                Vector2 nextPos = transform.position;
                nextPos += Velocity * deltaTime;
                transform.position = nextPos;

                DoCollisions(deltaTime);
            }
        }

        private void DoCollisions(float deltaTime)
        {
            Vector2 position = transform.position;
            float velocityMag = Velocity.magnitude;
            Vector2 dir = velocityMag > 0f ? (Velocity / velocityMag) : Vector2.down;

            int hitCount = Physics2D.CircleCastNonAlloc(position, collisionRadius, dir, hitResultsCache, velocityMag * deltaTime, collisionMask);

            for (int i = 0; i < hitCount; ++i)
            {
                var hit = hitResultsCache[i];

                if (hit.collider.isTrigger)
                {
                    continue;
                }

                var platform = hit.collider.GetComponent<OneWayPlatform>();
                if (platform != null && !platform.CanCollideInDirection(dir))
                {
                    continue;
                }

                // Ignore one-way platforms if we're moving up
                // if (dir.y > 0f)
                // {
                //     GroundTile groundTile = Utils.GetGroundTile(hit, out Matrix4x4 tileTransform, true);
                //     if (groundTile != null && groundTile.IsOneWayPlatform) // TODO: Also check angle
                //     {
                //         continue;
                //     }
                // }

                // Debug.Log($"HIT {hit.collider.name}");
                // DebugUtils.DrawDiagonalCross(hit.point, 2f, Color.red, 0f);
                position = hit.point + (hit.normal * (collisionRadius + collisionSkinWidth));
                Vector2 newVelocity = Vector2.Reflect(Velocity, hit.normal);

                // Reflect the ring's velocity, but bias the bounced Y velocity upwards so rings don't just slide down ramps
                newVelocity.x *= bounceX;
                newVelocity.y = Mathf.Max(newVelocity.y * bounceY, (Velocity.y * bounceY * -0.5f));
                Velocity = newVelocity;
                transform.position = position;
                break;
            }
        }

        protected override void PostCollectSequenceFinished()
        {
            if (Pool != null)
            {
                Pool.Release(this);
            }
            else
            {
                // No pool, just destroy this object
                Destroy(gameObject);
            }
        }

        public void ResetRing()
        {
            IsCollected = false;
            Velocity = Vector2.zero;
            elapsed = 0f;
            animator.SetBool(fadeoutHash, false);
            animator.SetFloat(speedHash, 1f);
        }
    }
}
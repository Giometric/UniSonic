using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.Tilemaps;
using Giometric.UniSonic.Objects;

namespace Giometric.UniSonic
{
    public enum SpringVelocityMode
    {
        Vertical = 0,
        Horizontal = 1,
        Diagonal = 2,
    }

    public class Movement : MonoBehaviour
    {
        enum GroundMode
        {
            Floor = 0,
            RightWall = 1,
            Ceiling = 2,
            LeftWall = 3,
        }

        public struct GroundInfo
        {
            /// <Summary>
            /// The world-space position of the ground point.
            /// </Summary>
            public Vector3 Point;
            /// <Summary>
            /// The normal vector of the ground point's surface.
            /// </Summary>
            public Vector2 Normal;
            /// <Summary>
            /// The angle (in radians) of the ground point's surface.
            /// </Summary>
            public float Angle;
            /// <Summary>
            /// The RaycastHit2D info associated with this ground.
            /// </Summary>
            public RaycastHit2D Hit;
            /// <Summary>
            /// Whether or not the GroundInfo contains valid data.
            /// </Summary>
            public bool IsValid;

            public static GroundInfo Invalid
            {
                get { return new GroundInfo { IsValid = false }; }
            }
        }

        [Header("Debug")]
        public bool ShowDebug = false;
        [SerializeField] private GUISkin debugGUISkin;

        [Header("Animation")]
        [SerializeField] private SpriteRenderer sprite;
        [SerializeField] private Animator animator;
        [Tooltip("When enabled, the character will rotate smoothly to match the ground angle they are standing on. When disabled, the character's rotation will snap to 45-degree increments.")]
        [SerializeField] private bool smoothRotation = false;

        [Header("Hitbox")]
        [Tooltip("The collider used as the character's hitbox for interacting with objects. Certain actions will cause it to be resized / repositioned.")]
        [SerializeField] private BoxCollider2D hitbox;
        [SerializeField] private Vector2 standingHitboxSize = new Vector2(18f, 34f);
        [SerializeField] private Vector2 shortHitboxSize = new Vector2(18f, 20f);

        [Header("Damage")]
        [SerializeField] private ScatterRing scatterRingPrefab;
        [Tooltip("The maximum number of rings that will be spawned around the character when rings are lost.")]
        [SerializeField] private int scatterRingsCountLimit = 32;
        [SerializeField] private int scatterRingsPerCircle = 16;
        [SerializeField] private float scatterRingBaseSpeed = 240f;

        [Header("General")]
        [Tooltip("Half the character's height when standing.")]
        [SerializeField] private float standingHeightHalf = 20f;
        [Tooltip("Half the character's height when rolling or jumping.")]
        [SerializeField] private float ballHeightHalf = 15f;

        private float heightHalf
        {
            get
            {
                if (IsBall) { return ballHeightHalf; }
                else { return standingHeightHalf; }
            }
        }
        
        private float rollingPositionOffset
        {
            get { return (standingHeightHalf - ballHeightHalf); }
        }

        [Tooltip("The distance used for ground raycast checks.")]
        [SerializeField] private float groundRaycastDist = 36f;

        [Tooltip("Half the width of this character's collision for ground / ceiling checks, when grounded and standing.")]
        [SerializeField] private float standingWidthHalf = 10f;
        [Tooltip("Half the width of this character's collision for ground / ceiling checks, when rolling or airborne.")]
        [SerializeField] private float ballWidthHalf = 7f;
        [Tooltip("When checking for a ground location, the maximum distance upward from the character's feet that can be considered valid ground to step up on.")]
        [SerializeField] private float stepUpHeight = 15f;
        [Tooltip("When checking for a ground location, the minimum distance downward from the character's feet that can be considered valid ground to step down to.")]
        [SerializeField] private float stepDownHeightMin = 4f;
        [Tooltip("When checking for a ground location, the maximum distance downward from the character's feet that can be considered valid ground to step down to.")]
        [SerializeField] private float stepDownHeightMax = 15f;
        [Tooltip("When checking for wall collisions, the distance away from a wall the character will try to maintain.")]
        [SerializeField] private float wallCollisionWidthHalf = 11f;
        [Tooltip("When grounded, wall collision raycasts are with this offset applied to the player's Y position.")]
        [SerializeField] private float flatGroundSideRaycastOffset = -8f;
        [Tooltip("When launched by a spring while grounded, become airborne if the angle between the spring's launch direction and the current ground normal is less than this value (in degrees).")]
        [SerializeField] private float springAirborneAngleThreshold = 45f;
        [Min(0f)]
        [Tooltip("The amount of time the character will be invulnerable after landing from a hit state.")]
        [SerializeField] private float postHitInvulnerabilityDuration = 2f;

        [SerializeField] private LayerMask collisionMaskA;
        [SerializeField] private LayerMask collisionMaskB;
        
        [Header("Movement Settings")]
        [SerializeField] private MovementSettings baseMovementSettings;
        [SerializeField] private MovementSettings underwaterMovementSettings;

        /// <Summary>
        /// The current MovementSettings in use by the character. Will change if the
        /// character goes underwater or enters a zone that modifies movement settings.
        /// </Summary>
        public MovementSettings CurrentMovementSettings { get { return underwater ? underwaterMovementSettings : baseMovementSettings; } }

        [Header("Ground Movement")]
        [Tooltip("The highest possible ground speed or vertical / horizontal velocity the character can have.")]
        [SerializeField] private float globalSpeedLimit = 960f;
        [Tooltip("The minimum ground speed the character must have to be able to start rolling.")]
        [SerializeField] private float rollingMinSpeed = 61.875f;
        [Tooltip("The minimum ground speed the character must maintain while rolling, or they will unroll.")]
        [SerializeField] private float unrollThreshold = 30f;
        [SerializeField] private float defaultSlopeFactor = 450f;
        [SerializeField] private float rollUphillSlopeFactor = 281.25f;
        [SerializeField] private float rollDownhillSlopeFactor = 1125f;
        [Tooltip("The minimum ground speed the character must maintain while moving on walls or ceiling, or they will lose their footing.")]
        [SerializeField] private float fallVelocityThreshold = 150f;
        [Tooltip("The duration of the horizontal control lock applied to the character after they lose their footing from a wall or ceiling.")]
        [SerializeField] private float horizontalControlLockTime = 0.5f;
        [Tooltip("If a ceiling is found at this distance above the character's Y position or closer, they will be unable to jump.")]
        [SerializeField] private float lowCeilingHeight = 25f;

        [Header("Air Movement")]
        [Tooltip("The maximum Y velocity the character can have after which air drag is no longer applied.")]
        [SerializeField] private float airDragMaxYVelocity = 4f;
        [Tooltip("The minimum absolute X velocity the character must have for air drag to be applied.")]
        [SerializeField] private float airDragMinAbsoluteXVelocity = 7.5f;
        [Tooltip("While in the air, the rate at which character will rotate to their upright angle (degrees per second).")]
        [SerializeField]private float uprightRotationRate = 168.75f;
        [Tooltip("The animator state with this tag will have its duration checked to see if the braking animation should be stopped.")]
        [SerializeField]private string brakeTagName = "brake";
        [Tooltip("If input movement is opposite ground speed direction and the character is moving at least this fast, play the brake animation.")]
        [SerializeField]private float brakeGroundSpeedThreshold = 240f;
        [Tooltip("The animator state with this tag will have its duration checked to see if the jump spin animation should be stopped.")]
        [SerializeField]private string jumpSpinTagName = "jumpSpin";
        [Tooltip("When using the plain jump spin animation, the sprite will be held for this duration before returning to the walk animation.")]
        [SerializeField]private float springJumpDuration = 0.8f;

        /// <Summary>
        /// The transform used to define where the water level is for this scene, if any.
        /// If the character's position is ever below this transform, underwater movement will activate.
        /// </Summary>
        [System.NonSerialized] public Transform WaterLevel;

        /// <Summary>
        /// The global speed limit for both ground speed and overall velocity.
        /// </Summary>
        public float GlobalSpeedLimit { get { return globalSpeedLimit; } }

        /// <Summary>
        /// The input movement the character will use the next time it runs a physics update.
        /// The X and Y values are expected to be *exactly* 0.0f for no input, and (ideally)
        /// exactly 1.0f or -1.0f when directional input is engaged.
        /// </Summary>
        [System.NonSerialized] public Vector2 InputMove;

        /// <Summary>
        /// The input jump state the character will use the next time it runs a physics update.
        /// </Summary>
        [System.NonSerialized] public bool InputJump;
        private bool inputJumpLastFrame;

        /// <Summary>
        /// Returns whether the character is currently grounded.
        /// </Summary>
        public bool Grounded { get; set; }

        /// <Summary>
        /// Returns whether the character is currently rolling.
        /// </Summary>
        public bool Rolling { get; private set; }

        /// <Summary>
        /// Returns whether the character is currently jumping due to a jump input (simply falling doesn't count).
        /// </Summary>
        public bool Jumped { get; private set; }

        /// <Summary>
        /// Returns whether the character is currently in a ball state (either from rolling or jumping).
        /// </Summary>
        public bool IsBall { get { return Rolling || Jumped; } }

        private float groundSpeed;
    
        /// <Summary>
        /// Speed along the ground. Only valid if the character is grounded.
        /// </Summary>
        public float GroundSpeed
        {
            get { return groundSpeed; }
            set { groundSpeed = value; }
        }

        /// <Summary>
        /// Facing direction, encoded as a float. Always either -1 for left, 1 for right.
        /// </Summary>
        public float FacingDirection { get; private set; }

        private bool isBraking = false;
        private bool isJumpSpinning = false;
        private bool isSpringJumping = false;
        private float springJumpTimer = 0f;

        /// <Summary>
        /// True if the character is currently in the hit state.
        /// </Summary>
        public bool IsHit { get; private set; }
        private float postHitInvulnerabilityTimer = 0f;

        private int rings = 0;

        /// <Summary>
        /// The number of rings this character currently holds.
        /// </Summary>
        public int Rings
        {
            get { return rings; }
            set { rings = value; }
        }

        /// <Summary>
        /// True if the character is currently invulnerable due to being in the hit state or being in post-hit invulnerability time
        /// </Summary>
        public bool IsInvulnerable { get { return IsHit || postHitInvulnerabilityTimer > 0f; } }

        /// <Summary>
        /// True if the character is currently looking up.
        /// </Summary>
        public bool LookingUp { get; private set; }

        /// <Summary>
        /// True if the character is currently looking down.
        /// </Summary>
        public bool LookingDown { get; private set; }

        /// <Summary>
        /// The incoming movement from any dynamic platforms this character is attached to.
        /// Applied to the character during their movement and then cleared every frame.
        /// </Summary>
        [System.NonSerialized] public Vector2 PlatformMovement;

        private bool hControlLock;
        private float hControlLockTimer = 0f;
        public bool HorizontalControlLock { get { return hControlLock; } }
        private GroundInfo currentGroundInfo;
        private GroundMode groundMode = GroundMode.Floor;

        private Vector2 velocity;

        /// <Summary>
        /// Current velocity. Setting this while the character is not airborne has no effect.
        /// </Summary>
        public Vector2 Velocity
        {
            get { return velocity; }
            set { velocity = value; }
        }

        private float characterAngle;
        private bool lowCeiling;
        private bool underwater;

        private IObjectPool<ScatterRing> scatterRingsPool;

        private void OnScatterRingPostCollectFinished(ScatterRing scatterRing)
        {
            scatterRing.ResetRing();
            scatterRingsPool.Release(scatterRing);
        }

        private ScatterRing CreatePooledScatterRing()
        {
            if (scatterRingPrefab == null)
            {
                return null;
            }

            var scatterRing = Instantiate(scatterRingPrefab, Vector3.zero, Quaternion.identity);
            scatterRing.Pool = scatterRingsPool;
            scatterRing.gameObject.SetActive(false);
            return scatterRing;
        }

        private void OnScatterRingTakeFromPool(ScatterRing scatterRing)
        {
            scatterRing.gameObject.SetActive(true);
            scatterRing.ResetRing();
        }

        private void OnScatterRingReturnToPool(ScatterRing scatterRing)
        {
            scatterRing.ResetRing();
            scatterRing.gameObject.SetActive(false);
        }

        private void OnScatterRingDestroyPooledObject(ScatterRing scatterRing)
        {
            Destroy(scatterRing.gameObject);
        }

        private void GetGroundRaycastPositions(GroundMode groundMode, bool ceilingCheck, out Vector2 leftRaycastPosition, out Vector2 rightRaycastPosition)
        {
            // Add these small (TODO: Configurable?) offsets to avoid sampling exactly between tiles in some situations
            float coord = (IsBall ? ballWidthHalf : standingWidthHalf) + 0.005f;
            switch (groundMode)
            {
                case GroundMode.Floor:
                    leftRaycastPosition = new Vector2(-coord, 0f);
                    rightRaycastPosition = new Vector2(coord, 0f);
                    break;
                case GroundMode.RightWall:
                    leftRaycastPosition = new Vector2(0f, -coord);
                    rightRaycastPosition = new Vector2(0f, coord);
                    break;
                case GroundMode.Ceiling:
                    leftRaycastPosition = new Vector2(coord, 0f);
                    rightRaycastPosition = new Vector2(-coord, 0f);
                    break;
                case GroundMode.LeftWall:
                    leftRaycastPosition = new Vector2(0f, coord);
                    rightRaycastPosition = new Vector2(0f, -coord);
                    break;
                default:
                    leftRaycastPosition = Vector2.zero;
                    rightRaycastPosition = Vector2.zero;
                    break;
            }

            if (ceilingCheck)
            {
                leftRaycastPosition = -leftRaycastPosition;
                rightRaycastPosition = -rightRaycastPosition;
            }
        }

        private Vector2 GetGroundRaycastDirection(GroundMode groundMode, bool ceilingCheck)
        {
            Vector2 dir = Vector2.down;
            if (Grounded)
            {
                switch (groundMode)
                {
                    case GroundMode.Floor:
                        dir = Vector2.down;
                        break;
                    case GroundMode.RightWall:
                        dir = Vector2.right;
                        break;
                    case GroundMode.Ceiling:
                        dir = Vector2.up;
                        break;
                    case GroundMode.LeftWall:
                        dir = Vector2.left;
                        break;
                }
            }
            if (ceilingCheck)
            {
                dir = -dir;
            }
            return dir;
        }

        private bool shouldApplyAirDrag
        {
            get { return velocity.y > 0f && velocity.y < airDragMaxYVelocity && Mathf.Abs(velocity.x) > airDragMinAbsoluteXVelocity; }
        }

        private LayerMask currentGroundMask;
        private RaycastHit2D[] hitResultsCache = new RaycastHit2D[10];

        private int speedHash;
        private int standHash;
        private int spinHash;
        private int pushHash;
        private int lookUpHash;
        private int lookDownHash;
        private int brakeHash;
        private int brakeTagHash;
        private int springJumpHash;
        private int jumpSpinHash;
        private int jumpSpinTagHash;
        private int hitHash;
        private int postHitHash;

        /// <Summary>
        /// Reset velocity and other movement state.
        /// </Summary>
        public void ResetMovement()
        {
            InputMove = Vector2.zero;
            InputJump = false;
            inputJumpLastFrame = false;
            groundSpeed = 0f;
            hControlLock = false;
            hControlLockTimer = 0f;
            currentGroundInfo = GroundInfo.Invalid;
            Grounded = false;
            Rolling = false;
            Jumped = false;
            groundMode = GroundMode.Floor;
            velocity = Vector2.zero;
            characterAngle = 0f;
            lowCeiling = false;
            underwater = false;
            isBraking = false;
            isSpringJumping = false;
            springJumpTimer = 0f;
            isJumpSpinning = false;
            SetCollisionLayer(0);
        }

        public void SetCollisionLayer(int layer)
        {
            switch (layer)
            {
                case 0:
                    currentGroundMask = collisionMaskA;
                    break;
                case 1:
                    currentGroundMask = collisionMaskB;
                    break;
            }
        }

        public void SetSpringState(Vector2 launchVelocity, bool forceAirborne, SpringVelocityMode springVelocityMode,
            float horizontalControlLockTime = 0f, bool useJumpSpinAnimation = true)
        {
            if (launchVelocity.sqrMagnitude < 0.001f)
            {
                return;
            }

            characterAngle = 0f;

            // If currently grounded and this spring isn't forcing us airborne, check if the launch direction and the current
            // ground normal vector are close enough enough to each other that we should become airborne anyway
            if (Grounded)
            {
                if (forceAirborne)
                {
                    Grounded = false;
                }
                else
                {
                    Vector2 launchDirection = launchVelocity.normalized;
                    if (Vector2.Angle(launchDirection, currentGroundInfo.Normal) < springAirborneAngleThreshold)
                    {
                        Grounded = false;
                    }
                }
            }

            // If still grounded after the previous block, we'll be setting our ground speed instead of velocity
            if (Grounded)
            {
                float newSpeed = launchVelocity.magnitude;
                // Adjust ground speed direction based on current ground mode
                switch (groundMode)
                {
                    case GroundMode.Floor:
                        groundSpeed = newSpeed * Mathf.Sign(launchVelocity.x);
                        break;
                    case GroundMode.RightWall:
                        groundSpeed = newSpeed * Mathf.Sign(launchVelocity.y);
                        break;
                    case GroundMode.Ceiling:
                        groundSpeed = newSpeed * -Mathf.Sign(launchVelocity.x);
                        break;
                    case GroundMode.LeftWall:
                        groundSpeed = newSpeed * -Mathf.Sign(launchVelocity.y);
                        break;
                }
                FacingDirection = Mathf.Sign(groundSpeed);
            }
            else
            {
                switch (springVelocityMode)
                {
                    case SpringVelocityMode.Vertical:
                        velocity.y = launchVelocity.y;
                        break;
                    case SpringVelocityMode.Horizontal:
                        velocity.x = launchVelocity.x;
                        FacingDirection = Mathf.Sign(velocity.x);
                        break;
                    case SpringVelocityMode.Diagonal:
                    default:
                        velocity = launchVelocity;
                        FacingDirection = Mathf.Sign(velocity.x);
                        break;
                }
                Jumped = false;
                Rolling = false;
                groundSpeed = 0f;
                if (useJumpSpinAnimation)
                {
                    // Set a slow-but-not-zero speed hash to get the walk animation at the lowest speed
                    // For some reason in the original games only springs that use the spin jump animation do this
                    animator.SetFloat(speedHash, 0.1f);
                    isJumpSpinning = true;
                    isSpringJumping = false;
                    springJumpTimer = 0f;
                    animator.SetBool(springJumpHash, false);
                    animator.SetBool(jumpSpinHash, true);
                }
                else
                {
                    animator.SetFloat(speedHash, Mathf.Max(Mathf.Abs(groundSpeed), 0.1f));
                    isJumpSpinning = false;
                    isSpringJumping = true;
                    springJumpTimer = springJumpDuration;
                    animator.SetBool(springJumpHash, true);
                    animator.SetBool(jumpSpinHash, false);
                }
                EndHitState();
            }

            if (horizontalControlLockTime > 0f)
            {
                SetHorizontalControlLock(horizontalControlLockTime);
            }
        }

        public void SetHorizontalControlLock(float time, bool keepLongerTime = true)
        {
            hControlLock = true;
            hControlLockTimer = keepLongerTime ? Mathf.Max(time, hControlLockTimer) : time;
        }

        public void ScatterRings(int numRings)
        {
            if (numRings == 0 || scatterRingsPerCircle == 0 || scatterRingsCountLimit == 0)
            {
                return;
            }

            int numCircles = Mathf.Max(1, Mathf.CeilToInt(numRings / (float)scatterRingsPerCircle));
            int remaining = numRings;
            float scatterSpeed = scatterRingBaseSpeed;
            float angleSpacing = Mathf.PI * 2f / scatterRingsPerCircle;
            float startAngle = (Mathf.PI * 0.5f) + (angleSpacing * 0.5f * FacingDirection);
            for (int circle = 0; circle < numCircles; ++circle)
            {
                float currentAngle = 0f;
                bool flip = false;
                for (int i = 0; i < scatterRingsPerCircle && remaining > 0; ++i)
                {
                    float angleRad = startAngle + (FacingDirection * currentAngle);
                    Vector2 velocity = new Vector2(Mathf.Cos(angleRad) * scatterSpeed, Mathf.Sin(angleRad) * scatterSpeed);

                    if (flip)
                    {
                        velocity.x = -velocity.x;
                        currentAngle += angleSpacing;
                    }

                    flip = !flip;
                    var scatterRing = scatterRingsPool.Get();
                    if (scatterRing != null)
                    {
                        scatterRing.transform.position = transform.position;
                        scatterRing.Velocity = velocity;
                    }
                    --remaining;
                }
                scatterSpeed *= 0.5f;
            }
        }

        public void SetHitState(Vector2 source, bool damage = true)
        {
            if (damage)
            {
                ScatterRings(rings);
                rings = 0;
            }
            IsHit = true;
            postHitInvulnerabilityTimer = 0f;
            isBraking = false;
            isSpringJumping = false;
            isJumpSpinning = false;
            characterAngle = 0f;
            Grounded = false;
            Jumped = false;

            // Jumping resets the horizontal control lock
            hControlLock = false;
            hControlLockTimer = 0f;
            Vector2 hitStateVelocity = CurrentMovementSettings.HitStateVelocity;
            float positionDif = transform.position.x - source.x;
            // If the damage source is nearly directly above or below us, default to getting knocked away from where we are facing at a lower speed
            if (Mathf.Abs(positionDif) < 1f)
            {
                velocity = new Vector2(hitStateVelocity.x * -FacingDirection, hitStateVelocity.y);
            }
            else
            {
                velocity = new Vector2(hitStateVelocity.x * Mathf.Sign(positionDif), hitStateVelocity.y);
            }
            animator.SetBool(postHitHash, false);
            animator.SetBool(hitHash, true);
            animator.SetFloat(speedHash, 0.1f);
        }

        private void Awake()
        {
            speedHash = Animator.StringToHash("Speed");
            standHash = Animator.StringToHash("Stand");
            spinHash = Animator.StringToHash("Spin");
            pushHash = Animator.StringToHash("Push");
            lookUpHash = Animator.StringToHash("LookUp");
            lookDownHash = Animator.StringToHash("LookDown");
            brakeHash = Animator.StringToHash("Brake");
            brakeTagHash = Animator.StringToHash(brakeTagName);
            springJumpHash = Animator.StringToHash("SpringJump");
            jumpSpinHash = Animator.StringToHash("JumpSpin");
            jumpSpinTagHash = Animator.StringToHash(jumpSpinTagName);
            hitHash = Animator.StringToHash("Hit");
            postHitHash = Animator.StringToHash("PostHit");

            FacingDirection = 1f;
            Rings = 0;
            SetCollisionLayer(0);

            scatterRingsPool = new ObjectPool<ScatterRing>(
                CreatePooledScatterRing,
                OnScatterRingTakeFromPool,
                OnScatterRingReturnToPool,
                OnScatterRingDestroyPooledObject,
                true,
                scatterRingsCountLimit
            );


            if (scatterRingPrefab != null)
            {
                // Unity's object pool provides no method for pre-creating the pooled items, so we do it ourselves
                ScatterRing[] precreatedPool = new ScatterRing[scatterRingsCountLimit];
                for (int i = 0; i < scatterRingsCountLimit; ++i)
                {
                    precreatedPool[i] = scatterRingsPool.Get();
                }
                for (int i = 0; i < scatterRingsCountLimit; ++i)
                {
                    scatterRingsPool.Release(precreatedPool[i]);
                }
            }
            else
            {
                Debug.LogWarning("Scatter ring prefab not set!", gameObject);
            }
        }

        private void OnGUI()
        {
            if (ShowDebug)
            {
                GUI.skin = debugGUISkin != null ? debugGUISkin : GUI.skin;
                Rect areaRect = new Rect(5, 5, 180, 290);

                // Background box
                Color oldColor = GUI.color;
                GUI.color = new Color32(0, 0, 0, 64);
                GUI.DrawTexture(areaRect, Texture2D.whiteTexture);
                GUI.color = oldColor;

                GUILayout.BeginArea(areaRect);
                GUILayout.Toggle(InputJump, "Input Jump");
                GUILayout.Label($"Input Move: {InputMove}");
                GUILayout.Toggle(underwater, "Underwater");
                GUILayout.Toggle(Jumped, "Jumped");
                GUILayout.Toggle(Rolling, "Rolling");
                GUILayout.Toggle(hControlLock, $"Control Lock: {hControlLockTimer:F2}");
                GUILayout.Toggle(Grounded, "Grounded");
                GUILayout.Toggle(shouldApplyAirDrag, "Air Drag");
                GUILayout.Label($"Mode: {groundMode}");
                GUILayout.Label($"Ground Speed: {GroundSpeed:F1}");
                GUILayout.Label($"Velocity: {Velocity} ({Velocity.magnitude:F1})");
                GUILayout.Label($"Platform: {PlatformMovement}");
                if (currentGroundInfo.IsValid)
                {
                    GUILayout.Label($"Angle (Deg): {(currentGroundInfo.Angle * Mathf.Rad2Deg):F0}");
                }
                else
                {
                    GUILayout.Label("Angle (Deg): --");
                }
                GUILayout.Label($"Layer: {(currentGroundMask == collisionMaskA ? 'A' : 'B')}");
                GUILayout.Toggle(IsHit, "Is Hit");
                GUILayout.Toggle(IsInvulnerable, "Is Invulnerable");
                GUILayout.Label($"Rings: {Rings}");
                GUILayout.EndArea();
            }
        }

        private void ApplyMovement(float deltaTime)
        {
            // Clamp velocity to global speed limit
            velocity.x = Mathf.Clamp(velocity.x, -globalSpeedLimit, globalSpeedLimit);
            velocity.y = Mathf.Clamp(velocity.y, -globalSpeedLimit, globalSpeedLimit);

            // Apply movement
            transform.position += new Vector3(velocity.x, velocity.y, 0f) * deltaTime;

            // Apply incoming platform movement directly to position, then clear it
            transform.position += new Vector3(PlatformMovement.x, PlatformMovement.y, 0f);
            PlatformMovement = Vector2.zero;
        }

        private void DoWallCollisions(float deltaTime, bool grounded, GroundMode groundMode = GroundMode.Floor)
        {
            Vector2 startPosition = transform.position;
            Vector2 leftCastDir = Vector2.left;
            Vector2 rightCastDir = Vector2.right;
            float castDistance = wallCollisionWidthHalf;

            // If grounded, calculate wall collision check position based on current velocity (as if we had already moved)
            // The collision response is also a little different - instead of setting the player's position,
            // we simply set their X velocity to the remaining distance / deltaTime and set groundSpeed to 0
            // TODO: This could probably be optimized if ground and air update loops can be consolidated
            if (grounded)
            {
                startPosition += velocity * deltaTime;

                // Adjust cast direction based on current ground mode
                switch (groundMode)
                {
                    case GroundMode.Floor:
                        leftCastDir = Vector2.left;
                        rightCastDir = Vector2.right;
                        break;
                    case GroundMode.RightWall:
                        leftCastDir = Vector2.down;
                        rightCastDir = Vector2.up;
                        break;
                    case GroundMode.Ceiling:
                        leftCastDir = Vector2.right;
                        rightCastDir = Vector2.left;
                        break;
                    case GroundMode.LeftWall:
                        leftCastDir = Vector2.up;
                        rightCastDir = Vector2.down;
                        break;
                }

                // When standing upright on totally flat ground, wall collisions are done from slightly lower
                if (Mathf.Approximately(currentGroundInfo.Angle, 0f))
                {
                    startPosition.y += flatGroundSideRaycastOffset;

                    // If rolling, our Y position is already a bit lower, raise the cast position back up a bit to match
                    if (Rolling)
                    {
                        startPosition.y += rollingPositionOffset;
                    }
                }
            }
            else
            {
                // When not grounded, we can extend the raycast distance a bit if we're moving very fast,
                // to help prevent going through walls
                castDistance = Mathf.Max(wallCollisionWidthHalf, Mathf.Abs(velocity.x) * deltaTime);
            }

            ContactFilter2D filter = new ContactFilter2D();
            filter.SetLayerMask(currentGroundMask);

            if ((grounded && groundSpeed < 0f) || (!grounded && velocity.x < 0f))
            {
                int hitCount = Physics2D.Raycast(startPosition, leftCastDir, filter, hitResultsCache, castDistance);

                for (int i = 0; i < hitCount; ++i)
                {
                    var wallHit = hitResultsCache[i];
                    var platform = wallHit.collider.GetComponent<OneWayPlatform>();
                    if (platform != null)
                    {
                        // Wall collisions don't hit one-way platforms
                        continue;
                    }

                    var groundTile = Utils.GetGroundTile(wallHit, out var tileTransform, ShowDebug);
                    if (groundTile != null && groundTile.IsOneWayPlatform)
                    {
                        // Wall collisions don't hit one-way platform tiles
                        continue;
                    }

                    if (grounded)
                    {
                        switch (groundMode)
                        {
                            case GroundMode.Floor:
                                velocity.x = (wallHit.point.x - (transform.position.x - castDistance)) / deltaTime;
                                break;
                            case GroundMode.RightWall:
                                velocity.y = (wallHit.point.y - (transform.position.y - castDistance)) / deltaTime;
                                break;
                            case GroundMode.Ceiling:
                                velocity.x = (wallHit.point.x - (transform.position.x + castDistance)) / deltaTime;
                                break;
                            case GroundMode.LeftWall:
                                velocity.y = (wallHit.point.y - (transform.position.y + castDistance)) / deltaTime;
                                break;
                        }
                        groundSpeed = 0f;
                    }
                    else
                    {
                        transform.position = new Vector2(wallHit.point.x + wallCollisionWidthHalf, transform.position.y);
                        velocity.x = 0f;
                    }
                    break;
                }

                Debug.DrawLine(startPosition, startPosition + (leftCastDir * castDistance), new Color32(255, 240, 4, 255));
                Debug.DrawLine(startPosition, startPosition + (rightCastDir * castDistance), new Color32(255, 240, 4, 160));
            }
            else if ((grounded && groundSpeed > 0f) || (!grounded && velocity.x > 0f))
            {
                int hitCount = Physics2D.Raycast(startPosition, rightCastDir, filter, hitResultsCache, castDistance);

                for (int i = 0; i < hitCount; ++i)
                {
                    var wallHit = hitResultsCache[i];
                    var platform = wallHit.collider.GetComponent<OneWayPlatform>();
                    if (platform != null)
                    {
                        // Wall collisions don't hit one-way platforms
                        continue;
                    }

                    var groundTile = Utils.GetGroundTile(wallHit, out var tileTransform, ShowDebug);
                    if (groundTile != null && groundTile.IsOneWayPlatform)
                    {
                        // Wall collisions don't hit one-way platform tiles
                        continue;
                    }

                    if (grounded)
                    {
                        switch (groundMode)
                        {
                            case GroundMode.Floor:
                                velocity.x = (wallHit.point.x - (transform.position.x + castDistance)) / deltaTime;
                                break;
                            case GroundMode.RightWall:
                                velocity.y = (wallHit.point.y - (transform.position.y + castDistance)) / deltaTime;
                                break;
                            case GroundMode.Ceiling:
                                velocity.x = (wallHit.point.x - (transform.position.x - castDistance)) / deltaTime;
                                break;
                            case GroundMode.LeftWall:
                                velocity.y = (wallHit.point.y - (transform.position.y - castDistance)) / deltaTime;
                                break;
                        }
                        groundSpeed = 0f;
                    }
                    else
                    {
                        transform.position = new Vector2(wallHit.point.x - wallCollisionWidthHalf, transform.position.y);
                        velocity.x = 0f;
                    }
                    break;
                }

                Debug.DrawLine(startPosition, startPosition + (rightCastDir * castDistance), new Color32(255, 240, 4, 255));
                Debug.DrawLine(startPosition, startPosition + (leftCastDir * castDistance), new Color32(255, 240, 4, 160));
            }
            else
            {
                Debug.DrawLine(startPosition, startPosition + (rightCastDir * castDistance), new Color32(255, 240, 4, 160));
                Debug.DrawLine(startPosition, startPosition + (leftCastDir * castDistance), new Color32(255, 240, 4, 160));
            }
        }

        private void FixedUpdate()
        {
            float accelSpeedCap = CurrentMovementSettings.GroundTopSpeed;
            float deltaTime = Time.fixedDeltaTime;

            if (postHitInvulnerabilityTimer > 0f)
            {
                postHitInvulnerabilityTimer = Mathf.MoveTowards(postHitInvulnerabilityTimer, 0f, deltaTime);
                if (postHitInvulnerabilityTimer <= 0f)
                {
                    animator.SetBool(postHitHash, false);
                }
            }

            if (Grounded)
            {
                // TODO: Check for special animations disabling control, such as balancing on a ledge

                float slopeFactor = 0f;
                float sinGroundAngle = Mathf.Sin(currentGroundInfo.Angle);
                float cosGroundAngle = Mathf.Cos(currentGroundInfo.Angle);

                if (Rolling)
                {
                    // When rolling, slope factor is more intense going downhill and less intense going uphill
                    bool isUphill = (sinGroundAngle >= 0f && groundSpeed >= 0f) || (sinGroundAngle <= 0f && groundSpeed <= 0);
                    slopeFactor = isUphill ? rollUphillSlopeFactor : rollDownhillSlopeFactor;
                }
                else
                {
                    // Default slope factor is the same going both up and downhill
                    slopeFactor = defaultSlopeFactor;
                }

                // Modify ground speed using the chosen slope factor and the angle of the ground we're currently on
                groundSpeed += (slopeFactor * -sinGroundAngle) * deltaTime;

                // TODO: Keep track of when jump button was last pressed so we can do looser jump timing
                if ((!Jumped && InputJump && !inputJumpLastFrame) && !lowCeiling)
                {
                    float jumpVel = CurrentMovementSettings.JumpVelocity;
                    velocity.x -= jumpVel * sinGroundAngle;
                    velocity.y += jumpVel * cosGroundAngle;
                    isBraking = false;
                    Grounded = false;
                    Jumped = true;

                    // Jumping resets the horizontal control lock
                    hControlLock = false;
                    hControlLockTimer = 0f;

                    // TODO: The real games exit the entire update loop here. Investigate?
                    // Would be more accurate to do the same, but doesn't make a whole lot of sense
                }
                else
                {
                    if (hControlLock)
                    {
                        hControlLockTimer -= deltaTime;
                        if (hControlLockTimer <= 0f)
                        {
                            hControlLock = false;
                        }
                    }

                    float prevGroundSpeedSign = Mathf.Sign(groundSpeed);

                    // Decelerate if rolling or not applying directional input
                    if (Rolling || Mathf.Approximately(InputMove.x, 0f))
                    {
                        float friction = Rolling ? CurrentMovementSettings.RollingFriction : CurrentMovementSettings.Friction;
                        groundSpeed = Mathf.MoveTowards(groundSpeed, 0f, friction * deltaTime);
                    }

                    if (!hControlLock && !Mathf.Approximately(InputMove.x, 0f))
                    {
                        float acceleration = 0f;
                        bool movingAgainstCurrentSpeed = !Mathf.Approximately(groundSpeed, 0f) && Mathf.Sign(InputMove.x) != Mathf.Sign(groundSpeed);

                        if (Rolling && movingAgainstCurrentSpeed)
                        {
                            // Decelerating while rolling
                            acceleration = CurrentMovementSettings.RollingDeceleration;
                        }
                        else if (!Rolling && movingAgainstCurrentSpeed)
                        {
                            // Decelerating while running
                            acceleration = CurrentMovementSettings.Deceleration;
                            if (!isBraking && groundMode == GroundMode.Floor && Mathf.Abs(groundSpeed) >= brakeGroundSpeedThreshold)
                            {
                                // We were moving fast enough, start braking if we weren't already
                                isBraking = true;
                            }
                        }
                        else if (!Rolling && !movingAgainstCurrentSpeed)
                        {
                            // Accelerating or maintaining speed
                            acceleration = CurrentMovementSettings.GroundAcceleration;
                        }

                        // Once the acceleration speed cap is reached in either direction, the character won't accelerate past it,
                        // but will instead maintain that speed so long as the player keeps trying to move in that direction.
                        // If the character is already moving faster than the cap, continuing to run will just maintain that speed.
                        if (InputMove.x < 0f && groundSpeed > -accelSpeedCap)
                        {
                            groundSpeed = Mathf.Max(-accelSpeedCap, groundSpeed + (InputMove.x * acceleration) * deltaTime);
                        }
                        else if (InputMove.x > 0f && groundSpeed < accelSpeedCap)
                        {
                            groundSpeed = Mathf.Min(accelSpeedCap, groundSpeed + (InputMove.x * acceleration) * deltaTime);
                        }

                        // Turn to face ground speed direction if our input is the same direction
                        if (Mathf.Sign(InputMove.x) == Mathf.Sign(groundSpeed))
                        {
                            FacingDirection = Mathf.Sign(InputMove.x);
                        }
                    }

                    // Clamp ground speed to global speed limit
                    groundSpeed = Mathf.Clamp(groundSpeed, -globalSpeedLimit, globalSpeedLimit);

                    // We're now moving the other direction, stop braking early if needed
                    if (isBraking && Mathf.Sign(groundSpeed) != prevGroundSpeedSign)
                    {
                        isBraking = false;
                    }

                    if (Rolling && Mathf.Abs(groundSpeed) < unrollThreshold)
                    {
                        Rolling = false;
                        transform.position += new Vector3(0f, rollingPositionOffset);
                    }
                    
                    Vector2 angledSpeed = new Vector2(groundSpeed * Mathf.Cos(currentGroundInfo.Angle), groundSpeed * Mathf.Sin(currentGroundInfo.Angle));
                    velocity = angledSpeed;
                }

                DoWallCollisions(deltaTime, grounded: true, groundMode);

                bool hasVerticalInput = !Mathf.Approximately(0f, InputMove.y);

                // If we're not moving, check if we're looking up or down
                if (Mathf.Approximately(0f, groundSpeed))
                {
                    // Also stop braking if needed
                    isBraking = false;

                    if (hasVerticalInput)
                    {
                        if (InputMove.y < 0f)
                        {
                            groundSpeed = 0f;
                            LookingDown = true;
                            LookingUp = false;
                        }
                        else if (InputMove.y > 0f)
                        {
                            groundSpeed = 0f;
                            LookingDown = false;
                            LookingUp = true;
                        }
                    }
                    else
                    {
                        LookingDown = false;
                        LookingUp = false;
                    }
                }
                else
                {
                    LookingDown = false;
                    LookingUp = false;

                    if (!Rolling && hasVerticalInput)
                    {
                        if (InputMove.y < 0f && Mathf.Abs(groundSpeed) >= rollingMinSpeed)
                        {
                            Rolling = true;
                            isBraking = false;
                            // When rolling, offset position downwards
                            transform.position -= new Vector3(0f, rollingPositionOffset);
                        }
                    }
                }

                ApplyMovement(deltaTime);
            }
            else
            {
                if (!IsHit)
                {
                    // If we are moving up faster than the threshold and the jump button is released,
                    // clamp our upward velocity to the threshold to allow for some jump height control
                    float jumpReleaseThreshold = CurrentMovementSettings.JumpReleaseThreshold;
                    if (Jumped && !InputJump && velocity.y > jumpReleaseThreshold)
                    {
                        velocity.y = jumpReleaseThreshold;
                    }

                    // If jumping (but not rolling) and there's any directional input, allow air acceleration
                    if (!(Rolling && Jumped) && !Mathf.Approximately(InputMove.x, 0f))
                    {
                        // Similar to the 'accelerate up to cap unless already going faster' code from ground acceleration
                        float airAcc = CurrentMovementSettings.AirAcceleration;
                        if (InputMove.x < 0f && velocity.x > -accelSpeedCap)
                        {
                            velocity.x = Mathf.Max(-accelSpeedCap, velocity.x + (InputMove.x * airAcc * deltaTime));
                        }
                        else if (InputMove.x > 0f && velocity.x < accelSpeedCap)
                        {
                            velocity.x = Mathf.Min(accelSpeedCap, velocity.x + (InputMove.x * airAcc * deltaTime));
                        }

                        // Turn to face the direction of input
                        FacingDirection = Mathf.Sign(InputMove.x);
                    }
                    
                    // Apply air drag, if our X and Y velocities are within the thresholds
                    if (shouldApplyAirDrag)
                    {
                        // TODO: Is this correct?
                        // Guide says "X Speed -= ((X Speed div 0.125) / 256);"
                        // Here extrapolated to velocity.x * 8 / 256 == velocity.x * 0.03125, using that as "AirDrag" value
                        velocity.x -= velocity.x * CurrentMovementSettings.AirDrag;
                    }

                    // Rotate character towards being fully upright, if needed
                    if (characterAngle > 0f && characterAngle <= 180f)
                    {
                        characterAngle -= deltaTime * uprightRotationRate;
                        if (characterAngle < 0f) { characterAngle = 0f; }
                    }
                    else if (characterAngle < 360f && characterAngle > 180f)
                    {
                        characterAngle += deltaTime * uprightRotationRate;
                        if (characterAngle >= 360f) { characterAngle = 0f; }
                    }
                }

                ApplyMovement(deltaTime);

                // Apply gravity
                float gravity = IsHit ? CurrentMovementSettings.HitStateGravity : CurrentMovementSettings.Gravity;
                velocity.y = Mathf.Max(velocity.y + (gravity * deltaTime), -CurrentMovementSettings.TerminalVelocity);

                DoWallCollisions(deltaTime, grounded: false);
            }

            bool ceilingLeft = false;
            bool ceilingRight = false;
            GroundInfo ceil = VerticalCollisionCheck(groundRaycastDist, groundMode, ceilingCheck: true, out ceilingLeft, out ceilingRight);

            bool groundedLeft = false;
            bool groundedRight = false;

            if (Grounded)
            {
                currentGroundInfo = GroundCheck(deltaTime, out groundedLeft, out groundedRight);
                Grounded = groundedLeft || groundedRight;
            }
            else
            {
                if (ceil.IsValid && velocity.y > 0f)
                {
                    bool hitCeiling = transform.position.y >= (ceil.Point.y - heightHalf);
                    float angleDeg = ceil.Angle * Mathf.Rad2Deg;

                    // Check for attaching to ceiling
                    if (hitCeiling && ((angleDeg >= 225f && angleDeg < 270f) || (angleDeg > 90f && angleDeg <= 135f)))
                    {
                        // Hit a ceiling at an angle we can attach to
                        Grounded = true;
                        Jumped = false;
                        Rolling = false;
                        currentGroundInfo = ceil;
                        groundMode = GroundMode.Ceiling;

                        // Recalculate ground speed based on how fast we were moving up and the angle of the ceiling we hit
                        groundSpeed = velocity.y * Mathf.Sign(Mathf.Sin(currentGroundInfo.Angle));
                        velocity.y = 0f;
                    }
                    else if (hitCeiling)
                    {
                        // Hit the ceiling but didn't attach
                        transform.position = new Vector2(transform.position.x, ceil.Point.y - heightHalf);
                        velocity.y = 0f;
                    }
                }
                else if (velocity.y < 0f)
                {
                    GroundInfo info = VerticalCollisionCheck(groundRaycastDist, GroundMode.Floor, ceilingCheck: false, out groundedLeft, out groundedRight);

                    Grounded = (groundedLeft || groundedRight) && velocity.y <= 0f && transform.position.y <= (info.Point.y + heightHalf);

                    // Re-calculate ground velocity based on previous air velocity
                    if (Grounded)
                    {
                        // If in a roll jump, add offset to position upon landing
                        if (Jumped && Rolling)
                        {
                            transform.position += new Vector3(0f, rollingPositionOffset);
                        }

                        Jumped = false;
                        Rolling = false;

                        currentGroundInfo = info;
                        groundMode = GroundMode.Floor;
                        float angleDeg = currentGroundInfo.Angle * Mathf.Rad2Deg;

                        if (angleDeg <= 22.5f || (angleDeg >= 337.5 && angleDeg <= 360f))
                        {
                            // Angle is close to level with ground, just use X velocity as ground velocity
                            groundSpeed = velocity.x;
                        }
                        else if ((angleDeg > 22.5f && angleDeg <= 45f) || (angleDeg >= 315f && angleDeg < 337.5f))
                        {
                            // Angle is slightly steep, ground speed will be the X velocity if it is greater than Y velocity,
                            // otherwise it will use Y velocity * half the sin of the ground angle
                            if (Mathf.Abs(velocity.x) > Mathf.Abs(velocity.y)) { groundSpeed = velocity.x; }
                            else { groundSpeed = velocity.y * 0.5f * Mathf.Sign(Mathf.Sin(currentGroundInfo.Angle)); }
                        }
                        else if ((angleDeg > 45f && angleDeg <= 90f) || (angleDeg >= 270f && angleDeg < 315f))
                        {
                            // Angle is steep, again ground speed will be X velocity if it is greater than Y velocity,
                            // otherwise use Y velocity * full sin of the ground angle
                            if (Mathf.Abs(velocity.x) > Mathf.Abs(velocity.y)) { groundSpeed = velocity.x; }
                            else { groundSpeed = velocity.y * Mathf.Sign(Mathf.Sin(currentGroundInfo.Angle)); }
                        }
                        velocity.y = 0f;
                    }
                }
            }

            if (Grounded)
            {
                StickToGround(currentGroundInfo);
                animator.SetFloat(speedHash, Mathf.Abs(groundSpeed));
                animator.SetBool(lookUpHash, LookingUp);
                animator.SetBool(lookDownHash, LookingDown);

                if (IsHit)
                {
                    EndHitState();

                    // Landing after being hit also resets X velocity and ground speed
                    velocity.x = 0f;
                    groundSpeed = 0f;
                }

                // Check if we've got a low ceiling, prevents jumping
                lowCeiling = ceil.IsValid && transform.position.y > ceil.Point.y - lowCeilingHeight;

                // If on the walls or ceiling, the character must maintain a minimum ground speed or lose their footing
                if (groundMode != GroundMode.Floor && Mathf.Abs(groundSpeed) < fallVelocityThreshold)
                {
                    // Losing your footing starts the horizontal control lock timer
                    SetHorizontalControlLock(horizontalControlLockTime);

                    // Round to int here to avoid problems like the angle of vertical walls having values like 89.99999
                    int angleDeg = Mathf.RoundToInt(currentGroundInfo.Angle * Mathf.Rad2Deg);
                    // If the character is far enough away from upright, they will also no longer be grounded
                    if (angleDeg >= 90 && angleDeg <= 270)
                    {
                        Grounded = false;
                    }
                }

                // Check if we need to attach to a DynamicPlatform
                if (Grounded && groundMode == GroundMode.Floor)
                {
                    if (currentGroundInfo.Hit.rigidbody != null)
                    {
                        var dynamicPlatform = currentGroundInfo.Hit.rigidbody.GetComponent<DynamicPlatform>();
                        if (dynamicPlatform != null)
                        {
                            dynamicPlatform.PlayerGroundedOnPlatform(this);
                        }
                    }
                }
            }

            if (hitbox != null)
            {
                bool shortHitbox = LookingDown || IsBall;
                hitbox.size = shortHitbox ? shortHitboxSize : standingHitboxSize;
                hitbox.offset = shortHitbox ? new Vector2(0f, ((shortHitboxSize.y - standingHitboxSize.y) / 2f) + (IsBall ? rollingPositionOffset : 0f)) : Vector2.zero;
            }

            if (!Grounded)
            {
                groundSpeed = 0f;
                currentGroundInfo = GroundInfo.Invalid;
                groundMode = GroundMode.Floor;
                lowCeiling = false;
                LookingUp = false;
                LookingDown = false;
                animator.SetBool(lookUpHash, false);
                animator.SetBool(lookDownHash, false);
            }

            if (WaterLevel != null)
            {
                if (!underwater && transform.position.y <= WaterLevel.position.y)
                {
                    EnterWater();
                }
                else if (underwater && transform.position.y > WaterLevel.position.y)
                {
                    ExitWater();
                }
            }
            else if (underwater)
            {
                ExitWater();
            }

            if (sprite != null)
            {
                sprite.flipX = FacingDirection < 0f;
            }

            if (IsBall)
            {
                transform.localRotation = Quaternion.identity;
            }
            else
            {
                transform.localRotation = Quaternion.Euler(0f, 0f, smoothRotation ? characterAngle : SnapAngle(characterAngle));
            }
            animator.SetBool(spinHash, IsBall);

            // If braking, check if the braking animation (identified by tag) is finished playing through,
            // or if we're no longer in ground mode - if so, stop the animation
            if (isBraking)
            {
                if (groundMode != GroundMode.Floor)
                {
                    isBraking = false;
                }
                else
                {
                    var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                    if (stateInfo.tagHash == brakeTagHash && stateInfo.normalizedTime >= 1f)
                    {
                        isBraking = false;
                    }
                }
            }
            animator.SetBool(brakeHash, isBraking);

            if (isSpringJumping)
            {
                springJumpTimer -= deltaTime;
                if (springJumpTimer <= 0f)
                {
                    isSpringJumping = false;
                    animator.SetBool(springJumpHash, false);
                }
            }

            // If jump spinning, check if the animation (identified by tag) is finished playing through,
            // or if we're no longer airborne - if so, stop the animation
            if (isJumpSpinning)
            {
                if (Grounded)
                {
                    isJumpSpinning = false;
                }
                else
                {
                    var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                    if (stateInfo.tagHash == jumpSpinTagHash && stateInfo.normalizedTime >= 1f)
                    {
                        isJumpSpinning = false;
                    }
                }
            }
            animator.SetBool(jumpSpinHash, isJumpSpinning);

            inputJumpLastFrame = InputJump;

            if (velocity.sqrMagnitude > 0.0001f)
            {
                Vector3 arrowDir = Vector3.Normalize(new Vector3(velocity.x, velocity.y));
                Vector3 endPoint = transform.position + (arrowDir * 14f);
                DebugUtils.DrawArrow(transform.position, endPoint, 3f, Color.white);
            }
        }

        private void EnterWater()
        {
            underwater = true;
            groundSpeed *= 0.5f;
            velocity.x *= 0.5f;
            velocity.y *= 0.25f;
        }

        private void ExitWater()
        {
            underwater = false;

            // Double y velocity, up to a limit so that springs don't launch the character way up into the air
            velocity.y = Mathf.Max(velocity.y, Mathf.Min(velocity.y * 2f, underwaterMovementSettings.JumpVelocity * 2f));
        }

        private void EndHitState(bool startPostHitInvulnerability = true)
        {
            if (IsHit)
            {
                animator.SetBool(hitHash, false);
                IsHit = false;
                postHitInvulnerabilityTimer = startPostHitInvulnerability ? postHitInvulnerabilityDuration : 0f;
                animator.SetBool(postHitHash, startPostHitInvulnerability);
            }
        }

        private bool GroundRaycast(Vector2 castStart, Vector2 dir, float distance, ContactFilter2D filter,
            float minValidDistance, float maxValidDistance, bool ceilingCheck, out RaycastHit2D resultHit)
        {
            resultHit = new RaycastHit2D();
            int hitCount = Physics2D.Raycast(castStart, dir, filter, hitResultsCache, distance);

            // Physics2D.Raycast results should be sorted by distance, so find the first valid result
            for (int i = 0; i < hitCount; ++i)
            {
                var hit = hitResultsCache[i];

                if (hit.distance < minValidDistance || hit.distance > maxValidDistance)
                {
                    // The hit is not within the valid distance range - it's outside of our step-up / step-down limits
                    continue;
                }

                if (ceilingCheck || groundMode != GroundMode.Floor)
                {
                    var platform = hit.collider.GetComponent<OneWayPlatform>();
                    Vector2 oneWayPlatformCheckDirection = Grounded ? -currentGroundInfo.Normal : Vector2.down;
                    if (platform != null && (ceilingCheck || !platform.CanCollideInDirection(oneWayPlatformCheckDirection))) // TODO: Revisit this, it might cause trouble
                    {
                        continue;
                    }

                    var groundTile = Utils.GetGroundTile(hit, out var tileTransform, ShowDebug);
                    if (groundTile != null && groundTile.IsOneWayPlatform) // TODO: Also check angle
                    {
                        continue;
                    }
                }

                resultHit = hit;
                return true;
            }
            return false;
        }

        private GroundInfo GroundCheck(float deltaTime, out bool groundedLeft, out bool groundedRight)
        {
            Vector2 leftLocalCastPos;
            Vector2 rightLocalCastPos;
            GetGroundRaycastPositions(groundMode, false, out leftLocalCastPos, out rightLocalCastPos);
            float stepDownHeight = Mathf.Min(stepDownHeightMin + Mathf.Abs(groundSpeed * deltaTime), stepDownHeightMax);
            float minValidDistance = Mathf.Max(0.001f, heightHalf - stepUpHeight);
            float maxValidDistance = heightHalf + stepDownHeight;

            Vector2 dir = GetGroundRaycastDirection(groundMode, false);
            Vector2 pos = new Vector2(transform.position.x, transform.position.y);

            Vector2 leftCastStart = pos + leftLocalCastPos;
            Vector2 rightCastStart = pos + rightLocalCastPos;

            DebugUtils.DrawDiagonalCross(leftCastStart + dir * minValidDistance, 3f, Color.white);
            DebugUtils.DrawDiagonalCross(leftCastStart + dir * maxValidDistance, 3f, Color.cyan);

            DebugUtils.DrawDiagonalCross(rightCastStart + dir * minValidDistance, 3f, Color.white);
            DebugUtils.DrawDiagonalCross(rightCastStart + dir * maxValidDistance, 3f, Color.cyan);

            ContactFilter2D filter = new ContactFilter2D();
            filter.SetLayerMask(currentGroundMask);

            groundedLeft = false;
            groundedRight = false;

            RaycastHit2D leftHit = new RaycastHit2D();
            groundedLeft = GroundRaycast(leftCastStart, dir, groundRaycastDist, filter, minValidDistance, maxValidDistance, false, out leftHit);

            RaycastHit2D rightHit = new RaycastHit2D();
            groundedRight = GroundRaycast(rightCastStart, dir, groundRaycastDist, filter, minValidDistance, maxValidDistance, false, out rightHit);

            Debug.DrawLine(leftCastStart, leftCastStart + (dir * groundRaycastDist), Color.magenta);
            Debug.DrawLine(rightCastStart, rightCastStart + (dir * groundRaycastDist), Color.red);

            if (groundedLeft) { DebugUtils.DrawCross(leftHit.point, 4f, Color.yellow); }
            if (groundedRight) { DebugUtils.DrawCross(rightHit.point, 4f, Color.yellow); }

            GroundInfo found = GroundInfo.Invalid;

            if (groundedLeft && groundedRight)
            {
                float leftCompare = 0f;
                float rightCompare = 0f;

                switch (groundMode)
                {
                    case GroundMode.Floor:
                        leftCompare = leftHit.point.y;
                        rightCompare = rightHit.point.y;
                        break;
                    case GroundMode.RightWall:
                        leftCompare = -leftHit.point.x;
                        rightCompare = -rightHit.point.x;
                        break;
                    case GroundMode.Ceiling:
                        leftCompare = -leftHit.point.y;
                        rightCompare = -rightHit.point.y;
                        break;
                    case GroundMode.LeftWall:
                        leftCompare = leftHit.point.x;
                        rightCompare = rightHit.point.x;
                        break;
                    default:
                        break;
                }

                if (leftCompare >= rightCompare) { found = GetGroundInfo(leftHit, groundMode); }
                else { found = GetGroundInfo(rightHit, groundMode); }
            }
            else if (groundedLeft) { found = GetGroundInfo(leftHit, groundMode); }
            else if (groundedRight) { found = GetGroundInfo(rightHit, groundMode); }
            else { found = GroundInfo.Invalid; }

            return found;
        }

        private GroundInfo VerticalCollisionCheck(float distance, GroundMode groundMode, bool ceilingCheck,
            out bool hitLeft, out bool hitRight)
        {
            Vector2 leftLocalCastPos;
            Vector2 rightLocalCastPos;
            GetGroundRaycastPositions(groundMode, ceilingCheck, out leftLocalCastPos, out rightLocalCastPos);

            Vector2 dir = GetGroundRaycastDirection(groundMode, ceilingCheck);
            Vector2 pos = new Vector2(transform.position.x, transform.position.y);
            Vector2 leftCastStart = pos + leftLocalCastPos;
            Vector2 rightCastStart = pos + rightLocalCastPos;

            ContactFilter2D filter = new ContactFilter2D();
            filter.SetLayerMask(currentGroundMask);

            hitLeft = false;
            hitRight = false;

            RaycastHit2D leftHit = new RaycastHit2D();
            hitLeft = GroundRaycast(leftCastStart, dir, distance, filter, 0.001f, heightHalf, ceilingCheck, out leftHit);

            RaycastHit2D rightHit = new RaycastHit2D();
            hitRight = GroundRaycast(rightCastStart, dir, distance, filter, 0.001f, heightHalf, ceilingCheck, out rightHit);

            Debug.DrawLine(leftCastStart, leftCastStart + (dir * distance), Color.magenta);
            Debug.DrawLine(rightCastStart, rightCastStart + (dir * distance), Color.red);

            if (hitLeft) { DebugUtils.DrawCross(leftHit.point, 4f, Color.green); }
            if (hitRight) { DebugUtils.DrawCross(rightHit.point, 4f, Color.green); }

            GroundInfo found = GroundInfo.Invalid;

            if (hitLeft && hitRight)
            {
                float leftCompare = 0f;
                float rightCompare = 0f;

                switch (groundMode)
                {
                    case GroundMode.Floor:
                        leftCompare = leftHit.point.y;
                        rightCompare = rightHit.point.y;
                        break;
                    case GroundMode.RightWall:
                        leftCompare = -leftHit.point.x;
                        rightCompare = -rightHit.point.x;
                        break;
                    case GroundMode.Ceiling:
                        leftCompare = -leftHit.point.y;
                        rightCompare = -rightHit.point.y;
                        break;
                    case GroundMode.LeftWall:
                        leftCompare = leftHit.point.x;
                        rightCompare = rightHit.point.x;
                        break;
                    default:
                        break;
                }

                if (ceilingCheck)
                {
                    leftCompare = -leftCompare;
                    rightCompare = -rightCompare;
                }

                if (leftCompare >= rightCompare) { found = GetGroundInfo(leftHit, groundMode); }
                else { found = GetGroundInfo(rightHit, groundMode); }
            }
            else if (hitLeft) { found = GetGroundInfo(leftHit, groundMode); }
            else if (hitRight) { found = GetGroundInfo(rightHit, groundMode); }
            else { found = GroundInfo.Invalid; }

            return found;
        }

        private GroundInfo GetGroundInfo(RaycastHit2D hit)
        {
            return GetGroundInfo(hit, GroundMode.Floor);
        }

        private GroundInfo GetGroundInfo(RaycastHit2D hit, GroundMode groundOrientation)
        {
            GroundInfo info = new GroundInfo();
            info.Hit = hit;
            if (hit.collider != null)
            {
                GroundTile groundTile = Utils.GetGroundTile(hit, out Matrix4x4 tileTransform, ShowDebug);
                if (groundTile != null && groundTile.UseFixedGroundAngle)
                {
                    info.Point = hit.point;
                    Vector2 tileNormalVector = Vector2.up;
                    if (groundTile.IsAngled)
                    {
                        tileNormalVector = tileTransform * (Quaternion.Euler(0f, 0f, groundTile.Angle) * Vector2.up);
                    }
                    else
                    {
                        switch (groundOrientation)
                        {
                            case GroundMode.Floor:
                                tileNormalVector = Vector2.up;
                                break;
                            case GroundMode.RightWall:
                                tileNormalVector = Vector2.left;
                                break;
                            case GroundMode.Ceiling:
                                tileNormalVector = Vector2.down;
                                break;
                            case GroundMode.LeftWall:
                                tileNormalVector = Vector2.right;
                                break;
                        }
                    }
                    info.Normal = tileNormalVector;
                    info.Angle = Vector2ToAngle(tileNormalVector);
                }
                else
                {
                    info.Point = hit.point;
                    info.Normal = hit.normal;
                    info.Angle = Vector2ToAngle(hit.normal);
                }
                info.IsValid = true;
            }

            return info;
        }

        private void StickToGround(GroundInfo info)
        {
            float angle = info.Angle * Mathf.Rad2Deg;
            characterAngle = angle;
            Vector3 pos = transform.position;

            switch (groundMode)
            {
                case GroundMode.Floor:
                    if (angle < 315f && angle > 225f) { groundMode = GroundMode.LeftWall; }
                    else if (angle > 45f && angle < 180f) { groundMode = GroundMode.RightWall; }
                    pos.y = info.Point.y + heightHalf;
                    break;
                case GroundMode.RightWall:
                    if (angle < 45f && angle > 0f) { groundMode = GroundMode.Floor; }
                    else if (angle > 135f && angle < 270f) { groundMode = GroundMode.Ceiling; }
                    pos.x = info.Point.x - heightHalf;
                    break;
                case GroundMode.Ceiling:
                    if (angle < 135f && angle > 45f) { groundMode = GroundMode.RightWall; }
                    else if (angle > 225f && angle < 360f) { groundMode = GroundMode.LeftWall; }
                    pos.y = info.Point.y - heightHalf;
                    break;
                case GroundMode.LeftWall:
                    if (angle < 225f && angle > 45f) { groundMode = GroundMode.Ceiling; }
                    else if (angle > 315f) { groundMode = GroundMode.Floor; }
                    pos.x = info.Point.x + heightHalf;
                    break;
                default:
                    break;
            }

            transform.position = pos;
        }

        /// <summary>
        /// Returns angle snapped to the closest 45-degree increment
        /// </summary>
        private float SnapAngle(float angle)
        {
            int mult = (int)(angle + 22.5f);
            mult /= 45;
            return mult * 45f;
        }

        /// <summary>
        /// Converts vector to 0-2*PI degree (counter-clockwise) angle in radians, with a vector pointing straight up being zero.
        /// </summary>
        private float Vector2ToAngle(Vector2 vector)
        {
            float angle = Mathf.Atan2(vector.y, vector.x) - (Mathf.PI / 2f);
            if (angle < 0f) { angle += Mathf.PI * 2f; }
            return angle;
        }
    }
}
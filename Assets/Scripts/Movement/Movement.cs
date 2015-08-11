using UnityEngine;
using System.Collections;

public class Movement : MonoBehaviour
{
    enum GroundMode
    {
        Floor,
        RightWall,
        Ceiling,
        LeftWall,
    }

    public Animator animator;

    public bool grounded { get; private set; }
    public bool rolling { get; private set; }
    public bool jumped { get; private set; }

    // General
    public float standingHeight = 40f;
    public float ballHeight = 30f;
    private float heightHalf
    {
        get
        {
            if (rolling || jumped) { return ballHeight / 2f; }
            else { return standingHeight / 2f; }
        }
    }

    public float standWidthHalf = 9f;
    public float spinWidthHalf = 7f;

    // Ground Movement
    public float groundAcceleration = 168.75f;
    public float groundTopSpeed = 360f;
    public float speedLimit = 960f;
    public float rollingMinSpeed = 61.875f;
    public float unrollThreshold = 30f;
    public float friction = 168.75f;
    public float rollingFriction = 84.375f;
    public float deceleration = 1800f;
    public float rollingDeceleration = 450f;
    public float slopeFactor = 450f;
    public float rollUphillSlope = 281.25f;
    public float rollDownhillSlope = 1125f;
    public float sideRaycastOffset = -4f;
    public float sideRaycastDist = 11f;
    public float groundRaycastDist = 36f;
    public float fallVelocityThreshold = 150f;

    private float groundVelocity;
    private bool hControlLock;
    private float hControlLockTime = 0.5f;
    private GroundInfo currentGroundInfo;
    private GroundMode groundMode = GroundMode.Floor;

    // Air Movement
    public float airAcceleration = 337.5f;
    public float jumpVelocity = 390f;
    public float jumpReleaseThreshold = 240f;
    public float gravity = -787.5f;
    public float terminalVelocity = 960f;
    public float airDrag = 0.96875f;

    // Underwater
    public float uwAcceleration = 84.375f;
    public float uwDeceleration = 900f;
    public float uwFriction = 84.375f;
    public float uwRollingFriction = 42.1875f;
    public float uwGroundTopSpeed = 180f;
    public float uwAirAcceleration = 168.75f;
    public float uwGravity = -225f;
    public float uwJumpVelocity = 210f;
    public float uwJumpReleaseThreshold = 120f;

    private Vector2 velocity;
    private float characterAngle;
    private bool lowCeiling;
    private bool underwater;

    private Vector2 standLeftRPos;
    private Vector2 spinLeftRPos;
    private Vector2 standRightRPos;
    private Vector2 spinRightRPos;

    private Transform waterLevel;

    private Vector2 leftRaycastPos
    {
        get
        {
            if (rolling || jumped) { return spinLeftRPos; }
            else { return standLeftRPos; }
        }
    }
    private Vector2 rightRaycastPos
    {
        get
        {
            if (rolling || jumped) { return spinRightRPos; }
            else { return standRightRPos; }
        }
    }
    
    private int speedHash;
    private int standHash;
    private int spinHash;
    private int pushHash;

    void Awake()
    {
        waterLevel = GameObject.FindWithTag("WaterLevel").transform;

        standLeftRPos = new Vector2(-standWidthHalf, 0f);
        standRightRPos = new Vector2(standWidthHalf, 0f);
        spinLeftRPos = new Vector2(-spinWidthHalf, 0f);
        spinRightRPos = new Vector2(spinWidthHalf, 0f);
        
        speedHash = Animator.StringToHash("Speed");
        standHash = Animator.StringToHash("Stand");
        spinHash = Animator.StringToHash("Spin");
        pushHash = Animator.StringToHash("Push");
    }

    bool debug = false;

    void OnGUI()
    {
        if (debug)
        {
            GUILayout.BeginArea(new Rect(10, 10, 200, 160), "Stats", "Window");
            GUILayout.Label("Underwater: " + (underwater ? "YES" : "NO"));
            GUILayout.Label("Jumped: " + (jumped ? "YES" : "NO"));
            GUILayout.Label("Rolling: " + (rolling ? "YES" : "NO"));
            if (currentGroundInfo != null && currentGroundInfo.valid)
            {
                GUILayout.Label("Ground Speed: " + groundVelocity);
                GUILayout.Label("Angle (Deg): " + (currentGroundInfo.angle * Mathf.Rad2Deg));
            }
            GUILayout.EndArea();
        }
    }
    
    void FixedUpdate()
    {
        if (Input.GetKeyDown(KeyCode.Tab)) { debug = !debug; }

        Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

        float accelSpeedCap = underwater ? uwGroundTopSpeed : groundTopSpeed;

        if (grounded)
        {
            if (!rolling && input.y < -0.005f && Mathf.Abs(groundVelocity) >= rollingMinSpeed)
            {
                rolling = true;
                transform.position -= new Vector3(0f, 5f);
            }

            float slope = 0f;
            if (rolling)
            {
                float sin = Mathf.Sin(currentGroundInfo.angle);
                bool uphill = (sin >= 0f && groundVelocity >= 0f) || (sin <= 0f && groundVelocity <= 0);
                slope = uphill ? rollUphillSlope : rollDownhillSlope;
            }
            else { slope = slopeFactor; }
            groundVelocity += (slope * -Mathf.Sin(currentGroundInfo.angle)) * Time.fixedDeltaTime;

            bool lostFooting = false;

            if (groundMode != GroundMode.Floor && Mathf.Abs(groundVelocity) < fallVelocityThreshold)
            {
                groundMode = GroundMode.Floor;
                grounded = false;
                hControlLock = true;
                hControlLockTime = 0.5f;
                lostFooting = true;
            }

            if (Input.GetButtonDown("Jump") && !lowCeiling)
            {
                float jumpVel = underwater ? uwJumpVelocity : jumpVelocity;
                velocity.x -= jumpVel * (Mathf.Sin(currentGroundInfo.angle));
                velocity.y += jumpVel * (Mathf.Cos(currentGroundInfo.angle));
                grounded = false;
                jumped = true;
            }
            else
            {
                if (hControlLock)
                {
                    hControlLockTime -= Time.fixedDeltaTime;
                    if (hControlLockTime <= 0f)
                    {
                        hControlLock = false;
                    }
                }

                if (rolling || Mathf.Abs(input.x) < 0.005f)
                {
                    // Mostly because I don't like chaining ternaries
                    float fric = underwater ? uwFriction : friction;
                    float rollFric = underwater ? uwRollingFriction : rollingFriction;

                    float frc = rolling ? rollFric : fric;
                    if (groundVelocity > 0f)
                    {
                        groundVelocity -= frc * Time.fixedDeltaTime;
                        if (groundVelocity < 0f) { groundVelocity = 0f; }
                    }
                    else if (groundVelocity < 0f)
                    {
                        groundVelocity += frc * Time.fixedDeltaTime;
                        if (groundVelocity > 0f) { groundVelocity = 0f; }
                    }
                }

                if (!hControlLock && Mathf.Abs(input.x) >= 0.005f)
                {
                    float accel = underwater ? uwAcceleration : groundAcceleration;
                    float decel = underwater ? uwDeceleration : deceleration;

                    if (input.x < 0f)
                    {
                        if (groundVelocity < 0f)
                        {
                            // TODO: Set a direction variable instead
                            Vector3 scale = Vector3.one;
                            scale.x *= Mathf.Sign(groundVelocity);
                            transform.localScale = scale;
                        }
                        float acceleration = 0f;
                        if (rolling && groundVelocity > 0f) { acceleration = rollingDeceleration; }
                        else if (!rolling && groundVelocity > 0f) { acceleration = decel; }
                        else if (!rolling && groundVelocity <= 0f) { acceleration = accel; }

                        if (groundVelocity > -accelSpeedCap)
                        {
                            groundVelocity = Mathf.Max(-accelSpeedCap, groundVelocity + (input.x * acceleration) * Time.deltaTime);
                        }
                    }
                    else
                    {
                        if (groundVelocity > 0f)
                        {
                            Vector3 scale = Vector3.one;
                            transform.localScale = scale;
                        }
                        float acceleration = 0f;
                        if (rolling && groundVelocity < 0f) { acceleration = rollingDeceleration; }
                        else if (!rolling && groundVelocity < 0f) { acceleration = decel; }
                        else if (!rolling && groundVelocity >= 0f) { acceleration = accel; }
                        if (groundVelocity < accelSpeedCap)
                        {
                            groundVelocity = Mathf.Min(accelSpeedCap, groundVelocity + (input.x * acceleration) * Time.deltaTime);
                        }
                    }
                }

                if (groundVelocity > speedLimit) { groundVelocity = speedLimit; }
                else if (groundVelocity < -speedLimit) { groundVelocity = -speedLimit; }

                if (rolling && Mathf.Abs(groundVelocity) < unrollThreshold)
                {
                    rolling = false;
                    transform.position += new Vector3(0f, 5f);
                }
                
                Vector2 angledSpeed = new Vector2(groundVelocity * Mathf.Cos(currentGroundInfo.angle), groundVelocity * Mathf.Sin(currentGroundInfo.angle));
                velocity = angledSpeed;
                if (lostFooting)
                {
                    groundVelocity = 0f;
                }
            }
        }
        else
        {
            float jumpRelThreshold = underwater ? uwJumpReleaseThreshold : jumpReleaseThreshold;
            if (jumped && velocity.y > jumpRelThreshold && Input.GetButtonUp("Jump"))
            {
                velocity.y = jumpRelThreshold;
            }
            else
            {
                // Air drag effect
                if (velocity.y > 0f && velocity.y < 4f && Mathf.Abs(velocity.x) > 7.5f)
                {
                    velocity.x *= airDrag;
                }

                float grv = underwater ? uwGravity : gravity;

                velocity.y = Mathf.Max(velocity.y + (grv * Time.fixedDeltaTime), -terminalVelocity);
            }

            if (!(rolling && jumped) && Mathf.Abs(input.x) >= 0.005f)
            {
                if ((input.x < 0f && velocity.x > -accelSpeedCap) || (input.x > 0f && velocity.x < accelSpeedCap))
                {
                    float airAcc = underwater ? uwAirAcceleration : airAcceleration;
                    velocity.x = Mathf.Clamp(velocity.x + (input.x * airAcc * Time.fixedDeltaTime), -accelSpeedCap, accelSpeedCap);
                }
            }
        }

        // Clamp velocity to global speed limit; going any faster could result in passing through things
        velocity.x = Mathf.Clamp(velocity.x, -speedLimit, speedLimit);
        velocity.y = Mathf.Clamp(velocity.y, -speedLimit, speedLimit);

        // Apply movement
        transform.position += new Vector3(velocity.x, velocity.y, 0f) * Time.fixedDeltaTime;

        // Now do collision testing

        RaycastHit2D leftHit;
        RaycastHit2D rightHit;
        WallCheck(sideRaycastDist, grounded ? sideRaycastOffset : 0f, out leftHit, out rightHit);

        if (leftHit.collider != null && rightHit.collider != null)
        {
            // Got squashed
            Debug.Log("GOT SQUASHED");
        }
        else if (leftHit.collider != null)
        {
            transform.position = new Vector2(leftHit.point.x + sideRaycastDist, transform.position.y);
            if (velocity.x < 0f)
            {
                velocity.x = 0f;
                groundVelocity = 0f;
            }
        }
        else if (rightHit.collider != null)
        {
            transform.position = new Vector2(rightHit.point.x - sideRaycastDist, transform.position.y);
            if (velocity.x > 0f)
            {
                velocity.x = 0f;
                groundVelocity = 0f;
            }
        }

        bool ceilingLeft = false;
        bool ceilingRight = false;
        int ceilDir = (int)groundMode + 2;
        if (ceilDir > 3) { ceilDir -= 4; }
        GroundInfo ceil = GroundedCheck(groundRaycastDist, (GroundMode)ceilDir, out ceilingLeft, out ceilingRight);

        bool groundedLeft = false;
        bool groundedRight = false;

        if (grounded)
        {
            currentGroundInfo = GroundedCheck(groundRaycastDist, groundMode, out groundedLeft, out groundedRight);
            grounded = groundedLeft || groundedRight;
        }
        else
        {
            if (ceil.valid && velocity.y > 0f)
            {
                bool hitCeiling = transform.position.y >= (ceil.point.y - heightHalf);
                float angleDeg = ceil.angle * Mathf.Rad2Deg;

                // Check for attaching to ceiling
                if (hitCeiling && ((angleDeg >= 225f && angleDeg <= 270f) || (angleDeg >= 90f && angleDeg <= 135f)))
                {
                    grounded = true;
                    jumped = false;
                    rolling = false;
                    currentGroundInfo = ceil;
                    groundMode = GroundMode.Ceiling;

                    groundVelocity = velocity.y * Mathf.Sign(Mathf.Sin(currentGroundInfo.angle));
                    velocity.y = 0f;
                }
                else if (hitCeiling)
                {
                    if (transform.position.y > ceil.point.y - heightHalf)
                    {
                        transform.position = new Vector2(transform.position.x, ceil.point.y - heightHalf);
                        velocity.y = 0f;
                    }
                }
            }
            else
            {
                GroundInfo info = GroundedCheck(groundRaycastDist, GroundMode.Floor, out groundedLeft, out groundedRight);

                grounded = (groundedLeft || groundedRight) && velocity.y <= 0f && transform.position.y <= (info.height + heightHalf);

                // Re-calculate ground velocity based on previous air velocity
                if (grounded)
                {
                    // If in a roll jump, add 5 to position upon landing
                    if (jumped)
                    {
                        transform.position += new Vector3(0f, 5f);
                    }

                    jumped = false;
                    rolling = false;

                    currentGroundInfo = info;
                    groundMode = GroundMode.Floor;
                    float angleDeg = currentGroundInfo.angle * Mathf.Rad2Deg;

                    // If angle is close to level with ground, just use x velocity as ground velocity
                    if (angleDeg < 22.5f || (angleDeg > 337.5 && angleDeg <= 360f))
                    {
                        groundVelocity = velocity.x;
                    }
                    else if ((angleDeg >= 22.5f && angleDeg < 45f) || (angleDeg >= 315f && angleDeg < 337.5f))
                    {
                        if (Mathf.Abs(velocity.x) > Mathf.Abs(velocity.y)) { groundVelocity = velocity.x; }
                        else { groundVelocity = velocity.y * 0.5f * Mathf.Sign(Mathf.Sin(currentGroundInfo.angle)); }
                    }
                    else if ((angleDeg >= 45f && angleDeg < 90f) || (angleDeg >= 270f && angleDeg < 315f))
                    {
                        if (Mathf.Abs(velocity.x) > Mathf.Abs(velocity.y)) { groundVelocity = velocity.x; }
                        else { groundVelocity = velocity.y * Mathf.Sign(Mathf.Sin(currentGroundInfo.angle)); }
                    }

                    velocity.y = 0f;
                }
            }
        }

        if (grounded)
        {
            StickToGround(currentGroundInfo);
            animator.SetFloat(speedHash, Mathf.Abs(groundVelocity));

            lowCeiling = ceil.valid && transform.position.y > ceil.point.y - 25f;
        }
        else
        {
            currentGroundInfo = null;
            groundMode = GroundMode.Floor;
            lowCeiling = false;

            if (Mathf.Abs(input.x) > 0.005f && !(rolling && jumped))
            {
                Vector3 scale = Vector3.one;
                scale.x *= Mathf.Sign(input.x);
                transform.localScale = scale;
            }

            if (characterAngle > 0f && characterAngle <= 180f)
            {
                characterAngle -= Time.deltaTime * 180f;
                if (characterAngle < 0f) { characterAngle = 0f; }
            }
            else if (characterAngle < 360f && characterAngle > 180f)
            {
                characterAngle += Time.deltaTime * 180f;
                if (characterAngle >= 360f) { characterAngle = 0f; }
            }
        }
        animator.SetBool(spinHash, rolling || jumped);

        if (!underwater && transform.position.y <= waterLevel.position.y)
        {
            EnterWater();
        }
        else if (underwater && transform.position.y > waterLevel.position.y)
        {
            ExitWater();
        }

        transform.localRotation = Quaternion.Euler(0f, 0f, SnapAngle(characterAngle));
    }

    void EnterWater()
    {
        underwater = true;
        groundVelocity *= 0.5f;
        velocity.x *= 0.5f;
        velocity.y *= 0.25f;
    }

    void ExitWater()
    {
        underwater = false;
        velocity.y *= 2f;
    }

    void WallCheck(float distance, float heightOffset, out RaycastHit2D hitLeft, out RaycastHit2D hitRight)
    {
        Vector2 pos = new Vector2(transform.position.x, transform.position.y + heightOffset);

        hitLeft = Physics2D.Raycast(pos, Vector2.left, distance);
        hitRight = Physics2D.Raycast(pos, Vector2.right, distance);

        Debug.DrawLine(pos, pos + (Vector2.left * distance), Color.yellow);
        Debug.DrawLine(pos, pos + (Vector2.right * distance), Color.yellow);
    }

    GroundInfo GroundedCheck(float distance, GroundMode groundMode, out bool groundedLeft, out bool groundedRight)
    {
        Quaternion rot = Quaternion.Euler(0f, 0f, (90f * (int)groundMode));
        Vector2 dir = rot * Vector2.down;
        Vector2 leftCastPos = rot * leftRaycastPos;
        Vector2 rightCastPos = rot * rightRaycastPos;

        Vector2 pos = new Vector2(transform.position.x, transform.position.y);
        RaycastHit2D leftHit = Physics2D.Raycast(pos + leftCastPos, dir, distance);
        groundedLeft = leftHit.collider != null;

        RaycastHit2D rightHit = Physics2D.Raycast(pos + rightCastPos, dir, distance);
        groundedRight = rightHit.collider != null;

        Debug.DrawLine(pos + leftCastPos, pos + leftCastPos + (dir * distance), Color.magenta);
        Debug.DrawLine(pos + rightCastPos, pos + rightCastPos + (dir * distance), Color.red);

        GroundInfo found = null;

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

            if (leftCompare >= rightCompare) { found = GetGroundInfo(leftHit); }
            else { found = GetGroundInfo(rightHit); }
        }
        else if (groundedLeft) { found = GetGroundInfo(leftHit); }
        else if (groundedRight) { found = GetGroundInfo(rightHit); }
        else { found = new GroundInfo(); }

        return found;
    }

    GroundInfo GetGroundInfo(Vector3 center)
    {
        GroundInfo info = new GroundInfo();
        RaycastHit2D groundHit = Physics2D.Raycast(center, Vector2.down, groundRaycastDist);
        if (groundHit.collider != null)
        {
            info.height = groundHit.point.y;
            info.point = groundHit.point;
            info.normal = groundHit.normal;
            info.angle = Vector2ToAngle(groundHit.normal);
            info.valid = true;
        }

        return info;
    }

    GroundInfo GetGroundInfo(RaycastHit2D hit)
    {
        GroundInfo info = new GroundInfo();
        if (hit.collider != null)
        {
            info.height = hit.point.y;
            info.point = hit.point;
            info.normal = hit.normal;
            info.angle = Vector2ToAngle(hit.normal);
            info.valid = true;
        }

        return info;
    }

    void StickToGround(GroundInfo info)
    {
        float angle = info.angle * Mathf.Rad2Deg;
        characterAngle = angle;
        Vector3 pos = transform.position;

        switch (groundMode)
        {
            case GroundMode.Floor:
                if (angle < 315f && angle > 225f) { groundMode = GroundMode.LeftWall; }
                else if (angle > 45f && angle < 180f) { groundMode = GroundMode.RightWall; }
                pos.y = info.point.y + heightHalf;
                break;
            case GroundMode.RightWall:
                if (angle < 45f && angle > 0f) { groundMode = GroundMode.Floor; }
                else if (angle > 135f && angle < 270f) { groundMode = GroundMode.Ceiling; }
                pos.x = info.point.x - heightHalf;
                break;
            case GroundMode.Ceiling:
                if (angle < 135f && angle > 45f) { groundMode = GroundMode.RightWall; }
                else if (angle > 225f && angle < 360f) { groundMode = GroundMode.LeftWall; }
                pos.y = info.point.y - heightHalf;
                break;
            case GroundMode.LeftWall:
                if (angle < 225f && angle > 45f) { groundMode = GroundMode.Ceiling; }
                else if (angle > 315f) { groundMode = GroundMode.Floor; }
                pos.x = info.point.x + heightHalf;
                break;
            default:
                break;
        }

        transform.position = pos;
    }

    /// <summary>
    /// Returns angle snapped to the closest 45-degree increment
    /// </summary>
    float SnapAngle(float angle)
    {
        int mult = (int)(angle + 22.5f);
        mult /= 45;
        return mult * 45f;
    }

    /// <summary>
    /// Converts vector to 0-360 degree (counter-clockwise) angle, with a vector pointing straight up as zero.
    /// </summary>
    float Vector2ToAngle(Vector2 vector)
    {
        float angle = Mathf.Atan2(vector.y, vector.x) - (Mathf.PI / 2f);
        if (angle < 0f) { angle += Mathf.PI * 2f; }
        return angle;
    }

    public class GroundInfo
    {
        public float height;
        public Vector3 point;
        public float distance;
        public Vector3 normal;
        public float angle;
        public bool valid = false;
    }
}
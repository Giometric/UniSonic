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

    // General
    public Sprite standSprite;
    public Sprite ballSprite;
    public float standingHeight = 40f;
    public float ballHeight = 30f;
    public float heightHalf
    {
        get
        {
            if (grounded) { return standingHeight / 2f; }
            else { return ballHeight / 2f; }
        }
    }

    // Ground Movement
    public float groundAcceleration = 168.75f;
    public float groundTopSpeed = 360f;
    public float friction = 168.75f;
    public float deceleration = 1800f;
    public float slopeFactor = 450f;
    public float sideRaycastOffset = -4f;
    public float sideRaycastDist = 11f;
    public float groundRaycastDist = 36f;
    public float fallVelocityThreshold = 150f;
    public bool grounded { get; private set; }

    private float groundVelocity;
    private bool hControlLock;
    private float hControlLockTime = 0.5f;
    private GroundInfo currentGroundInfo;
    private GroundMode groundMode = GroundMode.Floor;

    // Air Movement
    public float airAcceleration = 337.5f;
    public float jumpVelocity = 390f;
    public float gravity = -787.5f;
    public float terminalVelocity = 960f;
    public float leftRaycastX = -9f;
    public float rightRaycastX = 9f;

    private Vector2 leftRaycastPos;
    private Vector2 rightRaycastPos;
    private Vector2 velocity;
    private float characterAngle;

    void Awake()
    {
        leftRaycastPos = new Vector2(leftRaycastX, 0f);
        rightRaycastPos = new Vector2(rightRaycastX, 0f);
    }

    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 200, 180), "Stats", "Window");
        GUILayout.Label("Control Lock: " + (hControlLock ? "ON" : "OFF"));
        GUILayout.Label("Current Ground Info:");
        if (currentGroundInfo != null && currentGroundInfo.valid)
        {
            GUILayout.Label("Ground Speed: " + groundVelocity);
            GUILayout.Label("Angle (Deg): " + (currentGroundInfo.angle * Mathf.Rad2Deg));
            GUILayout.Label("Ground Mode: " + groundMode);
        }
        GUILayout.EndArea();
    }

    void FixedUpdate()
    {
        Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

        if (grounded)
        {
            groundVelocity += (slopeFactor * -Mathf.Sin(currentGroundInfo.angle)) * Time.fixedDeltaTime;

            bool lostFooting = false;

            if (groundMode != GroundMode.Floor && Mathf.Abs(groundVelocity) < fallVelocityThreshold)
            {
                groundMode = GroundMode.Floor;
                grounded = false;
                hControlLock = true;
                hControlLockTime = 0.5f;
                lostFooting = true;
            }

            if (Input.GetButtonDown("Jump"))
            {
                velocity.x -= jumpVelocity * (Mathf.Sin(currentGroundInfo.angle));
                velocity.y += jumpVelocity * (Mathf.Cos(currentGroundInfo.angle));
                grounded = false;
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
                else
                {
                    if (Mathf.Abs(input.x) >= 0.005f)
                    {
                        if (input.x < 0f)
                        {
                            if (groundVelocity < 0f)
                            {
                                Vector3 scale = Vector3.one;
                                scale.x *= Mathf.Sign(groundVelocity);
                                transform.localScale = scale;
                            }
                            float acceleration = groundVelocity > 0f ? deceleration : groundAcceleration;
                            if (groundVelocity > -groundTopSpeed)
                            {
                                groundVelocity = Mathf.Max(-groundTopSpeed, groundVelocity + (input.x * acceleration) * Time.deltaTime);
                            }
                        }
                        else
                        {
                            if (groundVelocity > 0f)
                            {
                                Vector3 scale = Vector3.one;
                                transform.localScale = scale;
                            }
                            float acceleration = groundVelocity < 0f ? deceleration : groundAcceleration;
                            if (groundVelocity < groundTopSpeed)
                            {
                                groundVelocity = Mathf.Min(groundTopSpeed, groundVelocity + (input.x * acceleration) * Time.deltaTime);
                            }
                        }
                    }
                    else
                    {
                        if (groundVelocity > 0f)
                        {
                            groundVelocity -= friction * Time.fixedDeltaTime;
                            if (groundVelocity < 0f) { groundVelocity = 0f; }
                        }
                        else if (groundVelocity < 0f)
                        {
                            groundVelocity += friction * Time.fixedDeltaTime;
                            if (groundVelocity > 0f) { groundVelocity = 0f; }
                        }
                    }
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
            if (Mathf.Abs(input.x) >= 0.005f)
            {
                if ((input.x < 0f && velocity.x > -groundTopSpeed) || (input.x > 0f && velocity.x < groundTopSpeed))
                {
                    velocity.x = Mathf.Clamp(velocity.x + (input.x * airAcceleration * Time.fixedDeltaTime), -groundTopSpeed, groundTopSpeed);
                }
            }

            velocity.y = Mathf.Max(velocity.y + (gravity * Time.fixedDeltaTime), -terminalVelocity);
        }

        // Apply movement
        transform.position += new Vector3(velocity.x, velocity.y, 0f) * Time.fixedDeltaTime;

        // Now do collision testing

        RaycastHit2D leftHit;
        RaycastHit2D rightHit;
        WallCheck(sideRaycastDist, -sideRaycastOffset, out leftHit, out rightHit);

        if (leftHit.collider != null && rightHit.collider != null)
        {
            // Got squashed
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

        bool groundedLeft = false;
        bool groundedRight = false;

        if (grounded)
        {
            currentGroundInfo = GroundedCheck(groundRaycastDist, groundMode, out groundedLeft, out groundedRight);
            grounded = groundedLeft || groundedRight;
        }
        else
        {
            bool ceilingLeft = false;
            bool ceilingRight = false;
            GroundInfo ceil = GroundedCheck(groundRaycastDist, GroundMode.Ceiling, out ceilingLeft, out ceilingRight);

            if ((ceilingLeft || ceilingRight) && velocity.y > 0f)
            {
                bool hitCeiling = transform.position.y >= (ceil.point.y - (ballHeight / 2f));
                float angleDeg = ceil.angle * Mathf.Rad2Deg;

                // Check for attaching to ceiling
                if (hitCeiling && ((angleDeg >= 225f && angleDeg <= 270f) || (angleDeg >= 90f && angleDeg <= 135f)))
                {
                    grounded = true;
                    currentGroundInfo = ceil;
                    groundMode = GroundMode.Ceiling;

                    groundVelocity = velocity.y * Mathf.Sign(Mathf.Sin(currentGroundInfo.angle));
                }
                else if (hitCeiling)
                {

                    // Set some kind of 'low-ceiling' flag
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

                grounded = (groundedLeft || groundedRight) && velocity.y <= 0f && transform.position.y <= (info.height + (standingHeight / 2f));

                // Re-calculate ground velocity based on previous air velocity
                if (grounded)
                {
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
                }
            }
        }

        if (grounded)
        {
            StickToGround(currentGroundInfo);
        }
        else
        {
            currentGroundInfo = null;
            groundMode = GroundMode.Floor;

            if (Mathf.Abs(input.x) > 0.005f)
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

        transform.localRotation = Quaternion.Euler(0f, 0f, SnapAngle(characterAngle));
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

        //RaycastHit2D centerHit = Physics2D.Raycast(pos, dir, distance);

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

        /*
        // This allows us to avoid the bug where being on a point with a different angle
        // between two grounded points results in having the wrong angle to the ground
        // Or it would, if it didn't cause other bugs
        if (centerHit.collider != null)
        {
            found.angle = Vector2ToAngle(centerHit.normal);
        }
        */
        return found;
    }

    void CeilingCheck(float distance, out RaycastHit2D ceilingLeft, out RaycastHit2D ceilingRight)
    {
        Vector2 pos = new Vector2(transform.position.x, transform.position.y);
        ceilingLeft = Physics2D.Raycast(pos + leftRaycastPos, Vector2.up, distance);

        ceilingRight = Physics2D.Raycast(pos + rightRaycastPos, Vector2.up, distance);

        Debug.DrawLine(pos + leftRaycastPos, pos + leftRaycastPos + (Vector2.up * distance), Color.blue);
        Debug.DrawLine(pos + rightRaycastPos, pos + rightRaycastPos + (Vector2.up * distance), Color.blue);
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
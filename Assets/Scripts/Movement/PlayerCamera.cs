using UnityEngine;
using System.Collections;

namespace Giometric.UniSonic
{
    public class PlayerCamera : MonoBehaviour
    {
        [SerializeField]
        new private Camera camera;
        [SerializeField]
        private Movement player;

        // These limit values match the original games
        [Header("Player Offset Limits")]
        [SerializeField]
        [Tooltip("If the player moves further down and left of the screen than these values, the camera will follow to keep them in frame.")]
        private Vector2 playerOffsetMin = new Vector2(-16f, -16f);
        [SerializeField]
        [Tooltip("If the player moves further up and to the right of the screen than these values, the camera will follow to keep them in frame.")]
        private Vector2 playerOffsetMax = new Vector2(0f, 48f);

        [Header("Player Look")]
        [SerializeField]
        [Tooltip("How many units per second to scroll the camera when looking up or down, or returning to the original vertical position.")]
        private float lookSpeed = 120f;
        [SerializeField]
        [Tooltip("How long to wait after the player has started looking up or down to begin scrolling.")]
        private float lookDelay = 2f;
        [SerializeField]
        [Tooltip("How far the player can look up.")]
        private float lookUpLimit = 104f;
        [SerializeField]
        [Tooltip("How far the player can look down.")]
        private float lookDownLimit = -88f;

        [Header("Camera View Limits")]
        [Tooltip("Global minimum and maximum coordinates that the camera should be able to see.")]
        [SerializeField]
        private Vector2 limitMin;
        [SerializeField]
        private Vector2 limitMax;
        [Header("Soft Y Centering on Ground")]
        [SerializeField]
        private bool useSoftYCentering = true;
        [SerializeField]
        [Tooltip("The speed the camera will move to center to the character's Y position when Y velocity is <= softYCenterSlowYVelocityMax.")]
        private float softYCenterSlowMoveSpeed = 360f;
        [SerializeField]
        [Tooltip("When grounded, the camera will move to center the character's Y position while their Y velocity is at or slower than this speed.")]
        private float softYCenterSlowYVelocityMax = 360f;
        [SerializeField]
        [Tooltip("The speed the camera will move to center to the character's Y position when ground speed is >= softYCenterFastGroundSpeedMin.")]
        private float softYCenterFastMoveSpeed = 960f;
        [SerializeField]
        [Tooltip("When grounded, the camera will move to center the character's Y position while their ground speed is at or faster than this speed.")]
        private float softYCenterFastGroundSpeedMin = 480f;

        private float lookTimer = 0f;
        private float lookOffset = 0f;

        private void OnDrawGizmosSelected()
        {
            Bounds playerOffsetBounds = new Bounds(playerOffsetMin, Vector3.zero);
            playerOffsetBounds.Encapsulate(playerOffsetMax);
            playerOffsetBounds.center += new Vector3(transform.position.x, transform.position.y);
            Gizmos.color = new Color32(16, 255, 64, 72);
            Gizmos.DrawWireCube(playerOffsetBounds.center, playerOffsetBounds.size);

            if (player != null && Application.isPlaying)
            {
                Gizmos.color = Color.white;
                if (useSoftYCentering && player.Grounded)
                {
                    if (Mathf.Abs(player.GroundSpeed) >= softYCenterFastGroundSpeedMin)
                    {
                        Gizmos.color = Color.red;
                    }
                    else if (Mathf.Abs(player.Velocity.y) <= softYCenterSlowYVelocityMax)
                    {
                        Gizmos.color = Color.yellow;
                    }
                }
                Gizmos.DrawWireCube(player.transform.position, new Vector3(6f, 2f, 0f));
            }

            Bounds camBounds = new Bounds(limitMin, Vector3.zero);
            camBounds.Encapsulate(limitMax);
            Gizmos.color = new Color32(255, 16, 16, 72);
            Gizmos.DrawWireCube(camBounds.center, camBounds.size);
            if (camera != null && camera.orthographic && Screen.height > 0f)
            {
                float aspect = (float)Screen.width / (float)Screen.height;
                float camViewSizeY = camera.orthographicSize * 2f;
                float camViewSizeX = camViewSizeY * aspect;

                Bounds xformBounds = camBounds;
                xformBounds.Expand(new Vector3(-camViewSizeX, -camViewSizeY));
                Gizmos.color = new Color32(255, 240, 4, 48);
                Gizmos.DrawWireCube(xformBounds.center, xformBounds.size);
            }
        }

        private void LateUpdate()
        {
            Vector3 pos = transform.position;
            pos.y -= lookOffset;
            Vector3 playerPos = player.transform.position;

            // Adjust the position we'll compare with to match the height adjustment the player gets when rolling / jumping
            if (player.Rolling || player.Jumped) { playerPos.y += 5f; }

            // Keep the player within a certain window of the center
            pos.x = Mathf.Max(pos.x, playerPos.x - playerOffsetMax.x);
            pos.x = Mathf.Min(pos.x, playerPos.x - playerOffsetMin.x);
            pos.y = Mathf.Max(pos.y, playerPos.y - playerOffsetMax.y);
            pos.y = Mathf.Min(pos.y, playerPos.y - playerOffsetMin.y);

            if (useSoftYCentering && player.Grounded)
            {
                float yCenter = playerPos.y - Mathf.Lerp(playerOffsetMin.y, playerOffsetMax.y, 0.5f);
                if (Mathf.Abs(player.GroundSpeed) >= softYCenterFastGroundSpeedMin)
                {
                    pos.y = Mathf.MoveTowards(pos.y, yCenter, Time.fixedDeltaTime * softYCenterFastMoveSpeed);
                }
                else if (Mathf.Abs(player.Velocity.y) <= softYCenterSlowYVelocityMax)
                {
                    pos.y = Mathf.MoveTowards(pos.y, yCenter, Time.fixedDeltaTime * softYCenterSlowMoveSpeed);
                }
            }

            if (player.LookingUp)
            {
                lookTimer += Time.fixedDeltaTime;
                if (lookTimer >= lookDelay || lookOffset != 0f)
                {
                    lookTimer = lookDelay;
                    lookOffset = Mathf.MoveTowards(lookOffset, lookUpLimit, lookSpeed * Time.fixedDeltaTime);
                }
            }
            else if (player.LookingDown)
            {
                lookTimer += Time.fixedDeltaTime;
                if (lookTimer >= lookDelay || lookOffset != 0f)
                {
                    lookTimer = lookDelay;
                    lookOffset = Mathf.MoveTowards(lookOffset, lookDownLimit, lookSpeed * Time.fixedDeltaTime);
                }
            }
            else
            {
                lookTimer = 0f;
                lookOffset = Mathf.MoveTowards(lookOffset, 0f, lookSpeed * Time.fixedDeltaTime);
            }

            pos.y += lookOffset;

            // Clamp to global camera view limits
            if (camera != null && camera.orthographic && Screen.height > 0f)
            {
                float aspect = (float)Screen.width / (float)Screen.height;
                float camViewSizeY = camera.orthographicSize;
                float camViewSizeX = camViewSizeY * aspect;

                pos.x = Mathf.Clamp(pos.x, limitMin.x + camViewSizeX, limitMax.x - camViewSizeX);
                pos.y = Mathf.Clamp(pos.y, limitMin.y + camViewSizeY, limitMax.y - camViewSizeY);
            }
            else
            {
                // Fallback in case something's not right
                pos.x = Mathf.Clamp(pos.x, limitMin.x, limitMax.x);
                pos.y = Mathf.Clamp(pos.y, limitMin.y, limitMax.y);
            }

            transform.position = pos;
        }
    }
}
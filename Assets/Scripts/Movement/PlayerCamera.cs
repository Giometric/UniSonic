using UnityEngine;
using System.Collections;

namespace Giometric.UniSonic
{
    public class PlayerCamera : MonoBehaviour
    {
        [SerializeField]
        new private Camera camera;
        public Movement player;

        // These limit values match the original games
        [Header("Player Offset Limits")]
        [SerializeField]
        [Tooltip("If the player moves further away from the center of the screen than these values, the camera will follow to keep them in frame.")]
        private Vector2 playerOffsetMin = new Vector2(-16f, -16f);
        private Vector2 playerOffsetMax = new Vector2(0f, 48f);

        [Header("Camera View Limits")]
        [Tooltip("Global minimum and maximum coordinates that the camera should be able to see.")]
        [SerializeField]
        private Vector2 limitMin;
        [SerializeField]
        private Vector2 limitMax;

        private void OnDrawGizmosSelected()
        {
            Bounds playerOffsetBounds = new Bounds(playerOffsetMin, Vector3.zero);
            playerOffsetBounds.Encapsulate(playerOffsetMax);
            playerOffsetBounds.center += new Vector3(transform.position.x, transform.position.y);
            Gizmos.color = new Color32(16, 255, 64, 72);
            Gizmos.DrawWireCube(playerOffsetBounds.center, playerOffsetBounds.size);

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
            Vector3 playerPos = player.transform.position;

            // Adjust the position we'll compare with to match the height adjustment the player gets when rolling / jumping
            if (player.Rolling || player.Jumped) { playerPos.y += 5f; }

            // Keep the player within a certain window of the center
            pos.x = Mathf.Max(pos.x, playerPos.x - playerOffsetMax.x);
            pos.x = Mathf.Min(pos.x, playerPos.x - playerOffsetMin.x);
            pos.y = Mathf.Max(pos.y, playerPos.y - playerOffsetMax.y);
            pos.y = Mathf.Min(pos.y, playerPos.y - playerOffsetMin.y);

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
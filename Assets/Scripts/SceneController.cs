using UnityEngine;

namespace Giometric.UniSonic
{
    public class SceneController : MonoBehaviour
    {
        [SerializeField] private float quickAccelerateMultiplier = 20f;
        [SerializeField] private Movement playerCharacter;

        [Tooltip("The transform used to define where the water level is for this scene, if any.")]
        [SerializeField] private Transform waterLevel;

        private Vector3 playerStartLocation;
        private bool debugQuickAccelerate;

        private void Start()
        {
            if (playerCharacter != null)
            {
                playerStartLocation = playerCharacter.transform.position;
                playerCharacter.WaterLevel = waterLevel;
            }
        }

        private void Update()
        {
            if (playerCharacter != null)
            {
                if (Input.GetButtonDown("Debug_ResetPlayer"))
                {
                    playerCharacter.transform.position = playerStartLocation;
                    playerCharacter.ResetMovement();
                }

                if (Input.GetButtonDown("Debug_TogglePlayerDebug"))
                {
                    playerCharacter.ShowDebug = !playerCharacter.ShowDebug;
                }
            }
            debugQuickAccelerate = Input.GetButton("Debug_QuickAccelerate");
        }

        private void FixedUpdate()
        {
            if (debugQuickAccelerate)
            {
                if (playerCharacter != null)
                {
                    if (playerCharacter.Grounded)
                    {
                        float acceleration = playerCharacter.CurrentMovementSettings.GroundAcceleration * quickAccelerateMultiplier;
                        playerCharacter.GroundSpeed = Mathf.Clamp(playerCharacter.GroundSpeed + (playerCharacter.FacingDirection * acceleration) * Time.deltaTime, -playerCharacter.GlobalSpeedLimit, playerCharacter.GlobalSpeedLimit);
                    }
                    else
                    {
                        float acceleration = playerCharacter.CurrentMovementSettings.AirAcceleration * quickAccelerateMultiplier;
                        Vector2 newVelocity = playerCharacter.Velocity;
                        newVelocity.x = Mathf.Clamp(newVelocity.x + (playerCharacter.FacingDirection * acceleration) * Time.deltaTime, -playerCharacter.GlobalSpeedLimit, playerCharacter.GlobalSpeedLimit);
                        playerCharacter.Velocity = newVelocity;
                    }
                }
            }
        }
    }
}
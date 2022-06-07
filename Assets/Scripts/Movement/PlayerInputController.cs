using UnityEngine;
using System.Collections;

namespace Giometric.UniSonic
{
    public class PlayerInputController : MonoBehaviour
    {
        [SerializeField] private Movement playerCharacter;
        public Movement PlayerCharacter { get { return playerCharacter; } }

        private void Awake()
        {
            if (playerCharacter == null)
            {
                enabled = false;
                return;
            }
        }

        private void Update()
        {
            Vector2 inputMove = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            playerCharacter.InputMove = inputMove.normalized;
            playerCharacter.InputJump = Input.GetButton("Jump");

            if (Input.GetButtonDown("Pause"))
            {
                // TODO: Pause ticks for player movement
            }
        }
    }
}
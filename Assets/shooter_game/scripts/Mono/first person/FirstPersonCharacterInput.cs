using System;
using ECM2;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

namespace shooter_game.scripts
{
    /// <summary>
    /// First person character input.
    /// </summary>
    
    public class FirstPersonCharacterInput : MonoBehaviour
    {
        public Vector2 lastMoveInput => _lastInput;
        
        private FirstPersonCharacter _character;
        private Vector2 _lastInput = Vector2.zero;
        private bool _isJumping = false;
        private bool _isCrouching = false;

        private void Awake()
        {
            _character = GetComponent<FirstPersonCharacter>();
        }

        private void OnEnable()
        {
            _character.Landed += OnLanded;
        }


        private void OnDisable()
        {
            _character.Landed -= OnLanded;
        }

        private void Update()
        {
            // Movement input, relative to character's view direction
            
            Vector3 movementDirection =  Vector3.zero;
            
            movementDirection += _character.GetRightVector() * _lastInput.x;
            movementDirection += _character.GetForwardVector() * _lastInput.y;

            _character.SetMovementDirection(movementDirection);
            
            // Crouch input
            
            if (_isCrouching)
                _character.Crouch();
            else
                _character.UnCrouch();
            
            // Jump input

            if (_isJumping)
            {
                _character.Jump();
            }
            else
                _character.StopJumping();
        }
        
        private void OnLanded(Vector3 landingVelocity)
        {
            if (_isJumping)
            {
                _character.StopJumping();
                _character.Jump();
            }
        }

        public void OnMove(InputAction.CallbackContext context)
        {
            _lastInput = context.ReadValue<Vector2>();
        }
        
        public void OnJump(InputAction.CallbackContext context)
        {
            _isJumping = context.ReadValueAsButton();
        }
        
        public void OnCrouch(InputAction.CallbackContext context)
        {
            _isCrouching = context.ReadValueAsButton();
        }
    }
}

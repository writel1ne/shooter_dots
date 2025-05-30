using UnityEngine;
using UnityEngine.InputSystem;

namespace shooter_game.scripts
{
    /// <summary>
    ///     First person character input.
    /// </summary>
    public class FirstPersonCharacterInput : MonoBehaviour
    {
        private FirstPersonCharacter _character;
        private bool _isCrouching;
        private bool _isJumping;
        private Vector2 _lastInput = Vector2.zero;
        public Vector2 lastMoveInput => _lastInput;

        private void Awake()
        {
            _character = GetComponent<FirstPersonCharacter>();
        }

        private void Update()
        {
            // Movement input, relative to character's view direction

            var movementDirection = Vector3.zero;

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
                _character.Jump();
            else
                _character.StopJumping();
        }

        private void OnEnable()
        {
            _character.Landed += OnLanded;
        }


        private void OnDisable()
        {
            _character.Landed -= OnLanded;
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
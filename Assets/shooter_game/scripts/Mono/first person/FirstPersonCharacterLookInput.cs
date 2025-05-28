using UnityEngine;
using UnityEngine.InputSystem;

namespace shooter_game.scripts
{
    public class FirstPersonCharacterLookInput : MonoBehaviour
    {
        public Vector2 lastLookInput => _lastInput;
        
        [Space(15.0f)]
        [SerializeField]private bool _invertLook = true;
        [Tooltip("Mouse look sensitivity")]
        [SerializeField]private Vector2 _mouseSensitivity = new Vector2(1.0f, 1.0f);
        
        [Space(15.0f)]
        [Tooltip("How far in degrees can you move the camera down.")]
        [SerializeField]private float _minPitch = -80.0f;
        [Tooltip("How far in degrees can you move the camera up.")]
        [SerializeField] private float _maxPitch = 80.0f;
        
        private Vector2 _lastInput;
        private FirstPersonCharacter _character;

        private void Awake()
        {
            _character = GetComponent<FirstPersonCharacter>();
        }

        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
        }

        private void Update()
        {
            _lastInput *= _mouseSensitivity;
            
            _character.AddControlYawInput(_lastInput.x);
            _character.firstPersonCamera?.AddControlPitchInput(_invertLook ? -_lastInput.y : _lastInput.y);
        }
        
        public void OnLook(InputAction.CallbackContext context)
        {
            _lastInput = context.ReadValue<Vector2>();
        }
    }
}

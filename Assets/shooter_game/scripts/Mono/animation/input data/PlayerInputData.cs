using ECM2;
using UnityEngine;

namespace shooter_game.scripts.animation.input_data
{
    public class PlayerInputData : VectorInputData
    {
        public Character.MovementMode movementMode;
        public Vector3 playerLocalVelocity;
        public Vector2 playerLookInput;
        public Vector2 playerMoveInput;

        public PlayerInputData(Character.MovementMode movementMode = Character.MovementMode.None,
            Vector3 localVelocity = default,
            Vector3 velocity = default,
            Vector2 moveInput = default,
            Vector2 lookInput = default)
            : base(velocity)
        {
            this.movementMode = movementMode;
            playerLocalVelocity = localVelocity;
            playerMoveInput = moveInput;
            playerLookInput = lookInput;
        }

        public PlayerInputData() : this(default)
        {
        }

        public override void UpdateData(IInputData data)
        {
            if (data is PlayerInputData thisData)
            {
                base.UpdateData(thisData);
                movementMode = thisData.movementMode;
                playerLookInput = thisData.playerLookInput;
                playerMoveInput = thisData.playerMoveInput;
                playerLocalVelocity = thisData.playerLocalVelocity;
            }
        }
    }
}
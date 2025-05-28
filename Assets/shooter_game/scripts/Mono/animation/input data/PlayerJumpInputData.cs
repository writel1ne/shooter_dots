using System;
using ECM2;
using UnityEngine;

namespace shooter_game.scripts.animation.input_data
{
    public class PlayerJumpInputData : AbstractInputData
    {
        public float instantValueSeconds;
        public Character.MovementMode movementMode;
        
        public PlayerJumpInputData(
            Character.MovementMode movementMode = Character.MovementMode.None, 
            float instantValueSeconds = 0)
        {
            this.movementMode = movementMode;
            this.instantValueSeconds = instantValueSeconds;
        }

        public PlayerJumpInputData() : this(default)
        {
            
        }

        public override void UpdateData(IInputData data)
        {
            if (data is PlayerJumpInputData thisData )
            {
                movementMode = thisData.movementMode;
                instantValueSeconds = thisData.instantValueSeconds;
            }
        }
    }
}
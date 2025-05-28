using System;
using shooter_game.scripts.animation.input_data;

namespace shooter_game.scripts.animation
{
    public abstract class AbstractInputData : IInputData
    {
        public AbstractInputData()
        {
            
        }

        public Type GetDataType()
        {
            return GetType();
        }

        public abstract void UpdateData(IInputData data);
    }
}
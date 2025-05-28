using System;

namespace shooter_game.scripts.animation.input_data
{
    public interface IInputData
    {
        public Type GetDataType();
        public void UpdateData(IInputData data);
    }
}
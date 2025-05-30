using UnityEngine;

namespace shooter_game.scripts.animation.input_data
{
    public class VectorInputData : AbstractInputData
    {
        public Vector3 lastVector;
        public Vector3 vector;

        public VectorInputData(Vector3 vector)
        {
            this.vector = vector;
            lastVector = Vector3.zero;
        }

        public VectorInputData()
        {
            vector = Vector3.zero;
            lastVector = Vector3.zero;
        }

        public override void UpdateData(IInputData data)
        {
            if (data is VectorInputData thisData)
            {
                lastVector = vector;
                vector = thisData.vector;
            }
        }
    }
}
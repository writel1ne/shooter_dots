using shooter_game.scripts.animation.input_data;
using Sirenix.OdinInspector;
using UnityEngine;

namespace shooter_game.scripts.animation.profiles
{
    public abstract class AnimationProfileBase : SerializedScriptableObject
    {
        public abstract IInputData GetGenericInputData();
        public abstract void Process(Animator animator, float deltaTime);
        public abstract void TrySendData(IInputData data);
    }
}
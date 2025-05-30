using ECM2;
using shooter_game.scripts.animation.input_data;
using UnityEngine;

namespace shooter_game.scripts.animation.profiles
{
    [CreateAssetMenu(fileName = "AnimationProfiles", menuName = "AnimationProfiles/JumpProfile")]
    public class JumpProfile : BaseCurveProfile<PlayerJumpInputData>
    {
        [SerializeField] private float _minValue;
        [SerializeField] private float _maxValue = 2f;
        [SerializeField] private float _maxCalmValue = 1.5f;

        protected override void Operation(float deltaTime)
        {
            UpdateAnimationState(deltaTime);

            if (animationStateManager.animationState == AnimationState.Activating)
                animationValue = Mathf.LerpUnclamped(animationValue,
                    inputData.instantValueSeconds > 0 ? _maxValue : _maxCalmValue,
                    inCurve.Evaluate(animationValue) * deltaTime * inMultiplier);
            else
                animationValue = Mathf.LerpUnclamped(animationValue,
                    inputData.instantValueSeconds > 0 ? _maxValue : _minValue,
                    outCurve.Evaluate(animationValue) * deltaTime * outMultiplier);

            inputData.instantValueSeconds -= 1 * Time.deltaTime;
            animationStateManager.IncreaseTime(deltaTime);
        }

        private void UpdateAnimationState(float deltaTime)
        {
            animationStateManager.SetState(inputData.movementMode == Character.MovementMode.Falling
                ? AnimationState.Activating
                : AnimationState.Deactivating);
        }
    }
}
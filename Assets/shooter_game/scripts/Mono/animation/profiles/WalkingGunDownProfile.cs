using System;
using ECM2;
using shooter_game.scripts.animation.input_data;
using UnityEngine;

namespace shooter_game.scripts.animation.profiles
{
    [CreateAssetMenu(fileName = "AnimationProfiles", menuName = "AnimationProfiles/WalkingGunDownProfile")]
    public class WalkingGunDownProfile : BaseCurveProfile<PlayerInputData>
    {
        [SerializeField] private float _minValue;
        [SerializeField] private float _maxValue = 10f;

        protected override void Operation(float deltaTime)
        {
            UpdateAnimationState(deltaTime);

            if (animationStateManager.animationState == AnimationState.Activating)
                animationValue = Mathf.LerpUnclamped(animationValue,
                    Math.Clamp(inputData.vector.magnitude, _minValue, _maxValue),
                    inCurve.Evaluate(animationValue) * deltaTime * inMultiplier);
            else
                animationValue = Mathf.LerpUnclamped(animationValue, minOutParameterValue,
                    outCurve.Evaluate(animationValue) * deltaTime * outMultiplier);

            animationStateManager.IncreaseTime(deltaTime);
        }

        private void UpdateAnimationState(float deltaTime)
        {
            if (inputData.movementMode == Character.MovementMode.Walking && inputData.vector.magnitude > 0.1)
                animationStateManager.SetState(AnimationState.Activating);
            else
                animationStateManager.SetState(AnimationState.Deactivating);
        }
    }
}
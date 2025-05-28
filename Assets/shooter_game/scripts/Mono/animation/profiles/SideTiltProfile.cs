using System;
using ECM2;
using shooter_game.scripts.animation.input_data;
using UnityEngine;

namespace shooter_game.scripts.animation.profiles
{
    [CreateAssetMenu(fileName = "AnimationProfiles", menuName = "AnimationProfiles/SideTiltProfile")]
    public class SideTiltProfile : BaseCurveProfile<PlayerInputData>
    {
        [SerializeField] private float _activationThreshold = 0.1f;
        [SerializeField] private float _minValue = -1f;
        [SerializeField] private float _maxValue = 1f;
        [SerializeField] private float _maxTiltWhileFalling = 0.3f;
        
        protected override void Operation(float deltaTime)
        {
            UpdateAnimationState(deltaTime);
            
            if (animationStateManager.animationState == AnimationState.Activating)
            {
                float max = Math.Sign(inputData.playerLocalVelocity.x) * 1;
                max = inputData.movementMode == Character.MovementMode.Falling
                    ? Math.Clamp(max, -_maxTiltWhileFalling, _maxTiltWhileFalling)
                    : max;
                
                animationValue = Mathf.LerpUnclamped(animationValue, max, inCurve.Evaluate(animationValue) * deltaTime * inMultiplier);
            }
            else
            {
                animationValue = Mathf.LerpUnclamped(animationValue, minOutParameterValue, outCurve.Evaluate(animationValue) * deltaTime * outMultiplier);
            }

            animationValue = Math.Clamp(animationValue, _minValue, _maxValue);
            
            animationStateManager.IncreaseTime(deltaTime);
        }
        
        private void UpdateAnimationState(float deltaTime)
        {
            if (Math.Abs(inputData.playerLocalVelocity.x) > _activationThreshold && (
                inputData.movementMode == Character.MovementMode.Walking || inputData.movementMode == Character.MovementMode.Falling))
            {
                animationStateManager.SetState(AnimationState.Activating);
            }
            else
            {
                animationStateManager.SetState(AnimationState.Deactivating);
            }
        }
    }
}
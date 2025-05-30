using ECM2;
using shooter_game.scripts.animation.input_data;
using UnityEngine;

namespace shooter_game.scripts.animation.profiles
{
    [CreateAssetMenu(fileName = "AnimationProfiles", menuName = "AnimationProfiles/WalkBobbingProfile")]
    public class WalkBobbing : BaseCurveProfile<PlayerInputData>
    {
        [SerializeField] private float _maxPlayerSpeed = 7f;

        protected override void Operation(float deltaTime)
        {
            UpdateAnimationState(deltaTime);

            if (animationStateManager.animationState == AnimationState.Activating)
                animationValue = Mathf.Lerp(animationValue, inputData.vector.magnitude / _maxPlayerSpeed,
                    inCurve.Evaluate(animationValue) * deltaTime * inMultiplier);
            else
                animationValue = Mathf.Lerp(animationValue, 0,
                    outCurve.Evaluate(animationValue) * deltaTime * outMultiplier);

            animationStateManager.IncreaseTime(deltaTime);
        }

        private void UpdateAnimationState(float deltaTime)
        {
            var newMagnitude = inputData.vector.magnitude;
            var lastMagnitude = inputData.lastVector.magnitude;

            if (newMagnitude > 0 && inputData.movementMode == Character.MovementMode.Walking)
                animationStateManager.SetState(AnimationState.Activating);
            else
                animationStateManager.SetState(AnimationState.Deactivating);
        }
    }
}
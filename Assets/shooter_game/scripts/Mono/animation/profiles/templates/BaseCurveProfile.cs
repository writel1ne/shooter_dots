using shooter_game.scripts.animation.input_data;
using shooter_game.scripts.animation.utils;
using UnityEngine;

namespace shooter_game.scripts.animation.profiles
{
    public abstract class BaseCurveProfile<T> : AnimationProfile<T> where T : class, IInputData, new()
    {
        [SerializeField] private AnimationCurve _inCurve = AnimationCurve.Linear(0, 1, 1, 1);
        [SerializeField] private AnimationCurve _outCurve = AnimationCurve.Linear(0, 1, 1, 1);
        [SerializeField] private float _inMultiplier = 2;
        [SerializeField] private float _outMultiplier = 2;


        public AnimationCurve inCurve => _inCurve;
        public AnimationCurve outCurve => _outCurve;
        public float inMultiplier => _inMultiplier;
        public float outMultiplier => _outMultiplier;

        protected override void UpdateTimeRange()
        {
            var inTimeRange = inCurve.GetTimeRange();
            var outTimeRange = outCurve.GetTimeRange();

            // minInParameterValue = animatorParameters == AnimatorParameters.LayerWeight ? 0 : inTimeRange.x;
            // maxInParameterValue = animatorParameters == AnimatorParameters.LayerWeight ? 1 : inTimeRange.y;
            // minOutParameterValue = animatorParameters == AnimatorParameters.LayerWeight ? 0 : outTimeRange.x;
            // maxOutParameterValue = animatorParameters == AnimatorParameters.LayerWeight ? 1 : outTimeRange.y;
        }
    }
}
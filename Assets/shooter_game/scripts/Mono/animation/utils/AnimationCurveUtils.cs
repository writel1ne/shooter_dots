using UnityEngine;

namespace shooter_game.scripts.animation.utils
{
    public static class AnimationCurveUtils
    {
        public static Vector2 GetTimeRange(this AnimationCurve curve)
        {
            var keyframes = curve.keys;
            var minTime = float.MaxValue;
            var maxTime = float.MinValue;

            foreach (var keyframe in keyframes)
            {
                minTime = keyframe.time < minTime ? keyframe.time : minTime;
                maxTime = keyframe.time > maxTime ? keyframe.time : maxTime;
            }

            return new Vector2(minTime, maxTime);
        }
    }
}
using System;
using UnityEngine;

namespace shooter_game.scripts.animation
{
    public class AnimationStateManager
    {
        public AnimationState animationState { get; private set; } = AnimationState.Deactivating;
        public AnimationState lastAnimationState { get; private set; } = AnimationState.Deactivating;
        public float time { get; private set; } = 0;
        public float lastTime { get; private set; } = 0;
        public bool firstTickAfterUpdate { get; private set; } = true;

        public void SetState(AnimationState newState)
        {
            if (animationState != newState)
            {
                lastAnimationState = animationState;
                animationState = newState;
                lastTime = time;
                time = 0;
                firstTickAfterUpdate = true;
               // Debug.Log($"set state {animationState} from {lastAnimationState}");
            }
        }

        public void IncreaseTime(float delta)
        {
            firstTickAfterUpdate = false;
            if (delta >= 0) time += delta;
            time = Math.Clamp(time, 0, 1);
        }

        public void ResetTime(bool resetLastTime)
        {
            time = 0;
            lastTime = resetLastTime ? 0 : lastTime;
            firstTickAfterUpdate = true;
        }
        
        public void ResetTime()
        {
            ResetTime(true);
        }
    }
}
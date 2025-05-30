using System;
using System.Collections.Generic;
using shooter_game.scripts.animation.input_data;
using shooter_game.scripts.animation.profiles;
using UnityEngine;

namespace shooter_game.scripts.animation
{
    public abstract class AnimationProfile<T> : AnimationProfileBase where T : class, IInputData, new()
    {
        [SerializeField] private HashSet<AnimatorParameter> _animatorParameters = new();

        protected float animationValue;
        protected AnimationStateManager animationStateManager = new();

        public Type animationProfileType => typeof(T);
        public Type inputDataType => inputData.GetDataType();
        public T inputData { get; } = new();

        public HashSet<AnimatorParameter> animatorParameters => _animatorParameters;

        protected float minInParameterValue { get; set; }
        protected float maxInParameterValue { get; set; }
        protected float minOutParameterValue { get; set; }
        protected float maxOutParameterValue { get; set; }

        protected abstract void Operation(float deltaTime);

        public override void Process(Animator animator, float deltaTime)
        {
            UpdateTimeRange();
            Operation(deltaTime);

            foreach (var animatorParameter in _animatorParameters)
                switch (animatorParameter.AnimatorParameterType)
                {
                    case AnimatorParameterType.LayerWeight:
                        SetLayerWeight(animator, animatorParameter);
                        break;
                    case AnimatorParameterType.Float:
                        animator.SetFloat(animatorParameter.Name, animationValue);
                        break;
                }
        }

        private void SetLayerWeight(Animator animator, AnimatorParameter parameter)
        {
            animator.SetLayerWeight(animator.GetLayerIndex(parameter.Name), animationValue);
        }

        public override void TrySendData(IInputData data)
        {
            UpdateInputData(data as T);
        }

        private void UpdateInputData(T data)
        {
            inputData.UpdateData(data);
        }

        public override IInputData GetGenericInputData()
        {
            return inputData;
        }

        protected virtual void UpdateTimeRange()
        {
            // minInParameterValue = animatorParameters == AnimatorParameters.LayerWeight ? 0 : minInParameterValue;
            // maxInParameterValue = animatorParameters == AnimatorParameters.LayerWeight ? 1 : maxInParameterValue;
            // minOutParameterValue = animatorParameters == AnimatorParameters.LayerWeight ? 0 : minOutParameterValue;
            // maxOutParameterValue = animatorParameters == AnimatorParameters.LayerWeight ? 1 : maxOutParameterValue;
        }
    }
}
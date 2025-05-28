using System;
using System.Collections.Generic;
using ECM2;
using JetBrains.Annotations;
using shooter_game.scripts.animation.input_data;
using shooter_game.scripts.animation.profiles;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace shooter_game.scripts.animation
{
    public class AnimationManager : MonoBehaviour
    {
        [SerializeField] private List<AnimationProfileBase> _animationProfilesList = new List<AnimationProfileBase>();
        [SerializeField] private FirstPersonCharacter _character;
        [SerializeField] private Animator _animator;
            
        private Dictionary<Type, HashSet<AnimationProfileBase>> _animationProfiles = new Dictionary<Type, HashSet<AnimationProfileBase>>();

        private void OnEnable()
        {
            _character.CharacterMovementUpdated += CharacterMovementUpdated;
        }
        
        private void OnDisable()
        {
            _character.CharacterMovementUpdated -= CharacterMovementUpdated;
        }

        private void Start()
        {
            if (_character == null) return;
            
            foreach (var animationProfile in _animationProfilesList)
            {
                if (animationProfile.GetGenericInputData() == null)
                {
                    Debug.Log("animationProfile inputData is null");
                    continue;
                }
                
                if (_animationProfiles.ContainsKey(animationProfile.GetGenericInputData().GetDataType()))
                {
                    _animationProfiles[animationProfile.GetGenericInputData().GetDataType()].Add(animationProfile);
                }
                else
                {
                    _animationProfiles.Add(animationProfile.GetGenericInputData().GetDataType(), new HashSet<AnimationProfileBase>(){animationProfile});
                }
            }

            foreach (var VARIABLE in _animationProfiles)
            {
                foreach (var VARIABLE1 in VARIABLE.Value)
                {
                    //Debug.Log(VARIABLE1);
                }
            }
        }

        private void LateUpdate()
        {
            BroadcastData(new PlayerInputData(
                movementMode: _character.movementMode, 
                localVelocity: _character.localVelocity,
                velocity: _character.velocity,
                moveInput: _character.moveInputComponent.lastMoveInput, 
                lookInput: _character.lookInputComponent.lastLookInput));
            
            foreach (var animationProfiles in _animationProfiles.Values)
            {
                foreach (var profile in animationProfiles)
                {
                    profile.Process(_animator, Time.deltaTime);
                }
            }
        }
        
        public void BroadcastData(AbstractInputData data)
        {
            if (_animationProfiles.ContainsKey(data.GetDataType()))
            {
                foreach (AnimationProfileBase animationProfile in _animationProfiles[data.GetDataType()])
                {
                    animationProfile.TrySendData(data);
                }
            }
        }
        
        private void CharacterMovementUpdated(float deltaTime)
        {
            
        }
    }
}
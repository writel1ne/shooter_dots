using System;
using System.Collections.Generic;
using ECM2;
using Sirenix.Serialization;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace shooter_game.scripts
{
    [RequireComponent(typeof(FirstPersonCharacter))]

    public class FirstPersonCamera : MonoBehaviour
    {
        [Header("Настройки Поворота")]
        [Tooltip("Максимальный угол поворота по оси Z в градусах (в каждую сторону).")]
        [SerializeField]
        private float _maxZRotationAngle = 5f;
        
        [Header("Настройки Поворота")]
        [Tooltip("Максимальный угол поворота по оси X в градусах (в каждую сторону).")]
        [SerializeField]
        private float _maxXRotationAngle = 5f;

        [Tooltip("Скорость поворота камеры при активном вводе.")]
        [SerializeField]
        private float _rotationSpeed = 5f;

        [Tooltip("Скорость возврата камеры в исходное положение (0 градусов по Z).")]
        [SerializeField]
        private float _returnSpeed = 5f;

        [Tooltip("Порог чувствительности ввода. Значения меньше этого порога (по модулю) считаются отсутствием ввода.")]
        [SerializeField]
        private float _inputThreshold = 0.1f;
        
        [SerializeField] private GameObject _cameraParent;
        
        private FirstPersonCharacter _character;
        private Camera _camera => _character.camera;
        private FirstPersonCamera _firstPersonCamera => _character.firstPersonCamera;
        private Quaternion _localRotation => _camera.transform.localRotation;
        private float _cameraPitch = 0;
        private float _horizontalInput = 0;
        private float _verticalInput = 0;
        private float _currentZRotation = 0f;
        private float _currentXRotation = 0f;


        private void Start()
        {
            _character = GetComponent<FirstPersonCharacter>();

            if (_cameraParent == null)
            {
                _cameraParent = transform.parent.gameObject;
            }
        }

        private void OnEnable()
        {
            // _character.Crouched += OnCrouched;
            // _character.UnCrouched += OnUnCrouched;
            // _character.Jumped += OnJumped;
            // _character.Landed += OnLanded;
        }
        
        private void OnDisable()
        {
            // _character.Crouched -= OnCrouched;
            // _character.UnCrouched -= OnUnCrouched;
            // _character.Jumped -= OnJumped;
            // _character.Landed -= OnLanded;
        }

        private void Update()
        {
            // float targetZRotation = GetTresholded(horizontalInput, _maxZRotationAngle);
            // float targetXRotation = GetTresholded(verticalInput, _maxXRotationAngle);
            //
            //
            // float speedZ = Math.Abs(horizontalInput) > _inputThreshold ? _rotationSpeed : _returnSpeed;
            // float speedX = Math.Abs(verticalInput) > _inputThreshold ? _rotationSpeed : _returnSpeed;
            // float newCurrentZRotation = Mathf.LerpAngle(currentZRotation, targetZRotation, speedZ * Time.deltaTime);
            // float newCurrentXRotation = Mathf.LerpAngle(currentXRotation, targetXRotation, speedX * Time.deltaTime);
            // currentZRotation = Math.Abs(newCurrentZRotation) > Math.Abs(currentZRotation) ? 0 : newCurrentZRotation;
            // currentXRotation = Math.Abs(newCurrentXRotation) > Math.Abs(currentXRotation) ? 0 : newCurrentXRotation;
            //
            // _camera.transform.localEulerAngles = new Vector3(currentXRotation, currentZRotation, currentZRotation);
            // verticalInput = Math.Clamp(verticalInput - (speedX * Time.deltaTime * Math.Sign(verticalInput)), -1, 1);
        }

        public void AddYawRotation(float value)
        {
            _horizontalInput = Mathf.Clamp(value, -1f, 1f);
        }
        
        public void AddPitchRotation(float value)
        {
            _verticalInput = Mathf.Clamp(value, -1f, 1f);
        }

        public void AddControlPitchInput(float value, float minPitch = -80.0f, float maxPitch = 80.0f)
        {
            if (value != 0.0f)
            {
                _cameraPitch = MathLib.ClampAngle(_cameraPitch + value, minPitch, maxPitch);
            }
        }

        protected void UpdateCameraParentRotation()
        {
            _cameraParent.transform.localRotation = Quaternion.Euler(_cameraPitch, 0.0f, 0.0f);
        }

        protected void LateUpdate()
        {
            UpdateCameraParentRotation();
        }
        
        protected void OnCrouched()
        {
            AddPitchRotation(-1);
        }
        
        protected void OnUnCrouched()
        {
            AddPitchRotation(1);
        }
        
        protected void OnJumped()
        {
            AddPitchRotation(1);
        }

        protected void OnLanded(Vector3 landingVelocity)
        {
            AddPitchRotation(-1);
        }

        private float GetTresholded(float input, float maxRotation)
        {
            float targetRotation;
            
            if (Mathf.Abs(input) > _inputThreshold)
            {
                targetRotation = -input * maxRotation;
            }
            else
            {
                targetRotation = 0f;
            }

            return targetRotation;
        }
    }
}
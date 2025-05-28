using ECM2;
using shooter_game.scripts.animation;
using shooter_game.scripts.animation.input_data;
using shooter_game.scripts.animation.profiles;
using Sirenix.Utilities;
using TMPro;
using Unity.Mathematics.Geometry;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

namespace shooter_game.scripts
{
    /// <summary>
    /// This example extends a Character (through inheritance), implementing a First Person control.
    /// </summary>
    
    [RequireComponent(typeof(FirstPersonCharacterInput))]
    [RequireComponent(typeof(FirstPersonCharacterLookInput))]
    public class FirstPersonCharacter : Character
    {
        [Header("Source Movement Settings")]
        [Tooltip("Maximum speed gain per second when air strafing. (sv_airaccelerate in Source)")]
        [SerializeField] private float _airStrafeAcceleration = 100.0f; // Значение из CS:GO для sv_airaccelerate 12, но юниты другие. Нужно подбирать.
        // Это значение будет тем, насколько сильно можно влиять на скорость в воздухе.
        // Высокое значение, как _maxAcceleration, может быть подходящим.
        // Либо использовать _maxAcceleration напрямую.

        [Tooltip("The maximum speed the player can reach horizontally in the air using air strafing, before friction/drag caps it. " +
                 "Note: Total speed can be higher due to initial jump speed. This primarily limits speed gain from pure strafing.")]
        [SerializeField] private float _maxAirSpeed = 30.0f; // В Source это обычно около 30 юнитов/с (sv_airspeed_cap).
        // Это *не* _maxWalkSpeed. Это ограничение на *прирост* скорости от стрейфа.
        // Часто _maxWalkSpeed используется как общий предел скорости в воздухе.
        
        [Header("Source Movement Settings")]
        [Tooltip("Absolute maximum speed the character can reach. (sv_maxvelocity in Source)")]
        [SerializeField] private float _maxOverallSpeed = 30.0f; // Например, 3500 юнитов/с в Source ~ 60-70 м/с. Подбирать!
        // Если хотите неограниченный банихоп, поставьте очень большое значение.
        
        [Header("Source Movement Settings")]
        [SerializeField] private float _crouchJumpBonus = 1.0f; // Дополнительный импульс
        
        [Header("Source Air Movement Specifics")]
        [Tooltip("This value is similar to sv_airaccelerate in Source games. It dictates how much control you have and how quickly you can gain speed by strafing. Needs careful tuning with Max Walk Speed.")]
        [SerializeField] private float _sourceAirAccelerate = 100.0f; // Значение по умолчанию, сильно зависит от ваших MaxWalkSpeed и общей настройки.
        // Может быть равно _maxAcceleration или быть отдельной настройкой.
        // В Source (HL2) sv_airaccelerate = 10.

        [Tooltip("This value is similar to cl_airspeed in Quake / sv_maxairspeed in some Source mods. It's a cap on how much speed can be directly 'added' by a strafe input along the wish_dir per tick. Typically set to something like 30 in Source units.")]
        [SerializeField] private float _sourceMaxAirWishSpeed = 3.0f; // Если MaxWalkSpeed = 7-10, то это значение может быть 2-4.
        // Это НЕ общая максимальная скорость в воздухе.
        [SerializeField] private float _sourceMaxAirVelocity = 15;

        
        [SerializeField] public FirstPersonCharacterLookInput lookInputComponent { get; private set; }
        [SerializeField] public FirstPersonCharacterInput moveInputComponent { get; private set; }
        [SerializeField] private Transform _planetTransform;
        [SerializeField] private AnimationManager _animationManager;
        public FirstPersonCamera firstPersonCamera => _fpvCamera;
        public Vector3 localVelocity => transform.InverseTransformDirection(velocity);
        
        private FirstPersonCamera _fpvCamera;

        private bool _isAttacking = false;

        protected override void Start()
        {
            base.Start();
            
            if (TryGetComponent<FirstPersonCamera>(out FirstPersonCamera _fpv))
            {
                _fpvCamera = _fpv;
            }

            moveInputComponent = GetComponent<FirstPersonCharacterInput>();
            lookInputComponent = GetComponent<FirstPersonCharacterLookInput>();
        }

        protected void UpdateRotation(float deltaTime)
        {
            base.UpdateRotation(deltaTime);

            if (_planetTransform.gameObject.activeSelf)
            {
                Vector3 fromPlanet = GetPosition() - _planetTransform.position;
                SetGravityVector(fromPlanet.normalized * System.Math.Clamp(GetGravityMagnitude(), -9.8f, 9.8f));
                
                Vector3 worldUp = GetGravityDirection() * -1.0f;
               
                Quaternion newRotation = Quaternion.FromToRotation(GetUpVector(), worldUp) * GetRotation();
                
                SetRotation(newRotation);
            }
        }
        
        public void AddControlYawInput(float value)
        {
            if (value != 0.0f)
            {
                AddYawInput(value);
                _fpvCamera?.AddYawRotation(value);
            }
        }
        
        protected override void Reset()
        {
            base.Reset();

            SetRotationMode(Character.RotationMode.None);
        }

        protected override void OnJumped()
        {
            base.OnJumped();
            _animationManager.BroadcastData(new PlayerJumpInputData(movementMode, 0.1f));
        }

        protected override void OnLanded(Vector3 landingVelocity)
        {
            base.OnLanded(landingVelocity);
            _animationManager.BroadcastData(new PlayerJumpInputData(movementMode, 0.1f));
        }

        protected override void OnAfterSimulationUpdate(float deltaTime)
        {
            base.OnAfterSimulationUpdate(deltaTime); // Вызываем базовую реализацию, если она что-то делает

            if (characterMovement.velocity.magnitude > _maxOverallSpeed)
            {
                characterMovement.velocity = characterMovement.velocity.normalized * _maxOverallSpeed;
            }
        }
        
       protected override void FallingMovementMode(float deltaTime)
        {
            Vector3 worldUp = -GetGravityDirection();

            // Разделяем скорость на боковую (горизонтальную) и вертикальную
            Vector3 lateralVelocity = Vector3.ProjectOnPlane(characterMovement.velocity, worldUp);
            Vector3 verticalVelocity = Vector3.Project(characterMovement.velocity, worldUp);

            // Получаем желаемое направление движения от инпута (уже должно быть мировым)
            // GetMovementDirection() должен возвращать вектор инпута (WASD), нормализованный.
            Vector3 wishDir = GetMovementDirection(); 
            wishDir = Vector3.ProjectOnPlane(wishDir, worldUp); // Убедимся, что он горизонтальный

            if (wishDir.sqrMagnitude > 0.001f)
            {
                wishDir.Normalize(); // Нормализуем, если есть инпут

                // --- Логика воздушного ускорения в стиле Source (PM_AirMove / PM_AirAccelerate) ---
                
                // wishspeed - это скорость, которую мы хотим достичь в направлении wishDir ЗА ЭТОТ ТИК.
                // В Source это часто cl_airspeed или sv_maxairspeed (например, 30 юнитов/с).
                // Это НЕ максимальная скорость ходьбы. Это именно "вклад" от стрейфа.
                float wishSpeed = _sourceMaxAirWishSpeed; 

                float currentSpeedProjection = Vector3.Dot(lateralVelocity, wishDir);
                float addSpeed = wishSpeed - currentSpeedProjection;

                if (addSpeed > 0) // Только если нам нужно добавить скорость в этом направлении
                {
                    // accelspeed - это фактическое ускорение, которое мы можем применить.
                    // В Source: accel = sv_airaccelerate * wishspeed_clamped_by_max_speed * frametime;
                    // У нас _sourceAirAccelerate это аналог sv_airaccelerate.
                    // Вместо wishspeed_clamped_by_max_speed можно использовать просто wishSpeed (т.е. _sourceMaxAirWishSpeed)
                    // или даже _maxWalkSpeed, если _sourceAirAccelerate настроен соответствующе.
                    // Для простоты, можно считать, что _sourceAirAccelerate это УЖЕ итоговое ускорение (м/с^2)
                    
                    float actualAcceleration = _sourceAirAccelerate; // Используем наше новое поле
                    // Если используете GetMaxAcceleration(), он учитывает _airControl.
                    // float actualAcceleration = GetMaxAcceleration(); 
                    
                    float accelAmount = actualAcceleration * deltaTime;

                    if (accelAmount > addSpeed)
                    {
                        accelAmount = addSpeed; // Не добавляем больше, чем нужно для достижения wishSpeed
                    }
                    
                    lateralVelocity += wishDir * accelAmount;
                }
                // --- Конец логики Source Air Accelerate ---
            }
            else
            {
                // Если нет инпута, применяем очень низкое воздушное трение (если _fallingLateralFriction > 0)
                // Важно: _fallingLateralFriction должен быть ОЧЕНЬ мал или 0.
                if (fallingLateralFriction > 0.001f && lateralVelocity.sqrMagnitude > 0.001f)
                {
                    float speed = lateralVelocity.magnitude;
                    float drop = speed * fallingLateralFriction * deltaTime;
                    lateralVelocity *= Mathf.Max(0.0f, speed - drop) / speed;
                }
            }

            // Применяем гравитацию
            verticalVelocity += gravity * deltaTime; // `gravity` здесь это свойство, которое возвращает GetGravityVector()

            // Ограничение максимальной скорости падения
            float actualFallSpeed = maxFallSpeed;
            if (physicsVolume)
                actualFallSpeed = physicsVolume.maxFallSpeed;

            // Проверка и ограничение вертикальной скорости
            Vector3 gravityDir = GetGravityDirection().normalized; // Направление "вниз"
            float verticalSpeedAlongGravity = Vector3.Dot(verticalVelocity, gravityDir);

            if (verticalSpeedAlongGravity > actualFallSpeed)
            {
                // Отнимаем избыточную скорость вдоль гравитации
                verticalVelocity -= gravityDir * (verticalSpeedAlongGravity - actualFallSpeed);
            }
            
            characterMovement.velocity = (lateralVelocity.magnitude <= _sourceMaxAirVelocity ? lateralVelocity : lateralVelocity.normalized * _sourceMaxAirVelocity) + verticalVelocity;
            _fallingTime += deltaTime; // _fallingTime - protected, доступен
        }
        
        protected override bool DoJump()
        {
            // ... (проверки)
            Vector3 worldUp = -GetGravityDirection();
            // ...

            // Вот эта часть важна:
            // verticalSpeed будет либо текущей вертикальной скоростью (если она > jumpImpulse), либо jumpImpulse.
            // Для прыжка с земли, нам нужно, чтобы verticalSpeed БЫЛ jumpImpulse, а горизонтальная скорость сохранилась.
            float currentVerticalSpeed = Vector3.Dot(characterMovement.velocity, worldUp);
            float finalVerticalSpeed = jumpImpulse; // Для прыжка с земли, просто берем jumpImpulse

            // Если мы уже движемся вверх быстрее, чем jumpImpulse (например, от взрыва), сохраняем эту скорость.
            // Но для обычного прыжка это не должно мешать.
            if (currentVerticalSpeed > jumpImpulse && !IsGrounded()) // Добавим !IsGrounded() для ясности
            {
                finalVerticalSpeed = currentVerticalSpeed;
            }
    
            // ЕСЛИ прыгаем с земли, то горизонтальную скорость надо взять текущую.
            // ЕСЛИ это воздушный прыжок (если разрешен), то тут могут быть варианты.
            // Для Source-like bhopping, первый прыжок самый важный.
            if (IsGrounded() || WasOnWalkableGround()) // WasOnGround для coyote time
            {
                characterMovement.velocity =
                    Vector3.ProjectOnPlane(characterMovement.velocity, worldUp) + worldUp * finalVerticalSpeed;
            }
            else // Логика для воздушного прыжка (если _jumpMaxCount > 1)
            {
                // Стандартное поведение - добавить импульс, возможно, обнулив текущую вертикальную скорость
                // или просто добавить к существующей. Для Source, скорее всего, просто замена вертикальной скорости.
                characterMovement.velocity =
                    Vector3.ProjectOnPlane(characterMovement.velocity, worldUp) + worldUp * finalVerticalSpeed;
            }
    
            return true;
        }
    }
}

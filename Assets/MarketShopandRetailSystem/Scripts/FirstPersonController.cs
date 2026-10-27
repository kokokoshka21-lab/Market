using System.Collections;
using UnityEngine;

namespace MarketShopandRetailSystem
{
    [RequireComponent(typeof(CharacterController))]
    public class FirstPersonController : MonoBehaviour
    {
        public static FirstPersonController Instance;
        public float MoveSpeed = 4.0f;
        public float SprintSpeed = 6.0f;
        public float RotationSpeed = 1.0f;
        public float SpeedChangeRate = 10.0f;
        public float JumpHeight = 1.2f;
        public float Gravity = -15.0f;
        public float JumpTimeout = 0.1f;
        public float FallTimeout = 0.15f;
        public bool Grounded = true;
        public float GroundedOffset = -0.14f;
        public float GroundedRadius = 0.5f;
        public LayerMask GroundLayers;
        public float TopClamp = 90.0f;
        public float BottomClamp = -90.0f;
        private float _speed;
        private float _verticalVelocity;
        private float _terminalVelocity = 53.0f;
        private CharacterController _controller;
        public GameObject Camera;
        private bool canJump = true;

        public Animation animation_Hand;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            _controller = GetComponent<CharacterController>();
        }

        private void Update()
        {
            JumpAndGravity();
            GroundedCheck();
            Move();
        }

        public void Jump()
        {
            if (canJump)
            {
                _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);
                Grounded = false;
                AudioManager.Instance.Play_Jump();
                StartCoroutine(ResetJump());
            }
        }

        public AudioSource audioSourceWalk;

        public void Play_Player_Walk()
        {
            if (Time.time > LastTimeWalkSound + WalkSoundPeriod)
            {
                audioSourceWalk.pitch = UnityEngine.Random.Range(1, 1.5f);
                audioSourceWalk.Play();
                LastTimeWalkSound = Time.time;
                if (!animation_Hand.isPlaying)
                {
                    animation_Hand.Play();
                }
                if (FirstPersonController.Instance.isRunning)
                {
                    WalkSoundPeriod = UnityEngine.Random.Range(0.25f, 0.5f);
                }
                else
                {
                    WalkSoundPeriod = UnityEngine.Random.Range(0.4f, 0.75f);
                }
            }
        }

        private float LastTimeWalkSound = 0;
        private float WalkSoundPeriod = 0.5f;

        IEnumerator ResetJump()
        {
            canJump = false;
            yield return new WaitForSeconds(1);
            canJump = true;
        }


        private void LateUpdate()
        {
            RotationUpdate();
        }

        private void RotationUpdate()
        {
            transform.eulerAngles = new Vector3(transform.eulerAngles.x, Camera.transform.eulerAngles.y, transform.eulerAngles.z);
        }


        private void GroundedCheck()
        {
            Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z);
            Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers, QueryTriggerInteraction.Ignore);
        }

        private Vector2 _input;
        private float lastStaminaSpendTime = 0;
        private void Move()
        {
            float targetSpeed = MoveSpeed;
            if (AdvancedGameManager.Instance.controllerType == ControllerType.Mobile)
            {
                _input = new Vector2(SimpleJoystick.Instance.HorizontalValue, SimpleJoystick.Instance.VerticalValue);
            }
            else
            {
                _input = AdvancedGameManager.Instance.moveAction.ReadValue<Vector2>();
            }
            if (_input == Vector2.zero) targetSpeed = 0.0f;

            if (AdvancedGameManager.Instance.controllerType == ControllerType.Mobile)
            {
                if (Mathf.Abs(_input.x) > 0.75f || Mathf.Abs(_input.y) > 0.75f)
                {
                    if (HeroPlayerScript.Instance.Stamina > 4)
                    {
                        if (FPSHandRotator.Instance.Current_HandType == Hand_Type.Free)
                        {
                            if (!HeroPlayerScript.Instance.isHoldingBox)
                            {
                                targetSpeed = SprintSpeed;
                            }
                        }
                        isRunning = true;
                        if (Time.time > lastStaminaSpendTime + 0.05f)
                        {
                            lastStaminaSpendTime = Time.time;
                            HeroPlayerScript.Instance.Stamina = HeroPlayerScript.Instance.Stamina - 1;
                            GameCanvas.Instance.UpdateStamina();
                        }

                    }
                    else
                    {
                        isRunning = false;
                        if (!AudioManager.Instance.audioSource.isPlaying)
                        {
                            AudioManager.Instance.Play_Audio_Breathing();
                        }
                    }

                }
            }
            else
            {
                if (AdvancedGameManager.Instance.sprintAction.IsPressed())
                {
                    if (HeroPlayerScript.Instance.Stamina > 4)
                    {
                        if (FPSHandRotator.Instance.Current_HandType == Hand_Type.Free)
                        {
                            if (!HeroPlayerScript.Instance.isHoldingBox)
                            {
                                targetSpeed = SprintSpeed;
                            }
                        }
                        isRunning = true;
                        if (Time.time > lastStaminaSpendTime + 0.1f)
                        {
                            lastStaminaSpendTime = Time.time;
                            HeroPlayerScript.Instance.Stamina = HeroPlayerScript.Instance.Stamina - 1;
                            GameCanvas.Instance.UpdateStamina();
                        }

                    }
                    else
                    {
                        isRunning = false;
                        if (!AudioManager.Instance.audioSource.isPlaying)
                        {
                            AudioManager.Instance.Play_Audio_Breathing();
                        }
                    }

                }
                if (AdvancedGameManager.Instance.sprintAction.WasReleasedThisFrame())
                {
                    isRunning = false;
                }
            }
            _speed = Mathf.Lerp(_speed, targetSpeed, Time.deltaTime * SpeedChangeRate);
            _speed = Mathf.Round(_speed * 1000f) / 1000f;

            Vector3 inputDirection = new Vector3(_input.x, 0.0f, _input.y).normalized;
            if (_input != Vector2.zero)
            {
                inputDirection = transform.right * _input.x + transform.forward * _input.y;
                Play_Player_Walk();

            }
            else
            {
                if (animation_Hand.isPlaying)
                {
                    animation_Hand.Stop();
                }
            }
            if (_controller.enabled)
            {
                _controller.Move(inputDirection.normalized * (_speed * Time.deltaTime) + new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);
            }
        }
        private bool isRunning = false;

        private void JumpAndGravity()
        {
            if (AdvancedGameManager.Instance.controllerType == ControllerType.PC)
            {
                if (AdvancedGameManager.Instance.jumpAction.WasReleasedThisFrame())
                {
                    Jump();
                }
            }

            if (Grounded)
            {
                if (_verticalVelocity < 0.0f)
                {
                    _verticalVelocity = -2f;
                }
            }

            if (_verticalVelocity < _terminalVelocity)
            {
                _verticalVelocity += Gravity * Time.deltaTime;
            }
        }

        private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
        {
            if (lfAngle < -360f) lfAngle += 360f;
            if (lfAngle > 360f) lfAngle -= 360f;
            return Mathf.Clamp(lfAngle, lfMin, lfMax);
        }

        private void OnDrawGizmosSelected()
        {
            Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
            Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

            if (Grounded) Gizmos.color = transparentGreen;
            else Gizmos.color = transparentRed;

            Gizmos.DrawSphere(new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z), GroundedRadius);
        }
    }
}
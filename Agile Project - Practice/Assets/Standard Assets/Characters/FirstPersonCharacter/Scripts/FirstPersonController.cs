using System;
using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;
using UnityStandardAssets.Utility;
using Random = UnityEngine.Random;

namespace UnityStandardAssets.Characters.FirstPerson
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(AudioSource))]
    public class FirstPersonController : MonoBehaviour
    {
        [SerializeField] private bool m_IsWalking;
        [SerializeField] private float m_WalkSpeed;
        [SerializeField] private float m_RunSpeed;
        [SerializeField] [Range(0f, 1f)] private float m_RunstepLenghten;
        [SerializeField] private float m_JumpSpeed;
        [SerializeField] private float m_StickToGroundForce;
        [SerializeField] private float m_GravityMultiplier;
        [SerializeField] private MouseLook m_MouseLook;
        [SerializeField] private bool m_UseFovKick;
        [SerializeField] private FOVKick m_FovKick = new FOVKick();
        [SerializeField] private bool m_UseHeadBob;
        [SerializeField] private CurveControlledBob m_HeadBob = new CurveControlledBob();
        [SerializeField] private LerpControlledBob m_JumpBob = new LerpControlledBob();
        [SerializeField] private float m_StepInterval;
        [SerializeField] private AudioClip[] m_FootstepSounds;    // an array of footstep sounds that will be randomly selected from.
        [SerializeField] private AudioClip m_JumpSound;           // the sound played when character leaves the ground.
        [SerializeField] private AudioClip m_LandSound;           // the sound played when character touches back on ground.

        private Camera m_Camera;
        private bool m_Jump;
        private bool m_Crouch = false;
        private float m_YRotation;
        private Vector2 m_Input;
        private Vector3 m_MoveDir = Vector3.zero;
        private CharacterController m_CharacterController;
        private CollisionFlags m_CollisionFlags;
        private bool m_PreviouslyGrounded;
        private Vector3 m_OriginalCameraPosition;
        private float m_StepCycle;
        private float m_NextStep;
        private bool m_Jumping;
        private bool m_Crouching = false;
        private bool m_StopMoving = false;
        private AudioSource m_AudioSource;

        //Code added by Joshua
        private bool m_MovingForward;
        int m_FixedPlayerSpeed = 7;
        bool m_LookingLeft = false;
        bool m_LookingRight = false;
        bool m_DirectionChanged = false;
        float m_OldHeight;
        int m_OldFixedPlayerSpeed = 0;

        //Make sure you attach a Rigidbody in the Inspector of this GameObject
        Rigidbody m_Rigidbody;
        Vector3 m_EulerAngleVelocity;
        
        enum Direction { forward = 0, left = -1, right = 1 };
        Direction direction = Direction.forward;
        // Use this for initialization
        private void Start()
        {
            m_CharacterController = GetComponent<CharacterController>();
            m_OldFixedPlayerSpeed = m_FixedPlayerSpeed;
            m_Camera = Camera.main;
            m_OriginalCameraPosition = m_Camera.transform.localPosition;
            m_FovKick.Setup(m_Camera);
            m_HeadBob.Setup(m_Camera, m_StepInterval);
            m_StepCycle = 0f;
            m_NextStep = m_StepCycle / 2f;
            m_Jumping = false;
            m_AudioSource = GetComponent<AudioSource>();
            m_MouseLook.Init(transform, m_Camera.transform);
            m_MouseLook.SetCursorLock(false);

            //Set the axis the Rigidbody rotates in (100 in the y axis)
            m_EulerAngleVelocity = new Vector3(0, -90, 0);

            //Fetch the Rigidbody from the GameObject with this script attached
            m_Rigidbody = GetComponent<Rigidbody>();
        }

        public void GoLeft()
        {
            if (direction == Direction.right)
            {
                m_MouseLook.LookLeft(transform, m_Camera.transform);
                direction = Direction.forward;
                m_LookingLeft = true;
            }
            else if (direction == Direction.left)
            {
                //do nothing
            }
            else
            {
                m_MouseLook.LookLeft(transform, m_Camera.transform);
                direction = Direction.left;
            }
        }

        public void GoRight()
        {
            if (direction == Direction.left)
            {
                m_MouseLook.LookRight(transform, m_Camera.transform);
                direction = Direction.forward;
            }
            else if (direction == Direction.right)
            {
                //do nothing
            }
            else
            {
                m_MouseLook.LookRight(transform, m_Camera.transform);
                m_LookingRight = true;
                direction = Direction.right;
            }
        }

        // Update is called once per frame
        private void Update()
        {


            // the jump state needs to read here to make sure it is not missed
            if (!m_Jump)
            {
                m_Jump = CrossPlatformInputManager.GetButtonDown("Jump") && m_CharacterController.isGrounded;
                if (CrossPlatformInputManager.GetButtonDown("Crouch") && m_CharacterController.isGrounded)
                { 
                    if (m_Crouch)
                    {
                        m_Crouch = false;
                    }
                    else
                    {
                        m_Crouch = true;
                    }
                }
            }


            if (!m_PreviouslyGrounded && m_CharacterController.isGrounded)
            {
                StartCoroutine(m_JumpBob.DoBobCycle());
                PlayLandingSound();
                m_MoveDir.y = 0f;
                m_Jumping = false;
            }
            if (!m_CharacterController.isGrounded && !m_Jumping && m_PreviouslyGrounded)
            {
                m_MoveDir.y = 0f;
            }

            m_PreviouslyGrounded = m_CharacterController.isGrounded;

            if (CrossPlatformInputManager.GetButtonDown("Start"))
            {
                m_StopMoving = true;
            }

            if (CrossPlatformInputManager.GetButtonDown("Left"))
            {

                GoLeft();

            }

            if (CrossPlatformInputManager.GetButtonDown("Right"))
            {
                GoRight();
            }


            if (Input.GetMouseButtonDown(1))
            {
                m_LookingLeft = false;
                m_LookingRight = false;
                m_DirectionChanged = false;
            }
        }

        private void PlayLandingSound()
        {
            m_AudioSource.clip = m_LandSound;
            m_AudioSource.Play();
            m_NextStep = m_StepCycle + .5f;
        }


        private void FixedUpdate()
        {
            float speed;
            GetInput(out speed);
            // always move along the camera forward as it is the direction that it being aimed at
            Vector3 desiredMove = transform.forward;

            if (m_LookingLeft)
            {
                desiredMove = transform.forward * m_Input.y + transform.right * -1;
            }

            if (m_LookingRight)
            {
                desiredMove = transform.forward * m_Input.y + transform.right * 1;
            }

            // get a normal for the surface that is being touched to move along it
            RaycastHit hitInfo;
            Physics.SphereCast(transform.position, m_CharacterController.radius, Vector3.down, out hitInfo,
                               m_CharacterController.height / 2f, Physics.AllLayers, QueryTriggerInteraction.Ignore);
            desiredMove = Vector3.ProjectOnPlane(desiredMove, hitInfo.normal).normalized;

            if (m_StopMoving)
            {
                //added by Joshua
                if (direction == Direction.right)
                {
                    m_MoveDir.x = m_FixedPlayerSpeed;
                    m_MoveDir.z = 0;
                }

                if (direction == Direction.left)
                {
                    m_MoveDir.x = -m_FixedPlayerSpeed;
                    m_MoveDir.z = 0;
                }

                if (direction == Direction.forward)
                {
                    m_MoveDir.x = 0;
                    m_MoveDir.z = m_FixedPlayerSpeed;
                }

                if (m_CharacterController.isGrounded)
                {
                    m_MoveDir.y = -m_StickToGroundForce;

                    if (m_Jump)
                    {
                        m_MoveDir.y = m_JumpSpeed;
                        PlayJumpSound();
                        m_Jump = false;
                        m_Jumping = true;
                    }

                    if (m_Crouch)
                    {
                        m_CharacterController.height = 0.5f;
                        m_FixedPlayerSpeed = 5; 
                        transform.position = new Vector3(transform.position.x, transform.position.y - 0.5f, transform.position.z);
                    }
                    else
                    {
                        m_CharacterController.height = 2.0f;
                        m_FixedPlayerSpeed = m_OldFixedPlayerSpeed;
                        transform.position = new Vector3(transform.position.x, transform.position.y + 0.5f, transform.position.z);
                    }

                }
                else
                {
                    m_MoveDir += Physics.gravity * m_GravityMultiplier * Time.fixedDeltaTime;
                }

                m_CollisionFlags = m_CharacterController.Move(m_MoveDir * Time.fixedDeltaTime);

                ProgressStepCycle(speed);
                UpdateCameraPosition(speed);

                }
            }

        private void PlayJumpSound()
        {
            m_AudioSource.clip = m_JumpSound;
            m_AudioSource.Play();
        }


        private void ProgressStepCycle(float speed)
        {
            if (m_CharacterController.velocity.sqrMagnitude > 0 && (m_Input.x != 0 || m_Input.y != 0))
            {
                m_StepCycle += (m_CharacterController.velocity.magnitude + (speed * (m_IsWalking ? 1f : m_RunstepLenghten))) *
                             Time.fixedDeltaTime;
            }

            if (!(m_StepCycle > m_NextStep))
            {
                return;
            }

            m_NextStep = m_StepCycle + m_StepInterval;

            PlayFootStepAudio();
        }


        private void PlayFootStepAudio()
        {
            if (!m_CharacterController.isGrounded)
            {
                return;
            }
            // pick & play a random footstep sound from the array,
            // excluding sound at index 0
            int n = Random.Range(1, m_FootstepSounds.Length);
            m_AudioSource.clip = m_FootstepSounds[n];
            m_AudioSource.PlayOneShot(m_AudioSource.clip);
            // move picked sound to index 0 so it's not picked next time
            m_FootstepSounds[n] = m_FootstepSounds[0];
            m_FootstepSounds[0] = m_AudioSource.clip;
        }


        private void UpdateCameraPosition(float speed)
        {
            Vector3 newCameraPosition;
            if (!m_UseHeadBob)
            {
                return;
            }
            if (m_CharacterController.velocity.magnitude > 0 && m_CharacterController.isGrounded)
            {
                m_Camera.transform.localPosition =
                    m_HeadBob.DoHeadBob(m_CharacterController.velocity.magnitude +
                                      (speed * (m_IsWalking ? 1f : m_RunstepLenghten)));
                newCameraPosition = m_Camera.transform.localPosition;
                newCameraPosition.y = m_Camera.transform.localPosition.y - m_JumpBob.Offset();
            }
            else
            {
                newCameraPosition = m_Camera.transform.localPosition;
                newCameraPosition.y = m_OriginalCameraPosition.y - m_JumpBob.Offset();
            }
            m_Camera.transform.localPosition = newCameraPosition;
        }


        private void GetInput(out float speed)
        {
            // Read input
            float horizontal = CrossPlatformInputManager.GetAxis("Horizontal");
            float vertical = CrossPlatformInputManager.GetAxis("Vertical");

            bool waswalking = m_IsWalking;

#if !MOBILE_INPUT
            // On standalone builds, walk/run speed is modified by a key press.
            // keep track of whether or not the character is walking or running
            m_IsWalking = !Input.GetKey(KeyCode.LeftShift);
#endif
            // set the desired speed to be walking or running
            speed = m_IsWalking ? m_WalkSpeed : m_RunSpeed;
            m_Input = new Vector2(horizontal, vertical);

            // normalize input if it exceeds 1 in combined length:
            if (m_Input.sqrMagnitude > 1)
            {
                m_Input.Normalize();
            }

            // handle speed change to give an fov kick
            // only if the player is going to a run, is running and the fovkick is to be used
            if (m_IsWalking != waswalking && m_UseFovKick && m_CharacterController.velocity.sqrMagnitude > 0)
            {
                StopAllCoroutines();
                StartCoroutine(!m_IsWalking ? m_FovKick.FOVKickUp() : m_FovKick.FOVKickDown());
            }
        }


        private void RotateView()
        {
            m_MouseLook.LookRotation(transform, m_Camera.transform);
        }


        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            Rigidbody body = hit.collider.attachedRigidbody;
            //dont move the rigidbody if the character is on top of it
            if (m_CollisionFlags == CollisionFlags.Below)
            {
                return;
            }

            if (body == null || body.isKinematic)
            {
                return;
            }
            body.AddForceAtPosition(m_CharacterController.velocity * 0.1f, hit.point, ForceMode.Impulse);
        }
    }

}

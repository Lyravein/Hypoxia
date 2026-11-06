using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Hypoxia
{
    /// <summary>
    /// Minimal owner-controlled movement for the networked player prefab.
    /// Moves a CharacterController and leaves replication to the NetworkTransform on the prefab.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    public sealed class OwnerPlayerMovement : MonoBehaviour
    {
        [SerializeField] private float walkSpeed = 4f;
        [SerializeField] private float sprintSpeed = 8f; // GDD A1: sprint rate >= 2x walk rate
        [SerializeField] private float mouseSensitivity = 2f;
        [SerializeField] private float gravity = -20f;
        [SerializeField] private float swayFrequency = 2.5f;
        [SerializeField] private float swayAmplitude = 0.04f;

        private CharacterController controller;
        private NetworkObject networkObject;
        private PlayerExposure exposure;
        private Camera targetCamera;
        private float verticalVelocity;
        private float cameraPitch;

        public bool IsSprinting { get; private set; }
        public bool IsMoving { get; private set; }

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            networkObject = GetComponent<NetworkObject>();
            exposure = GetComponent<PlayerExposure>();
        }

        private void Start()
        {
            if (IsOwnedByLocalPlayer())
            {
                targetCamera = Camera.main;
                if (targetCamera != null)
                {
                    targetCamera.transform.SetParent(transform);
                    targetCamera.transform.localPosition = new Vector3(0f, 1.6f, 0f);
                    targetCamera.transform.localRotation = Quaternion.identity;
                }
            }
        }

        private void Update()
        {
            if (!IsOwnedByLocalPlayer())
            {
                return;
            }

            if (exposure != null && exposure.PassedOut)
            {
                IsMoving = false;
                IsSprinting = false;
                return;
            }

            HandleMouseLook();

            Vector2 input = ReadMoveInput();
            float magnitude = Mathf.Clamp01(input.magnitude);
            IsMoving = magnitude > 0.001f;
            IsSprinting = IsMoving && ReadSprintInput();

            Vector3 planar = IsMoving
                ? transform.TransformDirection(new Vector3(input.x, 0f, input.y).normalized * magnitude)
                : Vector3.zero;

            if (controller.isGrounded && verticalVelocity < 0f)
            {
                // Small downward bias keeps isGrounded stable on flat ground.
                verticalVelocity = -2f;
            }
            else
            {
                verticalVelocity += gravity * Time.deltaTime;
            }

            float speed = IsSprinting ? sprintSpeed : walkSpeed;
            Vector3 motion = planar * speed;
            motion.y = verticalVelocity;

            controller.Move(motion * Time.deltaTime);
        }

        private void HandleMouseLook()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null) return;

            Vector2 delta = mouse.delta.ReadValue() * (mouseSensitivity * 0.1f);
            transform.Rotate(0f, delta.x, 0f);

            if (targetCamera != null)
            {
                cameraPitch = Mathf.Clamp(cameraPitch - delta.y, -80f, 80f);

                // E6: Camera sway prosedural berbasis Mathf.PerlinNoise saat bergerak (Modul 5)
                float swayZ = 0f;
                float swayY = 0f;
                if (IsMoving)
                {
                    float speedFactor = IsSprinting ? 1.6f : 1.0f;
                    float t = Time.time * swayFrequency * speedFactor;
                    swayZ = (Mathf.PerlinNoise(t, 0f) - 0.5f) * swayAmplitude * 25f;
                    swayY = (Mathf.PerlinNoise(0f, t) - 0.5f) * swayAmplitude;
                }

                targetCamera.transform.localRotation = Quaternion.Euler(cameraPitch, 0f, swayZ);
                targetCamera.transform.localPosition = new Vector3(0f, 1.6f + swayY, 0f);
            }
        }

        private static bool ReadSprintInput()
        {
            Keyboard kb = Keyboard.current;
            if (kb != null && (kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed)) return true;

            Gamepad gp = Gamepad.current;
            if (gp != null && gp.leftStickButton.isPressed) return true;

            return false;
        }

        // Direct device polling. InputSystem_Actions.inputactions has no generated wrapper and its
        // "Move" action evaluated to zero at runtime, so devices are read directly instead.
        private static Vector2 ReadMoveInput()
        {
            Vector2 input = Vector2.zero;

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) input.y += 1f;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) input.y -= 1f;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) input.x += 1f;
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) input.x -= 1f;
            }

            Gamepad gamepad = Gamepad.current;
            if (gamepad != null)
            {
                Vector2 stick = gamepad.leftStick.ReadValue();
                if (stick.sqrMagnitude > input.sqrMagnitude)
                {
                    input = stick;
                }
            }

            return Vector2.ClampMagnitude(input, 1f);
        }

        private bool IsOwnedByLocalPlayer()
        {
            // No NetworkObject: single-player or authoring context, act as owner.
            if (networkObject == null)
            {
                return true;
            }

            return networkObject.IsSpawned && networkObject.IsOwner;
        }
    }
}

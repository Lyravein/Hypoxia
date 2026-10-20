using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Hypoxia
{
    [DisallowMultipleComponent]
    public sealed class PlayerExposure : NetworkBehaviour
    {
        public static readonly List<PlayerExposure> AllPlayers = new();
        public static PlayerExposure LocalInstance { get; private set; }

        [SerializeField] private float rateMultiplier = 1.0f;

        public NetworkVariable<float> Exposure = new(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public NetworkVariable<int> OxygenTanks = new(3, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public NetworkVariable<bool> IsPassedOut = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public NetworkVariable<bool> IsSprinting = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        public NetworkVariable<bool> IsMoving = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        public NetworkVariable<bool> IsFlashlightOn = new(true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        private OwnerPlayerMovement movement;
        private Vector3 checkpointPosition;
        private Light flashlight;

        public float CurrentExposure => Exposure.Value;
        public int RemainingTanks => OxygenTanks.Value;
        public bool PassedOut => IsPassedOut.Value;
        public bool IsRian => OwnerClientId == NetworkManager.ServerClientId;
        public string CharacterName => IsRian ? "Rian (Geolog)" : "Maya (Peneliti Medis)";

        public static bool IsAnyPlayerPassedOut()
        {
            for (int i = 0; i < AllPlayers.Count; i++)
            {
                if (AllPlayers[i] != null && AllPlayers[i].PassedOut)
                {
                    return true;
                }
            }
            return false;
        }

        private void Awake()
        {
            movement = GetComponent<OwnerPlayerMovement>();
            checkpointPosition = transform.position;
            SetupFlashlight();
        }

        private void SetupFlashlight()
        {
            GameObject lightObj = new("Flashlight");
            lightObj.transform.SetParent(transform);
            lightObj.transform.localPosition = new Vector3(0f, 1.5f, 0.2f);
            lightObj.transform.localRotation = Quaternion.identity;

            flashlight = lightObj.AddComponent<Light>();
            flashlight.type = LightType.Spot;
            flashlight.range = 25f;
            flashlight.spotAngle = 60f;
            flashlight.innerSpotAngle = 35f;
            // URP punctual intensity is a plain multiplier, not HDRP lux/lumen: 1500 would blow out the frame.
            flashlight.intensity = 3f;
            flashlight.enabled = IsFlashlightOn.Value;
        }

        public override void OnNetworkSpawn()
        {
            AllPlayers.Add(this);

            if (IsOwner)
            {
                LocalInstance = this;
                if (Camera.main != null && flashlight != null)
                {
                    // Parent flashlight to camera for the owner so beam follows mouse pitch
                    flashlight.transform.SetParent(Camera.main.transform);
                    flashlight.transform.localPosition = Vector3.zero;
                    flashlight.transform.localRotation = Quaternion.identity;
                }
            }

            if (IsServer)
            {
                checkpointPosition = transform.position;
                // GDD Bab 6: Rian (Host) 20% lebih lambat (0.8x), Maya (Client) laju standar (1.0x)
                rateMultiplier = IsRian ? 0.8f : 1.0f;
            }

            // GDD Bab 6: Warna pakaian pembeda Rian (Oranye tambang) vs Maya (Toska medis)
            Renderer rend = GetComponent<Renderer>();
            if (rend != null)
            {
                rend.material.color = IsRian
                    ? new Color(0.9f, 0.5f, 0.15f)
                    : new Color(0.15f, 0.7f, 0.85f);
            }

            IsFlashlightOn.OnValueChanged += OnFlashlightStateChanged;
            if (flashlight != null) flashlight.enabled = IsFlashlightOn.Value;
        }

        public override void OnNetworkDespawn()
        {
            AllPlayers.Remove(this);
            IsFlashlightOn.OnValueChanged -= OnFlashlightStateChanged;

            if (IsOwner && LocalInstance == this)
            {
                LocalInstance = null;
            }
        }

        private void OnFlashlightStateChanged(bool previousValue, bool newValue)
        {
            if (flashlight != null)
            {
                flashlight.enabled = newValue;
            }
        }

        private void Update()
        {
            if (IsOwner)
            {
                if (movement != null)
                {
                    IsMoving.Value = movement.IsMoving;
                    IsSprinting.Value = movement.IsSprinting;
                }

                Keyboard kb = Keyboard.current;
                if (kb != null)
                {
                    // Key F: Toggle Senter (GDD 5.3)
                    if (kb.fKey.wasPressedThisFrame)
                    {
                        IsFlashlightOn.Value = !IsFlashlightOn.Value;
                    }

                    // Key Q: Pakai tabung oksigen (GDD 5.3)
                    if (!PassedOut && !IsAnyPlayerPassedOut() && kb.qKey.wasPressedThisFrame)
                    {
                        UseOxygenServerRpc();
                    }

                    // Key F1, F2, F3: Debug band CO (GDD 5.3)
                    if (kb.f1Key.wasPressedThisFrame) SetDebugExposureServerRpc(0f);
                    else if (kb.f2Key.wasPressedThisFrame) SetDebugExposureServerRpc(50f);
                    else if (kb.f3Key.wasPressedThisFrame) SetDebugExposureServerRpc(75f);
                }
            }

            if (IsServer && !IsPassedOut.Value)
            {
                Vector3 pos = transform.position;
                bool nearPipe = false;
                bool inOpenAir = false;

                for (int i = 0; i < ExposureZone.AllZones.Count; i++)
                {
                    ExposureZone zone = ExposureZone.AllZones[i];
                    if (zone != null && zone.Contains(pos))
                    {
                        if (zone.ZoneType == ExposureZoneType.LeakingPipe) nearPipe = true;
                        else if (zone.ZoneType == ExposureZoneType.OpenAir) inOpenAir = true;
                    }
                }

                bool sprinting = IsSprinting.Value && IsMoving.Value;
                float delta = ExposureCalculator.CalculateDelta(inOpenAir, nearPipe, sprinting, rateMultiplier);
                Exposure.Value = Mathf.Clamp(Exposure.Value + delta * Time.deltaTime, 0f, ExposureCalculator.MaxExposure);

                if (Exposure.Value >= ExposureCalculator.MaxExposure)
                {
                    IsPassedOut.Value = true;
                }
            }
        }

        [ServerRpc]
        public void UseOxygenServerRpc()
        {
            if (IsPassedOut.Value || OxygenTanks.Value <= 0) return;

            int tanks = OxygenTanks.Value;
            Exposure.Value = ExposureCalculator.ApplyOxygen(Exposure.Value, ref tanks);
            OxygenTanks.Value = tanks;
        }

        [ServerRpc]
        public void SetDebugExposureServerRpc(float targetExposure)
        {
            Exposure.Value = Mathf.Clamp(targetExposure, 0f, ExposureCalculator.MaxExposure);
            IsPassedOut.Value = Exposure.Value >= ExposureCalculator.MaxExposure;
        }

        [ServerRpc]
        public void RestartSessionServerRpc()
        {
            for (int i = 0; i < AllPlayers.Count; i++)
            {
                if (AllPlayers[i] != null)
                {
                    AllPlayers[i].ResetToCheckpoint();
                }
            }
        }

        public void ResetToCheckpoint()
        {
            Exposure.Value = 0f;
            OxygenTanks.Value = 3;
            IsPassedOut.Value = false;

            CharacterController cc = GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            transform.position = checkpointPosition;
            if (cc != null) cc.enabled = true;
        }
    }
}

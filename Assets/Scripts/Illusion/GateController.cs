using Unity.Netcode;
using UnityEngine;

namespace Hypoxia
{
    /// <summary>
    /// Gerbang sekat antar area (G1). State asli di host; klien hanya membaca hasil verifikasi (B2).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GateController : NetworkBehaviour
    {
        [SerializeField] private GameObject visual;

        [SerializeField] private Collider blockingCollider;

        public NetworkVariable<bool> IsOpen = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public override void OnNetworkSpawn()
        {
            IsOpen.OnValueChanged += OnOpenChanged;
            Apply(IsOpen.Value);
        }

        public override void OnNetworkDespawn()
        {
            IsOpen.OnValueChanged -= OnOpenChanged;
        }

        private void OnOpenChanged(bool previous, bool current) => Apply(current);

        private void Apply(bool open)
        {
            if (visual != null) visual.SetActive(!open);
            if (blockingCollider != null) blockingCollider.enabled = !open;
        }

        public void OpenServer()
        {
            if (IsServer) IsOpen.Value = true;
        }
    }
}
